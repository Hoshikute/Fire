# 玩家帧同步 ECS（Player FrameSync ECS）

## 一句话
按「TEngine 模块规范 + FrameSync 设计哲学」实现的角色控制：逻辑层用确定性 ECS（定点数 + 200ms 逻辑帧 + 可回滚）算移动，表现层渲染帧读逻辑状态驱动 Unity Transform。与传统 `ThirdPersonController` 控制器并存，是帧同步版的最小可跑路径。

## 目录与文件（符合 FrameSync 分层哲学）
```
Module/FrameSync/
├── GameLogic/                      ← 确定性逻辑层（GameLogic 命名空间，禁止 float/Random/Time.deltaTime）
│   ├── Component/
│   │   ├── PlayerInputComponent.cs     单例组件：当前帧输入意图（定点 moveDir + jump）
│   │   └── PlayerMoveComponent.cs      可回滚时刻组件：pos/faceDir/verticalSpeed/isOnGround，含 DeepCopy
│   ├── System/
│   │   └── PlayerMoveSystem.cs         逻辑帧确定性移动：读输入→定点积分位移/跳跃/重力
│   └── World/
│       └── PlayerWorld.cs              组装 World：GetSystemTypes + GetRecordTypes
└── ClientLogic/                    ← 表现层（读逻辑层，可用 Unity API，只读不反写逻辑）
    ├── Component/
    │   └── PlayerViewComponent.cs      表现组件：绑定 Unity Transform（不进快照/网络）
    ├── System/
    │   ├── PlayerInputCollectSystem.cs 渲染帧采集 Unity 输入→定点写入单例
    │   └── PlayerViewSystem.cs         渲染帧读逻辑状态→插值驱动 Transform 位置/朝向
    └── PlayerFrameSyncEntry.cs         MonoBehaviour 入口：创建 World+玩家实体+启动
```

## 数据流（一帧的因果链）
```
渲染帧(Update)：PlayerInputCollectSystem 采集 Unity 输入(Vector2/Jump)
              → 定点化、归一化 → 写 PlayerInputComponent(单例)
逻辑帧(FixedUpdate, 200ms)：PlayerMoveSystem 读单例输入
              → 定点整数推进 PlayerMoveComponent.pos/verticalSpeed/isOnGround
              → 消费 jump 边沿标志
渲染帧(Update)：PlayerViewSystem 读 PlayerMoveComponent(只读)
              → ToVector() 转 Unity 坐标 → Lerp/Slerp 平滑驱动 Transform
```
System 注册顺序 = 调用顺序：采集 → 移动 → 渲染。逻辑帧前输入已就绪，渲染帧渲染最新逻辑态。

## 确定性要点（命根子）
- 移动/跳跃/重力全程 **int / SyncVector3 整数运算**，位移公式 `delta = speed × deltaTimeMs / 1000`（long 中转防溢出）。
- 方向归一化、开方走 `SyncVector3.Normalized()` / `SyncVector3.Sqrt()`（整数牛顿迭代），跨端结果一致。
- `SyncVector3` 已补确定性运算：`+ - *`、`MulFixed`(定点缩放)、`SqrMagnitude`、`Magnitude`、`Normalized`、`Sqrt`、`FromRaw`、`Zero`、`ONE=1000`。
- 表现层的 `Vector3.Lerp` / `Quaternion.Slerp` 只影响画面平滑，**不回写逻辑**，不破坏确定性。

## 可回滚
- `PlayerMoveComponent : MomentComponentBase`，在 `PlayerWorld.GetRecordTypes()` 声明，World 自动用 `RecordSystem<PlayerMoveComponent>` 每逻辑帧快照。
- `DeepCopy()` 对 `SyncVector3` 调 `.DeepCopy()`（值类型范式一致），新增引用字段务必深拷贝（见 pitfalls/rollback-bugs.md）。

## 如何运行（Unity 编辑器内）
1. 场景里建空 GameObject，挂 `PlayerFrameSyncEntry`。
2. 把要被驱动的角色 Transform 拖到 `viewRoot`（留空则用自身）。
3. 设 `spawnPos`（米，自动转定点）。运行后 `PlayerWorld` 启动，WASD 移动、Space 跳跃。
> 注意：当前是单机本地验证路径（单例输入、本地世界）。接网络多人时，输入应改为按玩家 ID 的 `CommandComponent`/`PlayerCommandBase`，并接 PursueMsg 驱动回滚（见 network-sync.md / rollback.md）。

## 注意事项 / 坑
- `InputButtonType` 在 `TEngine` 命名空间，表现层采集系统需 `using TEngine`。
- 实体经 `CreateEntity` 进 createCache，下一 FixedLoop 的 `LazyExecuteEntityOperation` 才真正入世界——首帧逻辑/表现要容忍实体尚未就绪。
- `PlayerViewComponent` 持有 UnityEngine 引用，**绝不能继承 MomentComponentBase**（不可进快照/协议），这是逻辑/表现的硬边界。
- 落地检测目前用 `pos.y <= 0` 简化（地面高度 0）；接真实地形需替换为确定性碰撞查询。

## 相关代码位置
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/GameLogic/`
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ClientLogic/`

## 关联文档
- 帧同步循环：`architecture/lockstep.md`
- ECS 框架：`architecture/ecs.md`
- 回滚：`architecture/rollback.md`
- 网络/指令：`architecture/network-sync.md`
- 传统控制器（并存）：`modules/player-controller.md`
- 规范：`conventions/coding-style.md`
