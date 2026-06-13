## ADDED Requirements

### Requirement: 游戏相机响应鼠标视角输入
系统 SHALL 由 `CameraModule` 在 Game 场景激活、玩家相机已绑定、输入未锁定且 gameplay 光标已锁定时，根据 TEngine `GameModule.Input.Look` 旋转已绑定的 gameplay Cinemachine 相机。

#### Scenario: 鼠标移动会旋转已绑定相机
- **WHEN** Game 场景已将 `CameraController` 绑定到本地玩家的 `LookAt` 目标，并且 `GameModule.Input.Look` 报告非零鼠标增量
- **THEN** `CameraModule` 必须更新激活的 Cinemachine POV 水平轴和/或垂直轴值，使其响应该增量并发生变化

#### Scenario: 移动输入与相机视角保持分离
- **WHEN** 玩家在同一个渲染帧内同时提供移动输入和鼠标视角输入
- **THEN** 移动输入必须继续驱动确定性玩家输入意图，鼠标视角输入必须只影响表现层相机状态

### Requirement: 相机视角尊重输入锁定和光标状态
系统 SHALL 在 TEngine 输入被锁定或 gameplay 光标未锁定时抑制鼠标视角相机旋转。

#### Scenario: 输入锁定阻止相机旋转
- **WHEN** `GameModule.Input.InputLocked` 为 true 且报告了鼠标增量
- **THEN** 鼠标视角桥接必须不修改激活的 Cinemachine POV 轴值

#### Scenario: 光标未锁定阻止相机旋转
- **WHEN** `Cursor.lockState` 不是 `CursorLockMode.Locked` 且报告了鼠标增量
- **THEN** 鼠标视角桥接必须不修改激活的 Cinemachine POV 轴值

### Requirement: 缺失相机视角配置自动补齐并可诊断
当相机已绑定但缺少鼠标视角所需的 Cinemachine 相机组件时，系统 SHALL 由 `CameraModule` 自动补齐可恢复配置并输出清晰诊断。

#### Scenario: 绑定时动态添加 POV 组件
- **WHEN** gameplay virtual camera 已绑定，但无法找到可用的 `CinemachinePOV`
- **THEN** `CameraModule` SHALL 自动添加 `CinemachinePOV`，配置默认输入参数，并记录一次性诊断日志标明相机和补齐结果

#### Scenario: POV 组件无法补齐时输出失败诊断
- **WHEN** gameplay virtual camera 已绑定，但 `CameraModule` 无法找到 virtual camera 或无法补齐 `CinemachinePOV`
- **THEN** 系统 SHALL 记录诊断日志，标明相机状态并说明无法应用鼠标视角旋转

#### Scenario: 不依赖已删除桥接脚本残留
- **WHEN** Game 场景启动并加载 `CameraController`
- **THEN** 相机 prefab/scene instance 必须不依赖已删除的 `ThirdPersonController` 相机脚本或 missing-script 组件来实现鼠标视角控制

### Requirement: 相机视角不进入回滚状态
系统 SHALL 将鼠标视角相机旋转保持在确定性 FrameSync 回滚状态之外。

#### Scenario: 相机视角在渲染更新中应用
- **WHEN** 鼠标视角输入被用于旋转相机
- **THEN** 不得仅因为该相机视角输入而修改任何 `MomentComponentBase` 或回滚记录的玩家组件
