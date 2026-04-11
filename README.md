# FlowWheel

<div align="center">

 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="30%" alt="Demo 1" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="30%" alt="Demo 2" />
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="30%" alt="Demo 3" />

 <br>

 [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
 [![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-1.7.3-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

中文 | [English](./README.en.md)

</div>

---

## 简介

**FlowWheel** 是一款强大的 Windows 全局自动滚动工具，它将浏览器式的流畅滚屏体验带到了系统的每一个角落。与传统滚轮不同，FlowWheel 允许你通过鼠标拖拽来滚动任意内容，并配备基于物理的惯性滚动和先进的生产力功能。

### 它能解决什么问题？

想象一下阅读长篇文章、浏览代码文件或审阅文档时，无需不断移动滚轮的便利。FlowWheel 将你的鼠标变成了强大的导航工具：

- **解放双手阅读**：激活自动滚屏，让内容自动流动
- **精准控制**：自然地拖拽滚动，释放时带惯性滑行
- **多窗口工作流**：滚动一个窗口时，保持另一个窗口的位置

### 为什么选择 FlowWheel？

| 功能 | 传统滚轮 | FlowWheel |
|------|---------|-----------|
| 滚动速度 | 恒定，需要转动滚轮 | 基于距离，直观控制 |
| 长文档阅读 | 滚轮转动导致手臂疲劳 | 解放双手的阅读模式 |
| 多窗口同步 | 手动滚动 | 自动同步滚动 |
| 物理手感 | 无 | 惯性、抓取与抛掷 |
| 自定义程度 | 有限 | 完全可定制曲线 |

### v1.7.3 更新

- **阅读模式独立快捷键**：支持设置独立的快捷键激活阅读模式，与滚屏触发键分离
- **快捷键冲突检测**：配置阅读模式快捷键时会提示是否与其他快捷键冲突
- **延时启动机制**：中键短按延迟触发，减少与中键单击功能的误触发冲突
- **阅读速度实时显示**：阅读模式激活时锚点旁显示当前滚动速度
- **恢复默认值功能**：高级参数支持一键恢复出厂设置，带确认对话框
- **更新日志显示**：检查更新时显示版本更新内容（Release Notes）
- **UI 流畅动画**：面板展开/折叠带平滑过渡效果

---

## 核心功能

### 基础滚动

- **全局自动滚屏**：支持资源管理器、Word、IDE、Discord、浏览器、PDF 阅读器——几乎所有 Windows 应用
- **距离速度控制**：从锚点拖拽越远，滚动越快——自然且直观
- **惯性物理**：在移动时释放鼠标，让内容"抛掷"滑行
- **防误触死区**：防止手部轻微颤抖导致的意外滚动

### 高级模式

- **阅读模式（提词器）**：**双击**鼠标中键或使用独立快捷键，激活解放双手的连续滚动
  - 实时用滚轮调整速度
  - 点击任意按钮立即停止
- **多屏同步滚动**：在主屏滚动文档时，副屏的参考文档会同步跟随——非常适合代码比对、翻译对照
- **轴锁定**：更喜欢垂直或水平滚动？启用轴锁定防止意外的方向变化

### 自定义选项

- **触发按键**：配置激活自动滚动的鼠标按钮或键盘快捷键
  - 中键、XButton1、XButton2
  - 键盘组合：Ctrl+Alt+F1、Shift+中键 等
- **自定义热键**：设置全局快捷键（如 Ctrl+Alt+S）随时切换滚动
- **阅读模式快捷键**：独立的快捷键激活阅读模式
- **加速度曲线**：5 种预设曲线类型 + 完全可自定义曲线
  - 线性：匀速增加
  - 指数：快速起步，渐变减速
  - 对数：慢速起步，快速加速
  - S形：带拐点的 S 曲线
  - 自定义：用控制点绘制你自己的曲线
- **突破速度限制**：移除速度上限，支持极高滚动速度
- **延时启动**：中键短按延迟触发，防止误操作
- **应用配置**：为不同应用配置不同的滚动行为

### 智能特性

- **智能透明度**：快速滚动或鼠标靠近时，锚点图标自动淡出
- **黑名单/白名单**：在游戏或全屏应用中禁用自动滚动，或仅在特定应用中启用
- **应用检测**：当 FlowWheel 自身窗口激活时自动暂停
- **DPI 感知**：完美适配高 DPI 显示器

### 视觉反馈

- **方向指示器**：清晰的箭头显示滚动方向
- **空闲动画**：微妙的旋转轮盘显示就绪状态
- **阅读模式指示器**：显示当前阅读速度和模式状态
- **突破速度标识**：突破模式激活时显示脉冲动画
- **自定义图标**：使用你自己的锚点图标或从预设中选择
- **主题支持**：明暗主题切换，流畅过渡动画

---

## 架构设计

FlowWheel 基于 .NET 10 和 WPF 构建，采用清晰、模块化的架构：

```
┌─────────────────────────────────────────────────────────────┐
│                        UI 层                                │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────────┐  │
│  │ OverlayWindow│  │  SettingsWindow │  │ SplashWindow   │  │
│  └──────────────┘  └─────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       核心引擎                               │
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
│                     平台集成层                                │
│  ┌──────────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │    MouseHook     │  │ KeyboardHook  │  │ NativeMethods│  │
│  │  (User32 Hook)   │  │ (User32 Hook) │  │ (SendInput)  │  │
│  └──────────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件

1. **输入捕获（全局钩子）**
   - `MouseHook`：低级 Windows 鼠标钩子，拦截点击、移动和滚轮事件
   - `KeyboardHook`：全局键盘钩子，用于热键支持
   - `NativeMethods`：User32 函数的 P/Invoke 包装（SendInput、SetWindowsHookEx）

2. **交互编排**
   - `AutoScrollManager`：状态机，管理触发模式（切换/按住）、双击检测和事件路由
   - `WindowManager`：带缓存的进程检测、黑名单/白名单评估和按应用配置查询

3. **运动与物理**
   - `ScrollEngine`：核心滚动逻辑，包含：
     - 滚动状态机（空闲 → 拖拽 → 惯性滚动 → 空闲）
     - 基于距离和加速度曲线的速度计算
     - 低通滤波实现平滑速度过渡
     - 指数摩擦衰减的惯性模拟
   - `AccelerationCurve`：支持线性、指数、对数、S形和自定义曲线类型的插值引擎
   - `SyncScrollManager`：通过 FindWindow 和 SendMessage 实现多窗口同步滚动

4. **UI 与反馈**
   - `OverlayWindow`：透明、可穿透的悬浮窗，显示：
     - 锚点位置和图标
     - 方向箭头
     - 阅读模式指示器和实时速度
     - 基于速度/距离的智能透明度
     - 突破速度模式标识
   - `SettingsWindow`：配置界面，包含常规、曲线、热键、应用、关于等标签页

5. **配置与平台**
   - `ConfigManager`：基于 JSON 的持久化配置（config.json）
   - `ThemeManager`：通过 WPF 资源字典切换实现主题切换
   - `LanguageManager`：基于 XAML 的本地化（en-US、zh-CN）
   - `UpdateManager`：GitHub API 集成检查版本更新并显示更新日志

### 技术亮点

- **性能优化**：30fps 悬浮窗渲染、事件过滤减少开销、PID 缓存
- **DPI 感知**：通过 VisualTreeHelper 实现高 DPI 显示器的正确缩放
- **事件注入**：通过 SendInput 发送带特殊签名的滚动事件，防止重复捕获
- **线程安全**：正确使用锁或 Dispatcher 进行跨线程 UI 更新
- **优雅退出**：IDisposable 模式，正确清理钩子和定时器

---

## 安装说明

### 系统要求

- **操作系统**：Windows 10/11（64 位）
- **运行时**：.NET 10.0（包含在自包含版本中）
- **显示器**：任意分辨率（支持 DPI 感知）

### 快速安装

#### 方法 1：Scoop（推荐）

```powershell
# 直接从 GitHub 安装
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json

# 更新到最新版本
scoop update flowwheel
```

#### 方法 2：直接下载

1. 访问 [Releases](https://github.com/humanfirework/FlowWheel/releases) 页面
2. 下载最新的 `FlowWheel.exe`
3. 运行即可——无需安装

> **注意**：设置中的内置更新检查器在不使用 VPN 时效果最佳。如果遇到 API 错误，请尝试手动下载。

---

## 使用指南

### 快速入门

1. **启动 FlowWheel**——它将在系统托盘运行
2. **找到系统托盘中的轮盘图标**（右下角）
3. **点击并拖动**任意位置即可滚动！

### 触发模式

FlowWheel 支持两种触发模式，可在设置中配置：

| 模式 | 激活方式 | 行为 |
|------|---------|------|
| **切换** | 单击中键 | 点击开始，再点停止（或释放触发惯性） |
| **按住拖拽** | 按住中键 | 拖拽滚动，释放抛掷带惯性 |

### 阅读模式（提词器）

**激活**：双击鼠标中键，或使用独立快捷键（如已配置 Ctrl+Alt+R）

- 内容以稳定速度自动滚动
- 使用**鼠标滚轮**实时调整滚动速度，锚点旁实时显示当前速度
- 按**任意鼠标按钮**或 **Escape 键**停止
- 非常适合阅读长文档、追踪直播或演示

### 延时启动

启用延时启动后，中键按下后会延迟一段时间再触发滚动功能，有效减少与中键单击（如在浏览器中打开新标签页）的冲突。可在设置中调节延迟时间。

### 同步滚动

1. 打开设置 → 启用"同步滚动"
2. 打开两个文档并排显示（或在多显示器上）
3. 在其中一个窗口滚动——另一个会自动跟随

### 自定义加速度曲线

1. 打开设置 → 曲线标签页
2. 选择预设曲线或点击"编辑自定义"
3. 拖动控制点塑造你理想的滚动手感
4. 实时预览效果

### 应用过滤

**黑名单模式**（默认）：列表中的应用禁用自动滚动
**白名单模式**：仅在列表中的应用启用自动滚动

添加应用方法：
- 直接将 `.exe` 文件拖放到列表中
- 或点击"添加"并浏览到应用程序

---

## 配置参考

### config.json

位于 `%APPDATA%\FlowWheel\config.json`

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

### 关键参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `TriggerMode` | string | "Toggle" | "Toggle" 或 "Hold" |
| `TriggerKey` | string | "MiddleMouse" | 触发鼠标/按键组合 |
| `ToggleHotkey` | string | "" | 全局热键（如 "Ctrl+Alt+S"） |
| `ReadingModeHotkey` | string | "Ctrl+Alt+R" | 阅读模式独立快捷键 |
| `MiddleClickDelay` | int | 0 | 延时启动延迟（毫秒），0=关闭 |
| `BreakSpeedLimit` | bool | false | 是否启用突破速度限制 |
| `BreakSpeedLimitMax` | double | 2000 | 突破模式下的最大速度 |
| `Sensitivity` | float | 0.8 | 基础滚动速度倍率（0.1-3.0） |
| `Deadzone` | int | 20 | 开始滚动前的像素数 |
| `AccelerationCurve` | enum | "Linear" | 速度曲线的类型 |
| `IsWhitelistMode` | bool | false | true=白名单，false=黑名单 |

---

## 常见问题

### FlowWheel 不响应点击

- 检查 FlowWheel 是否正在运行（查看托盘图标）
- 尝试右键点击托盘图标并选择"设置"
- 确认目标应用不在黑名单/白名单中

### 滚动速度感觉不对

- 调整设置中的灵敏度滑块
- 尝试不同的加速度曲线
- 修改死区以获得更多/更少的启动阻力
- 启用"突破速度限制"以获得更高上限

### 自动滚动意外停止

- 某些应用（游戏、视频播放器）可能阻止全局钩子
- 将有问题的应用添加到黑名单

### 悬浮窗不显示

- 检查 Windows 通知设置
- 确保杀毒软件没有阻止 FlowWheel

---

## 贡献指南

欢迎贡献！请在提交 PR 前阅读我们的指南。

### 开发环境设置

```powershell
# 克隆仓库
git clone https://github.com/humanfirework/FlowWheel.git
cd FlowWheel

# 以开发模式运行（Debug 配置编译更快）
dotnet run --configuration Debug
```

### 代码规范

1. **风格**：遵循标准 C# 约定（Microsoft 的 .NET 指南）
2. **命名**：使用描述性名称——避免缩写，除非是普遍理解的
3. **文档**：为公共 API 添加 XML 注释
4. **线程处理**：跨线程操作必须使用锁或 Dispatcher

### Pull Request 流程

1. **Fork** 仓库
2. **创建功能分支**：`git checkout -b feature/my-feature`
3. **进行更改**，提交信息清晰描述
4. **彻底测试**——确认现有功能仍然正常
5. **提交 PR**，清晰描述更改内容
6. **及时回复**审阅反馈

### 提交信息格式

```
<类型>(<范围>): <描述>

[可选正文]

[可选页脚]
```

**类型**：`feat`、`fix`、`docs`、`style`、`refactor`、`test`、`chore`

**示例**：
```
feat(curves): 添加自定义曲线编辑器
fix(scroll): 修复高 DPI 显示器的速度计算
docs(readme): 更新安装说明
```

---

## 隐私保护

FlowWheel 从设计之初就注重隐私：

- **无遥测**：不向任何服务器发送数据
- **本地存储**：所有设置存储在本地
- **最小权限**：仅在需要时请求输入钩子和管理员权限
- **开源透明**：完整源代码可供审查

---

## 开源许可

本项目采用 [MIT License](LICENSE) 开源。

## 支持项目

如果 FlowWheel 对你有帮助，欢迎请我喝杯咖啡！☕

<div align="center">
 <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.png" alt="Alipay" width="180" style="max-width: 100%; height: auto;" />
 <br>
 <span>扫码支持（支付宝）</span>
</div>
