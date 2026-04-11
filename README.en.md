# FlowWheel

<div align="center">

 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="30%" alt="Demo 1" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="30%" alt="Demo 2" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="30%" alt="Demo 3" />

 <br>

 [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
 [![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-1.7.3-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

[中文](./README.md) | English

</div>

---

## Overview

**FlowWheel** is a powerful Windows global auto-scrolling tool that brings browser-style smooth scrolling to every corner of your system. Unlike traditional scroll wheels, FlowWheel lets you scroll anything by dragging with your mouse, complete with physics-based inertial scrolling and advanced productivity features.

### What Problem Does It Solve?

Imagine the convenience of reading long articles, browsing code files, or reviewing documents without constantly moving your scroll wheel. FlowWheel turns your mouse into a powerful navigation tool:

- **Hands-free reading**: Activate auto-scrolling and let content flow automatically
- **Precise control**: Drag naturally to scroll, with inertia glide on release
- **Multi-window workflows**: Scroll one window while keeping another's position

### Why Choose FlowWheel?

| Feature | Traditional Scroll | FlowWheel |
|---------|-------------------|-----------|
| Scroll speed | Fixed, requires wheel spinning | Distance-based, intuitive control |
| Long document reading | Arm fatigue from wheel spinning | Hands-free reading mode |
| Multi-window sync | Manual scrolling | Auto sync scroll |
| Physics feel | None | Inertia, grab & throw |
| Customization | Limited | Fully customizable curves |

### What's New in v1.7.3

- **Reading mode blacklist fix**: Double middle-click no longer triggers reading mode on blacklisted apps
- **Independent reading mode hotkey**: Set a dedicated hotkey (e.g., Ctrl+Alt+R) to activate reading mode, separate from scroll trigger key
- **Hotkey conflict detection**: Warns when reading mode hotkey conflicts with other hotkeys
- **Delay start mechanism**: Middle click triggers after a short delay, reducing accidental conflicts with middle-click actions
- **Real-time reading speed display**: Shows current scroll speed next to anchor in reading mode
- **Break speed limit**: Optional removal of max speed cap, supporting up to 5000 px/s
- **Break speed visual indicator**: Pulse animation badge when break mode is active
- **Reset to defaults**: One-click restore factory settings for advanced parameters, with confirmation dialog
- **Changelog display**: Shows release notes when checking for updates
- **Smooth UI animations**: Panel expand/collapse with smooth transition effects

---

## Core Features

### Basic Scrolling

- **Global auto-scroll**: Works in File Explorer, Word, IDEs, Discord, browsers, PDF readers—almost any Windows app
- **Distance-speed control**: The further you drag from the anchor, the faster you scroll—natural and intuitive
- **Inertia physics**: Release the mouse while moving and let content "throw" and glide
- **Anti-accidental deadzone**: Prevents accidental scrolling from slight hand tremors

### Advanced Modes

- **Reading Mode (Auto-scroll)**: **Double-click** middle mouse or use the dedicated hotkey to activate hands-free continuous scrolling
  - Adjust speed in real-time with the mouse wheel
  - Stop instantly by clicking any button
- **Multi-window Sync Scroll**: Scroll a document on your main screen and a reference on the second screen follows automatically—perfect for code comparison, translation对照
- **Axis Lock**: Prefer vertical or horizontal scrolling? Enable axis lock to prevent accidental direction changes

### Customization Options

- **Trigger key**: Configure the mouse button or keyboard shortcut to activate auto-scrolling
  - Middle button, XButton1, XButton2
  - Keyboard combos: Ctrl+Alt+F1, Shift+Middle, etc.
- **Custom hotkey**: Set a global hotkey (e.g., Ctrl+Alt+S) to toggle scrolling anytime
- **Reading mode hotkey**: Dedicated hotkey to activate reading mode
- **Acceleration curve**: 5 preset curve types + fully customizable curves
  - Linear: Constant speed increase
  - Exponential: Fast start, gradual slow-down
  - Logarithmic: Slow start, rapid acceleration
  - S-curve: S-curve with inflection points
  - Custom: Draw your own curve with control points
- **Break speed limit**: Remove speed cap, supporting ultra-high scrolling speeds
- **Delay start**: Middle click triggers after a short delay, preventing mis操作
- **Per-app settings**: Configure different scrolling behavior for different apps

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

---

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

---

## Installation

### System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 10.0 (included in self-contained build)
- **Display**: Any resolution (DPI aware)

### Quick Install

#### Method 1: Scoop (Recommended)

```powershell
# Install directly from GitHub
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json

# Update to latest version
scoop update flowwheel
```

#### Method 2: Direct Download

1. Visit the [Releases](https://github.com/humanfirework/FlowWheel/releases) page
2. Download the latest `FlowWheel.exe`
3. Run it—no installation needed!

---

## Usage Guide

### Quick Start

1. **Launch FlowWheel**—it'll run in the system tray
2. **Find the wheel icon in the system tray** (bottom-right)
3. **Click and drag** anywhere to scroll!

### Trigger Modes

FlowWheel supports two trigger modes (configurable in settings):

| Mode | Activation | Behavior |
|------|-----------|----------|
| **Toggle** | Single middle click | Click to start, click again to stop (or release for inertia) |
| **Hold & Drag** | Hold middle button | Drag to scroll, release to throw with inertia |

### Reading Mode (Auto-scroll)

**Activate**: Double-click middle mouse, or use the dedicated hotkey (e.g., Ctrl+Alt+R if configured)

- Content scrolls automatically at a steady speed
- Use **mouse wheel** to adjust speed in real-time—current speed is displayed next to the anchor
- Press **any mouse button** or **Escape** to stop
- Perfect for reading long documents, following live streams, or presentations

### Delay Start

When enabled, pressing middle mouse triggers after a short delay, effectively reducing conflicts with middle-click actions (like opening a new tab in browser). Adjustable in settings.

### Sync Scroll

1. Open Settings → Enable "Sync Scroll"
2. Open two documents side by side (or on multiple monitors)
3. Scroll in one window—the other follows automatically

### Custom Acceleration Curves

1. Open Settings → Curves tab
2. Choose a preset or click "Edit Custom"
3. Drag control points to shape your ideal scroll feel
4. Preview the effect in real-time

### App Filtering

**Blacklist mode** (default): Apps in the list disable auto-scroll
**Whitelist mode**: Only apps in the list enable auto-scroll

How to add apps:
- Drag and drop `.exe` files directly into the list
- Or click "Add" and browse to the application

---

## Configuration Reference

### config.json

Located at `%APPDATA%\FlowWheel\config.json`

```json
{
  "TriggerMode": "Toggle",
  "TriggerKey": "MiddleMouse",
  "ToggleHotkey": "",
  "Sensitivity": 0.8,
  "Deadzone": 20,
  "AccelerationCurve": "Linear",
  "IsWhitelistMode": false,
  "AppProfiles": [],
  "Theme": "Dark",
  "Language": "en-US",
  "IconSize": 48
}
```

### Key Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TriggerMode` | string | "Toggle" | "Toggle" or "Hold" |
| `TriggerKey` | string | "MiddleMouse" | Trigger mouse/key combo |
| `ToggleHotkey` | string | "" | Global hotkey (e.g., "Ctrl+Alt+S") |
| `ReadingModeHotkey` | string | "Ctrl+Alt+R" | Reading mode dedicated hotkey |
| `MiddleClickDelay` | int | 0 | Delay start delay (ms), 0=off |
| `BreakSpeedLimit` | bool | false | Enable break speed limit |
| `BreakSpeedLimitMax` | double | 2000 | Max speed in break mode |
| `Sensitivity` | float | 0.8 | Base scroll speed multiplier (0.1-3.0) |
| `Deadzone` | int | 20 | Pixels before scroll starts |
| `AccelerationCurve` | enum | "Linear" | Speed curve type |
| `IsWhitelistMode` | bool | false | true=whitelist, false=blacklist |

---

## Privacy

FlowWheel is designed with privacy in mind from the ground up:

- **No telemetry**: No data sent to any servers
- **Local storage**: All settings stored locally
- **Minimal permissions**: Only requests input hook and admin rights when needed
- **Open source transparency**: Complete source code available for review

---

## Open Source License

This project is open source under the [MIT License](LICENSE).

## Support the Project

If FlowWheel has been helpful, feel free to buy me a coffee! ☕

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.png" alt="Alipay" width="180" style="max-width: 100%; height: auto;" />
 <br>
 <span>Scan to support (Alipay)</span>
</div>
