## Why

当前 Fire 客户端已经具备本地输入固化、命令缓存、快照和回滚基础，但权威命令接收、冲突检测、追帧重算、服务端广播接线尚未闭环；本地玩家实际仍由客户端先行决定，联网时容易出现不同客户端执行帧、输入内容或实体状态不一致。

需要以 `E:\EUGIT\UnityLockStepDemo\UnityLockStepDemo\LockStepDemo\Client\Assets\Script` 中的 `SyncFrameWork` / `SyncGameLogic` / `SyncClientLogic` 和 `Core` 网络输入分发为基准，把 Fire 修正为“客户端只上报帧输入并做预测，服务端汇总/广播权威帧命令，客户端按权威命令回滚重算”的模式。

## What Changes

- 新增服务端权威帧命令接线：客户端本地输入先记录为预测命令并上行，最终以服务端回传的同帧命令为准。
- 新增客户端 `StartSyncMsg` / `CommandMsg` 或单条 `CommandComponent` / `PursueMsg` / `AffirmMsg` 处理流程，对齐基准 Demo 的 `SyncSystem<T>` 职责。
- 将 `PlayerInputCommandSystem` 从“本地输入直接成为最终命令”调整为“本地输入只产生预测/待确认命令；存在权威命令时优先使用权威命令”。
- 在 `BattleWorld` 注册同步接线系统，使权威命令缓存、冲突检测、回滚重算和提前预测成为世界生命周期的一部分。
- 补齐服务端协议与命令聚合计划：服务端按逻辑帧收集玩家输入，缺帧时用默认/预测输入补齐，并向所有客户端广播同一帧的权威命令。
- 增加验证用例，覆盖双客户端同输入一致、迟到权威命令触发回滚、one-shot 输入不被预测重复、断线/缺帧默认命令不导致不同步。
- **BREAKING**：帧同步逻辑不再接受“客户端本地命令直接视为最终结果”的行为；联网模式下本地结果必须允许被服务端权威命令纠正。

## Capabilities

### New Capabilities
- `server-authoritative-frame-sync`: 定义服务端权威帧命令、客户端预测回滚、确认/追帧和一致性验证的行为契约。

### Modified Capabilities
- 无。

## Impact

- 客户端受影响范围：
  - `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/`
  - `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Network/`
  - `UnityProject/Assets/GameScripts/HotFix/GameLogic/Context/BattleContext.cs`
- 服务端受影响范围：
  - `Server/LockStepDemo/Service/ServiceLogic/`
  - `Server/LockStepDemo/Service/Game/CommandMessageService.cs`
  - `Server/LockStepDemo/Service/Message/SyncMessage.cs`
  - `Server/LockStepDemo/Generate/Protocol/`
- 需要保持的架构约束：
  - 逻辑帧固定步长，逻辑层只使用确定性数据。
  - `PlayerMoveComponent` / `PlayerStateComponent` 等可回滚状态必须继续通过 `DeepCopy()` 快照隔离。
  - 表现层只读逻辑结果，不得反写确定性逻辑状态。
  - 网络协议中的方向、位置和命令字段必须使用定点/可序列化结构，不直接传 Unity `Vector3` 或浮点逻辑结果。
