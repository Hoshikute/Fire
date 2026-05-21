# World 模块

## 设计思想

World 是整个 ECS 框架的顶层容器，承担三大职责：
1. **实体管理**：Entity 的创建、销毁、查询，延迟执行避免迭代器失效
2. **系统调度**：按注册顺序驱动所有 System 的生命周期方法
3. **状态快照与回滚**：每帧录制 Component 状态，支持任意帧回退和重演算

核心设计原则：
- 延迟执行：Entity 创建/销毁在帧末集中处理
- 事件驱动：System 通过 Entity 事件松耦合
- 快照回滚：MomentComponentBase 的 DeepCopy 实现状态恢复

## World 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| IsStart | bool | 是否已启动（控制 Loop/FixedLoop 是否执行） |
| m_isView | bool | 是否是客户端（true=客户端，false=服务端） |
| m_isRecalc | bool | 是否正在重演算 |
| m_isCertainty | bool | 是否处于确定性计算阶段 |
| m_isLocal | bool | 本地标记 |
| FrameCount | int | 当前逻辑帧号 |
| EntityIndex | int | 服务端实体 ID 计数器（正数递增） |
| ClientEntityIndex | int | 客户端实体 ID 计数器（负数递减） |
| SyncRule | SyncRule | 同步规则（Frame / Status） |
| isFinish | bool | 游戏是否结束 |

## World 生命周期

### 创建

```
WorldManager.CreateWorld<T>():
  ├─ T world = new T()
  ├─ world.Init(isView: true)    // 客户端传 true
  │   ├─ 创建 ECSEvent 事件系统
  │   ├─ 反射创建所有 System:
  │   │   for type in GetSystemTypes():
  │   │     system = Activator.CreateInstance(type)
  │   │     system.m_world = this
  │   │     system.Init()
  │   │     m_systemList.Add(system)
  │   ├─ 初始化 RecordSystem<T>:
  │   │   for type in GetRecordTypes():
  │   │     创建 RecordSystem<type> 实例
  │   │     添加到 m_recordList
  │   └─ 初始化 EntityRecordSystem:
  │       for type in GetRecordSystemTypes():
  │         添加到 m_recordList
  └─ s_worldList.Add(world)
```

### 运行

```
WorldManager 每帧调用:
  ├─ Update → world.Loop(deltaTime)       // 表现层
  ├─ 积累时间 > 200ms → world.FixedLoop() // 逻辑层
  └─ LateUpdate → world.LateUpdate()      // 后期更新
```

### 销毁

```
WorldManager.DestroyWorld(world):
  ├─ world.Dispose()
  │   ├─ 派发所有 Entity 的 OnEntityDestroyed 事件
  │   ├─ 调用所有 System 的 Dispose()
  │   └─ 清空 entityList、entityDict、systemList
  └─ s_worldList.Remove(world)
```

## 帧循环详解

### 渲染帧 — Loop(deltaTime)

每个 Unity 渲染帧调用，驱动表现层 System：

```
Loop(deltaTime):
  if (!IsStart) return;
  ├─ BeforeUpdate(deltaTime)
  │   └─ 所有 System.BeforeUpdate()
  ├─ Update(deltaTime)
  │   └─ 所有 System.Update()
  └─ LateUpdate(deltaTime)
      └─ 所有 System.LateUpdate()
```

### 逻辑帧 — FixedLoop(deltaTime)

每 200ms 触发一次，驱动核心游戏逻辑：

```
FixedLoop(deltaTime):
  if (!IsStart) return;
  ├─ Record(FrameCount)                    ← 快照当前帧状态
  ├─ FrameCount++
  ├─ NoRecalcBeforeFixedUpdate(deltaTime)  ← 仅首次执行（非重演算）
  │   └─ 所有 System.NoRecalcBeforeFixedUpdate()
  ├─ BeforeFixedUpdate(deltaTime)          ← 命令应用
  │   └─ 所有 System.BeforeFixedUpdate()
  ├─ FixedUpdate(deltaTime)                ← 核心逻辑
  │   └─ 所有 System.FixedUpdate()
  ├─ LateFixedUpdate(deltaTime)            ← 后处理
  │   └─ 所有 System.LateFixedUpdate()
  ├─ NoRecalcLateFixedUpdate(deltaTime)    ← 仅首次执行
  │   └─ 所有 System.NoRecalcLateFixedUpdate()
  ├─ LazyExecuteEntityOperation()          ← 延迟创建/销毁 Entity
  └─ EndFrame(deltaTime)
      └─ 所有 System.EndFrame()
```

