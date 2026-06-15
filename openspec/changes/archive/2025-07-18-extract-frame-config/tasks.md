## 1. FrameConfig 新建

- [x] 1.1 创建 `FrameConfig.cs` 在 `GameLogic/Module/` 目录下，`public static class FrameConfig`，包含 `public const int LogicFrameIntervalMs = 200` 和 `public const float LogicFrameDurationSeconds = 0.2f`

## 2. FrameSyncModule 适配

- [x] 2.1 修改 `FrameSyncModule.cs`：`m_intervalTime` 初始化改为 `FrameConfig.LogicFrameIntervalMs`，删除硬编码的 `200`

## 3. PlayerStateSystem 帧阈值解耦

- [x] 3.1 将 `MoveStartFrames`、`MoveEndFrames`、`LandBufferFrames`、`InteractMinFrames` 改为毫秒意图常量（`MoveStartDurationMs` 等），实际帧数在 `Step()` 中按 `FrameConfig.LogicFrameIntervalMs` 计算

## 4. PlayerViewSystem 插值适配

- [x] 4.1 删除 `PlayerViewSystem.LogicFrameDuration` 硬编码常量，改为在插值计算处读取 `FrameConfig.LogicFrameDurationSeconds`

## 5. 验证

- [x] 5.1 确认编译通过，运行项目验证逻辑帧推进、动画状态流转、插值表现均正常

## 6. FrameId 全局帧计数器

- [x] 6.1 在 `FrameConfig.cs` 中添加 `public static long FrameId { get; internal set; }`
- [x] 6.2 在 `FrameSyncModule.FixedUpdateWorlds` 中添加 `FrameConfig.FrameId++`
- [x] 6.3 确认编译通过
