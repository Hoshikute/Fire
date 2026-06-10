# 玩家帧同步 ECS（Player FrameSync ECS）

## 一句话
完全替代老 `ThirdPersonController` 的角色控制系统：逻辑层用确定性 ECS（定点数 `SyncVector3` + 200ms 逻辑帧 + 可回滚 `MomentComponentBase`）做移动/物理/状态推导；表现层渲染帧只读逻辑状态，Lerp 驱动 Transform + Animancer 播动画。老 TPC 已删除。

## 目录与文件（当前完整树）

```
Module/FrameSync/
├── Calc/                             ← 确定性数学
│   ├── SyncVector3.cs                  定点向量（int, SCALE=1000, Equals/Normalized/Sqrt/DeepCopy）
│   └── HashExtensions.cs               字符串→int 稳定哈希（跨端一致）
├── Core/
│   ├── WorldBase.cs                    世界容器（Loop/FixedLoop/Recalc/Record/Revert）
│   ├── ClientTime.cs                   客户端毫秒时间
│   └── SyncRule.cs                     同步规则枚举（Status/Frame）
├── ECS/                              ← 框架基类
│   ├── EntityBase.cs                   实体（ID + 组件字典）
│   ├── ComponentBase.cs                组件基类
│   ├── MomentComponentBase.cs          可回滚组件（Frame/ID/DeepCopy）
│   ├── SingletonComponent.cs           单例组件（全局唯一）
│   ├── SystemBase.cs                   系统基类（GetFilter→遍历实体 → FixedUpdate）
│   └── ViewSystemBase.cs               表现层系统（SystemBase子类, Update 渲染帧驱动）
├── GameLogic/                        ← 确定性逻辑层
│   ├── Component/
│   │   ├── PlayerInputComponent.cs     单例：当前帧输入意图（moveDir/jump/toggleLock）
│   │   ├── PlayerMoveComponent.cs      可回滚：pos/faceDir/verticalSpeed/isOnGround/
│   │   │                                currentSpeed/capsuleRadius/capsuleHeight/speedGear
│   │   ├── PlayerStateComponent.cs     可回滚：state/framesInState/prevState/
│   │   │                                isLocked/platformJumpRequested/wallObstructType
│   │   ├── ClimbFrameDelta.cs          攀爬逐帧位移结构（x/y/z → SyncVector3.ToDelta()）
│   │   ├── PlayerComponent.cs          通用玩家数据（生命/分数等，可回滚）
│   │   ├── MoveComponent.cs            泛用移动组件
│   │   ├── TransformComponent.cs       泛用变换组件
│   │   └── CommandComponent.cs         指令组件
│   ├── System/
│   │   ├── PlayerMoveSystem.cs        逻辑帧 200ms：定点积分位移/跳跃/重力/走跑双档/
│   │   │                                空中惯性/斜面投影/碰撞扫掠/攀爬轨迹/加减速/平滑转向
│   │   └── PlayerStateSystem.cs        逻辑帧 200ms：根据物理事实推导状态枚举
│   │                                     (Idle/MoveStart/MoveLoop/MoveEnd/Jump/Fall/Land/
│   │                                      LockIdle/Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall)
│   ├── Collision/
│   │   ├── ICollisionWorld.cs          确定性碰撞接口（CapsuleSweep/CapsuleOverlap）
│   │   ├── SimpleCollisionWorld.cs     AABB 包围盒碰撞世界（线段→AABB 距离检测）
│   │   └── CollisionResult.cs          碰撞结果（hit/normal/penetration/point）
│   ├── Ground/
│   │   ├── IDeterministicGround.cs     确定性地面接口（SampleHeight/GetNormal/IsGrounded）
│   │   └── FlatGround.cs              恒定高度平地实现（GetNormal 返回 (0,ONE,0)）
│   └── World/
│       └── PlayerWorld.cs             组装：GetSystemTypes（6 个系统）+ GetRecordTypes（2 个组件）
├── ClientLogic/                      ← 表现层
│   ├── Component/
│   │   └── PlayerViewComponent.cs      表现组件：viewRoot/animancer/animConfig/
│   │                                     lastLogicPos/prevLogicPos/interpT/initialized/animInitialized
│   ├── System/
│   │   ├── PlayerInputCollectSystem.cs 渲染帧：采集 Unity 输入→定点写 PlayerInputComponent/
│   │   │                                 speedGear 写 PlayerMoveComponent
│   │   ├── PlayerViewSystem.cs         渲染帧：两快照 Lerp(prevLogicPos→lastLogicPos, interpT)
│   │   │                                 + 3m snap 跳变保护 + 30% 速度外推 + 朝向 Slerp
│   │   └── PlayerAnimViewSystem.cs     渲染帧：读 PlayerStateComponent.state →
│   │   │                                 查 PlayerAnimConfig → Animancer.Play(TransitionAsset)
│   │   │                                 首帧无条件播放（animInitialized），后续状态切换触发
│   ├── Config/
│   │   └── PlayerAnimConfig.cs         ScriptableObject：14 个 TransitionAsset 字段
│   ├── ClimbConfig.cs                  ScriptableObject：Vault/Climb/LedgeClimb/PlatformerUp 逐帧位移表
│   └── PlayerFrameSyncEntry.cs        MonoBehaviour 入口：创建 PlayerWorld+玩家实体+相机绑定+
│                                        SetAnimConfig/SetLookAtTarget 运行时注入
└── Utility/
    └── PlayerMathUtil.cs               定点角度/输入转 SyncVector3 工具
```