### 重演算帧 — Recalc(frame, deltaTime)

回滚后逐帧重新执行逻辑：

```
Recalc(frame, deltaTime):
  ├─ FrameCount++
  ├─ OnlyCallByRecalc(frame, deltaTime)    ← 恢复历史命令
  │   └─ 所有 System.OnlyCallByRecalc()
  ├─ BeforeFixedUpdate(deltaTime)
  │   └─ 所有 System.BeforeFixedUpdate()
  ├─ FixedUpdate(deltaTime)
  │   └─ 所有 System.FixedUpdate()
  ├─ LateFixedUpdate(deltaTime)
  │   └─ 所有 System.LateFixedUpdate()
  └─ LazyExecuteEntityOperation()
```

注意：Recalc 不调用 `NoRecalcBeforeFixedUpdate` 和 `NoRecalcLateFixedUpdate`。

## System 生命周期方法

### 方法一览

| 方法 | 调用时机 | 频率 | 用途 |
|------|---------|------|------|
| Init() | World 初始化时 | 一次 | 注册事件监听、初始化数据 |
| Dispose() | World 销毁时 | 一次 | 清理资源 |
| BeforeUpdate | 渲染帧 Loop 中 | ~60fps | 表现层预处理 |
| Update | 渲染帧 Loop 中 | ~60fps | 插值、动画、输入采集 |
| LateUpdate | 渲染帧 Loop 中 | ~60fps | 摄像机、UI |
| NoRecalcBeforeFixedUpdate | 逻辑帧（非重演算） | 5fps | 输入构建、物品生成 |
| BeforeFixedUpdate | 逻辑帧 | 5fps | 命令应用 |
| FixedUpdate | 逻辑帧 | 5fps | 核心逻辑（移动、碰撞、伤害） |
| LateFixedUpdate | 逻辑帧 | 5fps | 逻辑后处理 |
| NoRecalcLateFixedUpdate | 逻辑帧（非重演算） | 5fps | 清理缓存 |
| EndFrame | 逻辑帧 | 5fps | 帧结束 |
| OnlyCallByRecalc | 重演算帧 | 按需 | 恢复历史命令 |

### System 与 World 交互

```csharp
// 获取 Entity 列表（按 Filter 过滤）
GetFilter() → Type[]                    // 子类重写，返回需要的 Component 类型
GetEntityList() → List<EntityBase>      // 返回拥有 Filter 中所有 Component 的 Entity
GetEntityList(string[] filter)          // 自定义 filter 获取

// 创建/销毁 Entity
m_world.CreateEntity(identifier, comps...)
m_world.DestroyEntity(entityID)

// 获取单例组件
m_world.GetSingletonComp<T>()

// 事件派发
m_world.eventSystem.DispatchEvent(key, entity, args...)
```

### System 事件监听

System 可在 Init() 中注册 Entity 事件：

```csharp
AddEntityCreaterLisnter()         → OnEntityCreate(entity)
AddEntityDestroyLisnter()         → OnEntityDestroy(entity)
AddEntityWillBeDestroyLisnter()   → OnEntityWillBeDestroy(entity)
AddEntityCompAddLisenter()        → OnEntityCompAdd(entity, name, comp)
AddEntityCompRemoveLisenter()     → OnEntityCompRemove(entity, name, comp)
AddEntityCompChangeLisenter()     → OnEntityCompChange(entity, name, old, new)
```

## Entity 管理

### 创建流程

