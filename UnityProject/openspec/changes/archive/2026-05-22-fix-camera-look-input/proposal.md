# Change: 修复鼠标移动无法控制相机旋转

## Why

鼠标移动输入被 InputModule 正确接收（日志显示 OnLook 有值），但相机不会旋转。原因是 `CinemachineLookInputBridge` 尝试获取 `CinemachinePOV` 组件失败，虚拟相机只有 `CinemachineFramingTransposer`，缺少 POV 组件。

## What Changes

- 在虚拟相机上添加 `CinemachinePOV` 组件（或通过代码动态添加）
- 确保 `CinemachineLookInputBridge` 能正确获取并控制 POV 组件
- 验证光标锁定状态下相机旋转功能正常

## Impact

- 受影响文件：
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Camera/CinemachineLookInputBridge.cs`
  - `Assets/GameScripts/HotFix/GameLogic/Player/Controller/Camera/CameraController.cs`（可能需要修改）
- 受影响功能：第三人称相机控制

## Root Cause Analysis

**输入链路**：
```
鼠标移动 → InputModule.OnLook → _lookValue → CinemachineLookInputBridge.LateUpdate → CinemachinePOV
```

**断点**：
```csharp
// CinemachineLookInputBridge.cs:34-44
_pov = GetComponentInChildren<CinemachinePOV>();  // 返回 null
if (_pov == null)
{
    Debug.LogWarning("未找到 CinemachinePOV，相机视角输入桥接将失效。");
    return;
}
```

**虚拟相机当前配置**：
- `CinemachineFramingTransposer` ✓（控制跟随和距离）
- `CinemachinePOV` ✗（缺失，控制视角旋转）
