# TestWindow 迁移计划

## Summary

- 目标：把源项目中的 `TestWindow.prefab` 迁移到当前 TEngine 项目，使用 `UICreator` 对应的 `UIBindComponent` 自动绑定链路重构为 `UIWindow`，并在 `Game` 场景进入后通过 `GameModule.UI` 自动加载显示。
- 约束：当前处于规划阶段，不能执行复制与改文件；源资源已确认在 `A:\animator-third-person-controller\Assets\Resources\UIWindow\TestWindow.prefab` 与 `A:\animator-third-person-controller\Assets\Scripts\GameWindow\TestWindow.cs`，执行阶段将以这两者为唯一迁移来源。
- 接入原则：遵循 TEngine 热更边界、YooAsset 资源寻址、`UIBindComponent` 生成规则与 `ShowUIAsync<T>()` 打开方式，不使用 `Resources.Load`，不手写非项目现有的 UI 框架接口。

## Current State Analysis

### 当前项目

- 热更 UI 业务代码位于 `Assets/GameScripts/HotFix/GameLogic/UI/`，启动入口位于 `Assets/GameScripts/HotFix/GameLogic/GameApp.cs`，当前 `StartGameLogic()` 会直接执行 `GameModule.UI.ShowUIAsync<BattleMainUI>()`。
- `UIModule` 通过 `[Window(..., location:"窗口名")]` + `LoadGameObjectAsync(location, parent: UIModule.UIRoot)` 加载窗口，说明新窗口必须是可被 YooAsset 通过文件名定位的 Prefab。
- 资源打包配置 `Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset` 已确认收集 `Assets/AssetRaw/UI`，寻址规则为 `AddressByFileName`，打包规则为 `PackSeparately`。因此迁移后的窗口 Prefab 最合适落点是 `Assets/AssetRaw/UI/TestWindow.prefab`。
- UI 脚本生成配置 `Assets/Editor/UIScriptGenerator/ScriptGeneratorSetting.asset` 已启用 `useBindComponent: 1`，对应实现为 `Assets/Editor/UIScriptGenerator/ScriptAutoGenerator.cs` / `ScriptGenerator.cs` 的自动绑定链路，生成代码输出到：
  - 生成层：`Assets/GameScripts/HotFix/GameLogic/UI/Gen`
  - 实现层：`Assets/GameScripts/HotFix/GameLogic/UI`
- 现有窗口示例 `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs` 使用标准 `UIWindow` + `[Window]` 模式，但项目真实配置已启用 `UIBindComponent`，因此本次重构应优先采用 `UICreator` 对应的自动绑定分层，而不是继续手写 `FindChild` 绑定。

### 源项目

- 真实源文件已明确：
  - `A:\animator-third-person-controller\Assets\Resources\UIWindow\TestWindow.prefab`
  - `A:\animator-third-person-controller\Assets\Scripts\GameWindow\TestWindow.cs`
- `TestWindow.prefab` 根对象名为 `TestWindow`，包含 `Canvas`、`CanvasScaler`、`GraphicRaycaster`，并含 `TimeScale`、`TargetAngle`、`CurrentState`、`CurrentSpeed`、`LockState` 等节点，文本/滑条节点命名仍为旧风格（如 `[Slider]TimeSlider`、`[TextMeshProUGUI]TimeTip`），不符合当前项目 `UICreator` 的 `m_` 前缀规则。
- `TestWindow.cs` 继承 `WindowBase` 并依赖 `TestWindowComp`、`CursorManager`、`roleSystem`、`managerService` 等旧框架类型；这些类型在当前 TEngine 热更 UI 体系中不存在，不能直接复用，需迁移为 `UIWindow` 生命周期与当前项目可用服务访问方式。
- 当前项目已存在 `Player` 与 `ReusableData` 相关数据结构（`targetAngle`、`currentState`、`speedValueParameter`、`lockTarget`），可作为“功能兼容”迁移时的数据来源候选。

## Proposed Changes

### 1. 源资源校验与迁移

- 目标文件：
  - 源 Prefab：`A:\animator-third-person-controller\Assets\Resources\UIWindow\TestWindow.prefab`
  - 源脚本：`A:\animator-third-person-controller\Assets\Scripts\GameWindow\TestWindow.cs`
  - 目标 Prefab：`Assets/AssetRaw/UI/TestWindow.prefab`