```
// 外部调用
world.CreateEntity("bullet", moveComp, flyComp, collisionComp)

内部流程:
  ├─ identifier = FrameCount + "bullet"     // 加帧号前缀
  ├─ ID = identifier.ToHash()               // 字符串转 hash 作为 ID
  ├─ entity = new EntityBase(ID)
  ├─ 添加所有 Component
  └─ createCache.Add(entity)                // 进入创建缓存

帧末 LazyExecuteEntityOperation():
  ├─ AddEntity(entity)
  │   ├─ entityDict[ID] = entity
  │   └─ entityList.Add(entity)
  └─ DispatchCreate(entity)                 // 派发 OnEntityCreated 事件
```

### 销毁流程

```
// 外部调用
world.DestroyEntity(entityID)

内部流程:
  ├─ entity = entityDict[entityID]
  └─ destroyCache.Add(entity)               // 进入销毁缓存

帧末 LazyExecuteEntityOperation():
  ├─ RemoveEntity(entity)
  │   ├─ entityDict.Remove(ID)
  │   └─ entityList.Remove(entity)
  ├─ DispatchWillBeDestroyed(entity)        // 派发销毁前事件
  └─ DispatchDestroy(entity)                // 派发销毁后事件
```

### 立即创建（服务端用）

```
world.CreateEntityImmediately(identifier, comps...)
  ├─ 不进缓存
  ├─ 直接 AddEntity
  └─ 直接 DispatchCreate
```

### 延迟执行的意义

Entity 创建/销毁不在 System.FixedUpdate() 中立即生效，而是缓存到帧末统一执行。原因：
1. 避免遍历 entityList 时修改集合导致崩溃
2. 保证同一帧内所有 System 看到一致的 Entity 集合
3. 创建的 Entity 下一帧才能被其他 System 访问

### Entity 查询

```csharp
world.GetEntity(ID)                   // 按 ID 获取，不存在抛异常
world.GetEntityIsExist(ID)            // 检查是否存在
world.GetEntiyList(string[] compNames) // 获取拥有指定所有 Component 的 Entity
```

## Component 体系

### 继承结构

```
ComponentBase (基类)
 ├─ MomentComponentBase (可回滚组件)
 │   ├─ MoveComponent
 │   ├─ PlayerComponent
 │   ├─ LifeComponent
 │   ├─ CDComponent
 │   ├─ SkillStatusComponent
 │   ├─ CommandComponent
 │   └─ BlowFlyComponent
 │
 ├─ SingletonComponent (World 级单例)
 │   ├─ RecordComponent<T>        (快照存储)
 │   ├─ EntityRecordComponent     (Entity 操作记录)
 │   ├─ GameTimeComponent         (游戏倒计时)
 │   └─ RankComponent             (排名)
 │
 └─ 普通 Component (不参与回滚)
     ├─ TransfromComponent
     ├─ CollisionComponent
     ├─ AssetComponent
     ├─ PerfabComponent (表现层)
     └─ ...
```

### MomentComponentBase

需要参与快照和回滚的 Component 必须继承此类：

```csharp
abstract class MomentComponentBase : ComponentBase
{
    int ID;           // 所属 Entity ID
    int Frame;        // 快照帧号

    abstract MomentComponentBase DeepCopy();  // 必须实现深拷贝
}
```

### SingletonComponent

全局唯一，通过 World 管理，不挂载在 Entity 上：

```csharp
world.GetSingletonComp<GameTimeComponent>()    // 获取或自动创建
world.ChangeSingletonComp<T>(newComp)          // 替换
```

### Entity 上的 Component 操作

```csharp
// 增
entity.AddComp<MoveComponent>(moveComp);
entity.AddComp("MoveComponent", moveComp);

// 查
entity.GetExistComp<MoveComponent>();          // bool
entity.GetComp<MoveComponent>();               // 获取实例

// 改（触发 OnComponentReplaced 事件）
entity.ChangeComp<MoveComponent>(newComp);

// 删
entity.RemoveComp<MoveComponent>();
```

## 状态快照与回滚

### 快照录制 — Record(frame)

每个逻辑帧开始时，对所有 MomentComponentBase 做深拷贝保存：

```
Record(frame):
  for each RecordSystem<T> in m_recordList:
    RecordSystem<T>.Record(frame):
      ├─ rc = GetSingletonComp<RecordComponent<T>>()
      └─ for each entity with T:
          record = entity.GetComp<T>().DeepCopy()
          record.Frame = frame
          record.ID = entity.ID
          rc.m_record.Add(record)
```

