## ADDED Requirements

### Requirement: 遵循 TEngine 资源系统契约
`PlayerAnimConfig` 的运行时加载 MUST 通过 TEngine `ResourceModule`/`GameModule.Resource` 完成，并 MUST 使用 YooAsset location `PlayerAnimConfig`。系统 SHALL NOT 为该配置新增绕过 TEngine 资源模块的 `Resources.Load`、AssetDatabase 运行时依赖或硬编码绝对文件路径加载。

#### Scenario: 使用默认资源包定位动画配置
- **WHEN** `TPBattleContext` 初始化 Game 场景并准备加载玩家动画配置
- **THEN** 它通过 `GameModule.Resource.CheckLocationValid("PlayerAnimConfig")` 和 `GameModule.Resource.LoadAssetAsync<PlayerAnimConfig>("PlayerAnimConfig")` 验证并加载配置

#### Scenario: Collector 规则生成预期 location
- **WHEN** `PlayerAnimConfig.asset` 位于 `Assets/AssetRaw/Configs`
- **THEN** `DefaultPackage` 的 `Configs` collector 使用 `AddressByFileName` 生成 location `PlayerAnimConfig`，并允许默认资源包解析该地址

### Requirement: PlayerAnimConfig 资源可加载
系统 MUST 提供一个 `PlayerAnimConfig` ScriptableObject 资源，并且该资源 MUST 能通过默认资源包以地址 `PlayerAnimConfig` 解析。

#### Scenario: 游戏启动时资源地址可解析
- **WHEN** `TPBattleContext` 初始化 Game 场景并调用 `GameModule.Resource.CheckLocationValid("PlayerAnimConfig")`
- **THEN** 检查结果为成功，并且 `GameModule.Resource.LoadAssetAsync<PlayerAnimConfig>("PlayerAnimConfig")` 返回非空配置

#### Scenario: 资源缺失时输出可定位错误
- **WHEN** `PlayerAnimConfig` 资源地址无法解析
- **THEN** 启动诊断日志包含准确地址 `PlayerAnimConfig`，并明确指出动画配置缺失是动画无法启动的原因

### Requirement: 基础地面动画映射存在
`PlayerAnimConfig` 资源 MUST 包含 Game 场景基础动画链路所需的非空过渡映射：Idle、MoveStart 八方向、MoveLoop、MoveEnd 左/右、MoveToWall 兜底、LockIdle 兜底。

#### Scenario: 玩家首个可见帧播放 Idle
- **WHEN** 本地 FrameSync 玩家实体创建，且 `PlayerStateComponent.state == Idle`
- **THEN** `PlayerAnimViewSystem` 在玩家的 `AnimancerComponent` 上播放配置好的 Idle 过渡，而不是让模型保持 T-Pose

#### Scenario: 地面移动状态切换播放配置动画
- **WHEN** `PlayerStateComponent.state` 从 `Idle` 切到 `MoveStart`、`MoveLoop`、`MoveEnd`，或切回 `Idle`
- **THEN** `PlayerAnimViewSystem` 为新状态选择对应配置过渡，并调用 `AnimancerComponent.Play`

#### Scenario: MoveToWall 和 LockIdle 使用安全兜底
- **WHEN** `PlayerStateComponent.state` 进入 `MoveToWall` 或 `LockIdle`，且尚未选择专用过渡
- **THEN** 系统使用配置好的兜底过渡，而不是跳过全部动画播放

### Requirement: 动画表现保持只读逻辑
动画资源加载和播放 MUST 保持纯表现层行为。它 SHALL NOT 修改 `PlayerMoveComponent`、`PlayerStateComponent`、`PlayerInputComponent` 或任何参与回滚记录的确定性逻辑组件。

#### Scenario: Animancer 播放不写逻辑状态
- **WHEN** `PlayerAnimViewSystem` 播放 Idle 或移动过渡
- **THEN** 只有 Animancer 表现状态发生变化，确定性 FrameSync 逻辑组件不会被动画系统写入

### Requirement: 可诊断可选映射缺失
如果 jump、fall、vault、climb、ledge climb、platformer 等高级可选动画映射缺失，系统 SHALL 在对应路径被触发时报告缺失状态或字段，但 SHALL NOT 让整个配置变成 null。

#### Scenario: 可选高级状态缺少过渡
- **WHEN** `PlayerStateComponent.state` 进入某个高级状态，且对应 `PlayerAnimConfig` 字段为 null
- **THEN** Console warning 指出缺失的动画状态或字段，同时已经配置的 Idle 和地面移动映射仍可继续使用