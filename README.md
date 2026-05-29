<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/flowwheel.png" width="120" alt="FlowWheel Logo" />
</div>

<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/1.gif" width="25%" alt="Demo 1" />
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/2.gif" width="25%" alt="Demo 2" />
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets_for_GitHub_Readme/3.gif" width="25%" alt="Demo 3" />
</div>

<h1 align="center">FlowWheel</h1>

<div align="center">

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)
[![Version](https://img.shields.io/badge/version-v1.7.7-green.svg)](https://github.com/humanfirework/FlowWheel/releases)

**中文** | [English](./README.en.md)

</div>

## 简介

将浏览器式的流畅滚屏体验带到 Windows 的每一个角落——用鼠标拖拽来滚动任意内容，享受基于物理的惯性滚动和先进的生产力功能。

## 它能解决什么问题？

想象一下阅读长篇文章、浏览代码文件或审阅文档时，无需不断移动滚轮的便利。FlowWheel 将你的鼠标变成了强大的导航工具：

- **解放双手阅读**：激活自动滚屏，让内容自动流动
- **精准控制**：自然地拖拽滚动，释放时带惯性滑行
- **多窗口工作流**：滚动一个窗口时，保持另一个窗口的位置

## 核心功能

### 基础滚动

- **拖拽滚动**：按住中键拖动，越远滚动越快，释放时带惯性滑行
- **距离速度控制**：从锚点拖拽越远，滚动越快——自然且直观
- **惯性物理**：在移动时释放鼠标，让内容"抛掷"滑行
- **防误触死区**：防止手部轻微颤抖导致的意外滚动


### 自定义选项

- **触发按键**：配置激活自动滚动的鼠标按钮或键盘快捷键
  - 中键、XButton1、XButton2
  - 键盘组合：Ctrl+Alt+F1、Shift+中键 等
- **自定义热键**：设置全局快捷键（如 Ctrl+Alt+S）随时切换滚动
- **加速度曲线**：5 种预设曲线类型 + 完全可自定义曲线
  - 线性：匀速增加
  - 指数：快速起步，渐变减速
  - 对数：慢速起步，快速加速
  - S形：带拐点的 S 曲线
  - 自定义：用控制点绘制你自己的曲线
- **突破速度限制**：移除速度上限，支持极高滚动速度
- **延时启动**：中键短按延迟触发，减少误操作
- **应用配置**：为不同应用配置不同的滚动行为

### 智能特性

- **智能透明度**：快速滚动或鼠标靠近时，锚点图标自动淡出
- **黑名单/白名单**：在游戏或全屏应用中禁用自动滚动，或仅在特定应用中启用
- **应用检测**：当 FlowWheel 自身窗口激活时自动暂停
- **DPI 感知**：完美适配高 DPI 显示器


## 快速开始

### 安装

```powershell
# Scoop（推荐）
scoop install https://github.com/humanfirework/FlowWheel/raw/main/flowwheel.json

# 更新到最新版本
scoop update flowwheel
```

或从 [Releases](https://github.com/humanfirework/FlowWheel/releases) 下载 `FlowWheel.exe` 直接运行。

### 使用

1. 启动 FlowWheel（托盘运行）
2. **按住中键**拖拽任意位置开始滚动
3. **双击中键**激活阅读模式，滚轮调整速度

## 触发模式

FlowWheel 支持两种触发模式，可在设置中配置：

| 模式 | 激活方式 | 行为 |
|------|---------|------|
| **切换模式** | 单击中键 | 点击开始，再点停止（或释放触发惯性） |
| **按住模式** | 按住中键 | 拖拽滚动，释放抛掷带惯性 |

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


## 架构设计

FlowWheel 基于 .NET 10 和 WPF 构建，采用清晰、模块化的架构：

## 系统要求

- 操作系统：Windows 10/11（64 位）
- 运行时：.NET 10.0（自包含版本已集成）
- 显示器：任意分辨率（支持 DPI 感知）

## 贡献

欢迎贡献！请在提交 PR 前阅读代码规范。

### 开发环境设置

```powershell
# 克隆仓库
git clone https://github.com/humanfirework/FlowWheel.git
cd FlowWheel

# 以开发模式运行（Debug 配置编译更快）
dotnet run --configuration Debug
```

## 隐私保护

FlowWheel 从设计之初就注重隐私：

- **无遥测**：不向任何服务器发送数据
- **本地存储**：全部设置存储在本地
- **最小权限**：仅在需要时请求输入钩子和管理员权限
- **开源透明**：完整源代码可供审查

## 支持

如果对你有帮助，欢迎请我喝杯咖啡

<div align="center">
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/alipay_qr.jpg" width="150" alt="Alipay" />
  &nbsp;&nbsp;
  <img src="https://github.com/humanfirework/FlowWheel/raw/main/Assets/weixin.jpg" width="150" alt="WeChat Pay" />
</div>

## 开源许可

[MIT License](LICENSE)
