# ECS 框架

## 设计思想

本项目采用自定义 ECS (Entity-Component-System) 架构，核心理念是**数据与逻辑分离**：Entity 是组件容器，Component 只存数据，System 只写逻辑。这种设计天然适合帧同步场景——组件可序列化用于网络传输和状态快照，系统的确定性执行保证前后端一致。

## 核心类

### WorldBase — 世界容器

World 是 ECS 的顶层容器，管理所有实体和系统的生命周期。

**关键属性：**
- `m_entityDict<int, EntityBase>` — 实体字典，O(1) 查询
- `m_entityList<EntityBase>` — 实体列表，顺序遍历
- `m_systemList<SystemBase>` — 系统列表，按注册顺序执行
- `m_singleCompDict<string, SingletonComponent>` — 单例组件（全局状态）
- `m_recordList<RecordSystemBase>` — 记录系统（用于回滚）
- `group: ECSGroupManager` — 组件过滤管理器

**状态标志：**
- `IsStart` / `IsFinish` — 世界生命周期
- `IsClient` — 是否客户端
- `IsCertainty` — 当前帧是否为确定帧（服务端已确认）
- `IsRecalc` — 是否正在重计算
- `FrameCount` — 当前帧号
- `SyncRule` — 同步规则（Status/Frame）

### EntityBase — 实体

实体是组件的容器，通过唯一 ID 标识。

**ID 生成规则：** `Hash(FrameCount + identifier)`

**组件操作：**
```
AddComp<T>()        — 添加组件
RemoveComp<T>()     — 移除组件
GetComp<T>()        — 获取组件
ChangeComp<T>(comp) — 替换组件（触发 OnEntityCompChange 回调）
```

### SystemBase — 系统

系统是游戏逻辑的执行者，通过 Filter 声明关心的组件类型。

**更新阶段（客户端专属）：**
```
BeforeUpdate → Update → LateUpdate
```

**同步阶段（前后端共用）：**
```
BeforeFixedUpdate → FixedUpdate → LateFixedUpdate → EndFrame
```

**实体生命周期回调：**
```
OnEntityOptimizeCreate(entity)
OnEntityOptimizeDestroy(entity)
OnEntityCompAdd(entity, compIndex, component)
OnEntityCompChange(entity, compIndex, prevComp, newComp)
```

**过滤机制：**
```csharp
public override Type[] GetFilter()
{
    return new Type[] { typeof(MoveComponent), typeof(TransfromComponent) };
}
// GetEntityList() 返回同时拥有这些组件的实体
```

### ComponentBase — 组件

纯数据容器，核心方法：
- `Init()` — 初始化
- `ToHash()` — 一致性校验用

**组件分类：**
| 类型 | 用途 |
|------|------|
| ComponentBase | 普通组件 |
| MomentComponentBase | 需要回滚记录的组件，支持 DeepCopy |
| SingletonComponent | 全局唯一组件（如 GameTimeComponent） |
| PlayerCommandBase | 玩家输入命令 |

### ECSGroupManager — 组件组合过滤

维护组件组合到实体列表的映射，实现高效过滤：
- `allGroupDic`: 组件组合 hashCode → ECSGroup
- `groupToEntityDic`: ECSGroup → 实体列表
- `entityToGroupDic`: 实体 → 所属的所有 Group

当实体的组件变化时，自动更新所有相关 Group。

## 调用链

### World 帧更新（FixedLoop）

```
WorldBase.FixedLoop(deltaTime)
  ├─ Record(FrameCount)              // 客户端记录当前状态快照
  ├─ FrameCount++                    // 帧号递增
  ├─ BeforeFixedUpdate(deltaTime)    // 遍历所有 System
  ├─ FixedUpdate(deltaTime)          // 遍历所有 System
  ├─ LateFixedUpdate(deltaTime)      // 遍历所有 System
  ├─ LazyExecuteEntityOperation()    // 批量处理延迟的创建/销毁
  └─ EndFrame(deltaTime)             // 遍历所有 System
```

### 实体创建流程

```
world.CreateEntity(id, comps[])
  ├─ 生成 EntityID = Hash(FrameCount + id)
  ├─ new EntityBase() + 添加组件
  ├─ AddEntity(entity)
  │   ├─ 注册到 m_entityDict / m_entityList
  │   ├─ ECSGroupManager 更新索引
  │   └─ 触发所有 System 的 OnEntityOptimizeCreate
  └─ 若在预测帧：记录操作以便回滚
```

### 实体销毁流程

```
world.DestroyEntity(ID)
  ├─ 标记 DestroyFrame
  ├─ 加入延迟销毁队列
  └─ LazyExecuteEntityOperation() 中批量处理：
      ├─ 从 m_entityDict / m_entityList 移除
      ├─ ECSGroupManager 清理索引
      └─ 触发所有 System 的 OnEntityOptimizeDestroy
```

## 回滚与记录

### RecordSystem

只记录 `MomentComponentBase` 类型的组件：

```
Record(frame)      — 深拷贝组件存入快照列表
RevertToFrame(frame) — 从快照恢复组件状态
ClearBefore(frame) — 清理旧快照
ClearAfter(frame)  — 清理未来快照
```

### EntityRecordComponent

记录实体的创建和销毁操作，用于回滚时撤销预测：
- 预测创建的实体 → 回滚时销毁
- 预测销毁的实体 → 回滚时恢复

## 文件位置

| 文件 | 路径 |
|------|------|
| WorldBase | Server/LockStepDemo/LockStepFrameWork/ECS/WorldBase.cs |
| EntityBase | Server/LockStepDemo/LockStepFrameWork/ECS/EntityBase.cs |
| SystemBase | Server/LockStepDemo/LockStepFrameWork/ECS/SystemBase.cs |
| ComponentBase | Server/LockStepDemo/LockStepFrameWork/ECS/ComponentBase.cs |
| ECSGroupManager | Server/LockStepDemo/LockStepFrameWork/ECS/ECSGroupManager.cs |
| RecordSystem | Server/LockStepDemo/LockStepFrameWork/ECS/Record/RecordSystem.cs |