### 状态回退 — RevertToFrame(frame)

```
RevertToFrame(frame):
  for each RecordSystem<T> in m_recordList:
    RecordSystem<T>.RevertToFrame(frame):
      ├─ list = rc.GetRecordList(frame)    // 获取该帧的所有快照
      └─ for each record in list:
          entity.ChangeComp<T>(record.DeepCopy())  // 恢复状态
  FrameCount = frame
```

### 快照清理

```csharp
ClearBefore(frame)   // 删除 frame 之前的快照（已确认，不再需要回滚）
ClearAfter(frame)    // 删除 frame 之后的快照（回滚后作废）
```

### 完整重演算流程

```
收到服务端权威数据，发现 Frame 10 的预测有误:

1. RevertToFrame(9)                    // 回退到 Frame 9 状态
2. ClearAfter(9)                       // 清除 Frame 10+ 的无效快照
3. m_isRecalc = true

4. for frame = 10 to currentFrame:
     Recalc(frame, deltaTime):
       ├─ OnlyCallByRecalc()           // 恢复该帧的历史命令
       ├─ BeforeFixedUpdate()          // 应用命令
       ├─ FixedUpdate()                // 重新计算
       ├─ LateFixedUpdate()
       ├─ LazyExecuteEntityOperation() // 处理实体变更
       └─ Record(frame)               // 保存新快照

5. m_isRecalc = false
6. EndRecalc()                         // 派发延迟事件
7. ClearBefore(confirmedFrame)         // 清除已确认帧之前的快照
```

## 重演算中的 Entity 处理

重演算期间，Entity 的创建/销毁需要特殊处理，避免重复派发事件：

### 回滚缓存

```csharp
List<EntityBase> rollbackCreateCache   // 重演算中"应该不存在"的 Entity
List<EntityBase> rollbackDestroyCache  // 重演算中"应该存在"的 Entity
```

### 创建 Entity（重演算期间）

```
RecalcCreateEntity(entity):
  if ID in rollbackCreateCache:
    // 这个 Entity 之前被回滚删除了，现在重新创建
    取出缓存的 Entity → 复制新数据 → 加入 World（不派发事件）
  else:
    // 全新 Entity
    CreateEntityAndDispatch(entity)  // 正常派发
```

### 销毁 Entity（重演算期间）

```
RecalcDestroyEntity(entity):
  if ID in rollbackDestroyCache:
    // 这个 Entity 之前被回滚恢复了，现在重新销毁
    从 World 移除（不派发事件）→ 保留在缓存中
  else:
    // 确定要销毁
    DestroyEntityAndDispatch(entity)  // 正常派发
```

### 重演算结束 — EndRecalc()

```
EndRecalc():
  // rollbackCreateCache 中剩余的 = 预测创建了但实际不该存在的
  for each entity in rollbackCreateCache:
    DispatchDestroy(entity)           // 通知表现层销毁 GameObject

  // rollbackDestroyCache 中剩余的 = 预测销毁了但实际应该存在的
  for each entity in rollbackDestroyCache:
    DispatchCreate(entity)            // 通知表现层创建 GameObject

  清空两个缓存
```

## 事件系统 — ECSEvent

### 双重事件机制

```csharp
class ECSEvent
{
    Dictionary<string, ECSEventHandle> m_EventDict           // 普通事件
    Dictionary<string, ECSEventHandle> m_certaintyEventDict  // 确定性事件
    List<EventCache> m_eventCache                            // 事件缓存
}
```

### 普通事件 vs 确定性事件

| 类型 | 触发时机 | 用途 |
|------|---------|------|
| 普通事件 | 立即派发 | 预测阶段的即时通知 |
| 确定性事件 | 缓存，确认帧后派发 | 需要等待服务端确认的事件（如击杀通知） |

### 确定性事件流程

```
预测阶段:
  DispatchEvent(key, entity, args)
    → 事件进入 m_eventCache（不立即执行）

服务端确认:
  DispatchCertainty(confirmedFrame)
    → 取出 confirmedFrame 之前的缓存事件
    → 依次执行回调

回滚发生:
  ClearCache(frame)
    → 清除 frame 之前的缓存事件（作废）
```

