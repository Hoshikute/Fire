## 1. P0 — 工具类迁移到 TEngine Utility

- [x] 1.1 在 TEngine Utility 目录创建 `MonoSingleton.cs`，从 `ThirdPersonController.Tool.Singleton` 迁移，namespace 改为 `TEngine.Utility`
- [x] 1.2 在 TEngine Utility 目录创建 `NoMonoSingleton.cs`，从 `ThirdPersonController.Tool.Singleton` 迁移，namespace 改为 `TEngine.Utility`
- [x] 1.3 在 TEngine Utility 目录创建 `BindableProperty.cs`，从 `ThirdPersonController.Tool.BindableProperty` 迁移，namespace 改为 `TEngine.Utility`
- [x] 1.4 在 TEngine Utility 目录创建 `ToolFunction.cs`，从 `ThirdPersonController.Tool.ToolFunction` 迁移，namespace 改为 `TEngine.Utility`
- [x] 1.5 更新所有原 `using ThirdPersonController.Tool.*` 的引用点改为 `using TEngine.Utility`，验证 dotnet build 0 错误
- [x] 1.6 验证帧同步逻辑层（`GameLogic` 命名空间）无 `BindableProperty` 引用

## 2. P1 — Character 模块去 TPC 化

- [x] 2.1 重命名 `ICharacterModule.SetThirdPersonPlayerPrefab` → `SetCharacterPrefab(string location, GameObject prefab)`，`LoadThirdPersonPlayerAsync` → `LoadCharacterAsync(string location)`
- [x] 2.2 更新 `CharacterModule` 实现：内部成员去 `ThirdPersonPlayer` 前缀，私有字段/方法改为通用命名
- [x] 2.3 更新 `GameModule.cs` 门面注册和 `Context/TPBattleContext.cs` 中的调用点，验证 dotnet build 0 错误

## 3. P2-3 — 确定性斜坡

- [x] 3.1 扩展 `IDeterministicGround` 接口：新增 `GetNormal(int x, int z): SyncVector3` 方法，`FlatGround` 返回垂直法线
- [x] 3.2 在 `PlayerMoveSystem` 中实现坡度检测逻辑：查询地面法线，计算坡度角（定点），若超过阈值（默认 45°）阻止水平移动
- [x] 3.3 在 `PlayerMoveSystem` 中实现斜面投影位移：水平位移沿法线投影到切平面，使角色沿斜面移动

## 4. P2-4 — 确定性碰撞

- [x] 4.1 定义 `ICollisionWorld` 接口：`CapsuleSweep()` / `CapsuleOverlap()`，返回 `CollisionResult`（hit/normal/penetration/point）
- [x] 4.2 实现默认碰撞世界（AABB 包围盒列表或空实现），在 `PlayerWorld` 中注册
- [x] 4.3 在 `PlayerMoveComponent` 中增加胶囊碰撞参数：`capsuleRadius`、`capsuleHeight`（定点整数）
- [x] 4.4 在 `PlayerMoveSystem.Step` 中集成碰撞检测：水平位移后扫掠，执行分离+滑动响应

## 5. P2-5 — 确定性攀爬/翻越

- [x] 5.1 验证 `PlayerLogicState` 枚举已包含 Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall（P2-2 已完成）
- [x] 5.2 定义 `ClimbTrajectory` 数据结构（逻辑帧索引 → SyncVector3 delta 表）和 `ClimbConfig` ScriptableObject
- [x] 5.3 在 `PlayerStateSystem` 中实现攀爬/翻越触发条件检测（前方障碍物高度+距离判定）
- [x] 5.4 在 `PlayerMoveSystem`（或新的 `PlayerClimbSystem`）中实现轨迹推进逻辑：每逻辑帧按 `ClimbTrajectory[framesInState]` 更新 pos

## 6. P2-6 — 表现层动画驱动

- [x] 6.1 创建 `PlayerAnimConfig` ScriptableObject：包含所有 `PlayerLogicState` 到 `TransitionAsset` 的映射
- [x] 6.2 创建 `PlayerAnimViewSystem`（继承 `ViewSystemBase`）：过滤 `PlayerStateComponent`+`PlayerViewComponent`，读状态枚举驱动 Animancer.Play
- [x] 6.3 在 `PlayerFrameSyncEntry` 中注入 `PlayerAnimConfig` 到 `PlayerViewComponent.animConfig`，验证动画随状态枚举切换

## 7. P2-7 — 移动手感打磨

- [x] 7.1 实现定点惯性系统：松开输入后 `moveSpeed` 按衰减曲线递减（指数衰减，定点参数可配置）
- [x] 7.2 实现定点转向插值：`faceDir` 每逻辑帧以最大旋转角逐步逼近目标方向（定点 Slerp 或等效近似）
- [x] 7.3 实现定点加减速曲线：从静止到满速逐步加速（加速率可配置），空中惯性系数（`AirControlFixed`）调优

## 8. P3 — 动画降为纯表现确认

- [x] 8.1 审查所有 Animancer 调用路径：确认无反向驱动逻辑位移的代码（`Animator.deltaPosition` 等不进逻辑层）
- [x] 8.2 确认 `PlayerAnimViewSystem` 的 Animancer 回调（OnEnd 等）中无逻辑状态变更

## 9. P4 — TPBattleContext 切换到 FrameSync

- [x] 9.1 在 `PlayerFrameSyncEntry` 中新增 `SetAnimConfig()` 和 `SetLookAtTarget()` 公开方法，支持运行时注入
- [x] 9.2 修改 `TPBattleContext.cs`：用 `Character.SetCharacterPrefab` + `LoadCharacterAsync` 加载，注入配置到 `PlayerFrameSyncEntry`
- [x] 9.3 验证战斗场景启动后相机绑定正常、角色移动正常，dotnet build 0 错误

## 10. P5 — 删除老 ThirdPersonController

- [x] 10.1 删除 `GameLogic/Player/Controller/` 目录（40 个 .cs 文件）
- [x] 10.2 修改 `Assets/AssetRaw/Actor/Player.prefab`：移除 `ThirdPersonController.Player` 脚本挂载，替换为 `PlayerFrameSyncEntry`（需在 Unity Editor 中手动操作）
- [x] 10.3 清理项目中所有 `using ThirdPersonController` 引用（替换为 `using TEngine.Utility` 或删除）
- [x] 10.4 清理 `PlayerFrameSyncEntry.cs` 头部过时注释（"老 TPC 仍可独立存在"），验证 dotnet build 0 错误
