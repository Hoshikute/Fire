# Tasks: 修复相机视角输入

## 1. 问题确认
- [x] 1.1 分析 Unity Console 日志，确认 InputModule.OnLook 有输入值
- [x] 1.2 追踪输入链路，定位断点在 CinemachineLookInputBridge
- [x] 1.3 确认虚拟相机缺少 CinemachinePOV 组件

## 2. 实现修复
- [x] 2.1 修改 CinemachineLookInputBridge，支持动态添加 CinemachinePOV 组件
- [x] 2.2 配置 POV 组件的默认参数（灵敏度、角度限制等）
- [x] 2.3 添加必要的日志输出便于调试
- [x] 2.4 将 CinemachineLookInputBridge 组件附加到 CameraController prefab
- [x] 2.5 禁用冲突的 InputAxisController 组件
- [x] 2.6 直接修改 POV Value 实现相机旋转控制

## 3. 验证测试
- [x] 3.1 在 Unity Editor 中运行游戏
- [x] 3.2 验证鼠标移动可以控制相机旋转
- [x] 3.3 验证光标锁定/解锁状态切换正常
- [x] 3.4 验证其他相机功能（滚轮缩放、跟随）不受影响
