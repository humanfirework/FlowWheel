# FlowWheel 🌊

<div align="center">

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://github.com/humanfirework/FlowWheel/actions/workflows/build.yml/badge.svg)](https://github.com/humanfirework/FlowWheel/actions)

[English](#english) | [中文](#中文)

</div>

---

<a name="english"></a>
##  English

**FlowWheel** is a lightweight Windows global auto-scroll utility that brings the "Middle-Click Auto-Scroll" experience from browsers to every corner of your operating system.

Whether you're reading long documents, browsing code, or navigating applications that don't support auto-scrolling natively, FlowWheel provides a silky-smooth scrolling experience.

### Key Features

- ** Universal Compatibility**: Works in almost all Windows applications, including File Explorer, Word, IDEs, Discord, and more.
- ** Dynamic Speed**: Non-linear speed control based on mouse distance—the further from the anchor, the faster the scroll.
- **↔ Omni-directional**: Supports not just vertical, but also horizontal scrolling (if the app supports it), making it perfect for wide tables or canvases.
- ** Visual Feedback**: Modern UI overlay showing the scroll anchor and direction indicators for intuitive interaction.
- **🛠️ Highly Customizable**:
  - Custom center anchor icon (just drop in an `anchor.png`).
  - Bilingual interface (English/Chinese).
  - Tray-based operation with minimal resource usage.

### Usage Guide

1. **Launch**: Run `FlowWheel.exe`. A small icon will appear in the system tray.
2. **Activate**: Press the **Middle Mouse Button** anywhere on the screen.
3. **Scroll**:
   - An anchor icon appears.
   - Move mouse Up/Down -> Page scrolls Up/Down.
   - Move mouse Left/Right -> Page scrolls Left/Right.
   - The further you move from the anchor, the faster it scrolls.
4. **Stop**: Click the Middle Mouse Button again (or Left/Right click) to exit.

### Customization

Want to personalize your anchor?
1. Prepare a transparent PNG image.
2. Rename it to `anchor.png`.
3. Place it in the `Assets` folder in the software's root directory.
4. Restart FlowWheel to see your custom icon!

### Build from Source

This project is built with .NET 10 (Windows).

1. Clone the repository:
   ```bash
   git clone https://github.com/humanfirework/FlowWheel.git
   ```
2. Open the solution in Visual Studio or VS Code.
3. Build and run.

I think it's good. Can you add a chicken leg for me?

---

<a name="中文"></a>
## 中文

**FlowWheel** 是一个轻量级的 Windows 全局自动滚动工具，旨在将浏览器的“中键无极滚屏”体验带到操作系统的每一个角落。

无论是阅读长文档、浏览代码，还是在不支持自动滚动的应用中漫游，FlowWheel 都能提供丝滑的滚动体验。

###  核心功能

- ** 全局通用**：突破软件限制，在资源管理器、Word、IDE、Discord 等几乎所有 Windows 应用中生效。
- ** 动态变速**：基于鼠标距离的非线性速度控制——离锚点越远，滚动越快，精准把控阅读节奏。
- ** 全向滚动**：不仅支持垂直滚动，还完美支持水平滚动（需应用本身支持），宽表格/画板浏览更轻松。
- ** 视觉反馈**：提供现代化的 UI 覆盖层，实时显示滚动锚点与方向指示，交互直观清晰。
- ** 高度客制化**：
  - 支持自定义中心锚点图标（只需放入 `anchor.png`）。
  - 支持中英文界面切换。
  - 托盘化运行，极低资源占用。

### 使用指南

1. **启动软件**：运行 `FlowWheel.exe`，系统托盘区会出现一个小图标。
2. **激活滚动**：在屏幕任意位置按下 **鼠标中键**。
3. **开始浏览**：
   - 屏幕出现锚点图标。
   - 鼠标向上/下移动 -> 页面向上/下滚动。
   - 鼠标向左/右移动 -> 页面向左/右滚动。
   - 距离锚点越远，滚动速度越快。
4. **停止滚动**：再次点击鼠标中键（或点击左键/右键）即可退出。

###  自定义图标

想要个性化你的滚动锚点？
1. 准备一张背景透明的 PNG 图片。
2. 重命名为 `anchor.png`。
3. 将其放入软件根目录下的 `Assets` 文件夹中。
4. 重启 FlowWheel，即可看到你的专属图标！

###  开发构建

本项目基于 .NET 10 (Windows) 开发。

1. 克隆仓库：
   ```bash
   git clone https://github.com/humanfirework/FlowWheel.git
   ```
2. 使用 Visual Studio 或 VS Code 打开解决方案。
3. 编译运行即可。

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
本项目采用 [MIT License](LICENSE) 开源。

---

## Buy me a coffee / 加个鸡腿

If you find this project helpful, feel free to buy me a coffee! ☕

如果觉得这个项目不错，欢迎请我喝杯咖啡或加个鸡腿！🍗


<div align="center">
  <img src="Assets/alipay_qr.png" alt="Alipay" width="180" style="max-width: 100%; height: auto;" />
  <br>
  <span>(扫描二维码支持我 / Click or Scan to Donate)</span>
</div>



