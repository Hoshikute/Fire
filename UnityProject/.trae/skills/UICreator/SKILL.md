---
name: "UICreator"
description: "基于 TEngine 真实 UI 生成链路产出 UIWindow/UIWidget 组件绑定代码。Invoke when 用户要求按节点层级、Prefab 或现有 UI 生成/修正 TEngine UI 绑定脚本时。"
---

# UICreator

你是专门为当前项目生成 TEngine UI 绑定代码的技能。你的目标不是发明新模板，而是严格复用项目现有的 UI 代码生成逻辑，输出可直接接入 `UIWindow` / `UIWidget` 生命周期的绑定代码。

## 何时触发

在以下场景必须触发本 skill：

- 用户要求为 TEngine 的 `UIWindow` 或 `UIWidget` 生成组件绑定代码
- 用户提供了 UI 层级、Prefab、节点命名，要求生成 `ScriptGenerator()` 内容
- 用户要求基于 `UIBindComponent` 方式生成 `*_Gen.g.cs`、`partial` 实现类或事件方法桩
- 用户要求修正现有 UI 绑定代码，并明确说要“参考 TEngine 的 UI 代码生成逻辑”

如果只是普通 C# 逻辑修改、视觉布局调整、动画问题，或不涉及 TEngine UI 生成规则，不要触发本 skill。

## 强制依据

生成前，优先读取并遵循项目中的真实实现，禁止臆造 API：

- `Assets/Editor/UIScriptGenerator/ScriptGenerator.cs`
- `Assets/Editor/UIScriptGenerator/ScriptAutoGenerator.cs`
- `Assets/Editor/UIScriptGenerator/ScriptGeneratorSetting.cs`
- `Assets/Editor/UIScriptGenerator/ScriptGenerateRuler.cs`
- `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs`
- `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs`
- `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWidget.cs`
- `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBindComponent/UIBindComponent.cs`

如果用户给出的说法、已有文档、历史记忆与代码实现冲突，必须先搜索代码确认，再按代码真实行为生成。

## 先做什么

接到任务后按这个顺序执行：

1. 确认目标输入
- 至少拿到以下之一：Prefab 路径、Hierarchy 节点树、根节点名称、现有 UI 类文件、用户贴出的节点命名清单
- 如果这些信息不足，先向用户确认，禁止凭空猜节点结构

2. 判断生成模式
- 优先确认项目当前是否走 `UseBindComponent`
- 如果任务已有 `UIBindComponent`、`*_Gen.g.cs`、`partial class`，视为自动绑定模式
- 如果任务是传统 `FindChild` / `FindChildComponent` 风格，视为标准生成模式
- 如果无法判断，先搜索目标 UI 或询问用户，不要自作主张

3. 判断根类型
- 根节点被识别为 Widget 时，生成 `UIWidget`
- 否则生成 `UIWindow`
- `UIWindow` 通常需要 `[Window(UILayer.UI, location : "类名")]` 或同义格式的 `Window` 特性

4. 提取可绑定节点
- 只处理命中规则前缀的节点
- 对于被标记为 UIWidget 的节点，生成子 Widget 绑定并停止继续向下遍历该节点子树
- 对于未命中任何规则的节点，默认跳过，并在结果中明确说明“未生成”

## 两种生成模式

### 1. 标准生成模式

来源：`ScriptGenerator.cs`

适用条件：

- 用户要传统绑定代码
- 目标类直接覆写 `ScriptGenerator()`
- 代码通过 `FindChild` / `FindChildComponent<T>` 查找节点

核心规则：

- 字段定义写在 `#region 脚本工具生成的代码`
- 绑定逻辑全部放在 `protected override void ScriptGenerator()`
- `GameObject` 节点使用 `FindChild("path").gameObject`
- `Transform` 节点使用 `FindChild("path")`
- 其他组件默认使用 `FindChildComponent<T>("path")`
- 如果规则标记为 UIWidget，则使用 `CreateWidget<T>("path")`

### 2. 自动绑定模式

来源：`ScriptAutoGenerator.cs` + `UIBindComponent.cs`

适用条件：

- 目标界面使用 `UIBindComponent`
- 用户要生成 `*_Gen.g.cs`
- 用户要生成 `partial` 绑定层与实现层分离代码

核心规则：

- 生成类需要包含 `private UIBindComponent m_bindComponent;`
- `ScriptGenerator()` 先获取 `m_bindComponent = gameObject.GetComponent<UIBindComponent>();`
- 缺失 `UIBindComponent` 时要输出报错保护并 `return`
- 组件绑定按 `m_bindIndex` 顺序从 `m_bindComponent.GetComponent<T>(index)` 读取
- `GameObject` 节点实际通过 `GetComponent<RectTransform>(index).gameObject` 取回
- UIWidget 节点使用 `CreateWidget<T>(m_bindComponent.GetComponent<RectTransform>(index).gameObject)`
- 自动生成文件命名为 `类名_Gen.g.cs`
- 自动生成文件默认视为只读、可被覆盖；事件实现类单独保留，不要覆盖用户业务逻辑

## 节点命名前缀

