## Why

`FrameConfig.cs` 当前放在 `GameLogic/Module/` 根目录下，但它是帧同步模块的专属配置。移到 `Module/FrameSync/` 下与 `FrameSyncModule.cs` 同目录，归属更清晰。

## What Changes

- `FrameConfig.cs` 从 `Module/` 移动到 `Module/FrameSync/`

## Capabilities

### Modified Capabilities

- `frame-sync-config`: `FrameConfig.cs` 文件路径变更为 `Module/FrameSync/FrameConfig.cs`

## Impact

- `Module/FrameConfig.cs` → `Module/FrameSync/FrameConfig.cs`（路径变更）