- 变更内容：
  - 执行阶段直接从 `A:` 盘指定路径复制 `TestWindow.prefab` 及其 `.meta` 到 `Assets/AssetRaw/UI/`。
  - 同时对 Prefab 依赖进行检查，按最小集补齐缺失资源（如字体、材质、贴图）。
- 原因：
  - 用户已给出准确源路径，执行阶段可以直接按路径迁移，避免在源仓库继续盲搜。
  - 目标目录 `Assets/AssetRaw/UI` 已被当前项目的 YooAsset 配置实际收集，符合窗口资源寻址规则。
- 实施方法：
  - 校验复制后的 Prefab 根名、文件名与 `location` 三者一致（均为 `TestWindow`）。

### 2. 用 `UICreator` 方式重构窗口绑定

- 目标文件：
  - `Assets/GameScripts/HotFix/GameLogic/UI/Gen/TestWindow_Gen.g.cs`
  - `Assets/GameScripts/HotFix/GameLogic/UI/TestWindow/TestWindow.cs`
- 变更内容：
  - 在 Prefab 根节点上补齐 `UIBindComponent`，并将旧命名节点按规则重命名为可生成前缀（如 `m_sliderTime`、`m_tmpTimeTip` 等）。
  - 使用 `UICreator` 所依据的自动绑定模式，生成 `TestWindow_Gen.g.cs`。
  - 创建或补齐 `TestWindow.cs` 作为业务实现层，保留 `[Window(UILayer.UI, location:"TestWindow")]` 声明与窗口逻辑。
- 原因：
  - 当前项目 `useBindComponent = 1`，必须走自动绑定生成链路，避免与真实工程生成规范脱节。
  - 用户明确要求“使用 UICreator skill 重构 TestWindow”。
- 实施方法：
  - 执行阶段读取真实 Prefab 节点树，按 `m_btn` / `m_toggle` / `m_slider` / `m_tmp` / `m_text` / `m_img` 等规则规范化节点命名并重新绑定。
  - 节点名规范化完成后再生成 `TestWindow_Gen.g.cs`，尽量避免在实现层手写路径绑定。
  - `TestWindow_Gen.g.cs` 只放自动生成内容，不承载业务逻辑。
  - `TestWindow.cs` 负责：
    - `partial class TestWindow`
    - `RegisterEvent()` 内注册按钮/滑条/下拉等 UI 事件
    - `OnCreate()` 做一次性初始化
    - `OnRefresh()` 做显示刷新
    - `OnDestroy()` 中释放非 `GameObject` 型动态资源（如有）

### 3. 兼容原窗口功能

- 目标文件：
  - `Assets/GameScripts/HotFix/GameLogic/UI/TestWindow/TestWindow.cs`
  - `Assets/GameScripts/HotFix/GameLogic/UI/TestWindow/` 下的必要辅助类（仅在迁移 `TestWindow.cs` 逻辑时确有需要）
- 变更内容：
  - 执行阶段基于 `A:` 盘原始 `TestWindow.cs` 逐项迁移：
    - 文本显示刷新逻辑
    - 滑条/按钮/Toggle/Dropdown 交互
    - 任何原始测试面板上的实时显示或切换功能
  - 对原脚本 `OnAwake/OnShow/OnHide` 逻辑映射到 `OnCreate/OnRefresh/OnDestroy` 与 UI 事件回调。
- 原因：
  - 用户要求“兼容它的功能到本项目中”，且要求尽量完整。
- 实施方法：
  - 先建立 `windowComp.*` 字段到新 `UIWindow` 字段的一一映射表。
  - 重点迁移原功能：
    - `OnTimeSliderChanged` 调整 `Time.timeScale` 并刷新时间文本
    - `targetAngle/currentState/speed/lockTarget` 的显示文本刷新
    - `Alt` 键触发的光标显示/隐藏交互（改用当前项目可用输入服务与光标控制方式）
  - 保留功能语义，不照搬 MonoBehaviour 的 `Start` / `Update` 写法；能转为 `OnRefresh()` 或事件驱动的优先转事件驱动。
  - 若存在必须每帧刷新的显示项，仅在确认必要后才覆写 `OnUpdate()`；否则优先考虑 `GameModule.Timer` 或显式事件刷新。
  - 严禁使用 `Resources.Load`；若需要加载额外资源，一律使用 `GameModule.Resource` 对应 API。

### 4. 在 `Game` 场景启动时加载 `TestWindow`

- 目标文件：
  - `Assets/GameScripts/HotFix/GameLogic/GameApp.cs`
