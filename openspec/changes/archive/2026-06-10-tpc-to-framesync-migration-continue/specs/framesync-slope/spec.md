# framesync-slope

确定性斜坡处理——在已完成的 `IDeterministicGround` 接口基础上扩展地面法线查询，支持坡度约束和沿斜面投影位移。

## ADDED Requirements

### Requirement: IDeterministicGround 接口扩展法线查询
`IDeterministicGround` 接口 MUST 新增方法 `GetNormal(int x, int z): SyncVector3`，返回指定水平坐标处的地面法线（定点单位向量）。默认实现 `FlatGround` MUST 返回 `SyncVector3(0, SyncVector3.ONE, 0)`（即法线垂直向上，表示无坡度平面）。

#### Scenario: FlatGround 返回垂直法线
- **WHEN** 调用 `FlatGround.GetNormal(anyX, anyZ)`
- **THEN** 返回 `(0, 1000, 0)`（SyncVector3.ONE 在 Y 分量）

#### Scenario: 接口保持确定性
- **WHEN** 两个客户端在同一逻辑帧调用 `GetNormal(x, z)`
- **THEN** 返回的 `SyncVector3` 完全一致（定点数，无浮点误差）

### Requirement: PlayerMoveSystem 坡度约束
`PlayerMoveSystem.Step` 在角色接地时 MUST 查询当前地面法线。若法线与垂直方向夹角超过最大坡度阈值（默认 45°，可配置），角色 SHALL NOT 沿输入方向水平移动，仅应用重力和竖直约束。

#### Scenario: 平地正常移动
- **WHEN** 角色在 FlatGround（法线垂直）上且有水平输入
- **THEN** 角色沿输入方向正常移动，与当前行为一致

#### Scenario: 超坡度停止水平移动
- **WHEN** 角色接地且地面法线角度 > 45°
- **THEN** 角色不产生水平位移，仅受重力影响下滑

### Requirement: 沿斜面投影位移
当角色接地且坡度在允许范围内（≤最大坡度阈值），角色水平位移 MUST 投影到斜面切平面。位移公式：`horizontalDelta` 沿斜面法线投影后的切向分量叠加到 `pos`，使角色沿斜面移动而非穿透。

#### Scenario: 缓坡沿斜面移动
- **WHEN** 角色在 20° 斜坡上且有沿坡面方向的输入
- **THEN** 角色沿斜面表面移动，位置 Y 分量随斜面上升/下降
