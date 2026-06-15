## MODIFIED Requirements

### Requirement: 帧同步世界命名为 BattleWorld

系统 SHALL 提供 `BattleWorld` 类（`WorldBase` 子类）作为 demo 唯一的帧同步世界，包含玩家角色控制所需的全部 ECS 系统。

#### Scenario: 创建战斗世界
- **WHEN** `TPBattleContext` 启动帧同步
- **THEN** 调用 `CreateWorld<BattleWorld>()` 创建世界实例

#### Scenario: 类名正确
- **WHEN** 开发者搜索帧同步世界
- **THEN** 找到 `BattleWorld` 类而非 `PlayerWorld`
