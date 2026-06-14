# 玩家帧同步 ECS（Player FrameSync ECS）

## 一句话
完全替代老 `ThirdPersonController` 的角色控制系统：`TPBattleContext` 作为 Game 场景启动入口，动态加载 Player prefab 后创建 `PlayerWorld` 和本地玩家实体；逻辑层用确定性 ECS（定点数 `SyncVector3` + 200ms 逻辑帧 + 可回滚 `MomentComponentBase`）做移动/物理/状态推导；表现层渲染帧只读逻辑状态，Lerp 驱动 Transform + Animancer 播动画。老 TPC 已删除。

## 目录与文件（当前完整树）

```
Context/
└── TPBattleContext.cs                  Game 场景入口：加载 Player prefab、创建 PlayerWorld、生成本地玩家实体、绑定相机、清理 world

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
│   │   ├── PlayerInputComponent.cs     单例：当前本地帧输入意图（moveDir/jump/toggleLock/speedGear）
│   │   ├── PlayerMoveComponent.cs      可回滚：pos/faceDir/moveIntentDir/speedGear/
│   │   │                                verticalSpeed/isOnGround/currentSpeed/capsule
│   │   ├── PlayerStateComponent.cs     可回滚：state/framesInState/prevState/
│   │   │                                isLocked/platformJumpRequested/wallObstructType
│   │   ├── ClimbFrameDelta.cs          攀爬逐帧位移结构（x/y/z → SyncVector3.ToDelta()）
│   │   ├── PlayerComponent.cs          玩家元数据（playerId/playerName/isLocal，不进回滚）
│   │   ├── MoveComponent.cs            泛用移动组件
│   │   ├── TransformComponent.cs       泛用变换组件
│   │   └── CommandComponent.cs         指令组件
│   ├── System/
│   │   ├── PlayerInputCommandSystem.cs 逻辑帧：本地输入→本地实体 CommandComponent/
│   │   │                                非本地实体按缓存/预测命令，重算时按帧补命令
│   │   ├── PlayerMoveSystem.cs        逻辑帧 200ms：定点积分位移/跳跃/重力/走跑双档/
│   │   │                                空中惯性/斜面投影/碰撞扫掠/攀爬轨迹/加减速/平滑转向
│   │   └── PlayerStateSystem.cs        逻辑帧 200ms：根据物理事实推导状态枚举
│   │                                     (Idle/MoveStart/MoveLoop/MoveEnd/Jump/Fall/Land/
│   │                                      LockIdle/Vault/Climb/LedgeClimb/PlatformerUp/MoveToWall)
│   ├── Collision/
│   │   ├── ICollisionWorld.cs          确定性碰撞接口（CapsuleSweep/CapsuleOverlap）
│   │   ├── SimpleCollisionWorld.cs     AABB 包围盒碰撞世界（整数步进扫掠 + 胶囊/AABB 分离）
│   │   └── CollisionResult.cs          碰撞结果（hit/normal/penetration/point）
│   ├── Ground/
│   │   ├── IDeterministicGround.cs     确定性地面接口（SampleHeight/GetNormal/IsGrounded）
│   │   └── FlatGround.cs              恒定高度平地实现（GetNormal 返回 (0,ONE,0)）
│   └── World/
│       └── PlayerWorld.cs             组装：GetSystemTypes（6 个系统）+ GetRecordTypes（2 个组件）
├── ClientLogic/                      ← 表现层
│   ├── Component/
│   │   └── PlayerViewComponent.cs      表现组件：viewRoot/animancer/animConfig/
│   │                                     lastLogicPos/prevLogicPos/interpT/initialized/animInitialized/
│   │                                     lastPlayedState/isCrouching
│   ├── System/
│   │   ├── PlayerInputCollectSystem.cs 渲染帧：采集 Unity 输入→定点写 PlayerInputComponent/
│   │   │                                 speedGear 写入 PlayerInputComponent，Crouch 写 PlayerViewComponent
│   │   ├── PlayerViewSystem.cs         渲染帧：两快照 Lerp(prevLogicPos→lastLogicPos, interpT)
│   │   │                                 + 3m snap 跳变保护 + 30% 速度外推 + 朝向 Slerp
│   │   └── PlayerAnimViewSystem.cs     渲染帧：读 PlayerStateComponent.state →
│   │   │                                 查 PlayerAnimConfig → Animancer.Play(TransitionAsset)
│   │   │                                 首帧无条件播放（animInitialized），后续状态切换触发
│   ├── Config/
│   │   └── PlayerAnimConfig.cs         ScriptableObject：14 个 TransitionAsset 字段
│   └── ClimbConfig.cs                  ScriptableObject：Vault/Climb/LedgeClimb/PlatformerUp 逐帧位移表
└── Utility/
    └── PlayerMathUtil.cs               定点角度/输入转 SyncVector3 工具
```

