# battle-framesync-switch

将 `TPBattleContext`（战斗业务入口）从老 TPC 的 `Character.SetThirdPersonPlayerPrefab` + `LoadThirdPersonPlayerAsync` 切换到 FrameSync ECS 的 `PlayerFrameSyncEntry`。

## ADDED Requirements

### Requirement: TPBattleContext 使用通用 Character API
`TPBattleContext.cs` MUST 将 `Character.SetThirdPersonPlayerPrefab` + `LoadThirdPersonPlayerAsync` 替换为通用化的 `Character.SetCharacterPrefab` + `Character.LoadCharacterAsync`。加载完成后，SHALL 在 prefab 实例上挂载（或激活已有的）`PlayerFrameSyncEntry` 组件，并注入动画配置和相机锚点。

#### Scenario: Battle 入口加载 FrameSync 角色
- **WHEN** TPBattleContext 初始化战斗场景
- **THEN** 通过 `GameModule.Character.LoadCharacterAsync("hero_01")` 加载角色 prefab，创建 `PlayerFrameSyncEntry` 组件驱动帧同步 ECS

#### Scenario: 老 API 无引用
- **WHEN** 在 `Context/` 目录下全文搜索 `ThirdPersonPlayer`
- **THEN** 无匹配结果

### Requirement: PlayerFrameSyncEntry 支持动态注入
`PlayerFrameSyncEntry` MUST 支持在运行时（而非仅在 Inspector 序列化）注入动画配置 SO 和相机锚点 Transform。提供公开方法 `SetAnimConfig(PlayerAnimConfig config)` 和 `SetLookAtTarget(Transform target)`，供 `TPBattleContext` 在加载后调用。

#### Scenario: 代码注入配置
- **WHEN** `TPBattleContext` 调用 `entry.SetAnimConfig(config)` 和 `entry.SetLookAtTarget(target)`
- **THEN** `PlayerFrameSyncEntry.Start()` 使用注入的配置启动 PlayerWorld，无需 Inspector 预赋值

### Requirement: CameraModule 绑定兼容
`TPBattleContext` 中的相机绑定逻辑 MUST 与 `PlayerFrameSyncEntry.TryBindCamera()` 兼容，避免重复绑定或绑定时机竞态。优先使用 `PlayerFrameSyncEntry` 自带的延迟绑定机制。

#### Scenario: 相机正常绑定
- **WHEN** 战斗场景启动且角色已加载
- **THEN** 相机通过 `GameModule.Camera.BindCinemachineToPlayer` 正确跟踪角色锚点
