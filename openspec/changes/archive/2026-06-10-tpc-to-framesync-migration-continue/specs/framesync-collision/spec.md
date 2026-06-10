# framesync-collision

确定性碰撞体系——AABB/胶囊碰撞体与定点静态几何的碰撞检测与响应，角色不穿墙。

## ADDED Requirements

### Requirement: ICollisionWorld 确定性碰撞查询接口
系统 MUST 定义 `ICollisionWorld` 接口，提供 `CapsuleSweep(SyncVector3 from, SyncVector3 to, int radius, int height): CollisionResult` 和 `CapsuleOverlap(SyncVector3 center, int radius, int height): bool`。`CollisionResult` MUST 包含 `hit: bool`、`normal: SyncVector3`（碰撞法线）、`penetration: int`（穿透深度，定点）、`point: SyncVector3`（碰撞点）。所有计算均使用定点数。

#### Scenario: 胶囊扫掠无碰撞
- **WHEN** 胶囊从 A 点扫掠到 B 点且路径上无几何体
- **THEN** `CollisionResult.hit` 为 false

#### Scenario: 胶囊扫掠碰到墙壁
- **WHEN** 胶囊向前方移动且前方 1 米处有墙壁
- **THEN** `CollisionResult.hit` 为 true，`normal` 指向角色，`penetration` 为正值

### Requirement: 角色碰撞响应——分离+滑动
`PlayerMoveSystem.Step` 在计算水平位移后 MUST 调用 `ICollisionWorld.CapsuleSweep` 检测碰撞。若发生碰撞，MUST 执行分离（沿法线推出穿透深度）和滑动（将剩余速度沿碰撞法线切平面投影，用投影后的速度继续尝试移动）。

#### Scenario: 角色撞墙停止
- **WHEN** 角色向墙壁移动且在碰撞距离内
- **THEN** 角色停在墙前，不穿透墙壁

#### Scenario: 角色沿墙滑动
- **WHEN** 角色以斜角向墙壁移动（输入方向与墙壁法线不平行）
- **THEN** 角色沿墙壁表面滑动，速度削弱为切向分量

### Requirement: 竖直胶囊碰撞体
角色碰撞体 MUST 建模为竖直胶囊（半径 `capsuleRadius`、高度 `capsuleHeight`，均为定点整数常量）。胶囊参数 MUST 在 `PlayerMoveComponent` 或 `PlayerWorld` 中可配置。

#### Scenario: 胶囊高度覆盖角色身高
- **WHEN** 角色站在地面上方
- **THEN** 胶囊底部在地面以上、顶部在角色头部高度，不穿透天花板