## 组件/系统全景表

| 组件 | 基类 | 可回滚 | 所在层 | 职责 |
|------|------|--------|--------|------|
| `PlayerInputComponent` | `SingletonComponent` | ❌ 单例 | GameLogic | 当前本地帧输入（moveDir/jump/toggleLock/speedGear） |
| `PlayerComponent` | `ComponentBase` | ❌ 元数据 | GameLogic | playerId/playerName/isLocal，本地输入采集只改 isLocal 玩家表现态 |
| `PlayerMoveComponent` | `MomentComponentBase` | ✅ | GameLogic | pos/faceDir/moveIntentDir/speedGear/verticalSpeed/currentSpeed/capsule |
| `PlayerStateComponent` | `MomentComponentBase` | ✅ | GameLogic | state/framesInState/prevState/辅助字段 |
| `PlayerViewComponent` | `ComponentBase` | ❌ 不可进快照 | ClientLogic | viewRoot/animancer/animConfig/插值状态/isCrouching 表现态 |
| `PlayerCommandRecordComponent` | `ComponentBase` | ❌ 指令缓存 | SyncLogic | 按逻辑帧记录/预测 `CommandComponent` |

| 系统 | 基类 | 执行时机 | 职责 |
|------|------|----------|------|
| `PlayerInputCollectSystem` | `ViewSystemBase` | 渲染帧 Update | 采集 Unity 输入→定点写入 |
| `PlayerInputCommandSystem` | `SystemBase` | 逻辑帧 Fixed 前 / Recalc | 本地输入→本地实体帧指令；非本地实体按缓存/预测命令 |
| `PlayerMoveSystem` | `SystemBase` | 逻辑帧 FixedUpdate | 按实体帧指令定点积分位移+跳跃+重力+碰撞+攀爬+加减速 |
| `PlayerStateSystem` | `SystemBase` | 逻辑帧 FixedUpdate | 按实体帧指令+物理事实→状态枚举推导 |
| `PlayerViewSystem` | `ViewSystemBase` | 渲染帧 Update | 两快照 Lerp 位置+Slerp 朝向+外推 |
| `PlayerAnimViewSystem` | `ViewSystemBase` | 渲染帧 Update | 状态枚举→Animancer.Play(TransitionAsset) |

`ViewSystemBase` 会 sealed 掉逻辑帧 / 回滚重算生命周期（`FixedUpdate`、`OnlyCallByRecalc`、`EndFrame` 等），只允许子类在渲染帧 `Update`/`LateUpdate` 做表现层工作。

**注册顺序 = 调用顺序**（在 `PlayerWorld.GetSystemTypes()`）：
```
PlayerInputCollectSystem → PlayerInputCommandSystem → PlayerMoveSystem → PlayerStateSystem → PlayerViewSystem → PlayerAnimViewSystem
```

## 数据流（一帧因果链）

