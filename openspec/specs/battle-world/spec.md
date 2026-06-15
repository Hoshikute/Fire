# battle-world

Demo 唯一的帧同步世界定义。

## Requirements

### Requirement: 帧同步世界命名为 BattleWorld

系统 SHALL 提供 `BattleWorld` 类（`WorldBase` 子类）作为 demo 唯一的帧同步世界，包含玩家角色控制所需的全部 ECS 系统。

#### Scenario: 创建战斗世界
- **WHEN** `BattleContext` 启动帧同步
- **THEN** 调用 `CreateWorld<BattleWorld>()` 创建世界实例

#### Scenario: 类名正确
- **WHEN** 开发者搜索帧同步世界
- **THEN** 找到 `BattleWorld` 类而非 `PlayerWorld`

### Requirement: Game 场景入口为 BattleContext

系统 SHALL 提供 `BattleContext` 类作为 Game 场景的帧同步启动入口，实现 `IBattleContext` 接口。通过 `GameModule.BattleContext` 访问。

#### Scenario: 启动战斗场景
- **WHEN** 调用 `GameModule.BattleContext.InitializeGameScene()`
- **THEN** 创建 `BattleWorld` 实例、生成本地玩家实体、绑定相机
