## Why

帧同步的逻辑帧间隔（200ms）当前硬编码在 `FrameSyncModule.cs:16`，无法在不动代码的情况下调整帧率。这导致调试时如需切换帧率（如 100ms/帧 提高响应精度，或 50ms/帧 降低网络负载），必须改动源码且同步修改多处帧计数阈值，容易遗漏出错。

## What Changes

- 在 `GameLogic/Module/FrameSync/` 下新增 `FrameConfig.cs`，集中存放帧同步时间配置（逻辑帧间隔 ms）及全局逻辑帧计数器 `FrameId`
- `FrameSyncModule` 启动时从 `FrameConfig` 读取 `LogicFrameIntervalMs` 替代硬编码的 200
- `PlayerStateSystem` 中帧计数阈值改为基于 `FrameConfig` 计算（而非假设固定 200ms）
- `PlayerViewSystem` 中 `LogicFrameDuration` 常量改为从 `FrameConfig` 读取
- `FrameSyncModule.FixedUpdateWorlds` 每次调用时递增 `FrameConfig.FrameId`

## Capabilities

### New Capabilities

- `frame-sync-config`: 帧同步逻辑帧间隔可配置化，通过 `FrameConfig` 集中管理，支持运行时调整调试

### Modified Capabilities

- `frame-sync-config`: 新增全局帧计数器 `FrameId`，由 `FrameSyncModule` 在每逻辑帧递增

## Impact

- `FrameSyncModule.cs` — `m_intervalTime` 初始化改为读取 `FrameConfig`
- `PlayerStateSystem.cs` — 帧计数阈值常量改为从 `FrameConfig` 计算
- `PlayerViewSystem.cs` — `LogicFrameDuration` 改为运行时读取
- 新增文件 `FrameConfig.cs`
