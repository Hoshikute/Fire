## 背景

当前 FrameSync 玩家启动链路由 `TPBattleContext` 加载 Player prefab，然后通过资源地址 `PlayerAnimConfig` 加载 `PlayerAnimConfig`，再注入到 `PlayerViewComponent.animConfig`。`PlayerAnimViewSystem` 依赖这个非空配置，才会播放首帧 Idle 动画和后续状态切换动画。

这次运行时问题非常集中：Player prefab 已加载、PlayerWorld 已启动、`PlayerStateSystem` 也能输出状态切换日志，但 `TPBattleContext` 报出 `PlayerAnimConfig missing. Expected resource location: PlayerAnimConfig.`。因为 `PlayerAnimViewSystem` 会跳过 `animConfig == null` 的实体，Animancer 没收到任何 Play 调用，角色就停在 T-Pose。

仓库中已经存在地面移动相关的 Animancer `TransitionAsset`，位置在 `Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/NoneLock/`。缺失的是新的聚合配置资产 `PlayerAnimConfig.asset`，以及它能通过 TEngine ResourceModule/YooAsset 默认资源包被加载的验证。

## TEngine 规范对齐

本设计按 TEngine RepoWiki 的资源管理规范收敛：

- `ResourceModule` 是项目统一资源入口，`GameModule.Resource.CheckLocationValid` 与 `GameModule.Resource.LoadAssetAsync<T>` 是运行时验证和加载 `PlayerAnimConfig` 的正确边界。
- TEngine 的 YooAsset 集成以包名和 location 共同定位资源；本修复使用默认包，不显式传包名，避免和现有 Game 场景启动链路分叉。
- 资源收集器使用“包-分组-收集器-规则”结构。当前 `DefaultPackage` 的 `Configs` 分组收集 `Assets/AssetRaw/Configs`，规则为 `AddressByFileName`、`PackDirectory`、`CollectAll`，所以文件名 `PlayerAnimConfig.asset` 会生成无后缀 location `PlayerAnimConfig`。
- TEngine 资源加载支持缓存、句柄与对象池生命周期；本变更只补资源与诊断，不在 FrameSync 逻辑组件中保存 UnityEngine 资源引用。
- 资源故障排查应先确认定位地址、包名和 collector 规则，再看加载返回值；因此验证任务必须覆盖 collector 配置和 EditorSimulateMode 下的 `CheckLocationValid`。

## 目标 / 非目标

**目标：**

- 提供真实的 `PlayerAnimConfig` 资源资产，并能通过 `GameModule.Resource` 以地址 `PlayerAnimConfig` 加载。
- 明确该资产受 `DefaultPackage` 的 `Assets/AssetRaw/Configs` collector 管理，符合 TEngine/YooAsset 的资源定位规则。
- 填入基础地面动画映射，让 Game 场景进入后能播放 Idle 和移动状态切换动画，而不是停在 T-Pose。
- 保持动画为纯表现层：`PlayerAnimViewSystem` 只读逻辑状态并驱动 Animancer，不修改 FrameSync 逻辑组件。
- 增加诊断信息，让缺失配置或缺失基础动画字段能从 Console 日志中快速定位。
- 在 Game 场景中验证：原来的 `PlayerAnimConfig missing` 错误消失，角色可见地脱离 T-Pose。

**非目标：**

- 不绕过 TEngine `ResourceModule` 增加 `Resources.Load`、AssetDatabase 运行时依赖或硬编码绝对文件路径。
- 不新增新的资源包或 collector 分组，除非验证发现现有 `Configs` collector 已经漂移且无法覆盖该资产。
- 不重新引入 `ThirdPersonController` 逻辑或 Root Motion 位移。
- 不恢复已经删除的 `PlayerFrameSyncEntry` 序列化注入路径。
- 不重写 `PlayerStateSystem`、`PlayerMoveSystem` 或确定性移动规则。
- 不要求在恢复基础 Game 场景动画前一次性迁移完所有跳跃、下落、翻越、攀爬、平台跳动画。

## 决策

### D1：将 `PlayerAnimConfig.asset` 放在 `Assets/AssetRaw/Configs`

