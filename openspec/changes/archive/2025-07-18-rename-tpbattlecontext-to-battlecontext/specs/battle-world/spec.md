## MODIFIED Requirements

### Requirement: Game 场景入口为 BattleContext

系统 SHALL 提供 `BattleContext` 类作为 Game 场景的帧同步启动入口，实现 `IBattleContext` 接口。通过 `GameModule.BattleContext` 访问。

#### Scenario: 启动战斗场景
- **WHEN** 调用 `GameModule.BattleContext.InitializeGameScene()`
- **THEN** 创建 `BattleWorld` 实例、生成本地玩家实体、绑定相机
