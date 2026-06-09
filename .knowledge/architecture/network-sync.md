# 网络同步 / 指令协议

## 一句话
服务器按帧下发指令和实体同步消息，客户端按帧号执行；支持两种同步规则（Status 状态同步 / Frame 帧同步），并通过追帧消息驱动回滚重算。

## 关键文件
- `Module/FrameSync/Message/SyncMessage.cs` — 所有同步消息与数据结构定义
- `Module/FrameSync/Core/SyncRule.cs` — 同步规则枚举
- `Module/FrameSync/GameLogic/Component/CommandComponent.cs` — 指令组件（玩家本帧输入）
- `Module/FrameSync/SyncLogic/Component/PlayerCommandBase.cs` — 玩家命令基类（id/frame/time + DeepCopy/EqualsCmd）
- `Module/FrameSync/SyncLogic/Component/` — 连接状态、服务器缓存、命令记录等组件
- `Module/Network/` — 网络传输层

## 两种同步规则（SyncRule）
- **Status（状态同步）**：所有对实体的操作交给服务器下发，客户端只接收结果。
- **Frame（帧同步）**：本地计算所有结果，只同步输入指令。本项目核心。

## 主要消息（SyncMessage.cs）
| 消息 | 作用 | 关键字段 |
|------|------|---------|
| `StartSyncMsg` | 同步开始 | frame, advanceCount, intervalTime, createEntityIndex, SyncRule |
| `CommandMsg` | 指令下发 | frame, serverTime, List<CommandInfo> |
| `PursueMsg` | 追帧（触发重算） | recalcFrame, frame, advanceCount, serverTime |
| `SyncEntityMsg` | 实体同步 | frame, List<EntityInfo>, destroyList |
| `ChangeComponentMsg` | 组件变更 | frame, id, ComponentInfo |
| `ChangeSingletonComponentMsg` | 单例组件变更 | frame, ComponentInfo |
| `DestroyEntityMsg` | 销毁实体 | frame, id |
| `AffirmMsg` | 确认（输入已被服务器接收） | frame, time, id |
| `DebugMsg` | 调试快照 | frame, List<EntityInfo> |

## 指令数据（CommandInfo）
玩家一帧的输入：`frame` / `id` / `moveDir`(SyncVector3) / `skillDir`(SyncVector3) / `element1` / `element2` / `isFire`。
- `FromCommand(CommandComponent)` / `ToCommand()` 在网络结构与 ECS 组件间转换，**用 SyncVector3.DeepCopy 保证定点数不被引用共享**。

## 核心流程（概念）
1. 本地采集输入 → `CommandComponent`，打上当前 frame/time。
2. 上行给服务器；服务器汇总后用 `CommandMsg` 按帧广播权威指令。
3. 客户端用权威指令在对应帧执行；预测与权威不符时，服务器/逻辑通过 `PursueMsg.recalcFrame` 指示从某帧重算 → 触发回滚（见 rollback.md）。
4. `AffirmMsg` 确认输入已被接收，用于清理本地待确认队列。

## 注意事项 / 坑
- 指令里的方向用 `SyncVector3`（定点数），不要在协议里塞 `float`/`Vector3` 直传，跨端浮点不一致会破坏确定性。
- `EqualsCmd` 用于判断预测指令与权威指令是否一致 → 决定是否需要回滚，实现要覆盖所有影响逻辑的字段。
- `advanceCount` 是客户端领先服务器的帧数（预测深度），影响手感与回滚频率。

## 相关代码位置
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/Message/`
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Network/`

## 关联文档
- 帧同步：`architecture/lockstep.md`
- 回滚：`architecture/rollback.md`
