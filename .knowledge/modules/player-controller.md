# 玩家控制器 (Player Controller)

## 一句话
第三人称角色控制器，命名空间 `ThirdPersonController`，以 `Player`（继承 `CharacterBase`）为核心，用 TEngine FSM 管理移动/跳跃/攀爬等状态，用 Animancer 播放动画。

## 关键文件
- `Player/Controller/Core/Player/Player.cs` — 控制器入口（Awake 建状态机、绑相机、持有 Animancer/ReusableData/ReusableLogic）
- `Player/Controller/Core/CharacterBase/CharacterBase.cs` — 角色基类
- `Player/Controller/Core/Player/Data/PlayerReusableData/PlayerReusableData.cs` — 跨状态共享的运行时数据
- `Player/Controller/Core/Player/State/PlayerReusableLogic.cs` — 跨状态复用的逻辑
- `Player/Controller/Core/Player/Data/PlayerStateDataSO/PlayerSO.cs` — 配置数据（ScriptableObject）
- `Player/Controller/Camera/CameraController.cs` — 相机控制（配合 Cinemachine）

## 入口与生命周期（Player.cs）
- `[RequireComponent(typeof(AnimancerComponent))]`，Awake 中：
  1. **相机引用三层回退**：序列化字段 `_cameraTransform` → `GameModule.Camera.MainCamera` → `Camera.main`，确保 `CamTransform` 不为 null。
  2. 实际相机绑定延迟到 `Start`（`_pendingCameraBind`），等 CameraModule 初始化完成。
  3. 取 `AnimancerComponent`，构造 `ReusableData` / `ReusableLogic`。
  4. **用 `GameModule.Fsm.CreateFsm("PlayerFSM", this, ...)` 注册全部状态**，`StateMachine.Start<PlayerIdleState>()` 进入默认空闲态。

## 状态清单（注册在 Player.Awake）
Idle / MoveStart / MoveLoop / MoveEnd / Jump / Climb / LedgeClimb / MoveToWall / FallLoop / PlatformerUp / Land / OutPlaceJump / LockIdle。
> 状态文件在 `Player/Controller/Core/Player/State/PlayerMovemenState/`（注意目录名拼写为 `Movemen`）。

## 数据组织
- **PlayerSO**（ScriptableObject）：各状态的可配置参数（如 `PlayerIdleData`、`PlayerJumpData`、`PlayerClimbData`…），状态在 `OnInit` 里从 `playerSO.playerMovementData.XxxData` 取。
- **PlayerReusableData**：运行时跨状态共享的可变数据（如当前 idle 索引、是否锁定、IsOnGround 等 BindableProperty）。
- **BindableProperty**：可绑定属性（`Player/Controller/Tool/BindableProperty/`），状态通过 `ValueChanged += xxx` 订阅变化驱动状态切换。

## 注意事项 / 坑
- 相机为 null 风险高，已用三层回退 + 延迟绑定缓解，改动相机初始化时注意时序。
- 状态切换不要在 Animancer 回调里直接做（见 animancer-fsm.md 的延迟切换）。
- 这是**表现层**（ThirdPersonController），与帧同步逻辑层（GameLogic）分离；不要在这里塞确定性逻辑。

## 相关代码位置
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Player/Controller/`

## 关联文档
- 动画状态机：`modules/animancer-fsm.md`
- 规范：`conventions/coding-style.md`
