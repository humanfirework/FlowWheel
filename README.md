# FlowWheel

<div align="center">

 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="30%" alt="Demo 1" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="30%" alt="Demo 2" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="30%" alt="Demo 3" />
 
 <br>
 
 [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
 [![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-1.7.0-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

 [English](#english) | [中文](#中文)

</div>

---

<a name="english"></a>

## English

**FlowWheel** is a powerful Windows utility that brings smooth, browser-style "Middle-Click Auto-Scroll" to the entire operating system. It now features advanced productivity tools like **Inertia Scrolling**, **Multi-Screen Sync** and **Reading Mode**.

### New in v1.5.5
- **Smooth Theme Switching**: Added smooth transition animations when switching between Light and Dark themes.
- **Custom Hotkeys**: Now you can customize your own global hotkeys for toggling auto-scroll.
- **Custom Icons**: New icon options with improved visual design for the overlay.
- **UI Enhancements**: Refined settings interface with better visual feedback and modern styling.
- **Bug Fix**: Fixed a startup crash caused by incorrect type binding for `DropShadowEffect.Color` in theme resources.

### Key Features

- **Universal Auto-Scroll**: Works in File Explorer, Word, IDEs, Discord, and almost any Windows application.
- **Inertia Physics**: Hold middle button to drag the page, release to throw it with inertia.
- **Reading Mode (Teleprompter)**: **Double-click** the middle mouse button to activate hands-free automatic scrolling. Perfect for reading long docs or logs while eating!
- **Multi-Screen Sync**: Scroll a document on your main screen, and reference documents on other screens (or side-by-side windows) will scroll in sync. Ideal for code reviews and translation.
- **Dynamic Speed**: Non-linear speed control—the further you move from the anchor, the faster it scrolls.
- **Smart Opacity**: The overlay anchor automatically fades out when your mouse is close to it or when moving fast, preventing text occlusion.
- **Modern UI**: Beautiful overlay with direction indicators and custom themes.

### Architecture

FlowWheel is built as a small set of focused modules with a clear event/data flow:

1) **Input Capture (Global Hooks)**
- `MouseHook` / `KeyboardHook`: Low-level global input hooks (User32) that receive raw OS events.

2) **Interaction Orchestration**
- `AutoScrollManager`: Interprets trigger mode (Toggle / Hold & Drag), starts/stops states, updates overlay, routes data to the engine.
- `WindowManager`: Detects the target window/process under cursor and applies blacklist/whitelist rules.

3) **Motion & Physics**
- `ScrollEngine`: Calculates scroll speed, handles inertia/reading mode, and emits wheel events via `SendInput`.
- `SyncScrollManager`: Optional multi-window/multi-monitor synchronized scrolling.

4) **UI & Feedback**
- `OverlayWindow`: Transparent overlay for anchor + direction indicators + reading mode state.
- `SettingsWindow`: User-facing configuration UI.

5) **Config & Updates**
- `ConfigManager`: Loads/saves persistent settings (`config.json`).
- `UpdateManager`: Checks latest GitHub Release and opens the release/asset download link.

**Runtime flow (simplified)**
`MouseHook/KeyboardHook` → `AutoScrollManager` → `ScrollEngine` → `SendInput (Wheel/HWheel)` → (optional) `SyncScrollManager`

### Installation

#### ~~Method 1: Scoop (Recommended)~~
You can easily install FlowWheel directly from this repository:

```powershell
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json
```

To update:
```powershell
scoop update flowwheel
```

#### Method 2: Manual Download
**(In the settings interface, "one-click check/automatic check" update and jump download are already supported. Be careful not to use a ladder, which may show API problems)**