```
Game 场景启动:
  TPBattleContext.InitializeGameScene
    → SetupMainCamera
    → CharacterModule.SetCharacterPrefab("Player")
    → CharacterModule.LoadCharacterAsync()
    → ResourceModule.LoadAssetAsync<PlayerAnimConfig>("PlayerAnimConfig")
    → FrameSyncModule.CreateWorld<PlayerWorld>()
    → CreateEntity(LocalPlayer, PlayerComponent(isLocal), PlayerMoveComponent, PlayerStateComponent, PlayerViewComponent, PlayerCommandRecordComponent)
    → FlushEntityOperations()
    → world.IsStart = true
    → CameraModule.BindCinemachineToPlayer(LookAt, LookAt)

渲染帧 Update（每个 Unity frame, ~16ms）:
  PlayerInputCollectSystem → 采集 Unity Input(Vector2/按键)
    → 定点化+相机修正 → PlayerInputComponent(单例, moveDir/jump/toggleLock/speedGear)
    → Crouch 边沿触发只切换 PlayerViewComponent.isCrouching（Animancer mixer 表现态）
  PlayerViewSystem → 读 PlayerMoveComponent(只读) + 读 PlayerViewComponent
    → 检测逻辑帧推进(move.pos != lastLogicPos) → prevLogicPos=旧, interpT=0
    → interpT += dt/0.2s
    → Lerp(prevLogicPos, lastLogicPos+外推, interpT) → viewRoot.position
    → Slerp(viewRoot.rotation, target, dt*12) → viewRoot.rotation
  PlayerAnimViewSystem → 读 PlayerStateComponent + PlayerMoveComponent + PlayerViewComponent
    → animInitialized?false → PlayAnim(Idle), 设 true
    → state!=view.lastPlayedState? → PlayAnim(newState), 更新 lastPlayedState
    → MoveStart/Rotation/Speed 参数来自 move.moveIntentDir + move.speedGear，不读全局输入单例

逻辑帧 FixedUpdate（200ms 间隔）:
  FrameSyncModule.Update → while(累积>=200ms) → World.FixedLoop(200)
    → Record(FrameCount) → 快照 PlayerMoveComponent+PlayerStateComponent
    → FrameCount++ → 进入本次逻辑执行帧
    → PlayerInputCommandSystem:
       isLocal? PlayerInputComponent.ToCommand(executionFrame, Entity.ID, 0)
       → PlayerCommandRecordComponent.RecordCommand(CommandComponent)
       → 非本地实体缺命令时 RecordForecastIfMissing(executionFrame)
       → PlayerInputComponent.ConsumeOneShot()
    → PlayerMoveSystem.FixedUpdate:
       按实体从 PlayerCommandRecordComponent 读 executionFrame 的 CommandComponent
       → 将本帧 moveDir/speedGear 固化到本实体 PlayerMoveComponent.moveIntentDir/speedGear
       ├─ 交互状态?(Vault/Climb/...) → 跳过水平输入, 推进 ClimbTrajectory[framesInState]
       ├─ 核心移动: 加减速曲线 → 水平积分 → 坡面投影 → 碰撞扫掠+分离+滑动
       ├─ 跳跃: jump 边沿消费 + verticalSpeed=JumpSpeed
       └─ 重力: v+=g*dt, y+=v*dt, 落地→SampleHeight+归一
    → PlayerStateSystem.FixedUpdate:
       按实体读取 CommandComponent.toggleLock/platformJump
       物理事实(isOnGround/verticalSpeed/move.moveIntentDir) → Decide(state)
```

## 确定性要点（命根子）

- **全程 int / SyncVector3**：位移 `delta = speed × dtMs / 1000`（long 中转防溢出），方向/归一化/开方全部定点牛顿迭代
- **固定步长 200ms**：逻辑帧恒为 `FrameSyncModule.IntervalTime = 200`，不依赖 `Time.deltaTime`
- **确定性碰撞**：`ICollisionWorld`/`SimpleCollisionWorld` 用 AABB + 胶囊查询，`CapsuleSweep` 按固定整数步长采样 from→to，不调用 Unity Physics
- **逻辑与表现分离**：`Vector3.Lerp/Slerp`/`Animancer.Play` 只影响画面，不回写逻辑组件；`isCrouching` 仅用于 Animancer mixer，放在 `PlayerViewComponent`，不进入 `PlayerInputComponent` / `CommandComponent`
- **回滚**: `PlayerMoveComponent`+`PlayerStateComponent` ∈ `GetRecordTypes()`，World 自动用 `RecordSystem<T>` 每帧 `DeepCopy()` 快照；`PlayerMoveComponent` 内含 `moveIntentDir`/`speedGear`，状态与动画表现按实体读取这份可回滚移动意图；`PlayerInputComponent` 只是本地输入采集单例，不进快照，也不被 Move/State 直接读取
- **帧指令承载**：`CommandComponent` / `CommandInfo` 已覆盖 `moveDir`、`jump`、`toggleLock`、`platformJump`、`speedGear` 等影响角色逻辑的输入；当前本地路径会在逻辑帧先把本地输入固化/记录到本地实体 `CommandComponent`，但不会覆盖已存在的同帧缓存命令；非本地实体不会套用本地输入，只读缓存命令或预测命令；`PlayerCommandRecordComponent` 写入和读取命令都用 `DeepCopy()`；缺帧预测只延续移动方向/速度档等持续输入，会清掉 jump/toggleLock/platformJump 边沿输入，并把预测命令写回缓存保持连续预测链，但还不是服务器权威命令回放

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

