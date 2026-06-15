---
title: ECS 框架
aliases: [ecs]
---
# ECS 框架（帧同步专用）

## 一句话
帧同步世界用自研轻量 ECS 组织：`World` 持有所有 `Entity`，`Entity` 挂 `Component`（纯数据），`System` 按组件过滤批量处理逻辑，`RecordSystem` 负责给可回滚组件做快照。

## 关键文件
- `Module/FrameSync/Core/WorldBase.cs` — 世界，ECS 容器与生命周期
- `Module/FrameSync/ECS/EntityBase.cs` — 实体，持有 `Dictionary<string, ComponentBase> m_compDict`
- `Module/FrameSync/ECS/ComponentBase.cs` — 组件基类（纯数据）
- `Module/FrameSync/ECS/SystemBase.cs` — 系统基类，靠 `GetFilter()` 返回的类型名过滤实体
- `Module/FrameSync/ECS/SingletonComponent.cs` — 单例组件（全局唯一数据，如时间/连接状态）
- `Module/FrameSync/ECS/MomentComponentBase.cs` — 可记录的"时刻组件"（带 Frame/ID/DeepCopy，参与回滚）
- `Module/FrameSync/ECS/ViewSystemBase.cs` — 表现层系统（渲染帧驱动）
- `Module/FrameSync/ECS/ECSEvent.cs` — 实体/组件增删改的事件分发

## 核心概念
- **World**：通过 `GetSystemTypes()` / `GetRecordTypes()` / `GetRecordSystemTypes()` 三个重载声明自己用哪些系统和记录系统，`Init` 时反射实例化。持有 `m_entityList/m_entityDict`、`m_systemList`、`m_recordList`、`m_singleCompDict`，并有实体对象池 `m_entitiesPool`。
- **Entity**：只有 `ID` + 组件字典。增删组件会触发 `OnComponentAdded/Removed/Replaced` 事件。
- **Component**：纯数据。逻辑组件继承 `ComponentBase`；需要回滚记录的继承 `MomentComponentBase`（带 `Frame`/`ID`/`DeepCopy`）。
- **System**：用 `GetFilter()` 声明关心的组件类型（按 `Type.Name` 字符串匹配），在逻辑帧遍历匹配实体执行逻辑。
- **SingletonComponent**：全局唯一的数据组件，挂在 World 上（`m_singleCompDict`），如 `RecordComponent<T>`、连接状态、时间缓存等。

## 业务组件示例（GameLogic/Component/）
- `PlayerComponent` / `MoveComponent` / `TransformComponent` / `CommandComponent` — 玩家、移动、变换、指令组件。

## 注意事项 / 坑
- **过滤器按类型名字符串匹配**（`SystemBase.Filter` 取 `Type.Name`）。注意不要出现同名类型，重命名组件类会影响过滤。
- **记录系统通过泛型 `RecordSystem<T>` 自动生成**（见 `WorldBase.Init` 中 `MakeGenericType`）。要让某组件可回滚，把它的类型加入 `GetRecordTypes()`，并确保继承 `MomentComponentBase` 且 `DeepCopy` 正确。
- **实体对象池**：实体走 `m_entitiesPool` 复用，注意回收时清干净组件，避免脏数据带到下一个实体。

## 相关代码位置
`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/ECS/`

## 关联文档
- 帧同步循环：[[architecture/帧同步|帧同步]]
- 回滚：[[architecture/预测回滚|预测回滚]]