使用 `Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 作为聚合 ScriptableObject 的位置。`DefaultPackage` 当前已经收集 `Assets/AssetRaw/Configs`，地址规则是 `AddressByFileName`，打包规则是 `PackDirectory`，过滤规则是 `CollectAll`，因此生成的 YooAsset location 正好是 `PlayerAnimConfig`，与 `TPBattleContext.PLAYER_ANIM_CONFIG_PATH` 一致。

备选方案：把资产放在现有 Animancer 过渡资源旁边。这样资源组织更集中，但当前 collector 设置没有把那棵目录纳入 DefaultPackage；除非同步修改 collector，否则 `CheckLocationValid("PlayerAnimConfig")` 仍会失败。

### D2：复用 TEngine ResourceModule 加载链路，不新增旁路加载

`TPBattleContext.LoadPlayerAnimConfigAsync()` 继续通过 `GameModule.Resource.CheckLocationValid(PLAYER_ANIM_CONFIG_PATH)` 和 `GameModule.Resource.LoadAssetAsync<PlayerAnimConfig>(PLAYER_ANIM_CONFIG_PATH)` 加载。这样可复用 TEngine 的 YooAsset 包初始化、资源定位、缓存、句柄和对象池生命周期。

备选方案：直接在 prefab 或场景中序列化拖拽 `PlayerAnimConfig`。这能绕过 location 失败，但会回到旧注入模式，破坏当前动态加载与资源包契约，也不能覆盖线上包清单缺资源的问题。

### D3：复用现有地面移动 `TransitionAsset`

基础字段按现有资源填入：

- `idle`：`NoneLock/IdleLoop.asset`
- `moveStart_*`：对应的 `NoneLock/MoveStart_*.asset`
- `moveLoop`：`NoneLock/MoveLoop.asset`
- `moveEnd_L` / `moveEnd_R`：对应的 `NoneLock/MoveEnd_*.asset`
- `moveToWall`：暂时兜底复用 `moveEnd_L`
- `lockIdle`：暂时兜底复用 `idle`

备选方案：先为每个 `PlayerLogicState` 都创建完整过渡资产再启用配置。这个方向更完整，但范围明显大于当前 T-Pose 修复，会拖慢基础 Game 场景动画恢复。

### D4：高级动画缺口按“不完整映射”处理，不阻塞启动

Jump、Fall、Vault、Climb、LedgeClimb、PlatformerUp 等高级状态如果暂时没有合适的 `TransitionAsset`，可以作为后续迁移项单独跟踪。基础配置必须能加载，并允许 Idle/移动动画播放。高级字段缺失时，应在进入对应状态时输出明确警告，而不是让整个动画系统不可用。

备选方案：只要 `PlayerAnimConfig` 任何字段为空就阻止 Game 场景启动。这样能避免隐藏资源债务，但当前代码已经把部分字段设计为可选/可兜底，而且这次用户可见故障是“整个配置为 null”，不是某个高级状态缺动画。

### D5：在资源边界做配置校验

校验应靠近 `TPBattleContext.LoadPlayerAnimConfigAsync()` 或 `PlayerAnimConfig` helper，因为这里最早知道资源地址和聚合映射是否可用。校验应在 `PlayerAnimViewSystem` 静默跳过之前报告缺失地址或缺失基础字段，并保留 TEngine 自带的 `Could not found location [...]` 语义。

备选方案：只在 `PlayerAnimViewSystem` 每帧发现 `animConfig == null` 时输出日志。这会很吵，而且定位太晚；资源边界能给出更清晰的一次性原因。

## 风险 / 权衡

- [风险] 手写 Unity YAML 可能引用错误脚本或错误 Transition GUID -> 缓解：优先通过 Unity Editor API 创建/更新 ScriptableObject，或在运行前核对 `.meta` GUID。
- [风险] collector 规则未来漂移导致文件存在但 location 失效 -> 缓解：任务中显式验证 `DefaultPackage/Configs` 的 `CollectPath`、`AddressByFileName`、`PackDirectory`、`CollectAll`，并在 EditorSimulateMode 下验证 `CheckLocationValid("PlayerAnimConfig")`。
- [风险] 资源包未初始化或播放模式配置不一致会造成编辑器与运行时表现不同 -> 缓解：验证时记录当前 YooAsset/TEngine 播放模式，并通过 Game 场景实际启动链路验证，而不是只看 Project 视图文件存在。
- [风险] `moveToWall` 和 `lockIdle` 的兜底动画只是近似表现 -> 缓解：在任务中明确这些是临时兜底，后续专用动画迁移单独处理。
- [风险] 基础修复后，高级状态仍可能没有动画 -> 缓解：警告中必须写清缺失的状态或字段，便于后续逐项补齐，不阻塞 Idle/移动恢复。