- 变更内容：
  - 调整 `StartGameLogic()`，在符合当前演示目标的前提下通过 `GameModule.UI.ShowUIAsync<TestWindow>()` 打开测试窗口。
  - 是否保留 `BattleMainUI` 取决于实际窗口定位：
    - 若 `TestWindow` 是独立演示入口，则替换当前的 `BattleMainUI` 打开语句。
    - 若 `TestWindow` 只是附加调试窗口，则在 `BattleMainUI` 之后额外打开。
- 原因：
  - 当前项目真正的游戏 UI 默认加载点就是 `GameApp.StartGameLogic()`，最符合 TEngine 热更入口规范。
- 实施方法：
  - 执行前先查看 `TestWindow` 是否全屏、是否覆盖主界面、是否需要和 `BattleMainUI` 同时存在。
  - 未得到相反新指令时，默认按“附加调试窗口”处理：保留 `BattleMainUI`，追加打开 `TestWindow`，避免破坏当前主界面演示。

### 5. 场景与运行验证

- 目标文件：
  - `Assets/AssetRaw/Scenes/Game.unity`（仅当运行后发现缺少 `UIRoot` 或其他接入必要对象时才调整；目前规划不预设修改）
- 变更内容：
  - 优先不修改 `Game.unity`，因为 `UIModule` 已通过场景中的 `UIRoot` 完成统一挂载。
  - 执行阶段主要验证 `Game` 场景运行时是否能自动打开 `TestWindow`、节点绑定是否正确、资源是否能通过 `location = TestWindow` 找到。
- 原因：
  - 当前场景加载链和 `UIRoot` 初始化链已存在，不需要为了新增普通窗口重构场景。
- 实施方法：
  - 进入 Play Mode 后验证：
    - `Game` 场景加载成功
    - `TestWindow` 成功实例化到 `UIModule.UIRoot`
    - 交互控件可响应
    - 没有资源定位错误与空引用

## Assumptions & Decisions

- 决策：本次实现目标名固定为 `TestWindow`，目标 Prefab 文件名和 `[Window(location)]` 也统一使用 `TestWindow`。
- 决策：遵循当前项目真实配置，优先使用 `UIBindComponent` 自动绑定模式，不回退到手写 `FindChild` 模式，除非真实节点命名严重不符合规则且无法调整。
- 决策：UI 资源最终放在 `Assets/AssetRaw/UI/`，以文件名作为地址，通过 `GameModule.UI.ShowUIAsync<TestWindow>()` 打开。
- 决策：窗口业务代码放在热更层 `Assets/GameScripts/HotFix/GameLogic/UI/TestWindow/`。
- 决策：执行阶段以 `A:\animator-third-person-controller\Assets\Resources\UIWindow\TestWindow.prefab` 与 `A:\animator-third-person-controller\Assets\Scripts\GameWindow\TestWindow.cs` 为固定源输入，不再以 `D:` 盘目录作为来源。
- 假设：若源 Prefab 依赖的材质、贴图、字体、脚本不在当前项目中，执行阶段允许一并迁移最小依赖集。
- 决策：若复制进来的子节点不符合 `UICreator` 前缀规则，执行阶段优先重命名节点并重建 `UIBindComponent` 索引，不改生成器全局规则。

## Verification Steps

1. 从 `A:\animator-third-person-controller\Assets\Resources\UIWindow\TestWindow.prefab` 复制到 `Assets/AssetRaw/UI/TestWindow.prefab`，同步 `.meta`，并校验源脚本 `A:\animator-third-person-controller\Assets\Scripts\GameWindow\TestWindow.cs` 可读。
2. 检查迁移后 Prefab 的依赖是否完整、根节点命名是否保持 `TestWindow`。
3. 通过 `UICreator` / `UIBindComponent` 链路生成或修正：
   - `Assets/GameScripts/HotFix/GameLogic/UI/Gen/TestWindow_Gen.g.cs`
   - `Assets/GameScripts/HotFix/GameLogic/UI/TestWindow/TestWindow.cs`
4. 修改 `Assets/GameScripts/HotFix/GameLogic/GameApp.cs`，让 `Game` 场景启动后通过 `GameModule.UI.ShowUIAsync<TestWindow>()` 加载窗口。
5. 进入 `Game` 场景运行验证：
   - 窗口资源能被定位并实例化
   - 自动绑定字段无空引用
   - 原窗口核心功能与交互可用
   - 控制台无新的脚本报错
6. 对本次改动的热更 UI 脚本执行诊断检查，确保没有新增明显编译/分析错误。
