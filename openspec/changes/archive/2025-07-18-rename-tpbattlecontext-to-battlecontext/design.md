## Context

`TPBattleContext` 是 Game 场景启动入口，负责创建 `BattleWorld`、生成本地玩家、绑定相机。`TP` 前缀是历史遗留。

## Goals / Non-Goals

**Goals:**
- `TPBattleContext` → `BattleContext`（类 + 接口 + 文件名 + 属性）
- 所有引用和注释同步更新

**Non-Goals:**
- 不改变任何逻辑或接口契约

## Decisions

**选择**: 纯文本替换重命名，覆盖 6 个 .cs 文件 + 3 个 .md 知识库文件

**理由**: 引用点集中在少数文件，批量替换风险低。
