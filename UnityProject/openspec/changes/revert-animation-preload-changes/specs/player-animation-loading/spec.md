## ADDED Requirements

### Requirement: No eager player animation preload
玩家动画加载系统 SHALL NOT 在 `Player.Awake()` 或玩家 FSM 初始化阶段遍历并预创建全部移动状态动画 transition。

#### Scenario: Player initialization starts without preloading every transition
- **WHEN** 玩家对象执行 `Awake()` 并创建 `PlayerFSM`
- **THEN** 系统 SHALL 启动初始玩家状态，而不是调用全量动画 transition 预热流程

#### Scenario: Preload diagnostics are absent during initialization
- **WHEN** `Game.unity` 加载玩家并完成初始化
- **THEN** 系统 SHALL NOT 输出 `[Preload]` 相关日志作为玩家初始化的常规路径

### Requirement: Animation state creation remains demand driven
玩家动画加载系统 SHALL 在具体玩家状态需要播放动画时，通过现有 `animancer.Play(...)` 路径按需创建或复用 Animancer state。

#### Scenario: Movement animation plays through state logic
- **WHEN** 玩家进入 `PlayerIdleState`、`PlayerMoveStartState`、`PlayerMoveLoopState` 或 `PlayerMoveEndState`
- **THEN** 对应状态 SHALL 继续使用自身持有的动画 transition 调用 `animancer.Play(...)`

#### Scenario: Jump and fall animation plays through state logic
- **WHEN** 玩家进入跳跃、下落或落地相关状态
- **THEN** 对应状态 SHALL 继续通过状态逻辑播放所需动画，而不依赖初始化阶段的全量预创建

### Requirement: Animation data classes stay reference-only
玩家动画数据类 SHALL 只承载状态播放所需的 serialized animation references，不暴露仅用于全量预加载的统一 transition 枚举接口。

#### Scenario: Player animation data has no preload collection API
- **WHEN** 开发者查看 `PlayerMovementData`、`PlayerMoveStartData`、`PlayerMoveLoopData`、`PlayerMoveEndData`、`PlayerJumpFallAndLandData`、`PlayerClimbData` 或 `PlayerHangWallData`
- **THEN** 这些数据类 SHALL NOT 实现 `IAnimationProvider` 或提供 `CollectTransitions()` 作为预加载入口
