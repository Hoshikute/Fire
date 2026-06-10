---
name: log-header
description: 日志统一Header规范 —— 定义 TEngine 项目的日志前缀格式，确保所有日志可通过 Header 快速搜索定位模块。触发场景：(1) 编写/审查日志代码；(2) 日志搜索困难需要统一；(3) 代码审查检查日志格式；(4) 现有日志改造对齐
---

# 日志统一 Header 规范

> **目标**：所有日志带上统一的 Header 前缀，实现 **「看到 Header 就知道来源，搜 Header 就能滤出全模块日志」**。

## 核心原则

**每条日志调用必须带 Header 前缀**，且 **全局统一以 `[Claude]` 开头**。

Header 格式为 `[Claude][模块缩写][子模块]`，放在日志消息最前面。

> **为什么是 `[Claude]`**：作为全局统一标记，在 Console 中搜 `[Claude]` 即可一键过滤所有 AI 辅助生成代码的日志，与第三方库 / Unity 引擎原生日志区分，极大提升排查效率。

### 使用哪种日志 API

| API | 优先级 | 说明 |
|-----|--------|------|
| **`Log.Info` / `Log.Warning` / `Log.Error` / `Log.Debug`** | ✅ 首选 | TEngine 封装，支持分类过滤 |
| `TEngine.Log.Info` / `TEngine.Log.Warning` / `TEngine.Log.Error` / `TEngine.Log.Debug` | ✅ 可接受 | 完整命名空间调用 |
| `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` | ❌ 避免 | Unity 原生 API，不支持 TEngine 日志过滤系统 |

> 新建日志一律使用 `Log.XXX()`，仅在与 TEngine 命名空间冲突的极个别场景使用 `TEngine.Log.XXX()`。

---

## Header 格式规范

### 标准格式

```
[Claude][模块缩写][具体子模块/方法] 消息内容
```

- **`[Claude]`**：全局统一前缀，所有日志必须以此开头（固定，不可省略）
- **模块缩写**：第二个标签，写在当前文件顶部为 `TRACE_HEADER` 常量
- **子模块/方法**：直接用 `[方法名]` 或 `[子模块名]` 放在 Header 中
- 各标签之间**不加空格**
- Header 与消息体之间**加一个空格**

### 示例

```
[Claude][TPC_CAM_TRACE][CameraModule][OnInit] uiCamera=UICamera, cameraMain=MainCamera
[Claude][TPC_CAM_TRACE][CameraModule][Bind] success. follow=Player
[Claude][BattleModule][OnInit] Initialized (sync)
[Claude][GameApp] Entrance called
[Claude][ServerDataLoader] 加载 5 个服务器配置
```

### 模块缩写对照表

> 维护此表，新增模块时补充。

| 模块 | 缩写 | 使用场景 |
|------|------|---------|
| CameraModule | `TPC_CAM_TRACE` | 第三人称相机模块 |
| InputModule | `TPC_CAM_TRACE` | 输入模块 |
| Player | `TPC_CAM_TRACE` | 玩家控制器 |
| BattleModule | `BattleModule` | 战斗模块 |
| FrameSyncModule | `FrameSync` | 帧同步模块 |
| CameraModule (独立日志) | `CameraModule` | 相机通用日志 |
| ServerDataLoader | `ServerDataLoader` | 服务器配置加载 |

---

## 实现方式

### 三种写法（优先级从高到低）

#### 方式 1：`TRACE_HEADER` 常量（推荐 — 类级别统一）

```csharp
public class BattleModule : GameModule
{
    private const string TRACE_HEADER = "[Claude][BattleModule]";

    public void OnInit()
    {
        Log.Info($"{TRACE_HEADER}[OnInit] Initialized (sync)");
    }

    public void Shutdown()
    {
        Log.Info($"{TRACE_HEADER} Shutdown");
    }
}
```

**场景**：一个类中大量日志调用，统一修改缩写。

#### 方式 2：`nameof` 动态前缀（推荐 — 重构友好）

```csharp
public class BattleModule : GameModule
{
    public void OnInit()
    {
        Log.Info($"[Claude][{nameof(BattleModule)}][{nameof(OnInit)}] Initialized (sync)");
    }
}
```

**场景**：类名或方法名可能变更，`nameof` 自动跟随重命名。

#### 方式 3：内联字面量（简单场景）

```csharp
Log.Info("[Claude][ServerDataLoader] 加载 5 个服务器配置");
```

