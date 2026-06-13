## Why

FrameSync 玩家现在可以加载 `PlayerAnimConfig`，并且 Idle、Jump/Fall/Land 的缺失配置已经逐步补齐；但最新运行表现显示奔跑和下蹲动作仍然丢失。现有证据指向同一个迁移缺口：旧 `Player SO.asset` 依赖多层 Animancer mixer 参数（`StandValue`、`SpeedValue`、`RotationValue`、`LockValue`）选择站/蹲、走/跑和方向，而当前 `PlayerAnimConfig` 只接入了部分子资源，并且 `PlayerAnimViewSystem` 只在 Idle 时强制写入站立参数。

需要把地面移动动画从“能播放某个 clip”提升到“能按 FrameSync 输入和逻辑状态正确驱动旧 locomotion mixer”，避免逻辑速度已经进入跑步但画面仍停留在走路，或修复站立 Idle 后完全无法进入蹲伏表现。

## What Changes

- 恢复 FrameSync 玩家地面 locomotion 的 Animancer 参数驱动，使表现层能根据当前输入/状态设置 `StandValue`、`SpeedValue`，并在需要时处理 `RotationValue`、`LockValue`。
- 校正 `PlayerAnimConfig.asset` 的地面移动资源层级，避免把旧外层 mixer（例如 `PlayerMoveLoop.asset`）错误替换成只包含部分分支的内部 `NoneLock/*` 子资源。
- 明确跑步动画与逻辑 `speedGear` 的关系：Shift 进入跑步速度时，动画 mixer 也应切到跑步分支。
- 明确下蹲动作的恢复边界：先确认当前输入系统是否已有蹲伏输入；若没有，则把新增蹲伏输入/状态作为本变更需要补齐的最小逻辑契约或记录为显式后续项。
- 保持 FrameSync 确定性边界：动画 mixer 参数、Animancer state、资源引用只存在于表现层或配置层，不写入回滚快照组件。
- 保留 Jump/Fall/Land 空中动画配置为既有变更范围，不在本变更重复处理。

## Capabilities

### New Capabilities

- `framesync-player-locomotion-mixer-params`: 定义 FrameSync 玩家地面移动动画 mixer 参数、资源层级、跑步/下蹲表现和诊断要求。

### Modified Capabilities

## Impact

- 资源资产：`UnityProject/Assets/AssetRaw/Configs/PlayerAnimConfig.asset` 以及 `UnityProject/Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Config/PlayerAnimacer/` 下的地面移动/待机 TransitionAsset 引用。
- 表现层代码：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/System/PlayerAnimViewSystem.cs`。
- 输入与逻辑边界：可能涉及 `PlayerInputCollectSystem.cs`、`PlayerInputComponent.cs`、`PlayerStateComponent.cs`、`PlayerStateSystem.cs`，但仅在确认需要恢复蹲伏输入/状态时最小化扩展。
- 验证路径：Unity EditorSimulateMode 的 Game 场景中验证站立 Idle、走、跑、下蹲待机、下蹲移动、停止回到正确姿态，以及 Console 不出现 mixer 参数或配置缺失的刷屏 warning。
