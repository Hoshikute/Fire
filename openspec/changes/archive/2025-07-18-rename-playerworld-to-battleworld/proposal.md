## Why

`PlayerWorld` 命名暗示它只处理"玩家"，但实际上它是 demo 唯一的帧同步世界，管理所有实体（包括未来可能加入的 NPC、投射物等）。重命名为 `BattleWorld` 更准确反映其定位——这是一个战斗世界。

## What Changes

- `PlayerWorld` 类重命名为 `BattleWorld`
- `PlayerWorld.cs` 文件重命名为 `BattleWorld.cs`
- `TPBattleContext.cs` 中所有引用更新
- `ClimbConfig.cs` 注释中的引用更新

## Capabilities

### Modified Capabilities

<!-- No spec-level behavior changes — pure rename -->

## Impact

- `GameLogic/World/PlayerWorld.cs` → `BattleWorld.cs`（文件 + 类名）
- `TPBattleContext.cs` — `CreateWorld<PlayerWorld>()` → `CreateWorld<BattleWorld>()`，日志文本更新
- `ClimbConfig.cs` — 注释更新