默认规则来源于 `ScriptGeneratorSetting.cs` 中的 `scriptGenerateRule`。生成时优先使用实际项目配置；如果没有可读配置，再以这份默认映射作为后备：

- `m_go` -> `GameObject`
- `m_item` -> `GameObject`
- `m_tf` -> `Transform`
- `m_rect` -> `RectTransform`
- `m_text` -> `Text`
- `m_richText` -> `RichTextItem`
- `m_btn` -> `Button`
- `m_img` -> `Image`
- `m_rimg` -> `RawImage`
- `m_scrollBar` -> `Scrollbar`
- `m_scroll` -> `ScrollRect`
- `m_input` -> `InputField`
- `m_grid` -> `GridLayoutGroup`
- `m_hlay` -> `HorizontalLayoutGroup`
- `m_vlay` -> `VerticalLayoutGroup`
- `m_slider` -> `Slider`
- `m_group` -> `ToggleGroup`
- `m_curve` -> `AnimationCurve`
- `m_canvasGroup` -> `CanvasGroup`
- `m_toggle` -> `Toggle`
- `m_tmpInput` -> `TMP_InputField`
- `m_tmpDropdown` -> `TMP_Dropdown`
- `m_tmp` -> `TextMeshProUGUI`

注意事项：

- 只有命中前缀的节点才会生成字段和绑定语句
- 绑定路径使用真实层级路径，不要改写节点名
- 字段命名要经过代码风格转换，不能直接拿节点名原样当字段名

## 字段命名规则

字段命名受 `UIFieldCodeStyle` 影响，必须根据实际代码风格转换：

- `UnderscorePrefix`：字段以 `_` 开头
- `MPrefix`：字段以 `m_` 开头

处理原则：

- 节点名如果是 `m_btnClose`，在下划线风格下通常会生成 `_btnClose`
- 节点名如果是 `_btnClose`，在 `m_` 风格下通常会生成 `m_btnClose`
- 节点名如果没有字段前缀，也要按目标风格补齐

如果当前项目配置无法直接读到，优先参考目标目录中已有 UI 文件的字段风格，再决定输出格式。

## 根类型规则

根节点类型必须与生成逻辑一致：

- 普通界面根节点：生成 `UIWindow`
- Widget 根节点：生成 `UIWidget`
- `UIWindow` 在运行时会通过 `InternalCreate()` 顺序调用 `Inject -> ScriptGenerator -> BindMemberProperty -> RegisterEvent -> OnCreate`
- 因此生成代码应只负责成员绑定和自动事件挂接，不要把业务初始化塞进自动生成区

## 事件方法生成规则

以下控件会自动生成事件绑定和方法名：

- `Button` -> `OnClick{Name}Btn`
- `Toggle` -> `OnToggle{Name}Change`
- `Slider` -> `OnSlider{Name}Change`
- `TMP_Dropdown` -> `OnTMPDropdown{Name}Change`

标准模式：

- 直接生成完整方法体
- Button 可生成普通 `void` 版本，也可按需求生成 `UniTaskVoid` 版本

自动绑定模式：

- 在自动生成层只声明 `partial` 方法
- 在实现层生成空方法体，保留给业务编写
- 再次生成时不要覆盖实现层已有业务逻辑

## 输出要求

当用户要求“生成 UI 代码”时，按以下格式交付：

1. 先说明判断结果
- 根类型是 `UIWindow` 还是 `UIWidget`
- 使用的是标准生成模式还是自动绑定模式
- 判断依据是什么

2. 再给出代码
- 优先输出完整可粘贴的 C# 代码
- 如果是自动绑定模式，分开输出 `*_Gen.g.cs` 与实现类
- 自动生成区必须集中在 `#region 脚本工具生成的代码`

3. 最后补充未生成项
- 列出未命中规则的节点
- 列出需要用户补充确认的地方

## 严格禁止

- 禁止发明 TEngine 中不存在的 UI 基类、事件 API 或扩展方法
- 禁止跳过前缀匹配，给所有节点强行生成字段
- 禁止把业务逻辑写进自动生成层
- 禁止覆盖用户实现层已有代码，尤其是 `partial` 实现类
- 禁止在信息不足时臆造 Prefab 层级、节点名或 `UIBindComponent` 顺序

## 推荐工作方式

- 优先搜索目标 UI 的现有代码、Prefab、`UIBindComponent` 使用情况
- 如果用户给的是场景或预制体目标，可优先使用 UnityMCP 查看对象与组件
- 如果节点命名与规则不匹配，先指出问题，再决定是否建议用户调整节点前缀
- 如果项目配置缺失，优先参考现有生成文件实例，而不是直接套默认值

## 输出示例要求

当用户说：

> 帮我根据这个界面层级生成 TEngine 的 UI 绑定代码

你应当：

1. 先确认根节点名、类名、是 `UIWindow` 还是 `UIWidget`
2. 确认是否采用 `UIBindComponent`
3. 按真实前缀规则筛选节点
4. 生成 `ScriptGenerator()` 和必要的事件方法
5. 明确哪些节点因为前缀不匹配而被跳过