Download the latest `FlowWheel.exe` from the [Releases](https://github.com/humanfirework/FlowWheel/releases) page. No installation required, just run it!

### Settings & Configuration

Manage all your preferences in the new Settings dashboard:

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/4.png" width="250" alt="FlowWheel Settings" />
</div>

- **Trigger Mode**: 
    - **Click Toggle**: Classic behavior. Click to start, click to stop.
    - **Hold & Drag**: Physics mode. Hold to drag, release to throw.
- **Sensitivity**: Fine-tune scroll speed and deadzone.
- **App Filtering**: 
    - **Blacklist**: Disable auto-scroll for specific games (e.g., CS:GO).
    - **Whitelist**: Only enable auto-scroll for specific apps.
    - **Drag & Drop**: Drag `.exe` files directly into the list to add them.
- **Global Hotkey**: Click the box and press your desired keys to set a toggle shortcut.

### Usage Guide

1.  **Auto-Scroll**: Click **Middle Mouse Button** once. Move mouse to scroll. Click again to stop (or release to throw if inertia is active).
2.  **Reading Mode**: **Double-click** Middle Mouse Button.
   *   Use **Mouse Wheel** to adjust reading speed on the fly.
   *   Click any button to stop.
3.  **Sync Scroll**: Enable it in Settings. Open two documents (on different screens or side-by-side). Start scrolling one, and the other follows!

---

<a name="中文"></a>

## 中文

**FlowWheel** 是一款强大的 Windows 全局自动滚动工具，它不仅将浏览器的"中键无极滚屏"体验带到了系统每个角落，还新增了**惯性滚动**、**多屏同步**和**阅读模式**等生产力功能。

###  v1.7.0 新功能
- **自定义加速度曲线**：用户现在可以通过拖拽控制点来创建和调整自定义加速度曲线。支持多种曲线类型，包括线性、指数、对数、S形以及完全自定义的曲线。
- **修复自定义曲线拖拽**：解决了自定义曲线控制点无法通过拖拽调整曲线形状的问题。

### 核心功能

- **全局自动滚屏**：支持资源管理器、Word、IDE、Discord 等几乎所有 Windows 应用。
- **抓取与抛掷 (Grab & Throw)**：类触摸屏物理手感！按住中键拖拽页面，松开产生惯性滑动。
- **阅读模式 (提词器)**：**双击鼠标中键**即可激活。解放双手，自动匀速滚动，看小说、看文档、看日志的摸鱼神器！
- **多屏/分屏同步**：在主屏滚动文档时，副屏（或并排）的文档会同步滚动。非常适合代码比对、翻译对照。
- **智能透明度**：当鼠标靠近锚点或快速滚动时，图标自动变淡，不再遮挡视线。
- **动态变速**：基于距离的非线性速度控制，精准把控浏览节奏。
- **现代化 UI**：提供美观的视觉反馈和方向指示。

### 架构说明

FlowWheel 由一组职责清晰的模块构成，整体数据/事件流非常直接：

1) **输入捕获（全局钩子）**
- `MouseHook` / `KeyboardHook`：基于 User32 的低级全局输入钩子，接收系统原始事件。

2) **交互编排**
- `AutoScrollManager`：解析触发模式（Toggle / Hold & Drag）、控制开始/停止状态、驱动 Overlay 更新，并把数据交给引擎。
- `WindowManager`：识别鼠标下的目标窗口/进程，应用黑名单/白名单规则。

3) **运动与物理**
- `ScrollEngine`：计算滚动速度、处理惯性/阅读模式，通过 `SendInput` 发送滚轮事件。
- `SyncScrollManager`：可选的多窗口/多屏同步滚动。

4) **UI 反馈**
- `OverlayWindow`：透明 Overlay（锚点、方向指示、阅读模式状态）。
- `SettingsWindow`：设置界面与配置入口。

5) **配置与更新**
- `ConfigManager`：加载/保存持久化配置（`config.json`）。
- `UpdateManager`：检查 GitHub 最新 Release，并打开 Release 页面或资产下载链接。

**运行时简化流程**
`MouseHook/KeyboardHook` → `AutoScrollManager` → `ScrollEngine` → `SendInput (Wheel/HWheel)` →（可选）`SyncScrollManager`

### 安装方法

#### ~~方法 1: Scoop (推荐)~~
如果你是开发者或极客，推荐使用 [Scoop](https://scoop.sh/) 直接安装：

```powershell
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json
```

更新软件：
```powershell
scoop update flowwheel
```

#### 方法 2: 手动下载
**(在设置界面，已经支持 "一键检查/自动检查" 更新并跳转下载，注意不要使用梯子，有可能会显示API问题)**

前往 [Releases](https://github.com/humanfirework/FlowWheel/releases) 页面下载最新的 `FlowWheel.exe`。绿色单文件，解压即用！

### 设置界面

您可以在全新的设置面板中管理所有功能：

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/4.png" width="250" alt="FlowWheel 设置界面" />
</div>

- **触发模式**：
    - **点击切换 (Toggle)**：经典模式。点击开启，再次点击关闭（支持惯性停止）。
    - **按住拖拽 (Hold & Drag)**：物理模式。按住拖拽，松开惯性滑动。
- **灵敏度调节**：自定义滚动速度倍率和防误触死区。
- **应用过滤**：
    - **黑名单**：在特定游戏（如 CS:GO）中禁用自动滚动。
    - **白名单**：仅在特定应用中启用。
    - **拖拽支持**：支持直接拖拽 `.exe` 文件进列表。
- **全局快捷键**：点击输入框并按下键盘，即可快速录制开关快捷键。

### 使用指南

1.  **自动滚屏**：单击 **鼠标中键** 激活。移动鼠标控制方向。再次点击停止（或利用惯性滑行停止）。
2.  **阅读模式**：**双击** 鼠标中键。
   *   滚动 **鼠标滚轮** 可实时调整自动播放速度。
   *   点击任意键停止。
3.  **同步滚动**：在设置中开启。打开两个文档（分屏或并排），滚动其中一个，另一个紧随其后！

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/5.png" width="250" alt="FlowWheel 暗夜模式" />
</div>

---

## License

This project is licensed under the [MIT License](LICENSE).
本项目采用 [MIT License](LICENSE) 开源。

## Buy me a coffee / 加个鸡腿

If you find this project helpful, feel free to buy me a coffee! ☕
如果觉得这个项目不错，欢迎请我喝杯咖啡或加个鸡腿！🍗

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.png" alt="Alipay" width="180" style="max-width: 100%; height: auto;" />
 <br>
 <span>(如果觉得不错，请使用支付宝扫码支持 / Scan to Donate with Alipay)</span>
</div>
