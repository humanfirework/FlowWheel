# Checklist

- [ ] `SettingsWindow` 构造期间(`InitializeComponent()` + `InitializeValues()`),不存在任何写 `ConfigManager` 或触发 `DebouncedSave()` 的代码路径
- [ ] 按 Issue #16 复现步骤走查:修改延迟启动值 → 退出 → 重开,代码路径上滑块显示 config.json 中的值而非 150
- [ ] 所有 XAML 订阅的 ValueChanged/IsOnChanged 处理器均有 `_isInitializing` 保护或等效保护
- [ ] `InitializeValues()` 显式设置全部 UI 状态(面板显隐、状态文本、数值文本),不依赖处理器副作用
- [ ] 初始化完成后,用户拖动滑块/切换开关,配置正常更新并保存,数值文本同步刷新
- [ ] UI 改动仅限视觉属性(颜色/间距/圆角/阴影/字号),无控件增删、无 `x:Name` 变更、无事件订阅变更
- [ ] 深色与浅色主题下设置页文本对比度充足、配色一致
- [ ] 处理器样板整理后,每个处理器的目标配置字段、取值转换、文本格式化与原逻辑逐一对应
- [ ] `Core/` 目录、`App.xaml.cs` 零改动
- [ ] 所有改动的 XAML 文件通过 XML 良构性校验
