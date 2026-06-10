# framesync-climb-vault

确定性攀爬/翻越——用逻辑层程序化定点轨迹替代老 TPC 的 Animancer 动画曲线位移。

## ADDED Requirements

### Requirement: 攀爬/翻越交互状态定义
`PlayerLogicState` 枚举 MUST 包含以下交互状态：`Vault`（翻越）、`Climb`（攀爬）、`LedgeClimb`（平台边缘攀爬）、`PlatformerUp`（平台跳上）、`MoveToWall`（走向墙壁）。这些状态在 `PlayerMoveSystem.IsInteractState()` 中 SHALL 屏蔽玩家主动水平移动。

#### Scenario: 交互状态屏蔽输入移动
- **WHEN** `PlayerStateComponent.state` 为 `Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall` 之一
- **THEN** `PlayerMoveSystem.Step` 不执行水平移动逻辑，仅维护重力

### Requirement: 定点攀爬轨迹表
系统 MUST 定义 `ClimbTrajectory` 数据结构——逻辑帧索引（`int frame`）到位移增量（`SyncVector3 delta`）的映射表。每种攀爬类型（Vault/Climb/LedgeClimb/PlatformerUp）有自己的轨迹表。轨迹数据 MUST 通过 `ClimbConfig` ScriptableObject 配置，从老 TPC 动画曲线的关键位移量提取并转为定点数。

#### Scenario: Vault 翻越轨迹推进
- **WHEN** 角色进入 Vault 状态
- **THEN** 每逻辑帧按 `ClimbTrajectory[framesInState]` 推进 `PlayerMoveComponent.pos`，`framesInState++`，直到轨迹表末尾自动切回 Idle/Fall

#### Scenario: 轨迹表数据来源配置化
- **WHEN** 检查 `ClimbConfig` ScriptableObject
- **THEN** 每种攀爬类型的轨迹表（帧索引→位移列表）作为序列化字段存在于 SO 中，可在 Unity Editor 中编辑

### Requirement: 攀爬/翻越进入条件
角色进入攀爬/翻越状态 MUST 通过确定性触发条件：检测前方特定距离内的墙壁/障碍物，结合 `PlayerStateComponent` 当前状态和输入意图（如前方有墙 + 高度 ≤ 阈值 + 输入方向朝墙则触发 Vault）。触发逻辑 SHALL 在 `PlayerStateSystem` 中实现。

#### Scenario: 检测可翻越障碍物触发 Vault
- **WHEN** 角色在地面移动中前方碰到低矮障碍物（高度 ≤ 可翻越阈值）且输入方向朝障碍物
- **THEN** `PlayerStateSystem` 将 `PlayerStateComponent.state` 设为 `Vault`，`framesInState` 重置为 0