## DemoWorld 配置示例

```csharp
class DemoWorld : WorldBase
{
    // 定义 System 执行顺序
    public override Type[] GetSystemTypes()
    {
        return new Type[] {
            // 逻辑层（FixedUpdate 执行）
            typeof(CollisionSystem),
            typeof(OperationSystem),
            typeof(BlowFlySystem),
            typeof(MoveSystem),
            typeof(FireSystem),
            typeof(SkillStatusSystem),
            typeof(SkillSystem),
            typeof(LifeSpanSystem),
            typeof(CollisionDamageSystem),
            typeof(FlyObjectCollisionSystem),
            typeof(GameSystem),
            typeof(ResurgenceSystem),
            typeof(CreateItemSystem),
            typeof(ItemSystem),
            typeof(RankSystem),
            typeof(BuffSystem),
            typeof(InitSystem),

            // 表现层（Update/LateUpdate 执行）
            typeof(HealthBarSystem),
            typeof(CameraSystem),
            typeof(CreatePerfabSystem),
            typeof(MovePerfabSystem),
            typeof(InputSystem),
            typeof(SyncSystem<CommandComponent>),
            typeof(PlayerAnimSystem),
            typeof(SkillBehaviorSystem),
            typeof(DestroyEffectSystem),
            typeof(ClientOperationSystem),
            typeof(SettlementUISystem),
            typeof(ResurgenceUISystem),
            typeof(BuffBehaviorSystem),
        };
    }

    // 定义需要快照回滚的 Component
    public override Type[] GetRecordTypes()
    {
        return new Type[] {
            typeof(CDComponent),
            typeof(LifeSpanComponent),
            typeof(MoveComponent),
            typeof(PlayerComponent),
            typeof(LifeComponent),
            typeof(SkillStatusComponent),
            typeof(BlowFlyComponent),
            typeof(SkillBehaviorComponent),
            typeof(BuffEffectComponent),
        };
    }

    // 定义 Entity 级别的录制系统
    public override Type[] GetRecordSystemTypes()
    {
        return new Type[] {
            typeof(EntityRecordSystem),
        };
    }
}
```

## 用法总结

### 创建自定义 World

```csharp
// 1. 继承 WorldBase
class MyWorld : WorldBase
{
    public override Type[] GetSystemTypes() { ... }
    public override Type[] GetRecordTypes() { ... }
    public override Type[] GetRecordSystemTypes() { ... }
}

// 2. 创建并启动
var world = WorldManager.CreateWorld<MyWorld>();
world.IsStart = true;
```

### 创建 System

```csharp
// 逻辑层 System
class MyLogicSystem : SystemBase
{
    public override Type[] GetFilter()
    {
        return new Type[] { typeof(MoveComponent), typeof(LifeComponent) };
    }

    public override void FixedUpdate(int deltaTime)
    {
        var list = GetEntityList();
        for (int i = 0; i < list.Count; i++)
        {
            var mc = list[i].GetComp<MoveComponent>();
            // 逻辑计算...
        }
    }
}

// 表现层 System
class MyViewSystem : ViewSystemBase
{
    public override void Init()
    {
        AddEntityCreaterLisnter();  // 监听实体创建
    }

    public override void Update(int deltaTime)
    {
        // 读取逻辑数据，驱动 Unity 渲染
    }

    public override void OnEntityCreate(EntityBase entity)
    {
        // 创建 GameObject
    }
}
```

### 创建 Entity

```csharp
// 在 System 中创建 Entity
var mc = new MoveComponent();
mc.pos = new SyncVector3(1000, 0, 2000);
mc.m_velocity = 3000;

var cc = new CollisionComponent();
cc.area.radius = 0.5f;

m_world.CreateEntity("enemy_" + id, mc, cc, new AssetComponent("monster_01"));
```

### 销毁 Entity

```csharp
m_world.DestroyEntity(entity.ID);
// 或客户端专用（负数ID）
m_world.ClientDestroyEntity(entity.ID);
```

