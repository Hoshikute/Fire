## Context

`PlayerWorld` 是项目唯一的 `WorldBase` 子类，混合了表现层和逻辑层系统。重命名为 `BattleWorld` 更符合 demo 的单一世界定位。

## Goals / Non-Goals

**Goals:**
- 类名 `PlayerWorld` → `BattleWorld`
- 文件名 `PlayerWorld.cs` → `BattleWorld.cs`
- 所有引用同步更新

**Non-Goals:**
- 不改变任何系统注册、组件、逻辑
- 不改变世界内部架构

## Decisions

**选择**: 纯文本替换重命名，不引入别名或中间层

**理由**: 类型名和引用点只有 3 处（类定义 + TPBattleContext 4 处 + ClimbConfig 注释），直接 rename 最干净。
