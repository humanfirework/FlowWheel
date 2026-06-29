<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/flowwheel.png" width="120" alt="FlowWheel Logo" />
</div>

<h1 align="center">FlowWheel</h1>

<div align="center">

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-v1.8.0-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

[中文](./README.md) | **English**

</div>

## Overview

Bring browser-style smooth scrolling to every corner of Windows—scroll anything by dragging with your mouse, complete with physics-based inertial scrolling and advanced productivity features.

## What Problem Does It Solve?

Imagine the convenience of reading long articles, browsing code files, or reviewing documents without constantly moving your scroll wheel. FlowWheel turns your mouse into a powerful navigation tool:

- **Hands-free reading**: Activate auto-scrolling and let content flow automatically
- **Precise control**: Drag naturally to scroll, with inertia glide on release
- **Multi-window workflows**: Scroll one window while keeping another's position

## Core Features

### Basic Scrolling

- **Drag-to-scroll**: Hold middle mouse and drag—further = faster, release for inertia glide
- **Distance-speed control**: The further you drag from the anchor, the faster you scroll—natural and intuitive
- **Inertia physics**: Release the mouse while moving and let content "throw" and glide
- **Anti-accidental deadzone**: Prevents accidental scrolling from slight hand tremors

<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="60%" alt="Drag-to-scroll Demo" />
</div>

### Advanced Modes

- **Reading Mode (Auto-scroll)**: **Double-click** middle mouse or use the dedicated hotkey to activate hands-free continuous scrolling
  - Adjust speed in real-time with the mouse wheel
  - Stop instantly by clicking any button
- **Sync Scroll**: Scroll a document on your main screen and a reference on the second screen follows automatically—perfect for code comparison, translation side-by-side
- **Axis Lock**: Prefer vertical or horizontal scrolling? Enable axis lock to prevent accidental direction changes

### Customization Options

- **Trigger key**: Configure the mouse button or keyboard shortcut to activate auto-scrolling
  - Middle button, XButton1, XButton2
  - Keyboard combos: Ctrl+Alt+F1, Shift+Middle, etc.
- **Custom hotkey**: Set a global hotkey (e.g., Ctrl+Alt+S) to toggle scrolling anytime
- **Acceleration curves**: 5 preset curve types + fully customizable curves
  - Linear: Constant speed increase
  - Exponential: Fast start, gradual slow-down
  - Logarithmic: Slow start, rapid acceleration
  - S-curve: S-curve with inflection points
  - Custom: Draw your own curve with control points
- **Break speed limit**: Remove speed cap, supporting ultra-high scrolling speeds
- **Delay start**: Middle click triggers after a short delay, preventing misoperation
- **Per-app settings**: Configure different scrolling behavior for different apps

<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="60%" alt="Customization Demo" />
</div>

### Smart Features

- **Smart transparency**: Anchor icon auto-fades during fast scrolling or when mouse is near
- **Blacklist/Whitelist**: Disable auto-scroll in games or fullscreen apps, or only enable in specific apps
- **App detection**: Automatically pauses when FlowWheel's own window is activated
- **DPI aware**: Perfect scaling on high DPI displays

### Visual Feedback

- **Direction indicator**: Clear arrows showing scroll direction
- **Idle animation**: Subtle spinning wheel showing ready state
- **Reading mode indicator**: Shows current reading speed and mode status
- **Break speed badge**: Pulse animation when break mode is active
- **Custom icon**: Use your own anchor icon or choose from presets
- **Theme support**: Light/dark theme with smooth transitions

## Quick Start

### Installation

```powershell
# Scoop (Recommended)
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json

# Update to latest version
scoop update flowwheel
```

Or download `FlowWheel.exe` directly from [Releases](https://github.com/humanfirework/FlowWheel/releases).

### Usage

1. Launch FlowWheel (runs in tray)
2. **Hold middle mouse** and drag anywhere to scroll
3. **Double-click middle mouse** for Reading Mode, adjust speed with scroll wheel

<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="60%" alt="Quick Start Demo" />
</div>

## Trigger Modes

FlowWheel supports two trigger modes (configurable in settings):

| Mode | Activation | Behavior |
|------|-----------|----------|
| **Toggle** | Single middle click | Click to start, click again to stop (or release for inertia) |
| **Hold & Drag** | Hold middle button | Drag to scroll, release to throw with inertia |

## FAQ

### FlowWheel doesn't respond to clicks

- Check if FlowWheel is running (look for tray icon)
- Try right-clicking tray icon and select "Settings"
- Confirm target app is not in blacklist/whitelist

### Scroll speed feels off

- Adjust sensitivity slider in settings
- Try different acceleration curves
- Modify deadzone for more/less resistance to start
- Enable "Break speed limit" for higher ceiling

### Auto-scroll stops unexpectedly

- Some apps (games, video players) may block global hooks
- Add problematic app to blacklist

### Overlay doesn't appear

- Check Windows notification settings
- Ensure antivirus isn't blocking FlowWheel

## Architecture

FlowWheel is built on .NET 10 and WPF with a clean, modular architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────────┐  │
│  │ OverlayWindow│  │  SettingsWindow │  │ SplashWindow   │  │
│  └──────────────┘  └─────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       Core Engine                            │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ AutoScrollManager│  │ ScrollEngine  │  │ WindowManager│  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ SyncScrollManager│  │Acceleration-  │  │ ConfigManager│  │
│  │                  │  │ Curve         │  │              │  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Platform Integration Layer                │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │    MouseHook      │  │ KeyboardHook  │  │ NativeMethods│  │
│  │  (User32 Hook)   │  │ (User32 Hook) │  │ (SendInput)  │  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Highlights

- **Performance Optimized**: 30fps overlay, PID caching, event filtering
- **DPI Aware**: Perfect scaling on high-resolution displays
- **Event Injection**: Send scroll events with special signature via SendInput to prevent duplicate capture
- **Thread Safe**: Proper lock/Dispatcher usage for cross-thread UI updates
- **Graceful Shutdown**: IDisposable pattern for proper hook and timer cleanup

## System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 10.0 (included in self-contained build)
- **Display**: Any resolution (DPI aware)

## Contributing

Contributions welcome! Please read code guidelines before submitting PR.

### Development Setup

```powershell
# Clone repo
git clone https://github.com/humanfirework/FlowWheel.git
cd FlowWheel

# Run in dev mode (Debug config builds faster)
dotnet run --configuration Debug
```

## Privacy

FlowWheel is designed with privacy in mind from the ground up:

- **No telemetry**: No data sent to any servers
- **Local storage**: All settings stored locally
- **Minimal permissions**: Only requests input hook and admin rights when needed
- **Open source transparency**: Complete source code available for review

## Support

If FlowWheel has been helpful, feel free to buy me a coffee

<div align="center">
  <table>
    <tr>
      <td align="center">
        <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.jpg" width="150" alt="Alipay" />
        <br/>
        <strong>Alipay</strong>
      </td>
      <td width="40"></td>
      <td align="center">
        <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/weixin.jpg" width="150" alt="WeChat Pay" />
        <br/>
        <strong>WeChat Pay</strong>
      </td>
    </tr>
  </table>
</div>

## License

[MIT License](LICENSE)
