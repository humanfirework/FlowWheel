# FlowWheel

<div align="center">

 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="30%" alt="Demo 1" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="30%" alt="Demo 2" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="30%" alt="Demo 3" />

 <br>

 [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
 [![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-1.7.0-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

[中文](./README.md) | English

</div>

---

## Introduction

**FlowWheel** is a powerful Windows utility that brings smooth, browser-style auto-scrolling to the entire operating system. Unlike traditional scroll wheels, FlowWheel allows you to scroll any content by clicking and dragging with your mouse, complete with physics-based inertia and advanced productivity features.

### What Problem Does It Solve?

Imagine reading a long article, scrolling through a code file, or reviewing a document without constantly moving your scroll wheel. FlowWheel transforms your mouse into a powerful navigation tool:

- **Hands-free reading**: Activate auto-scroll and just watch the content flow
- **Precise control**: Drag to scroll naturally, release to coast with inertia
- **Multi-document workflows**: Scroll one window while keeping your place in another

### Why Choose FlowWheel?

| Feature | Traditional Scroll | FlowWheel |
|---------|------------------|-----------|
| Scroll Speed | Constant, requires wheel spinning | Distance-based, intuitive control |
| Long Document Reading | Arm fatigue from wheel | Hands-free reading mode |
| Multi-Window Sync | Manual scrolling | Automatic synchronized scrolling |
| Physics Feel | None | Inertia, grab & throw |
| Customization | Limited | Fully customizable curves |

### New in v1.7.0
- **Custom Acceleration Curve**: Create your own scroll feel with the visual curve editor. Choose from Linear, Exponential, Logarithmic, Sigmoid presets, or draw a fully custom curve.
- **Fixed Custom Curve Dragging**: Improved control point manipulation for precise curve customization.

---

## Key Features

### Core Scrolling

- **Universal Auto-Scroll**: Works in File Explorer, Word, IDEs, Discord, browsers, PDF readers—virtually any Windows application
- **Distance-Based Speed**: The further you drag from the anchor point, the faster you scroll—natural and intuitive
- **Inertia Physics**: Release the mouse while moving to "throw" the content with realistic coasting behavior
- **Deadzone Protection**: Prevents accidental scrolling from small hand tremors

### Advanced Modes

- **Reading Mode (Teleprompter)**: **Double-click** the middle mouse button for hands-free continuous scrolling
  - Adjust speed in real-time with your scroll wheel
  - Click any button to stop instantly
- **Multi-Screen Sync**: Scroll a document on your main screen while reference documents on secondary monitors follow in lockstep—perfect for code reviews, translations, or comparing documents
- **Axis Lock**: Prefer vertical or horizontal scrolling? Enable axis lock to prevent unwanted direction changes

### Customization

- **Trigger Keys**: Configure which mouse buttons or keyboard shortcuts activate auto-scroll
  - Middle Mouse, XButton1, XButton2
  - Keyboard combinations: Ctrl+Alt+F1, Shift+MiddleMouse, etc.
- **Custom Hotkeys**: Set global shortcuts (Ctrl+Alt+S) to toggle scrolling from anywhere
- **Acceleration Curves**: Five preset curve types plus fully customizable curves
  - Linear: Constant rate increase
  - Exponential: Fast start, gradual slowdown
  - Logarithmic: Slow start, rapid acceleration
  - Sigmoid: S-curve with inflection point
  - Custom: Draw your own curve with control points
- **Per-App Profiles**: Configure different scroll behaviors for different applications

### Smart Features

- **Smart Opacity**: The overlay anchor fades when you're scrolling fast or when your mouse approaches it
- **Blacklist/Whitelist**: Disable auto-scroll in games or fullscreen apps, or only enable it in specific programs
- **Application Detection**: Automatically pauses when FlowWheel's own window is active
- **DPI-Aware**: Perfectly calibrated for high-DPI displays

### Visual Feedback

- **Direction Indicators**: Clear arrows show scroll direction
- **Idle Animation**: Subtle spinning wheel shows ready state
- **Custom Icons**: Use your own anchor icon or choose from presets
- **Theme Support**: Dark and Light themes with smooth transitions

---

## Architecture

FlowWheel is built with a clean, modular architecture on .NET 10 using WPF:

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                             │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────────┐  │
│  │ OverlayWindow│  │  SettingsWindow │  │ SplashWindow   │  │
│  └──────────────┘  └─────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Core Engine                            │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ AutoScrollManager│  │ ScrollEngine  │  │ WindowManager│  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ SyncScrollManager│  │Acceleration-  │  │ ConfigManager │  │
│  │                  │  │ Curve         │  │              │  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Platform Integration                       │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │    MouseHook     │  │ KeyboardHook  │  │ NativeMethods│  │
│  │  (User32 Hook)   │  │ (User32 Hook) │  │ (SendInput)   │  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Core Components

1. **Input Capture (Global Hooks)**
   - `MouseHook`: Low-level Windows mouse hook for intercepting clicks, movement, and wheel events
   - `KeyboardHook`: Global keyboard hook for hotkey support
   - `NativeMethods`: P/Invoke wrappers for User32 functions (SendInput, SetWindowsHookEx)

2. **Interaction Orchestration**
   - `AutoScrollManager`: State machine managing trigger modes (Toggle/Hold), double-click detection, and event routing
   - `WindowManager`: Process detection with caching, blacklist/whitelist evaluation, and per-app profile lookup

3. **Motion & Physics**
   - `ScrollEngine`: Core scroll logic with:
     - Scroll state machine (Idle → Dragging → InertialScrolling → Idle)
     - Speed calculation based on distance and acceleration curve
     - Low-pass filtering for smooth speed transitions
     - Inertia simulation with exponential friction decay
   - `AccelerationCurve`: Interpolation engine supporting Linear, Exponential, Logarithmic, Sigmoid, and Custom curve types
   - `SyncScrollManager`: Multi-window synchronized scrolling via FindWindow and SendMessage

4. **UI & Feedback**
   - `OverlayWindow`: Transparent, click-through overlay displaying:
     - Anchor position and icon
     - Direction arrows
     - Reading mode indicator
     - Smart opacity based on speed/distance
   - `SettingsWindow`: Configuration UI with tabs for General, Curves, Hotkeys, Apps, and About

5. **Configuration & Platform**
   - `ConfigManager`: JSON-based persistent configuration (config.json)
   - `ThemeManager`: WPF resource dictionary swapping for theme switching
   - `LanguageManager`: XAML-based localization (en-US, zh-CN)
   - `UpdateManager`: GitHub API integration for release checking

### Technical Highlights

- **Performance Optimized**: 30fps overlay rendering, event filtering to reduce overhead, PID caching
- **DPI Awareness**: Proper scaling for high-DPI displays via VisualTreeHelper
- **Event Injection**: Scroll events sent via SendInput with special signature to prevent re-capture
- **Thread Safety**: Proper use of locks for cross-thread UI updates
- **Clean Shutdown**: IDisposable pattern with proper cleanup of hooks and timers

---

## Installation

### System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 10.0 (included in self-contained build)
- **Display**: Any resolution (DPI-aware)

### Quick Install

#### Method 1: Scoop (Recommended for Developers)

```powershell
# Add the bucket and install
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json

# Update to latest version
scoop update flowwheel
```

#### Method 2: Direct Download

1. Visit the [Releases](https://github.com/humanfirework/FlowWheel/releases) page
2. Download the latest `FlowWheel.exe`
3. Run the executable—no installation required

> **Note**: The built-in update checker in Settings works best without a VPN. If you encounter API errors, try downloading manually.

---

## Usage Guide

### Getting Started

1. **Launch FlowWheel**—it will run in the system tray
2. **Look for the wheel icon** in your system tray (bottom-right corner)
3. **Click and drag** anywhere to scroll!

### Trigger Modes

FlowWheel supports two trigger modes, configurable in Settings:

| Mode | Activation | Behavior |
|------|------------|----------|
| **Toggle** | Single middle-click | Click to start, click again to stop (or release for inertia) |
| **Hold & Drag** | Hold middle mouse | Drag to scroll, release to throw with inertia |

### Reading Mode (Teleprompter)

**Activation**: Double-click the middle mouse button

- Content scrolls automatically at a steady pace
- Use the **mouse wheel** to adjust scroll speed in real-time
- Press **any mouse button** or **Escape key** to stop
- Perfect for reading long documents, following live streams, or presentations

### Synchronized Scrolling

1. Open Settings → Enable "Synchronized Scrolling"
2. Open two documents side-by-side (or on multiple monitors)
3. Scroll in one window—the other follows automatically

### Custom Acceleration Curves

1. Open Settings → Curves tab
2. Choose a preset curve or click "Edit Custom"
3. Drag control points to shape your ideal scroll feel
4. Preview in real-time

### Application Filtering

**Blacklist Mode** (default): Auto-scroll disabled for listed apps
**Whitelist Mode**: Auto-scroll enabled ONLY for listed apps

To add an application:
- Drag and drop the `.exe` file directly into the list
- Or click "Add" and browse to the application

---

## Configuration Reference

### config.json

Located in `%APPDATA%\FlowWheel\config.json`

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
| `TriggerKey` | string | "MiddleMouse" | Mouse/key combination to trigger |
| `ToggleHotkey` | string | "" | Global hotkey (e.g., "Ctrl+Alt+S") |
| `Sensitivity` | float | 0.8 | Base scroll speed multiplier (0.1-3.0) |
| `Deadzone` | int | 20 | Pixels before scrolling starts |
| `AccelerationCurve` | enum | "Linear" | Curve type for speed progression |
| `IsWhitelistMode` | bool | false | true=whitelist, false=blacklist |

---

## Troubleshooting

### FlowWheel doesn't respond to clicks

- Check that FlowWheel is running (look for tray icon)
- Try right-clicking the tray icon and selecting "Settings"
- Verify the target application isn't in your blacklist/whitelist

### Scroll speed feels wrong

- Adjust Sensitivity slider in Settings
- Try a different Acceleration Curve
- Modify Deadzone for more/less starting resistance

### Auto-scroll stops unexpectedly

- Some applications (games, video players) may block global hooks
- Add problematic apps to the Blacklist

### Overlay doesn't appear

- Check Windows notification settings
- Ensure your antivirus isn't blocking FlowWheel

---

## Contributing

Contributions are welcome! Please read our guidelines before submitting PRs.

### Development Setup

```powershell
# Clone the repository
git clone https://github.com/humanfirework/FlowWheel.git
cd FlowWheel

# Run in development mode (Debug configuration for faster builds)
dotnet run --configuration Debug
```

### Code Standards

1. **Style**: Follow standard C# conventions (Microsoft's .NET guidelines)
2. **Naming**: Use descriptive names—avoid abbreviations unless universally understood
3. **Documentation**: Add XML comments for public APIs
4. **Threading**: Always use locks or Dispatcher for cross-thread operations

### Pull Request Process

1. **Fork** the repository
2. **Create a feature branch**: `git checkout -b feature/my-feature`
3. **Make your changes** with clear, descriptive commits
4. **Test thoroughly**—verify existing functionality still works
5. **Submit a PR** with a clear description of the change
6. **Respond to review feedback** promptly

### Commit Message Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

**Examples**:
```
feat(curves): add custom curve editor
fix(scroll): correct speed calculation for high-DPI displays
docs(readme): update installation instructions
```

---

## Privacy

FlowWheel is designed with privacy in mind:

- **No telemetry**: No data is sent to any server
- **Local storage**: All settings stored locally on your machine
- **Minimal permissions**: Only requests input hook and admin privileges when needed
- **Open source**: Full source code available for transparency

---

## License

This project is licensed under the [MIT License](LICENSE).

## Support

If you find FlowWheel useful, consider buying me a coffee! ☕

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.png" alt="Alipay" width="180" style="max-width: 100%; height: auto;" />
 <br>
 <span>Scan to Donate with Alipay</span>
</div>
