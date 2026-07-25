# Tasks

- [x] Task 1: 修复设置窗口初始化覆盖配置的 bug(对应 GitHub Issue #16 及同类问题)
  - [x] SubTask 1.1: 在 `UI/SettingsWindow.xaml.cs` 构造函数中引入 `_isInitializing` 标志:`InitializeComponent()` 前置为 true,`InitializeValues()` 完成后置为 false
  - [x] SubTask 1.2: 审计所有在 XAML 中订阅的 `ValueChanged`/`IsOnChanged` 等会写 `ConfigManager` 的处理器,在入口处加 `if (_isInitializing) return;` 保护(重点:MiddleClickDelaySlider、FrictionSlider、ReadingSpeedSlider、曲线参数滑块、各 ToggleSwitch)
  - [x] SubTask 1.3: 审计 `InitializeValues()`,确认保护生效后所有 UI 状态(面板显隐、状态文本、数值文本)均由初始化代码显式设置,不依赖处理器副作用;缺失的补齐
  - [x] SubTask 1.4: 静态验证:通读 diff,确认初始化期间无任何路径写配置或触发 `DebouncedSave()`,初始化后用户操作路径行为不变

- [x] Task 2: UI 保守视觉打磨(不改布局结构与交互)
  - [x] SubTask 2.1: 统一 `Themes/Generic.xaml` 中卡片圆角、阴影、内边距的取值,消除相近但不一致的数值
  - [x] SubTask 2.2: 梳理 `UI/SettingsWindow.xaml` 的 Margin/Padding 节奏与字体层级(分区标题/标签/说明三级),统一间距刻度
  - [x] SubTask 2.3: 校验 `Themes/Light.xaml` 与 `Themes/Dark.xaml` 配色:文本对比度、Accent 一致性,修正违和颜色
  - [x] SubTask 2.4: 复查所有视觉改动,确认未移动/删除任何控件、未改动任何事件订阅与名称

- [x] Task 3: `SettingsWindow.xaml.cs` 内滑块处理器样板代码整理
  - [x] SubTask 3.1: 将"初始化保护 + 写配置 + 更新数值文本 + 防抖保存"的重复模式提取为私有辅助方法
  - [x] SubTask 3.2: 逐个处理器迁移到辅助方法,核对每个处理器的取值/格式化/目标配置字段与原逻辑完全一致

- [x] Task 4: 整体验证
  - [x] SubTask 4.1: 用 XML 解析器校验所有改动的 `.xaml` 文件良构性
  - [x] SubTask 4.2: 全量 diff 审查:确认除 bug 修复与视觉 token 外无行为变化,`Core/` 未被改动
  - [x] SubTask 4.3: 对照 GitHub Issue #16 复现步骤做代码级走查,确认修复闭环

# Task Dependencies
- [Task 3] depends on [Task 1](保护标志需在处理器整理前先就位)
- [Task 2] 与 [Task 1]/[Task 3] 无依赖,可并行
- [Task 4] depends on [Task 1], [Task 2], [Task 3]