**场景**：工具类/静态方法，日志调用较少。

---

## 日志级别使用规范

| 级别 | 何时用 | 示例 |
|------|--------|------|
| `Log.Debug` | 开发调试用，仅本地开启 | `Log.Debug($"{TRACE_HEADER}[CalcDamage] raw={raw}, final={final}")` |
| `Log.Info` | 正常流程关键节点 | `Log.Info($"{TRACE_HEADER}[OnInit] initialized.")` |
| `Log.Warning` | 非预期但可自动恢复 | `Log.Warning($"{TRACE_HEADER} fallback to default config")` |
| `Log.Error` | 功能必然失败的异常 | `Log.Error($"{TRACE_HEADER}[Load] failed: {e.Message}")` |

> **注意**：以上 `TRACE_HEADER` 常量均已包含 `[Claude]` 前缀（如 `"[Claude][BattleModule]"`）。

**注意**：
- `Log.Debug` 的日志在生产构建中默认被裁剪，不需要手动删除
- `Log.Error` 带异常参数的重载：`Log.Error($"{TRACE_HEADER} exception", exception)`
- 禁止用 `Log.Error` 记录预期内的业务流程（如"玩家等级不足"应返回错误码，而非日志）

---

## Unity `Debug.XXX` 的迁移

遇到以下写法，自动替换：

| ❌ 旧写法 | ✅ 新写法 |
|-----------|----------|
| `Debug.Log("msg")` | `Log.Info("[Claude][Module] msg")` |
| `Debug.LogWarning("msg")` | `Log.Warning("[Claude][Module] msg")` |
| `Debug.LogError("msg")` | `Log.Error("[Claude][Module] msg")` |
| `Debug.LogError(e)` | `Log.Error("[Claude][Module] exception", e)` |

> 仅 `Debug.LogError(e)` 这种传入 Exception 而不是字符串的，可以直接保留为 `Log.Error(e)`（TEngine 的 `Log.Error` 有 Exception 重载）。

---

## 搜索技巧

一旦日志带上统一 Header，在 Unity Console 或 IDE 搜索：

| 需求 | 搜索 |
|------|------|
| 查看所有 AI 辅助代码日志 | `[Claude]` |
| 查看所有相机日志 | `[Claude][TPC_CAM_TRACE` |
| 只查看相机初始化日志 | `[Claude][TPC_CAM_TRACE][OnInit` |
| 查看战斗模块所有日志 | `[Claude][BattleModule` |
| 查看战斗模块错误 | `[Claude][BattleModule` + Console 筛选 Error |
| 查看帧同步模块警告 | `[Claude][FrameSync` + Console 筛选 Warning |

> **`[Claude]` 是最常用的搜索词**：在 Console 中输入 `[Claude]` 即可过滤所有项目业务日志，排除 Unity 引擎噪音、第三方插件日志。

---

## 迁移指南（现有代码）

1. **找到没有 Header 的日志**：搜索 `Log\.(Info|Warning|Error|Debug)\(` 或 `Debug\.(Log|LogWarning|LogError)\(`
2. **确定所属模块缩写**：查阅模块缩写对照表
3. **添加 `TRACE_HEADER` 常量**：如果该类还没有
4. **逐条添加 Header**：`"msg"` → `$"{TRACE_HEADER} msg"` 或 `$"{TRACE_HEADER}[MethodName] msg"`
5. **替换 `Debug.XXX`**：改为 `Log.XXX` 并加 Header

### 无 header 日志排查清单

```csharp
// ❌ 无 Header — 无法过滤
Log.Info("Initialized.");
Debug.Log("player loaded");

// ❌ 缺少 [Claude] 前缀 — 无法全局过滤
Log.Info($"[{nameof(MyModule)}] Initialized.");

// ✅ 完整格式 — 可全局 + 模块搜索
Log.Info($"[Claude][{nameof(MyModule)}] Initialized.");
Log.Info($"{TRACE_HEADER}[LoadPlayer] player loaded");  // TRACE_HEADER = "[Claude][MyModule]"
```

---

## 交叉引用

- 命名规范见 [naming-rules.md](../tengine-dev/references/naming-rules.md)
- 事件系统日志最佳实践见 [event-antipatterns.md](../tengine-dev/references/event-antipatterns.md)
- 模块开发见 [modules.md](../tengine-dev/references/modules.md)
