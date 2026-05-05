using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Messenger;

public class MainForm : Form
{
    private const string MessengerUrl = "https://www.facebook.com/messages";
    private const string CurrentVersion = "1.0.24";
    private const string GitHubRepo = "MrPanda1609/Messengers-Webview";
    private readonly WebView2 _webView;
    private readonly NotifyIcon _trayIcon;
    private bool _forceClose;
    private Rectangle _restoreBounds;
    private bool _redirectedToMessages;
    private bool _messagesLoaded;
    private bool _hiddenToTray;
    private int _lastUnreadCount;
    private DateTime _lastNotificationAt = DateTime.MinValue;

    public MainForm()
    {
        // Load embedded icon
        var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("messenger.ico");
        if (iconStream != null)
            Icon = new Icon(iconStream);

        // Window setup
        Text = "Messenger";
        MinimumSize = new Size(400, 300);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1000, 700);

        // Load saved window bounds
        LoadWindowState();

        // WebView2
        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        _webView.CoreWebView2InitializationCompleted += OnWebViewReady;
        Controls.Add(_webView);

        // System tray
        _trayIcon = new NotifyIcon
        {
            Text = "Messenger",
            Icon = Icon,
            Visible = false
        };
        _trayIcon.Click += (_, args) =>
        {
            if (args is MouseEventArgs { Button: MouseButtons.Left })
                RestoreFromTray();
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        _trayIcon.BalloonTipClicked += (_, _) => RestoreFromTray();
        _trayIcon.BalloonTipClosed += (_, _) =>
        {
            if (!_hiddenToTray)
                _trayIcon.Visible = false;
        };

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, (_, _) => { _forceClose = true; Application.Exit(); });
        _trayIcon.ContextMenuStrip = trayMenu;

        // Initialize WebView2
        InitializeWebView();

