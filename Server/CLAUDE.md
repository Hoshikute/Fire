# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是 TEngine 框架的**帧同步服务器**（LockStepDemo），基于 .NET Framework 4.8 和 SuperSocket 构建。与 Unity 客户端配合使用，实现多人游戏的帧同步逻辑。

## 常用命令

### 构建服务器
```bash
# 使用 MSBuild 构建
msbuild LockStepDemo.sln /p:Configuration=Debug

# 或使用 PowerShell 脚本（自动启动 MySQL + 构建 + 运行）
powershell -ExecutionPolicy Bypass -File scripts/start-server.ps1
```

### 运行测试
```bash
# 运行 NUnit 测试（需要在 Server 目录下执行）
dotnet test LockStepDemo.Tests\LockStepDemo.Tests.csproj
```

### 启动服务器
```bash
# 构建后直接运行
.\LockStepDemo\bin\Debug\LockStepDemo.exe
```
服务器默认监听端口 `7500`，配置文件位于 `LockStepDemo\Config\superSocket.config`。

---

## 核心架构

### ECS 框架

服务器采用自定义 ECS（Entity-Component-System）架构实现帧同步：

```
ECS/
├── WorldBase.cs        # 世界容器，管理所有 Entity、System、RecordSystem
├── EntityBase.cs       # 实体，组件容器
├── ComponentBase.cs    # 组件基类
├── SystemBase.cs       # 系统基类，包含生命周期方法
├── SingletonComponent.cs # 单例组件
├── SyncComponentBase.cs  # 同步组件标记
├── MomentComponentBase.cs # 瞬时组件（支持回滚深拷贝）
└── Record/             # 回滚记录系统
    ├── RecordComponent.cs
    ├── RecordSystem.cs
    └── RecordSystemBase.cs
```

#### 核心概念

- **WorldBase**: 游戏世界容器，管理帧循环、实体生命周期、回滚机制
- **EntityBase**: 实体，通过 `GetComp<T>()` 获取组件
- **SystemBase**: 系统逻辑，通过 `GetEntityList()` 获取过滤后的实体列表
- **RecordSystem**: 记录每帧状态，支持 `RevertToFrame()` 回滚

#### 帧循环生命周期

```csharp
// 每帧执行顺序
FixedLoop(deltaTime) {
    Record(frame);                    // 1. 记录状态（用于回滚）
    NoRecalcBeforeFixedUpdate();      // 2. 重算时跳过
    BeforeFixedUpdate();              // 3. 帧前处理
    FixedUpdate();                    // 4. 核心逻辑
    LateFixedUpdate();                // 5. 帧后处理
    NoRecalcLateFixedUpdate();        // 6. 重算时跳过
    LazyExecuteEntityOperation();     // 7. 延迟执行实体创建/销毁
    EndFrame();                       // 8. 帧结束
}
```

### 服务层架构

```
Service/
├── SyncService.cs      # 主服务入口（SuperSocket AppServer）
├── SyncSession.cs      # 会话管理
├── Game/               # 游戏逻辑服务
│   ├── WorldManager.cs     # 世界管理器
│   ├── UpdateEngine.cs     # 帧更新引擎
│   ├── Box2D/             # Box2D 物理引擎移植
│   └── CommandMessageService.cs  # 命令消息处理
├── Service/            # 业务服务
│   ├── Login/          # 登录服务
│   ├── Match/          # 匹配服务
│   └── Reconnect/      # 重连服务
├── ServiceLogic/       # 服务逻辑
│   ├── Component/      # 服务专用组件
│   └── System/         # 服务专用系统
└── DataBase/           # 数据库服务（MySQL）
```

### 游戏逻辑层

```
SyncGameLogic/
├── Component/          # 游戏组件
│   ├── PlayerComponent.cs
│   ├── MoveComponent.cs
│   ├── SkillStatusComponent.cs
│   └── ...
├── System/             # 游戏系统
│   ├── MoveSystem.cs
│   ├── SkillSystem.cs
│   ├── CollisionSystem.cs
│   └── ...
├── World/
│   └── DemoWorld.cs    # 具体游戏世界实现
└── Utils/
```

---

## 关键约定

### 组件命名
- 普通组件继承 `ComponentBase`
- 需要同步的组件继承 `SyncComponentBase`
- 需要回滚深拷贝的组件继承 `MomentComponentBase`
- 单例组件继承 `SingletonComponent`

### 系统过滤器
```csharp
public override Type[] GetFilter()
{
    return new Type[] { typeof(PlayerComponent), typeof(MoveComponent) };
}
```

### 实体创建
```csharp
// 创建实体（延迟执行，帧末统一处理）
world.CreateEntity("player_identifier", new PlayerComponent(), new MoveComponent());

// 立即创建（客户端本地实体）
world.CreateEntityImmediately("local_entity", new ViewComponent());
```

### 回滚机制
```csharp
// 记录当前帧状态
world.Record(frame);

// 回滚到指定帧
world.RevertToFrame(targetFrame);

// 清除指定帧之前/之后的记录
world.ClearBefore(frame);
world.ClearAfter(frame);
```

---

## 协议与配置

### 协议定义
- 协议文件位于 `Network/` 目录
- 使用 `Protocol/` 目录下的工具生成协议代码
- 工具项目：`Tools/Server/Protocol2CSharp/`、`Tools/Server/CSharp2Protocol/`

### 游戏数据
- 配置数据位于 `Data/` 目录（TXT 格式）
- 生成工具：`Tools/Server/Tool_GenerateDataClass/`

---

## 与客户端协作

客户端热更代码位于 `UnityProject/Assets/GameScripts/HotFix/GameLogic/`，与服务器共享相同的 ECS 组件定义和游戏逻辑。

### 同步规则
```csharp
public enum SyncRule
{
    Status,  // 状态同步：服务器下发所有操作
    Frame,   // 帧同步：本地计算所有结果
}
```

---

## 数据库

服务器使用 MySQL 存储持久化数据：
- 连接池：`Service/DataBase/ConnectionPool.cs`
- 配置：需要本地 MySQL 运行在 3306 端口
- 启动脚本会自动检查并启动 MySQL
