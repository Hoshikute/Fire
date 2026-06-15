## Why

Game 场景中相机已经能被找到、绑定到玩家 `LookAt` 节点，并且光标也进入锁定状态，但移动鼠标不再能控制视角转向。
此前用于修复该问题的 `CinemachineLookInputBridge` 在动画/玩家控制器重构时被删除，导致 `GameModule.Input.Look` 仍然被 TEngine 输入模块采集，却没有被当前 `CameraModule` 相机链路消费。

## What Changes

- 恢复 Game 场景的鼠标视角控制，由 `CameraModule` 直接持有运行时更新逻辑，并将 TEngine `GameModule.Input.Look` 应用到当前绑定的 Cinemachine POV 相机路径。
- 将修复落在当前 `GameLogic`/TEngine 相机模块架构中，不恢复旧 `ThirdPersonController` 的运行时所有权。
- 在应用鼠标视角增量前，必须尊重相机绑定状态、光标锁定状态和输入锁定状态。
- 当绑定的 virtual camera 缺少 `CinemachinePOV` 时，像旧桥接一样运行时动态添加并配置默认参数。
- 清理或替换场景/prefab 中对已删除相机桥接脚本的陈旧引用，避免 missing-script 警告掩盖真实相机状态。
- 增加聚焦的诊断和验证步骤，证明 Look 输入确实到达 Cinemachine，同时不改变确定性移动或回滚逻辑。

## Capabilities

### New Capabilities
- `camera-mouse-look`: 运行时游戏相机鼠标视角控制。该能力由 `CameraModule` 持有更新逻辑，读取 TEngine 输入并应用到 Cinemachine，同时保持 FrameSync 逻辑层与表现层分离。

### Modified Capabilities

无。

## Impact

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/CameraModule/`
- `UnityProject/Assets/TEngine/Runtime/Module/InputModule/`
- `UnityProject/Assets/AssetRaw/ThirdPersonController/AnimancerController/Resources/Prefab/CameraController.prefab`
- `UnityProject/Assets/AssetRaw/Scenes/Game.unity`
- Game 场景使用的 Cinemachine virtual camera / POV 配置
