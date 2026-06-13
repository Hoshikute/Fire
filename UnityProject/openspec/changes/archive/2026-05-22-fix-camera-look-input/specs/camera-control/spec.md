## ADDED Requirements

### Requirement: 相机视角旋转输入

第三人称相机系统 SHALL 通过 CinemachinePOV 组件响应鼠标移动输入来旋转相机视角。

#### Scenario: 鼠标移动控制相机旋转
- **WHEN** 玩家移动鼠标且光标处于锁定状态
- **THEN** 相机视角 SHALL 根据鼠标移动方向和距离进行旋转

#### Scenario: 光标未锁定时忽略输入
- **WHEN** 光标未锁定（Cursor.lockState != Locked）
- **THEN** 相机视角 SHALL 不响应鼠标移动输入

### Requirement: CinemachinePOV 组件配置

虚拟相机 SHALL 配置 CinemachinePOV 组件以支持视角旋转控制。

#### Scenario: 动态添加 POV 组件
- **WHEN** 虚拟相机上不存在 CinemachinePOV 组件
- **THEN** 系统 SHALL 自动添加 CinemachinePOV 组件并配置默认参数

#### Scenario: POV 组件输入绑定
- **WHEN** CinemachineLookInputBridge 组件初始化
- **THEN** 系统 SHALL 将 InputModule.Look 输入绑定到 CinemachinePOV 的水平和垂直轴
