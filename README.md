<div align="center">

# 🐼 Messenger Lite Desktop

A lightweight desktop wrapper for Facebook Messenger — because Meta killed the official app.

[![GitHub stars](https://img.shields.io/github/stars/MrPanda1609/Messengers-Webview?style=social)](https://github.com/MrPanda1609/Messengers-Webview/stargazers)
[![GitHub release](https://img.shields.io/github/v/release/MrPanda1609/Messengers-Webview)](https://github.com/MrPanda1609/Messengers-Webview/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6?logo=windows)](https://github.com/MrPanda1609/Messengers-Webview/releases)

<img src="panda.png" width="128" alt="Messenger Lite">

**[⬇ Download](https://github.com/MrPanda1609/Messengers-Webview/releases/latest)** · **[⚡ One-Command Install](#-quick-install)** · **[🌐 Tiếng Việt](#-tiếng-việt)**

</div>

---

## 📖 Why?

In 2025, Meta officially **discontinued the Messenger desktop app**, forcing users to use [facebook.com/messages](https://www.facebook.com/messages) in a browser. This means:

- 🚫 No standalone app — must open a full browser
- 🚫 Mixed with other Facebook tabs, distractions everywhere
- 🚫 Heavy browser memory usage just for chatting

**Messenger Lite Desktop** solves this by wrapping Messenger in a **ultra-lightweight native window** (~1.5 MB) using Windows WebView2 — no Electron bloat, no extra browser needed.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🪶 **Ultra-light** | ~1.5 MB app size, uses system WebView2 (already on Windows 10/11) |
| 💬 **Chat-focused** | Opens directly to Messenger — designed for chatting only |
| 🔔 **Notifications** | Sound alert when new messages arrive (notification sound plays in-app) |
| 🔒 **Persistent login** | Login once, stay logged in forever |
| 📌 **System tray** | Minimize to tray (X button = hide, not quit) |
| 💾 **Remembers window** | Saves position and size between sessions |
| 🚫 **No distractions** | Navigation to Feed, Reels, Groups, Marketplace, Gaming is blocked |
| 🧠 **Low memory** | Optimized Chromium flags to reduce RAM usage |
| 🔁 **Single instance** | Opening twice brings the existing window to front |

---

## ⚡ Quick Install

### One-Command Install (Recommended)

Open **PowerShell** and run:

```powershell
irm https://raw.githubusercontent.com/MrPanda1609/Messengers-Webview/main/install.ps1 | iex
```

This will:
1. Download the latest release
2. Install to `%LocalAppData%\MessengerLite`
3. Create Desktop & Start Menu shortcuts
4. Launch the app

### Manual Download

1. Go to [**Releases**](https://github.com/MrPanda1609/Messengers-Webview/releases/latest)
2. Download `Messenger-Lite-vX.X.X.zip`
3. Extract and run `Messenger.exe`

### Requirements

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (most Windows PCs already have it)
- WebView2 Runtime (pre-installed on Windows 10/11)

---

## 🗑️ Uninstall

```powershell
irm https://raw.githubusercontent.com/MrPanda1609/Messengers-Webview/main/uninstall.ps1 | iex
```

---

## 🖥️ Usage

| Action | Behavior |
|---|---|
| **Close (X)** | Minimizes to system tray |
| **Minimize (−)** | Minimizes to system tray |
| **Quit** | Right-click tray icon → Exit |
| **New message** | Sound alert plays in-app when messages arrive |
| **Header buttons** | Avatar works normally (account switch / logout) |


> ⚠️ **Note:** Do not click on other pages (Home, Watch, Groups, Marketplace, Gaming...). This app is designed for Messenger only. If you accidentally navigate away, click the **Messenger icon** in the header bar to return, or restart the app.

---

## ⭐ Support

If you find this useful, please **star this repo** — it helps others discover it!

[![Star this repo](https://img.shields.io/github/stars/MrPanda1609/Messengers-Webview?style=for-the-badge&logo=github&label=Star%20this%20repo&color=yellow)](https://github.com/MrPanda1609/Messengers-Webview)

---

## 🌐 Tiếng Việt

### Tại sao có app này?

Năm 2025, Meta chính thức **khai tử ứng dụng Messenger Desktop**, buộc người dùng phải vào [facebook.com/messages](https://www.facebook.com/messages) trên trình duyệt. Điều này gây ra:

- 🚫 Không có app riêng — phải mở trình duyệt nặng nề
- 🚫 Lẫn với các tab Facebook khác, dễ mất tập trung
- 🚫 Tốn RAM chỉ để nhắn tin

**Messenger Lite Desktop** giải quyết vấn đề này bằng cách bọc Messenger trong một **cửa sổ native siêu nhẹ** (~1.5 MB), sử dụng WebView2 có sẵn trên Windows — không phình to như Electron, không cần trình duyệt thêm.

### Tính năng

- 🪶 **Siêu nhẹ** — Chỉ ~1.5 MB, dùng WebView2 có sẵn trên Windows 10/11
- 💬 **Tập trung nhắn tin** — Mở thẳng Messenger, chỉ dành cho nhắn tin
- 🔔 **Thông báo** — Âm thanh thông báo khi có tin nhắn mới (phát âm thanh trong app)
- 🔒 **Nhớ đăng nhập** — Đăng nhập 1 lần, dùng mãi
- 📌 **Khay hệ thống** — Thu nhỏ xuống tray (nút X = ẩn, không thoát)
- 💾 **Nhớ cửa sổ** — Lưu vị trí và kích thước giữa các lần mở
- 🧠 **Tiết kiệm RAM** — Tối ưu Chromium flags giảm bộ nhớ
- 🚫 **Chặn điều hướng** — Chặn chuyển trang sang Feed, Reels, Groups, Marketplace, Gaming

> ⚠️ **Lưu ý:** Không nên ấn vào các trang khác (Trang chủ, Watch, Groups, Marketplace, Gaming...). App này chỉ dành cho Messenger. Nếu lỡ chuyển trang, hãy ấn vào **biểu tượng Messenger** trên thanh header để quay lại, hoặc khởi động lại app.

### Cài đặt nhanh

Mở **PowerShell** và chạy:

```powershell
irm https://raw.githubusercontent.com/MrPanda1609/Messengers-Webview/main/install.ps1 | iex
```

### Yêu cầu

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (hầu hết máy Windows đã có sẵn)

### Gỡ cài đặt

```powershell
irm https://raw.githubusercontent.com/MrPanda1609/Messengers-Webview/main/uninstall.ps1 | iex
```

---

<div align="center">

Made with ❤️ because Meta took away our Messenger app.

[![Star History](https://img.shields.io/github/stars/MrPanda1609/Messengers-Webview?style=for-the-badge&logo=github&label=⭐%20Star&color=yellow)](https://github.com/MrPanda1609/Messengers-Webview)
[![Fork](https://img.shields.io/github/forks/MrPanda1609/Messengers-Webview?style=for-the-badge&logo=github&label=🍴%20Fork&color=blue)](https://github.com/MrPanda1609/Messengers-Webview/fork)

</div>
