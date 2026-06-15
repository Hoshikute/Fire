## ADDED Requirements

### Requirement: 默认 Idle 使用站立姿态
FrameSync 玩家在非锁定 `PlayerLogicState.Idle` 进入 Game 场景时，SHALL 显示站立待机姿态。

#### Scenario: Game 场景以 Idle 启动
- **WHEN** Game 场景初始化本地 FrameSync 玩家，且 `PlayerStateComponent.state` 为 `Idle`
- **THEN** `PlayerAnimViewSystem` MUST 将配置的 Idle 动画播放为站立待机姿态，而不是蹲伏或偏低待机姿态

#### Scenario: 移动后回到 Idle
- **WHEN** 玩家从 `MoveStart` 或 `MoveEnd` 回到 `Idle`
- **THEN** 下一次 Idle 播放 MUST 解析到和启动时一致的站立待机姿态

### Requirement: Idle 姿态选择只属于表现层
系统 SHALL 将 Idle 姿态选择保留在确定性 FrameSync 逻辑和回滚状态之外。

#### Scenario: 应用 Idle 姿态修复
- **WHEN** 通过 Animancer 过渡、mixer 参数或配置资产选择站立 Idle 姿态
- **THEN** 修复 MUST NOT 把 Unity 动画资源、mixer 参数或表现层专用标记写入 `PlayerMoveComponent`、`PlayerStateComponent` 或任何回滚快照组件

### Requirement: 不删除蹲伏资源
系统 SHALL 保留蹲伏和备用 Idle 过渡资源，供未来玩法状态使用，同时避免它们成为非锁定 Idle 的默认姿态。

#### Scenario: 修复后检查 Idle 资源
- **WHEN** 实现后检查 Idle 动画资源
- **THEN** `IdleCrouch` 和相关备用 Idle 资源 MUST 仍然存在，并且默认非锁定 `Idle` 路径 MUST 选择站立 Idle，除非未来蹲伏状态明确选择其他资源

### Requirement: Idle 映射回退可诊断
系统 SHALL 提供简洁方式诊断 `Idle` 是否播放，以及实际使用了哪条 Idle 映射路径。

#### Scenario: Idle 姿态再次错误
- **WHEN** 玩家进入 `Idle`，但可见姿态仍然错误
- **THEN** 日志或资产校验 MUST 能区分 `PlayerAnimConfig` 缺失、`Idle` 播放被跳过、以及 Idle mixer/默认参数选择错误这三类问题