## 组件/系统全景表

| 组件 | 基类 | 可回滚 | 所在层 | 职责 |
|------|------|--------|--------|------|
| `PlayerInputComponent` | `SingletonComponent` | ❌ 单例 | GameLogic | 当前帧输入（moveDir/jump/toggleLock） |
| `PlayerMoveComponent` | `MomentComponentBase` | ✅ | GameLogic | pos/faceDir/verticalSpeed/currentSpeed/capsule |
| `PlayerStateComponent` | `MomentComponentBase` | ✅ | GameLogic | state/framesInState/prevState/辅助字段 |
| `PlayerViewComponent` | `ComponentBase` | ❌ 不可进快照 | ClientLogic | viewRoot/animancer/animConfig/插值状态 |

| 系统 | 基类 | 执行时机 | 职责 |
|------|------|----------|------|
| `PlayerInputCollectSystem` | `ViewSystemBase` | 渲染帧 Update | 采集 Unity 输入→定点写入 |
| `PlayerMoveSystem` | `SystemBase` | 逻辑帧 FixedUpdate | 定点积分位移+跳跃+重力+碰撞+攀爬+加减速 |
| `PlayerStateSystem` | `SystemBase` | 逻辑帧 FixedUpdate | 物理事实→状态枚举推导 |
| `PlayerViewSystem` | `ViewSystemBase` | 渲染帧 Update | 两快照 Lerp 位置+Slerp 朝向+外推 |
| `PlayerAnimViewSystem` | `ViewSystemBase` | 渲染帧 Update | 状态枚举→Animancer.Play(TransitionAsset) |

**注册顺序 = 调用顺序**（在 `PlayerWorld.GetSystemTypes()`）：
```
PlayerInputCollectSystem → PlayerMoveSystem → PlayerStateSystem → PlayerViewSystem → PlayerAnimViewSystem
```

## 数据流（一帧因果链）

```
渲染帧 Update（每个 Unity frame, ~16ms）:
  PlayerInputCollectSystem → 采集 Unity Input(Vector2/按键)
    → 定点化+相机修正 → PlayerInputComponent(单例, moveDir/jump/toggleLock)
    → speedGear → PlayerMoveComponent
  PlayerViewSystem → 读 PlayerMoveComponent(只读) + 读 PlayerViewComponent
    → 检测逻辑帧推进(move.pos != lastLogicPos) → prevLogicPos=旧, interpT=0
    → interpT += dt/0.2s
    → Lerp(prevLogicPos, lastLogicPos+外推, interpT) → viewRoot.position
    → Slerp(viewRoot.rotation, target, dt*12) → viewRoot.rotation
  PlayerAnimViewSystem → 读 PlayerStateComponent + PlayerViewComponent
    → animInitialized?false → PlayAnim(Idle), 设 true
    → state!=prevState? → PlayAnim(newState)

逻辑帧 FixedUpdate（200ms 间隔）:
  FrameSyncModule.Update → while(累积>200ms) → World.FixedLoop(200)
    → Record(FrameCount) → 快照 PlayerMoveComponent+PlayerStateComponent
    → PlayerMoveSystem.FixedUpdate:
       读 PlayerInputComponent.moveDir/jump
       ├─ 交互状态?(Vault/Climb/...) → 跳过水平输入, 推进 ClimbTrajectory[framesInState]
       ├─ 核心移动: 加减速曲线 → 水平积分 → 坡面投影 → 碰撞扫掠+分离+滑动
       ├─ 跳跃: jump 边沿消费 + verticalSpeed=JumpSpeed
       └─ 重力: v+=g*dt, y+=v*dt, 落地→SampleHeight+归一
    → PlayerStateSystem.FixedUpdate:
       物理事实(isOnGround/verticalSpeed/moveDir) → Decide(state)
```

