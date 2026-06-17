## Context

Fire 当前帧同步角色链路已经完成了确定性移动、状态推导、命令缓存和快照回滚的基础：

- `BattleContext` 创建 `BattleWorld`，生成本地玩家实体，并立即 `IsStart = true`。
- `PlayerInputCollectSystem` 在渲染帧采集 Unity 输入，写入 `PlayerInputComponent`。
- `PlayerInputCommandSystem` 在逻辑帧把本地输入固化到本地实体 `PlayerCommandRecordComponent`；非本地实体缺命令时预测。
- `PlayerMoveSystem` / `PlayerStateSystem` 只按实体帧命令推进逻辑。
- `WorldBase` 已支持 `Record`、`RevertToFrame`、`Recalc`、`ClearBefore`、`ClearAfter` 和创建/销毁回滚缓存。
- `SyncMessage.cs` 已定义 `StartSyncMsg`、`CommandMsg`、`PursueMsg`、`AffirmMsg` 等协议结构。

缺口是：权威命令没有进入 `BattleWorld` 生命周期，本地命令会被视为当前帧的事实来源；服务端旧 LockStepDemo 虽有 `PlayerInputSystem`、`CommandMessageService<T>`、`ServiceSyncSystem` 等权威帧聚合雏形，但 Fire 客户端新角色命令尚未对齐接入。

基准工程的关键思路不是搬代码，而是迁移职责：

- 客户端 `SyncSystem<T>`：处理开始同步、权威命令、确认、追帧、冲突检测、回滚重算、提前预测。
- 客户端 `CommandSyncSystem<T>`：本地输入只作为预测命令上行，非本地实体只吃缓存/预测。
- 服务端 `PlayerInputSystem`：每个服务端逻辑帧选择玩家真实输入或默认/预测输入，并把服务端确定的命令广播出去。
- 服务端 `CommandMessageService<T>`：接收客户端输入、确认、迟到命令追帧、广播权威命令。

## Goals / Non-Goals

**Goals:**

- 把联网模式的权威来源改为服务端帧命令，客户端只负责输入上行、预测和回滚。
- 保留 Fire 已有 `BattleWorld`、`PlayerCommandRecordComponent`、`WorldBase` 回滚和 `FrameConfig` 固定逻辑帧机制。
- 新增客户端同步接线系统，能处理 `StartSyncMsg`、权威命令、`PursueMsg`、`AffirmMsg`。
- 让 `PlayerInputCommandSystem` 优先消费权威/缓存命令，本地输入只在缺权威命令时形成预测。
- 对齐服务端命令聚合：服务端按帧给所有玩家广播同一份权威命令序列。
- 增加可重复验证，证明双客户端同帧输入、迟到输入、缺帧预测和 one-shot 输入都不会导致逻辑分叉。

**Non-Goals:**

- 不在本 change 内重写角色移动、碰撞、状态机、动画表现和资源加载。
- 不把 Fire 客户端回退到 Demo 旧目录结构。
- 不把 Unity `Transform`、`Vector3` 或表现层状态纳入帧同步协议。
- 不要求第一阶段实现复杂反作弊校验；本 change 只先建立“服务端帧命令权威”的同步边界。

## Decisions

### Decision: 以“权威命令接线系统”补齐 `BattleWorld`

新增客户端同步系统，建议命名为 `FrameAuthoritySyncSystem` 或 `PlayerCommandAuthoritySystem`，注册在 `BattleWorld.GetSystemTypes()` 中，职责对应 Demo 的 `SyncSystem<T>`：

- 订阅 `GameModule.Network.MessageReceived` 或一个轻量的协议分发适配器。
- 接收 `StartSyncMsg` 后设置 `FrameCount`、`EntityIndex`、`SyncRule`、`FrameConfig/FrameSyncModule.IntervalTime`、`ConnectStatusComponent.aheadFrame`，再启动世界或从暂停状态恢复。
- 接收权威命令后写入对应实体的 `PlayerCommandRecordComponent`。
- 当权威命令帧小于等于当前 `WorldBase.FrameCount` 时，与预测命令 `EqualsCmd` 对比；冲突则从该帧回滚重算。
- 接收 `PursueMsg` 后从 `recalcFrame` 追帧，并按 `frame + advanceCount` 继续提前预测。
- 接收 `AffirmMsg` 后清理本地 `unConfirmFrame` 并更新 RTT。

备选方案是直接修改 `PlayerInputCommandSystem` 承担网络处理。放弃该方案，因为它会把“输入固化”和“网络权威/回滚调度”耦合在同一个系统，后续难以测试，也容易破坏表现/逻辑分层。

### Decision: 命令传输做适配层，内部统一为 `CommandComponent`

Fire 已有 `CommandMsg` 批量结构，Demo 当前活跃路径更多使用单条泛型 `T : PlayerCommandBase`。为了避免协议改造一次做太大，客户端新增一个命令传输适配层：

- 上行本地预测命令时发送 `CommandComponent` 或现有协议生成器支持的等价消息。
- 下行可接受单条 `CommandComponent` 或 `CommandMsg`，进入同步系统前统一转换为 `CommandComponent` 列表。
- `CommandInfo.FromCommand` / `ToCommand` 继续保持深拷贝，避免网络层复用对象污染历史帧。

