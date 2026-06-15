## 1. 基线排查

- [x] 1.1 确认当前 Game 场景运行日志仍显示 `CameraModule` 找到 `CameraController`，并将其绑定到本地玩家的 `LookAt` 目标。
- [x] 1.2 确认 `InputSystem_Actions` 仍将 `Look` 映射到 `<Pointer>/delta`，并且 `InputModule.OnLook` 会更新 `GameModule.Input.Look`。
- [x] 1.3 确认当前没有 `GameLogic` 系统或相机组件消费 `GameModule.Input.Look` 来驱动 Cinemachine。
- [x] 1.4 检查 `CameraController.prefab` 和 `Game.unity` 是否存在与已删除相机桥接脚本相关的 missing-script 残留或 prefab instance 移除项。

## 2. 相机 Look 桥接

- [x] 2.1 在 `CameraModule` 中直接持有鼠标 Look 运行时更新逻辑，不新增独立旧式桥接组件。
- [x] 2.2 实现 `CameraModule` 对已绑定 virtual camera 的 `CinemachinePOV` 定位；缺失时动态添加并配置默认参数。
- [x] 2.3 将 `GameModule.Input.Look` 应用到 POV 水平/垂直轴值，并清空或绕过 Cinemachine 旧输入轴名。
- [x] 2.4 只在 `GameModule.Input.InputLocked == false` 且 `Cursor.lockState == CursorLockMode.Locked` 时应用视角输入。
- [x] 2.5 保持 Look 应用为表现层行为，避免写入任何回滚记录的 `MomentComponentBase` 或确定性玩家逻辑状态。
- [x] 2.6 增加清晰诊断，覆盖绑定成功、virtual camera 缺失、POV 动态添加、输入被抑制、非零 Look 已应用等情况，同时避免逐帧刷屏。

## 3. Prefab 与场景清理

- [x] 3.1 从 `CameraController.prefab` 移除或替换陈旧的 `ThirdPersonController` 相机脚本引用。
- [x] 3.2 更新 `Game.unity` 中的 `CameraController` prefab instance，确保它不再保留已删除桥接脚本的移除项或 missing-script 残留。
- [x] 3.3 保留现有 Cinemachine follow/look-at 绑定、framing transposer、collider、距离和光标锁定行为，除非桥接实现明确替代它们。
- [x] 3.4 验证场景不再从相机控制路径输出 missing-script 警告。

## 4. 验证

- [x] 4.1 运行与 HotFix/GameLogic 改动规模匹配的静态编译/构建校验。
- [x] 4.2 运行 `openspec validate restore-camera-mouse-look-bridge --strict`。
- [ ] 4.3 在 Unity Play Mode 进入 Game 场景，验证光标锁定时移动鼠标会旋转相机。
- [ ] 4.4 验证输入锁定或光标解锁时，相机旋转会停止。
- [ ] 4.5 验证玩家移动、动画以及 FrameSync 回滚记录组件不会被相机 Look 输入修改。
- [x] 4.6 如实报告验证结果，明确区分静态/构建校验与实际 Unity 运行时观察。
