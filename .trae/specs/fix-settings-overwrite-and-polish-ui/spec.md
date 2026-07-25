# 修复设置值初始化覆盖 Bug 及 UI 视觉打磨 Spec

## Why
GitHub Issue #16 报告"延迟启动"的值在重启应用后被重置为默认值 150。根因是 `SettingsWindow` 构造函数中 `InitializeComponent()` 解析 XAML 时会触发各 Slider/Toggle 的 `ValueChanged`/`IsOnChanged` 处理器,把 XAML 设计默认值写入 `ConfigManager` 并 `DebouncedSave()`,早于 `InitializeValues()` 读取真实配置——这是一个影响多个设置项的 bug 类别,不止延迟启动。同时用户反馈 UI 界面不够美观,希望在零风险前提下做视觉打磨与代码整理。

## What Changes
- **Bug 修复(Issue #16 及同类)**:在 `SettingsWindow` 引入 `_isInitializing` 初始化保护标志;所有在 XAML 中订阅、且会写 `ConfigManager` 的事件处理器,在初始化期间直接返回,不再用 XAML 默认值覆盖用户配置。
- **初始化路径审计**:确保 `_isInitializing` 保护生效后,`InitializeValues()` 显式完成所有必要的 UI 状态设置(面板显隐、状态文本、数值文本等),不依赖事件处理器的副作用。
- **UI 保守视觉打磨**:不改布局结构与交互,只调整视觉层面——间距节奏、字体层级、圆角与阴影一致性、深浅主题配色一致性。
- **代码顺带整理**:仅在本次修改触及的 `SettingsWindow.xaml.cs` 内,统一滑块处理器的样板模式(保护判断 + 写配置 + 更新数值文本 + 防抖保存),不改公共行为与架构。
- 无 **BREAKING** 变更;除上述 bug 修复外,所有运行时行为保持不变。

## Impact
- Affected specs: 设置窗口(显示与持久化)、主题样式
- Affected code:
  - `UI/SettingsWindow.xaml.cs`(初始化保护、处理器整理)
  - `UI/SettingsWindow.xaml`(仅间距/字体等视觉属性微调)
  - `Themes/Generic.xaml`、`Themes/Light.xaml`、`Themes/Dark.xaml`(视觉 token 统一)
- 明确不动:`Core/` 下所有滚动引擎与钩子逻辑、`App.xaml.cs`、Overlay/Update/Splash 窗口的交互行为。

## ADDED Requirements
### Requirement: 设置值初始化保护
系统 SHALL 保证设置窗口打开(构造)过程中,任何控件的事件处理器都不得将 XAML 设计时默认值写入 `ConfigManager` 或触发配置保存;窗口完成初始化后,各控件显示的数值 SHALL 与 `config.json` 中持久化的值一致。

#### Scenario: 延迟启动值重启后保持(对应 Issue #16)
- **GIVEN** 用户已将延迟启动设置为非默认值(如 200ms)并正常退出应用
- **WHEN** 用户再次启动应用并打开设置窗口
- **THEN** 延迟启动滑块与数值文本显示 200ms,且 `config.json` 中的值不被改写

#### Scenario: 其他设置项同类保护
- **GIVEN** 用户修改过任意滑块设置(摩擦、惯量、阅读速度、曲线参数等)并退出
- **WHEN** 再次打开设置窗口
- **THEN** 所有设置项显示用户保存的值,无一项被重置为 XAML 默认值

#### Scenario: 修改设置仍正常保存
- **WHEN** 初始化完成后用户拖动任意滑块或切换开关
- **THEN** 对应配置值正常更新并防抖保存到 `config.json`,数值文本同步刷新

## MODIFIED Requirements
### Requirement: 设置窗口视觉呈现
设置窗口 SHALL 在不改变现有布局结构与交互方式的前提下,呈现统一的视觉规范:一致的卡片间距节奏、清晰的三级字体层级(分区标题/设置项标签/辅助说明)、统一的圆角与阴影,且深色与浅色主题下同一界面的对比度与观感一致、无错位或违和配色。

#### Scenario: 深浅主题一致性
- **WHEN** 用户在深色与浅色主题间切换并浏览设置窗口各分区
- **THEN** 文本可读、对比度充足,控件无明显视觉异常

## REMOVED Requirements
无。