## 确定性要点（命根子）

- **全程 int / SyncVector3**：位移 `delta = speed × dtMs / 1000`（long 中转防溢出），方向/归一化/开方全部定点牛顿迭代
- **固定步长 200ms**：逻辑帧恒为 `FrameSyncModule.IntervalTime = 200`，不依赖 `Time.deltaTime`
- **逻辑与表现分离**：`Vector3.Lerp/Slerp`/`Animancer.Play` 只影响画面，不回写逻辑组件
- **回滚**: `PlayerMoveComponent`+`PlayerStateComponent` ∈ `GetRecordTypes()`，World 自动用 `RecordSystem<T>` 每帧 `DeepCopy()` 快照

## 渲染插值（视线平滑的核心）

`PlayerViewSystem` 不在两个 Transform 位置间 lerp——而是在**两个逻辑快照**间插值：

```
interpT += dt / 0.2s                           ← 每个渲染帧累加进度
position = Lerp(prevLogicPos, lastLogicPos + extrap, interpT)
```

| 机制 | 说明 |
|------|------|
| **两快照插值** | `prevLogicPos`→`lastLogicPos` 线性过渡，t 归一时必然到达，不 overshoot |
| **跳变保护** | 位置差 > 3m（回滚/传送）→ 直接 snap，不追逐 |
| **死推外推** | `interpT > 1.0` 时用 `faceDir × currentSpeed × extra × 30%` 向前微推，手感更跟手 |

## 动画配置

- **`PlayerAnimConfig`**（ScriptableObject）：14 个 `TransitionAsset` 字段（idle/moveStart/moveLoop/…/platformerUp），从旧 TPC `PlayerAnimacer/` 目录的 `.asset` 加载引用
- **首帧播放**：`PlayerViewComponent.animInitialized` 标记防止 `state==prevState`（均为 `Idle`）导致跳过
- **`ClimbConfig`**（ScriptableObject）：4 个 `List<ClimbFrameDelta>` 逐帧位移表

## 注意事项 / 坑

- **`TEngine.Utility` 命名空间冲突**：TEngine 已有 `static partial class Utility`，工具类只能放 `namespace TEngine`
- **动画首帧不播放**：`SpawnPlayer` 时 `state=Idle, prevState=Idle`，`PlayerAnimViewSystem` 曾用 `state==prevState` 跳过——已通过 `animInitialized` 首帧无条件播放修复
- **`TransitionAsset` vs `ClipTransition`**：`PlayerAnimConfig` 字段类型应为 `TransitionAsset`（可直接 `Play(asset)`），原 `ClipTransition` 导致创建 SO 时无法序列化嵌套引用
- **实体延迟就绪**：`CreateEntity` 进入 createCache，下一 `FixedLoop` 的 `LazyExecuteEntityOperation` 才真正入世界——首帧容忍无实体
- **`PlayerViewComponent` 不进快照**：继承 `ComponentBase`（非 `MomentComponentBase`），持有 `Transform`/`AnimancerComponent` 等 UnityEngine 引用，是逻辑/表现的硬边界
- **`PlayerInputCollectSystem` 写 `PlayerMoveComponent.speedGear`**：可回滚组件在渲染帧被写入，当前单机路径可行；接真实回滚时需确保回滚重算中 `speedGear` 也正确恢复

## 相关代码位置
- 逻辑层：`Module/FrameSync/GameLogic/`
- 表现层：`Module/FrameSync/ClientLogic/`
- 框架基类：`Module/FrameSync/ECS/`
- 世界循环：`Module/FrameSync/Core/WorldBase.cs`
- 主循环：`Module/FrameSync/FrameSyncModule.cs`

## 关联文档
- 帧同步循环：`architecture/lockstep.md`
- ECS 框架：`architecture/ecs.md`
- 回滚：`architecture/rollback.md`
- 网络/指令：`architecture/network-sync.md`
- 规范：`conventions/coding-style.md`
- 迁移决策：`decisions/0002-tpc-to-framesync-migration.md`
- 迁移踩坑：`decisions/0004-tpc-migration-lessons.md`
- 旧 TPC（已删）：`modules/player-controller.md`
