## Context

`FrameConfig` 是帧同步专属配置，与 `FrameSyncModule` 强关联。放在 `Module/FrameSync/` 下更符合模块化原则。

## Goals / Non-Goals

**Goals:**
- 将 `FrameConfig.cs` 移到 `Module/FrameSync/` 目录

**Non-Goals:**
- 不修改文件内容（命名空间、类名、常量值均不变）
- 不涉及 `.meta` 文件的手动处理（Unity 自动管理）

## Decisions

**选择**: 直接移动文件，`namespace GameLogic` 保持不变

**理由**: `FrameSyncModule` 等引用方已在同一命名空间下，移动后无需修改 `using`。