备选方案是强制改成只使用 `CommandMsg` 批量包。放弃该方案作为第一阶段硬要求，因为服务端现有 `CommandMessageService<T>` 已围绕单条命令工作，先加适配能降低迁移风险；后续可以把批量包作为带宽优化。

### Decision: 客户端本地输入只产生预测命令

`PlayerInputCommandSystem` 保留“每个逻辑帧为本地实体生成命令”的职责，但语义改为：

- 若同帧已有权威命令或网络缓存命令，优先使用缓存命令。
- 若缺失权威命令，才用 `PlayerInputComponent.ToCommand(...)` 生成预测命令并记录为待确认。
- 预测命令必须上行服务端，并进入 `ConnectStatusComponent.unConfirmFrame`。
- 非本地实体只使用权威命令或预测命令，不读取本地 `PlayerInputComponent`。
- 回滚重算期间不发送网络消息，不触发表现副作用。

备选方案是等服务端命令到齐才推进逻辑。放弃该方案，因为会牺牲手感；当前项目已有预测和回滚基础，应保留客户端提前帧体验。

### Decision: 服务端负责每帧补齐输入并广播权威命令

服务端沿用旧 LockStepDemo 的职责划分：

- `CommandMessageService<T>` 接收客户端输入，立即回 `AffirmMsg`。
- 若输入帧仍在未来，加入该连接的命令队列，并向其他客户端广播。
- 若输入帧已落后服务端当前帧，服务端发送 `PursueMsg` 让该客户端回滚/追帧。
- `PlayerInputSystem` 每个服务端逻辑帧为每个连接实体选择真实命令或默认/预测命令，设置 `id/frame` 后写入服务端世界。
- 服务端缺输入时生成预测命令并广播给所有客户端，使所有客户端对该帧拥有同一权威输入。

备选方案是服务端只转发客户端输入、不在缺帧时补齐。放弃该方案，因为任何客户端收不到某玩家同帧命令时都会各自预测，预测差异会扩大成不同步。

### Decision: 网络模式下由 `StartSyncMsg` 决定世界起跑

`BattleContext` 可以继续负责加载 Player prefab 和创建 `BattleWorld`，但联网模式下不应把本地世界立即视为权威起跑：

- 离线/本地调试模式可继续直接 `IsStart = true`。
- 联网模式应在实体、相机和表现对象就绪后等待服务端 `StartSyncMsg`。
- `StartSyncMsg` 到达后设置帧号、提前帧数、间隔、实体索引，并执行必要的实体同步缓存。

备选方案是维持当前立即启动，再收到 `StartSyncMsg` 后强行修正。放弃该方案，因为起跑帧不一致会让第一批输入和快照窗口很难解释。

## Risks / Trade-offs

- [Risk] 当前客户端协议生成/反序列化路径与服务端生成协议不完全一致 → 先实现命令适配层，并用单条 `CommandComponent` 与 `CommandMsg` 双路径测试覆盖。
- [Risk] 回滚到已清理帧会失败或产生错误表现 → `ConnectStatusComponent.ClearFrame` 必须作为硬保护，超过窗口时记录错误并触发重新同步策略。
- [Risk] one-shot 输入在预测帧重复触发 → 保持 `CommandComponent.ClearOneShotInputs()`，并给 `jump/toggleLock/platformJump` 写预测测试。
- [Risk] `WorldBase.Recalc` 期间发送网络消息或表现事件 → 同步系统和输入系统必须检查 `m_world.m_isRecalc`，回滚重算只改确定性数据。
- [Risk] 服务端和客户端命令字段不一致 → 先对齐 `CommandComponent` / `PlayerCommandBase` 字段，再更新协议生成物和测试。
- [Risk] 旧服务端 `SyncRule.Status` 默认路径与新客户端 `SyncRule.Frame` 语义冲突 → 匹配/建房阶段明确设置 `SyncRule.Frame`，并在 `StartSyncMsg` 中下发。

## Migration Plan

1. 客户端先新增协议/网络适配层和 `FrameAuthoritySyncSystem`，但用开关保持现有本地调试模式可跑。
2. 调整 `PlayerInputCommandSystem` 的命令优先级：权威缓存优先，本地输入只预测。
3. 在 `BattleWorld` 注册同步系统，并让 `BattleContext` 在联网模式等待 `StartSyncMsg`。
4. 对齐服务端 `CommandComponent` 字段、`StartSyncMsg.SyncRule`、缺帧预测和权威广播。
5. 补齐单元测试和双客户端模拟测试。
6. 验证通过后，把联网入口默认切到服务端权威路径；离线调试保留显式本地模式。

回退策略：如果服务端权威路径验证失败，可以通过联网开关回到本地调试模式；已新增的协议适配和同步系统不影响离线 `BattleWorld` 基础移动验证。

## Open Questions

- Fire 当前客户端协议生成链路是否已经能直接序列化 `CommandComponent`，还是需要优先落到 `CommandMsg` 批量包。
- 多玩家实体 ID 的最终来源是服务端 `SyncEntityMsg`，还是客户端登录/匹配阶段已有稳定 playerId。
- 第一阶段是否需要状态哈希/DebugMsg 校验。建议作为验证增强项，不阻塞服务端权威命令闭环。
