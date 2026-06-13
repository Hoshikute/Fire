## Why

FrameSync 玩家现在已经能加载 `PlayerAnimConfig`，并且 Idle、站立姿态、地面移动动画已经恢复正常；但运行日志显示跳跃逻辑进入了 `JumpInPlace -> Fall -> Land` 后，`PlayerAnimViewSystem` 因 `jumpInPlace`、`fallLoop`、`land` 映射为空而无法播放空中动画，画面会停留在上一段行走或待机姿势。

这个问题需要作为地面动画恢复之后的独立补齐项处理：它不是输入或状态机错误，而是旧 `Player SO.asset` 中的空中/落地动画资源还没有迁移到新的 FrameSync `PlayerAnimConfig` 聚合配置。

## What Changes

- 为 FrameSync 玩家补齐 Jump/Fall/Land 的动画资源映射，使 `Jump`、`JumpInPlace`、`Fall`、`Land` 状态进入时都能播放对应 Animancer 过渡，而不是沿用上一段动画姿势。
- 从现有旧配置和动画资源中迁移空中动画来源，包括原地起跳、前跳起跳、下落开始、下落循环、落地动画，以及必要的平台跳三阶段资源。
- 保持动画资源为表现层配置，不把动画资源、播放阶段或 Animancer 状态写入 `PlayerMoveComponent`、`PlayerStateComponent`、`PlayerInputComponent` 或回滚快照。
- 补强 `PlayerAnimConfig` 校验或诊断，让 Jump/Fall/Land 这类已恢复的运行路径不再只是运行时才暴露为空字段。
- 保留 Vault、Climb、LedgeClimb 等交互动画缺口为后续专项迁移，除非实现时发现已有明确可用的专用过渡资源。

## Capabilities

### New Capabilities

- `framesync-player-air-animation-config`: 定义 FrameSync 玩家空中和落地状态的动画配置、播放行为、资源迁移边界和诊断要求。

### Modified Capabilities

## Impact

- 资源资产：`UnityProject/Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 及可能新增的 Animancer `TransitionAsset` 资源。
- 表现层代码：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/Config/PlayerAnimConfig.cs`、`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerAnimViewSystem.cs`。
- 证据来源：Unity Console 已确认 `Idle -> JumpInPlace -> Fall -> Land` 状态链正确，但当前配置缺少 `jumpInPlace`、`fallLoop`、`land`。
- 旧资源参考：`UnityProject/Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/Player SO.asset` 中的 `PlayerJumpFallAndLandData`。
