---
name: tengine-dev
description: "TEngine Unity 游戏框架开发指导。Use when Codex works on Fire 项目的 TEngine 框架代码、UIWindow/UIWidget、GameEvent/AddUIEvent、GameModule 模块、YooAsset/LoadAssetAsync/SetSprite 资源加载、HybridCLR 热更、Luban 配置表、TEngine API 排查或相关 Unity 框架实现。"
---

# TEngine 开发指导

TEngine 是基于 HybridCLR、YooAsset、UniTask、Luban 的 Unity 游戏框架。本 skill 给 Codex 提供 Fire 项目里 TEngine 开发的精炼路由，避免凭空猜接口或写出和框架不一致的代码。

## 路径约定

- 仓库根目录：`E:\EUGIT\Fire`
- Unity 工程：`UnityProject/`
- 热更代码：`UnityProject/Assets/GameScripts/HotFix/`
- 框架源码：`UnityProject/Assets/TEngine/`
- 项目知识库入口：`.knowledge/INDEX.md`

## 使用流程

1. 先确认任务是否涉及 TEngine 框架机制、UI、事件、资源、热更或配置。
2. 先用 `rg` 搜索现有调用方式和真实 API，再根据下方表格读取对应 reference。
3. 如果 reference 不足，读 `.knowledge/framework/tengine-repowiki.md`，再按它路由到 `UnityProject/repowiki/` 的完整框架文档。
4. 改代码时复用项目现有模式，避免创造新接口、绕过 GameModule，或把表现层和框架层职责混在一起。
5. 完成后做与改动规模匹配的验证；不能实际跑 Unity Editor 或运行时就如实说明。

## 核心红线

1. 异步优先：IO 和资源相关操作优先用 `UniTask`，不要改成同步加载或 Coroutine。
2. 模块访问：通过 `GameModule.XXX` 访问模块，不要臆造 `ModuleSystem.GetModule<T>()` 用法。
3. 资源成对释放：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 使用框架的 `LoadGameObjectAsync`。
4. 热更边界：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 才是热更业务代码。
5. 事件解耦：模块间用 `GameEvent`，UI 内部优先用 `AddUIEvent`，避免手写不可控的注册/反注册流程。

## 文档路由

根据任务类型读取对应 reference 文档：

| 任务类型 | 必读文档 | 进阶文档 | 优先级 |
|---------|---------|---------|--------|
| UI 开发 | [ui-lifecycle.md](references/ui-lifecycle.md) | [ui-patterns.md](references/ui-patterns.md) | P0 |
| 事件系统 | [event-system.md](references/event-system.md) | [event-antipatterns.md](references/event-antipatterns.md) | P0 |
| 资源加载 | [resource-api.md](references/resource-api.md) | [resource-patterns.md](references/resource-patterns.md) | P0 |
| 模块使用 | [modules.md](references/modules.md) | | P0 |
| 热更代码 | [hotfix-workflow.md](references/hotfix-workflow.md) | | P1 |
| 代码规范 | [naming-rules.md](references/naming-rules.md) | | P1 |
| Luban 配置 | [luban-config.md](references/luban-config.md) | | P1 |
| 项目结构 | [architecture.md](references/architecture.md) | | P2 |
| 问题排查 | [troubleshooting.md](references/troubleshooting.md) | | P2 |
| MCP 场景、GameObject、UI、脚本、Editor | [mcp-tools.md](references/mcp-tools.md) | | P1 |
| MCP 材质、Shader、动画、VFX | [mcp-visual.md](references/mcp-visual.md) | | P2 |

