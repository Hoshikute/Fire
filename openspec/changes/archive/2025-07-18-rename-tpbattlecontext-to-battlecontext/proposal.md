## Why

`TPBattleContext` 的 `TP` 前缀是 "ThirdPerson" 的缩写，但项目已完全迁移到帧同步 ECS 架构，不再有 ThirdPersonController。去掉 `TP` 前缀更简洁准确。

## What Changes

- `TPBattleContext` 类重命名为 `BattleContext`
- `ITPBattleContext` 接口重命名为 `IBattleContext`
- 文件 `TPBattleContext.cs` → `BattleContext.cs`，`ITPBattleContext.cs` → `IBattleContext.cs`
- `GameModule.TPBattleContext` 属性 → `GameModule.BattleContext`
- 所有注释、日志中的引用同步更新

## Capabilities

### Modified Capabilities

- `battle-world`: 入口类更名为 `BattleContext`

## Impact

- `Context/TPBattleContext.cs` → `BattleContext.cs`（类、TraceHeader、接口实现）
- `Context/ITPBattleContext.cs` → `IBattleContext.cs`（接口、文件）
- `GameModule.cs` — 属性 `TPBattleContext` → `BattleContext`
- `LoginUI.cs` / `LoginWindow.cs` — 调用 `GameModule.BattleContext`
- `PlayerViewComponent.cs` — 2 处注释
- `PlayerAnimConfig.cs` — 1 处注释
- `.knowledge/` — 所有文档引用同步更新