### 使用单例组件

```csharp
var gameTime = m_world.GetSingletonComp<GameTimeComponent>();
gameTime.GameTime -= deltaTime;

if (gameTime.GameTime <= 0)
{
    m_world.eventSystem.DispatchEvent("gameFinish", null);
}
```

### 派发和监听事件

```csharp
// 派发
m_world.eventSystem.DispatchEvent("player_die", entity, killerID);

// 监听（在 System.Init 中）
m_world.eventSystem.AddListener("player_die", OnPlayerDie);

void OnPlayerDie(EntityBase entity, params object[] args)
{
    int killerID = (int)args[0];
    // ...
}
```

## 调用关系总览

```
WorldManager
 ├─ CreateWorld<T>() → World.Init()
 ├─ DestroyWorld()   → World.Dispose()
 ├─ Update()         → World.Loop()
 │                     ├─ System.BeforeUpdate()
 │                     ├─ System.Update()
 │                     └─ System.LateUpdate()
 ├─ FixedUpdate()    → World.FixedLoop()
 │                     ├─ Record()
 │                     ├─ System.NoRecalcBeforeFixedUpdate()
 │                     ├─ System.BeforeFixedUpdate()
 │                     ├─ System.FixedUpdate()
 │                     ├─ System.LateFixedUpdate()
 │                     ├─ System.NoRecalcLateFixedUpdate()
 │                     ├─ LazyExecuteEntityOperation()
 │                     │   ├─ AddEntity → DispatchCreate → System.OnEntityCreate()
 │                     │   └─ RemoveEntity → DispatchDestroy → System.OnEntityDestroy()
 │                     └─ System.EndFrame()
 └─ LateUpdate()     → World.LateUpdate()

World
 ├─ m_systemList[]     System 有序列表
 ├─ m_entityList[]     Entity 列表
 ├─ m_entityDict{}     Entity 字典 (ID → Entity)
 ├─ m_recordList[]     RecordSystem 列表
 ├─ m_singleCompDict{} 单例 Component 字典
 ├─ createCache[]      待创建 Entity 缓存
 ├─ destroyCache[]     待销毁 Entity 缓存
 ├─ rollbackCreateCache[]   回滚创建缓存
 ├─ rollbackDestroyCache[]  回滚销毁缓存
 └─ eventSystem        ECSEvent 事件系统

Entity
 ├─ ID                 唯一标识
 ├─ World              所属 World
 ├─ m_compDict{}       Component 字典 (name → Component)
 └─ 事件: OnComponentAdded / Removed / Replaced
```

## 文件位置

| 文件 | 路径 |
|------|------|
| WorldBase | Client/Assets/Script/SyncFrameWork/ECS/WorldBase.cs |
| WorldManager | Client/Assets/Script/SyncFrameWork/WorldManager.cs |
| SystemBase | Client/Assets/Script/SyncFrameWork/ECS/SystemBase.cs |
| ViewSystemBase | Client/Assets/Script/SyncFrameWork/ECS/ViewSystemBase.cs |
| EntityBase | Client/Assets/Script/SyncFrameWork/ECS/EntityBase.cs |
| ComponentBase | Client/Assets/Script/SyncFrameWork/ECS/ComponentBase.cs |
| MomentComponentBase | Client/Assets/Script/SyncFrameWork/ECS/MomentComponentBase.cs |
| SingletonComponent | Client/Assets/Script/SyncFrameWork/ECS/SingletonComponent.cs |
| ECSEvent | Client/Assets/Script/SyncFrameWork/ECS/ECSEvent.cs |
| RecordSystemBase | Client/Assets/Script/SyncFrameWork/ECS/Record/RecordSystemBase.cs |
| RecordSystem | Client/Assets/Script/SyncFrameWork/ECS/Record/RecordSystem.cs |
| RecordComponent | Client/Assets/Script/SyncFrameWork/ECS/Record/RecordComponent.cs |
| EntityRecordSystem | Client/Assets/Script/SyncFrameWork/SyncLogic/System/EntityRecordSystem.cs |
| DemoWorld | Client/Assets/Script/SyncGameLogic/World/DemoWorld.cs |