        // Check for updates on startup
        CheckForUpdate();
    }

    private async void InitializeWebView()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MessengerWrapper");

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder,
            options: new CoreWebView2EnvironmentOptions(
                "--disable-background-networking --disable-default-apps --disable-extensions "
                + "--disable-sync --disable-translate --metrics-recording-only --no-first-run "
                + "--renderer-process-limit=1 --js-flags=--max-old-space-size=512"
            ));

        await _webView.EnsureCoreWebView2Async(env);
    }

    private void OnWebViewReady(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;

        var settings = _webView.CoreWebView2.Settings;
        settings.IsStatusBarEnabled = false;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsZoomControlEnabled = true;
        settings.AreBrowserAcceleratorKeysEnabled = true;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsPinchZoomEnabled = false;
        settings.IsSwipeNavigationEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.IsWebMessageEnabled = true;

        // Tell WebView2 to minimize memory usage
        _webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;

        _webView.CoreWebView2.PermissionRequested += (_, args) =>
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Notifications)
                args.State = CoreWebView2PermissionState.Allow;
        };

        _webView.CoreWebView2.NotificationReceived += (_, args) =>
        {
            args.Handled = true;
            var notification = args.Notification;
            var title = string.IsNullOrWhiteSpace(notification.Title) ? "Messenger" : notification.Title;
            var body = string.IsNullOrWhiteSpace(notification.Body) ? "Bạn có tin nhắn mới." : notification.Body;
            ShowMessageNotification(title, body);

            try { notification.ReportShown(); } catch { }
        };

        // Inject SPA navigation guard + header hiding BEFORE page scripts run
        _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            "(function() {" +

            // --- Nav guard ---
            "  window.__messengerGuardActive = false;" +
            "  var allowed = ['/messages', '/photo', '/photos', '/stories', '/login', '/logout', '/checkpoint'," +
            "    '/cookie', '/privacy', '/dialog', '/deauth', '/two_step_verification'," +
            "    '/api', '/ajax', '/rti', '/v1', '/v2'];" +
            "  function ok(url) {" +
            "    if (!url) return true;" +
            "    try {" +
            "      var p = new URL(url, location.origin).pathname.toLowerCase();" +
            "      return allowed.some(function(a) { return p.startsWith(a); });" +
            "    } catch(e) { return true; }" +
            "  }" +
            "  function goBack() { location.replace('/messages'); }" +
            "  function openExternal(url) {" +
            "    try { window.chrome.webview.postMessage('open:' + new URL(url, location.href).href); } catch(e) {}" +
            "  }" +
            "  var origPush = history.pushState;" +
            "  var origReplace = history.replaceState;" +
            "  history.pushState = function() {" +
            "    if (window.__messengerGuardActive && !ok(arguments[2])) { goBack(); return; }" +
            "    return origPush.apply(this, arguments);" +
            "  };" +
            "  history.replaceState = function() {" +
            "    if (window.__messengerGuardActive && !ok(arguments[2])) { goBack(); return; }" +
            "    return origReplace.apply(this, arguments);" +
            "  };" +
            // Open external links in the default browser even when Facebook does same-tab SPA navigation.
            "  document.addEventListener('click', function(e) {" +
            "    if (!window.__messengerGuardActive) return;" +
            "    var a = e.target.closest('a[href]');" +
            "    if (!a) return;" +
            "    var h = a.getAttribute('href');" +
            "    if (!h || h.startsWith('#') || h.startsWith('javascript:')) return;" +
            "    var u; try { u = new URL(h, location.href); } catch(ex) { return; }" +
            "    if (u.protocol !== 'http:' && u.protocol !== 'https:') return;" +
            "    if (u.origin !== location.origin || !ok(u.href)) {" +
            "      e.preventDefault(); e.stopPropagation(); openExternal(u.href);" +
            "    }" +
            "  }, true);" +
            "  setInterval(function() {" +
            "    if (!window.__messengerGuardActive) return;" +
            "    var p = location.pathname.toLowerCase();" +
            "    if (!allowed.some(function(a) { return p.startsWith(a); }) && p !== '/') {" +
            "      goBack();" +
            "    }" +
            "  }, 300);" +


            "})();");


        // Block prefetch/preload for non-messenger resources
        _webView.CoreWebView2.AddWebResourceRequestedFilter("*://www.facebook.com/watch*", CoreWebView2WebResourceContext.All);
        _webView.CoreWebView2.AddWebResourceRequestedFilter("*://www.facebook.com/groups*", CoreWebView2WebResourceContext.All);
        _webView.CoreWebView2.AddWebResourceRequestedFilter("*://www.facebook.com/marketplace*", CoreWebView2WebResourceContext.All);
        _webView.CoreWebView2.AddWebResourceRequestedFilter("*://www.facebook.com/gaming*", CoreWebView2WebResourceContext.All);
        _webView.CoreWebView2.AddWebResourceRequestedFilter("*://www.facebook.com/reel*", CoreWebView2WebResourceContext.All);
        _webView.CoreWebView2.WebResourceRequested += (_, args) =>
        {
            args.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
        };

        // Allow notifications
        _webView.CoreWebView2.PermissionRequested += (_, args) =>
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Notifications)
                args.State = CoreWebView2PermissionState.Allow;
        };


        // Open all new-window/popup links in default browser
        _webView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            OpenInDefaultBrowser(args.Uri);
        };

        // Handle logout message from JS
        _webView.CoreWebView2.WebMessageReceived += async (_, args) =>
        {
            var message = args.TryGetWebMessageAsString();
            if (message.StartsWith("open:", StringComparison.OrdinalIgnoreCase))
            {
                OpenInDefaultBrowser(message[5..]);
                return;
            }

            if (message == "logout")
            {
                _messagesLoaded = false;
                var cm = _webView.CoreWebView2.CookieManager;
                var cookies = await cm.GetCookiesAsync("https://www.facebook.com");
                foreach (var c in cookies) cm.DeleteCookie(c);
                _webView.CoreWebView2.Navigate(MessengerUrl);
            }
        };

        // Block navigation away from Messenger (only after messages loaded)
        _webView.CoreWebView2.NavigationStarting += (_, args) =>
        {
            if (!_messagesLoaded) return;
            if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri)) return;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;

            if (!uri.Host.Contains("facebook.com"))
            {
                args.Cancel = true;
                OpenInDefaultBrowser(args.Uri);
                return;
            }

            var path = uri.AbsolutePath.ToLower();
            if (!path.StartsWith("/messages") && !path.StartsWith("/photo")
                && !path.StartsWith("/photos") && !path.StartsWith("/stories")
                && !path.StartsWith("/login")
                && !path.StartsWith("/logout") && !path.StartsWith("/checkpoint")
                && !path.StartsWith("/ajax") && !path.StartsWith("/api")
                && !path.StartsWith("/rti") && !path.StartsWith("/cookie")
                && !path.StartsWith("/privacy") && !path.StartsWith("/dialog")
                && !path.StartsWith("/v1") && !path.StartsWith("/v2")
                && !path.StartsWith("/deauth") && !path.StartsWith("/two_step_verification"))
            {
                args.Cancel = true;
                OpenInDefaultBrowser(args.Uri);
            }
        };

        // After login completes and lands on homepage, redirect to Messenger (once)
        _webView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            var currentUri = new Uri(_webView.CoreWebView2.Source);

            // Hide Facebook header + add logout button (not on login/checkpoint pages)
            if (currentUri.Host.Contains("facebook.com") && !currentUri.AbsolutePath.StartsWith("/login") && !currentUri.AbsolutePath.StartsWith("/checkpoint"))
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(
                    "if(!document.getElementById('__msng_hh')){" +
                    "  var s=document.createElement('style');" +
                    "  s.id='__msng_hh';" +
                    "  s.textContent='" +
                    "    div[role=banner]{visibility:hidden!important;height:0!important;" +
                    "      min-height:0!important;max-height:0!important;padding:0!important;" +
                    "      margin:0!important;overflow:hidden!important;" +
                    "      background:transparent!important;box-shadow:none!important;border:none!important}" +
                    "    #__msng_logout{position:fixed!important;top:8px!important;right:14px!important;" +
                    "      height:28px!important;padding:0 10px!important;border-radius:6px!important;" +
                    "      background:#0866ff!important;color:#fff!important;" +
                    "      font-size:13px!important;font-family:Segoe UI,Helvetica,Arial,sans-serif!important;" +
                    "      display:flex!important;align-items:center!important;gap:5px!important;" +
                    "      cursor:pointer!important;z-index:10000!important;user-select:none!important;" +
                    "      transition:background .2s!important;border:none!important}" +
                    "    #__msng_logout:hover{background:#0756d6!important}" +
                    "    #__msng_logout svg{width:14px;height:14px;fill:currentColor}" +
                    "  ';" +
                    "  document.head.appendChild(s);" +
                    "  var btn=document.createElement('div');" +
                    "  btn.id='__msng_logout';" +
                    "  btn.innerHTML='<svg viewBox=\"0 0 24 24\"><path d=\"M5 3h6a1 1 0 010 2H5v14h6a1 1 0 010 2H5a2 2 0 01-2-2V5a2 2 0 012-2zm12.71 8.29l-3-3a1 1 0 00-1.42 1.42L14.59 11H9a1 1 0 000 2h5.59l-1.3 1.29a1 1 0 001.42 1.42l3-3a1 1 0 000-1.42z\"/></svg>Log out';" +
                    "  btn.title='Log out';" +
                    "  btn.onclick=function(){window.chrome.webview.postMessage('logout');};" +
                    "  document.body.appendChild(btn);" +
                    "  var el=document.querySelector('div[role=banner]');" +
                    "  if(el&&el.nextElementSibling){" +
                    "    var cs=getComputedStyle(el.nextElementSibling);" +
                    "    var h=parseFloat(cs.height)||0;" +
                    "    if(el.nextElementSibling.childElementCount===0&&h>=40&&h<=70)" +
                    "      el.nextElementSibling.style.setProperty('height','0','important');" +
                    "  }" +
                    "}"
                );
            }

            if (currentUri.Host.Contains("facebook.com") && currentUri.AbsolutePath == "/"
                && !_redirectedToMessages)
            {
                _redirectedToMessages = true;
                _webView.CoreWebView2.Navigate(MessengerUrl);
            }
            if (currentUri.AbsolutePath.StartsWith("/messages"))
            {
                _redirectedToMessages = false;
                _messagesLoaded = true;
                await _webView.CoreWebView2.ExecuteScriptAsync("window.__messengerGuardActive = true;");
            }
            else if (_messagesLoaded)
            {
                await _webView.CoreWebView2.ExecuteScriptAsync("window.__messengerGuardActive = true;");
            }
            if (currentUri.AbsolutePath.StartsWith("/login") || currentUri.AbsolutePath.StartsWith("/checkpoint"))
            {
                _messagesLoaded = false;
                await _webView.CoreWebView2.ExecuteScriptAsync(
                    "['__msng_hh','__msng_logout'].forEach(function(id){" +
                    "  var e=document.getElementById(id);if(e)e.remove();});"
                );
            }
        };

        _webView.CoreWebView2.Navigate(MessengerUrl);

        // Flash taskbar + notify when unread count changes
        _webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
        {
            var title = _webView.CoreWebView2.DocumentTitle;
            Text = string.IsNullOrEmpty(title) ? "Messenger" : title;
            var unreadCount = GetUnreadCount(title);

            if (unreadCount > 0)
                FlashWindow(Handle, true);

            if (unreadCount > _lastUnreadCount)
                ShowMessageNotification("Messenger", unreadCount == 1
                    ? "Bạn có tin nhắn mới."
                    : $"Bạn có {unreadCount} tin nhắn chưa đọc.");

            _lastUnreadCount = unreadCount;
        };
    }

    private void MinimizeToTray()
    {
        _restoreBounds = Bounds;
        _hiddenToTray = true;
        ShowInTaskbar = false;
        Location = new Point(-32000, -32000);
        _trayIcon.Visible = true;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool FlashWindow(IntPtr hWnd, bool invert);

    private static void OpenInDefaultBrowser(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void RestoreFromTray()
    {
        if (_hiddenToTray)
            Bounds = _restoreBounds;
        _hiddenToTray = false;
        ShowInTaskbar = true;
        _trayIcon.Visible = false;
        WindowState = FormWindowState.Normal;
        NativeMethods.ShowWindow(Handle, NativeMethods.SW_RESTORE);
        Activate();
        NativeMethods.SetForegroundWindow(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Program.RestoreMessage || m.Msg == NativeMethods.WM_TOAST_CLICKED)
        {
            RestoreFromTray();
            return;
        }

        base.WndProc(ref m);
    }

    // X button = minimize to tray; actual exit via tray menu only
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_forceClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            MinimizeToTray();
            return;
        }
        SaveWindowState();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosing(e);
    }

    private string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MessengerWrapper", "window.txt");

    private void ShowMessageNotification(string title, string message)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastNotificationAt < TimeSpan.FromSeconds(2))
                return;

            _lastNotificationAt = now;
            _trayIcon.Visible = true;
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.ShowBalloonTip(5000);
        }
        catch { }
    }

    private static int GetUnreadCount(string title)
    {
        var match = Regex.Match(title, @"\((\d+)\)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 0;
    }

    private void SaveWindowState()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);

            var bounds = _hiddenToTray ? _restoreBounds : Bounds;
            File.WriteAllText(SettingsPath, $"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}");
        }
        catch { }
    }

    private void LoadWindowState()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var parts = File.ReadAllText(SettingsPath).Split(',');
            if (parts.Length != 4) return;

            var bounds = new Rectangle(
                int.Parse(parts[0]), int.Parse(parts[1]),
                int.Parse(parts[2]), int.Parse(parts[3]));

            // Verify the saved position is still on a visible screen
            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds)))
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
            }
        }
        catch { }
    }

    private async void CheckForUpdate()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "MessengerLite");
            http.Timeout = TimeSpan.FromSeconds(10);

            var json = await http.GetFromJsonAsync<JsonElement>(
                $"https://api.github.com/repos/{GitHubRepo}/releases/latest");

            var latestTag = json.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            if (!IsNewer(latestTag, CurrentVersion)) return;

            var assets = json.GetProperty("assets");
            string? downloadUrl = null;
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            if (downloadUrl == null) return;

            var result = MessageBox.Show(
                $"A new version (v{latestTag}) is available. Update now?",
                "Messenger Update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes) return;

            // Download to temp
            var zipPath = Path.Combine(Path.GetTempPath(), "messenger-update.zip");
            var zipBytes = await http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(zipPath, zipBytes);

            // Launch updater script and exit
            var appDir = AppContext.BaseDirectory;
            var exePath = Path.Combine(appDir, "Messenger.exe");
            var script = $@"
                Start-Sleep -Seconds 2
                Expand-Archive -Path '{zipPath}' -DestinationPath '{appDir}' -Force
                Remove-Item '{zipPath}' -Force

                # Refresh Windows icon cache so new icon shows immediately
                $icDir = Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Explorer'
                Get-ChildItem $icDir -Filter 'iconcache*' -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
                $oldCache = Join-Path $env:LOCALAPPDATA 'IconCache.db'
                if (Test-Path $oldCache) {{ Remove-Item $oldCache -Force -ErrorAction SilentlyContinue }}
                ie4uinit.exe -show

                # Recreate shortcuts with new icon
                $shell = New-Object -ComObject WScript.Shell
                $desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Messenger.lnk'
                if (Test-Path $desktopLink) {{
                    $sc = $shell.CreateShortcut($desktopLink)
                    $sc.TargetPath = '{exePath}'
                    $sc.WorkingDirectory = '{appDir}'
                    $sc.Description = 'Messenger Lite Desktop'
                    $sc.Save()
                }}
                $startLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'Messenger.lnk'
                if (Test-Path $startLink) {{
                    $sc = $shell.CreateShortcut($startLink)
                    $sc.TargetPath = '{exePath}'
                    $sc.WorkingDirectory = '{appDir}'
                    $sc.Description = 'Messenger Lite Desktop'
                    $sc.Save()
                }}

                Start-Process '{exePath}'
            ";
            var scriptPath = Path.Combine(Path.GetTempPath(), "messenger-update.ps1");
            await File.WriteAllTextAsync(scriptPath, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            _forceClose = true;
            Application.Exit();
        }
        catch { }
    }

    private static bool IsNewer(string latest, string current)
    {
        var lParts = latest.Split('.').Select(int.Parse).ToArray();
        var cParts = current.Split('.').Select(int.Parse).ToArray();
        for (int i = 0; i < Math.Max(lParts.Length, cParts.Length); i++)
        {
            int l = i < lParts.Length ? lParts[i] : 0;
            int c = i < cParts.Length ? cParts[i] : 0;
            if (l > c) return true;
            if (l < c) return false;
        }
        return false;
    }
}
