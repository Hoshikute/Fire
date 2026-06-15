---
title: TEngine 框架文档
aliases: [tengine-repowiki]
---
# TEngine 框架文档（RepoWiki）

> 本页是 `.knowledge` 对 **TEngine 框架百科**（`UnityProject/repowiki/`）的收录与导航。
> repowiki 是自动生成的完整框架文档（137 篇、约 3.7M），覆盖 TEngine 全部子系统。
> 它与 `.knowledge` 分工：`.knowledge` 放 **Fire 项目特有**的精炼知识（帧同步/角色控制/踩坑）；
> repowiki 放 **TEngine 框架通用**知识（模块系统/资源/UI/事件/热更新等）。
> 遇到 TEngine 框架机制问题，按下表定位到 repowiki 的具体文档，再用 read_file 读。

## 路径约定
所有 repowiki 文档根目录：`UnityProject/repowiki/zh/content/`
总目录入口：`UnityProject/repowiki/zh/content/index.md`（自带全量索引）
元数据：`UnityProject/repowiki/zh/meta/repowiki-metadata.json`

## 按任务类型查找（TEngine 框架）

| 你要做的事 | 先读这个文档（相对 repowiki/zh/content/）|
|-----------|------------------------------------------|
| 理解 TEngine 整体架构 / 根模块 / 生命周期 | `核心架构/核心架构.md`、`核心架构/根模块设计.md`、`核心架构/模块生命周期管理.md` |
| 写/注册一个自定义 Module（如 FrameSyncModule 那种）| `模块系统/自定义模块开发.md`、`模块系统/模块注册机制.md`、`模块系统/模块间通信.md` |
| 用 FSM 状态机（FsmModule / FsmState）| `流程管理/FSM原理与实现.md`、`流程管理/自定义流程开发.md` |
| 事件系统（GameEvent / 订阅派发）| `事件系统/事件系统.md`、`事件系统/事件使用指南.md`、`事件系统/事件管理机制.md` |
| 资源加载 / YooAsset / Addressables | `资源管理/资源管理.md`、`资源管理/YooAsset集成.md`、`资源管理/资源加载机制.md` |
| 热更新 / HybridCLR / GameApp 入口 | `热更新系统/热更新系统.md`、`热更新系统/HybridCLR原理与集成.md`、`热更新系统/GameApp入口设计.md` |
| UI 窗口 / UIModule / 脚本生成 | `UI系统/UI系统.md`、`UI系统/UI窗口管理.md`、`UI系统/UI脚本生成器.md` |
| 音频播放 / AudioModule | `音频系统/音频系统.md`、`音频系统/音频播放控制.md` |
| 本地化 / 多语言 | `本地化系统/本地化系统.md`、`本地化系统/本地化核心机制.md` |
| 配置表 / Luban | `配置系统/配置系统.md`、`配置系统/Luban配置系统集成.md`、`配置系统/配置使用指南.md` |
| 对象池 / 内存管理 | `内存管理/内存管理.md`、`内存管理/对象池实现机制.md` |
| 性能优化 / 调试器 / 监控 | `性能优化/性能优化.md`、`性能优化/调试器工具/调试器工具.md` |
| 编辑器工具 / 工具栏扩展 / 引用查找 | `编辑器工具/编辑器工具.md`、`编辑器工具/引用查找器.md`、`编辑器工具/资源收集器.md` |
| 打包发布 / 多平台构建 / 图集 | `部署发布/部署发布.md`、`部署发布/多平台构建配置.md`、`部署发布/图集优化配置.md` |
| 代码规范 / 最佳实践 / 故障排除 | `开发者指南/代码规范与标准.md`、`开发者指南/最佳实践指南/最佳实践指南.md`、`开发者指南/故障排除指南/故障排除指南.md` |
| 查某个 API 的签名 | `API参考/` 下对应子系统（核心API/模块系统API/资源管理API/UI系统API/事件系统API/音频与本地化API/工具与扩展API）|

## 顶层主题目录（每个目录下还有更细的子文档）
项目概述 · 快速开始 · 核心架构 · 模块系统 · 资源管理 · 热更新系统 · 事件系统 · UI系统 · 音频系统 · 本地化系统 · 流程管理 · 配置系统 · 内存管理 · 性能优化 · 编辑器工具 · 部署发布 · 开发者指南 · API参考

> 完整文件清单见 `index.md`，或用 search_files 在 `UnityProject/repowiki/` 下按关键词搜。

## 与 Fire 项目知识的关系
- 改 Fire 的帧同步/角色控制 → 读本库 `architecture/`、`modules/`（项目特有，优先）。
- 涉及 TEngine 框架机制（Module 怎么挂、FSM 怎么用、资源怎么加载）→ 来 repowiki 查（框架通用）。
- 例：`FrameSyncModule` 继承 TEngine `Module` 的写法 → 看 `模块系统/自定义模块开发.md`；
  Player 用的 `GameModule.Fsm.CreateFsm` → 看 `流程管理/FSM原理与实现.md`。