- **`PlayerAnimConfig`**（ScriptableObject）：14 个 `TransitionAsset` 字段（idle/moveStart/moveLoop/…/platformerUp），资源地址约定为 `"PlayerAnimConfig"`，由 `TPBattleContext` 加载后注入 `PlayerViewComponent`
- **首帧播放**：`PlayerViewComponent.animInitialized` 首次无条件播放当前状态；后续用 `lastPlayedState` 做表现层切换检测
- **`ClimbConfig`**（ScriptableObject）：4 个 `List<ClimbFrameDelta>` 逐帧位移表

## 注意事项 / 坑

- **`TEngine.Utility` 命名空间冲突**：TEngine 已有 `static partial class Utility`，工具类只能放 `namespace TEngine`
- **动画首帧不播放**：`SpawnPlayer` 后表现层尚未播放过任何状态；`PlayerAnimViewSystem` 通过 `animInitialized` 首帧无条件播放，之后用 `PlayerViewComponent.lastPlayedState` 检测切换
- **`TransitionAsset` vs `ClipTransition`**：`PlayerAnimConfig` 字段类型应为 `TransitionAsset`（可直接 `Play(asset)`），原 `ClipTransition` 导致创建 SO 时无法序列化嵌套引用
- **实体延迟就绪**：`CreateEntity` 进入 createCache，下一 `FixedLoop` 的 `LazyExecuteEntityOperation` 才真正入世界——首帧容忍无实体
- **启动入口在 TPBattleContext**：`PlayerWorld` 只声明系统和快照组件，不加载 prefab、不绑定相机、不管理场景生命周期
- **本地表现输入只改本地实体**：`TPBattleContext` 给本地玩家挂 `PlayerComponent(isLocal=true)`；`PlayerInputCollectSystem` 只切换 isLocal 玩家 `PlayerViewComponent.isCrouching`，避免未来远端表现实体被本地 Crouch 输入影响
- **`PlayerViewComponent` 不进快照**：继承 `ComponentBase`（非 `MomentComponentBase`），持有 `Transform`/`AnimancerComponent`/`isCrouching` 等纯表现状态，是逻辑/表现的硬边界
- **`PlayerInputCollectSystem` 写 `PlayerInputComponent.speedGear`**：当前本地路径避免渲染帧直写 `PlayerMoveComponent` 等可回滚组件；进入逻辑帧后由 `PlayerInputCommandSystem` 只转成本地实体 `CommandComponent` 并记录，真正网络权威指令下发/冲突回滚仍需后续接入
- **逻辑/表现不读全局输入单例**：`PlayerMoveSystem` / `PlayerStateSystem` 按实体读取 `PlayerCommandRecordComponent` 的本帧 `CommandComponent`；`PlayerAnimViewSystem` 的 MoveStart 方向、RotationValue、SpeedValue 都从本实体 `PlayerMoveComponent` 读取，避免未来远端实体被本地输入影响
- **预测帧 one-shot**：`PlayerCommandRecordComponent.GetForecastInput` 从上一帧预测下一帧时必须清掉 jump/toggleLock/platformJump，否则缺帧回滚会把一次按键重复成多帧触发

## 相关代码位置
- 逻辑层：`Module/FrameSync/GameLogic/`
- 表现层：`Module/FrameSync/ClientLogic/`
- Game 场景入口：`Context/TPBattleContext.cs`
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
