using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Messenger;

static class Program
{
    internal const int RestoreMessage = NativeMethods.WM_APP + 1609;
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        const string mutexName = "MessengerDesktopWrapper_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            RestoreExistingInstance();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void RestoreExistingInstance()
    {
        var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        {
            if (process.Id == current.Id)
                continue;

            if (TryPostRestoreMessage(process.MainWindowHandle)
                || TryPostRestoreMessage(ReadSavedWindowHandle()))
                return;
        }
    }

    private static bool TryPostRestoreMessage(IntPtr handle)
    {
        return handle != IntPtr.Zero
            && NativeMethods.IsWindow(handle)
            && NativeMethods.PostMessage(handle, RestoreMessage, IntPtr.Zero, IntPtr.Zero);
    }

    internal static string WindowHandlePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MessengerWrapper", "window-handle.txt");

    private static IntPtr ReadSavedWindowHandle()
    {
        try
        {
            if (!File.Exists(WindowHandlePath))
                return IntPtr.Zero;

            return long.TryParse(File.ReadAllText(WindowHandlePath), out var handle)
                ? new IntPtr(handle)
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }
}

internal static class NativeMethods
{
    public const int WM_APP = 0x8000;
    public const int WM_USER = 0x0400;
    public const int WM_TOAST_CLICKED = WM_USER + 1024;
    public const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);
}
