## Context

当前 Game 场景的初始化链路是成功的：`TPBattleContext` 设置主相机、加载本地玩家、解析玩家子节点 `LookAt`，并调用 `CameraModule.BindCinemachineToPlayer`。
运行日志也确认 `CameraController` 被找到、完成绑定，并且光标已锁定。

故障发生在绑定之后。
TEngine `InputModule` 仍然会从 `InputSystem_Actions` 的 `<Pointer>/delta` 采集 `Look`，并写入 `GameModule.Input.Look`，但当前 `CameraModule` 只负责查找、绑定和锁定光标，没有在运行时消费这个值来驱动相机旋转。
之前可工作的修复是 `ThirdPersonController.CinemachineLookInputBridge`：它在 `LateUpdate` 里读取 `GameModule.Input.Look`，然后直接更新 `CinemachinePOV` 的轴值。
旧桥接还会在 virtual camera 缺少 `CinemachinePOV` 时调用 `AddCinemachineComponent<CinemachinePOV>()` 动态补齐组件。
这个桥接脚本和相机距离脚本在旧 `Player/Controller/Camera` 脚本删除时一起被移除，当前 `Game.unity` 的 `CameraController` prefab instance 又移除了残留的桥接组件。

当前玩家控制器已经迁移到 FrameSync ECS。
鼠标视角控制只属于表现层，必须留在确定性移动、回滚快照和固定 200 ms 逻辑帧之外。

## Goals / Non-Goals

**Goals:**
- 在玩家相机绑定后恢复 Game 场景运行时鼠标视角旋转。
- 将 Look 到 Cinemachine 的桥接迁移到当前 `GameLogic`/TEngine 模块架构中，并由 `CameraModule` 直接持有更新逻辑。
- 尊重 `GameModule.Input.InputLocked`、光标锁定状态，以及相机/POV 缺失状态。
- 当绑定相机缺少 `CinemachinePOV` 时，运行时自动添加并配置默认参数。
- 将相机视角增量保持为 Unity/Cinemachine 表现层状态。
- 清理场景或 prefab 中对已删除相机桥接脚本的陈旧引用。
- 提供足够诊断，用来区分“相机未绑定”、“POV 缺失”和“Look 输入未应用”。

**Non-Goals:**
- 不恢复已删除的 `ThirdPersonController` 命名空间或旧玩家控制器所有权。
- 不修改确定性移动、`PlayerInputComponent` 回滚行为或逻辑帧时序。
- 除非鼠标视角功能必须依赖，否则不重设计 Cinemachine 构图、碰撞或缩放。
- 不替换 TEngine `InputModule`，也不引入新的输入抽象。

## Decisions

1. **由 `CameraModule` 直接桥接 `GameModule.Input.Look` 到 Cinemachine。**

   当前 TEngine 约定是通过 `GameModule` 访问模块，历史上可工作的桥接脚本也证明 `GameModule.Input.Look` 是正确输入来源。
   当前 `CameraModule` 已经保存 `_virtualCamera`，并负责 `BindCinemachineToPlayer`、`SetMainCamera` 和 gameplay 光标锁定，因此它是鼠标视角运行时更新的唯一所有者。
   实现应在 `CameraModule` 的 Unity 更新生命周期中读取 `GameModule.Input.Look`，并把增量应用到当前绑定的 virtual camera。
   这样可以让行为贴近相机生命周期，同时避免新增旧 TPC 风格桥接组件，或把相机职责泄漏到确定性 ECS 系统。

   备选方案：
   - 新增当前命名空间下的独立 `GameLogic` 相机桥接 MonoBehaviour：拒绝。所有权已经确定为 `CameraModule`，避免再次出现 prefab instance 移除桥接组件导致输入链路断开的回归。
   - 重新创建 `ThirdPersonController.CinemachineLookInputBridge`：拒绝。旧命名空间已经明确删除，不应重新获得运行时所有权。
   - 把鼠标视角逻辑放进 `PlayerInputCollectSystem`：拒绝。该系统负责写入确定性移动输入意图，不应拥有 Cinemachine 状态。
   - 只依赖 Cinemachine 旧轴名 `Mouse X` / `Mouse Y`：拒绝。项目已经有 TEngine 输入模块，历史修复也刻意避开了旧轴处理链路的不确定性。

2. **定位或动态添加 `CinemachinePOV`，再直接驱动轴值。**

   `CameraModule` 应先从当前绑定 virtual camera 定位可用的 `CinemachinePOV`。
   如果缺少该组件，应像旧桥接一样调用 `AddCinemachineComponent<CinemachinePOV>()` 动态添加，并配置默认参数。
   配置完成后清空或忽略 POV 的旧输入轴名，并将鼠标增量应用到水平和垂直轴 `Value`。
   垂直旋转必须受 POV 轴范围或与相机配置一致的显式安全范围限制。

   备选方案：
   - 根据鼠标增量旋转玩家或 `LookAt` transform：拒绝。这容易把表现层输入反向写入 gameplay 相关 transform，改变移动语义。
   - 新增相机 ECS 组件：拒绝。相机视角不是回滚状态，也不需要 ECS 快照。

3. **把陈旧 prefab/script 引用视为修复的一部分。**

   `CameraController.prefab` 和 `Game.unity` prefab instance 里仍保留了已删除相机脚本的历史痕迹。
   实现时应移除 missing-script 残留，或替换为当前命名空间的新桥接组件，避免 Unity warning 掩盖真正的相机回归。

   备选方案：
   - 只要相机代码可工作就保留陈旧组件：拒绝。当前运行日志已经出现 missing-script 警告，会让后续排查成本变高。

## Risks / Trade-offs

- [风险] 场景里存在多个 Cinemachine virtual camera，可能全局搜索选错相机 -> 缓解：只绑定/配置 `CameraModule` 实际保存并打印日志的同一个 virtual camera，并在诊断中包含相机名。
- [风险] UI 或模态窗口锁定输入时仍应用鼠标增量 -> 缓解：修改 POV 前必须满足 `!GameModule.Input.InputLocked` 且 `Cursor.lockState == CursorLockMode.Locked`。
- [风险] 配置的相机缺少 `CinemachinePOV` -> 缓解：由 `CameraModule` 运行时动态添加 POV，配置默认参数，并输出一次性诊断日志。
- [风险] 场景 prefab override 再次移除旧桥接残留或相机配置 -> 缓解：实施时同时检查并更新 prefab 与 Game 场景实例；核心更新逻辑不依赖独立桥接组件。
- [风险] 静态校验通过但运行时仍依赖 Unity Play Mode 行为 -> 缓解：要求 Unity Play Mode 检查；如果只跑了静态/构建校验，必须如实说明。

## Migration Plan

1. 在 `CameraModule` 中新增运行时 Look 更新逻辑，并保存/刷新当前绑定 virtual camera 的 POV 引用。
2. 在 `CameraModule` 找到/绑定 virtual camera 时定位 `CinemachinePOV`；缺失时动态添加并配置默认参数。
3. 从 `CameraController.prefab` 和 `Game.unity` 清理陈旧 missing-script 引用。
4. 验证光标锁定时，移动鼠标会改变 Cinemachine POV 轴值。
5. 验证 UI/input lock 会阻止相机旋转，且玩家移动不受影响。

回滚策略：如果运行时相机行为回归，移除新的桥接接线，并通过版本控制恢复此前相机 prefab/scene 状态。

## Open Questions

- 是否沿用旧灵敏度 `0.2f` 每像素，还是在新桥接上暴露序列化/配置化灵敏度？
