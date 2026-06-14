# Fire 知识库索引

> Agent 的「地图」。遇到任务先在这里定位该读哪个文档，再用 read_file 读对应文件。
> 文档按主题组织，文件名即关键词（搜 rollback 就读 rollback.md）。

## 按任务类型查找

| 你要做的事 | 先读这个文档 |
|-----------|------------|
| 改动同步逻辑 / 逻辑帧 / World 循环 | `architecture/lockstep.md` |
| 涉及状态保存、回滚、重放、快照 | `architecture/rollback.md` |
| 处理网络消息、指令下发、追帧 | `architecture/network-sync.md` |
| 理解 ECS（World/Entity/Component/System） | `architecture/ecs.md` |
| 改 Player 控制器代码 | `modules/player-controller.md`（已删除，迁移至 `player-framesync-ecs.md`） |
| 改帧同步版角色控制 / 角色 ECS / 定点移动 / 斜坡 / 碰撞 / 动画配置 / 渲染插值 / Game 场景启动入口 | `modules/player-framesync-ecs.md` |
| 改动画切换 / FSM 状态 | `modules/animancer-fsm.md` |
| 调一个诡异的 Bug | 先扫 `pitfalls/` 下所有文件 |
| 想知道某个设计为什么这么做 | `decisions/` 下的 ADR |
| 命名 / 署名 / 代码风格 | `conventions/coding-style.md` |
| **TEngine 框架机制**（Module/FSM/资源/UI/事件/热更新/配置/音频等）| `framework/tengine-repowiki.md`（再路由到 repowiki）|

## 文档清单

### architecture/ — 架构设计
- `lockstep.md` — 帧同步整体设计、逻辑帧循环、确定性保证
- `rollback.md` — 预测回滚机制、快照记录与重放
- `network-sync.md` — 网络同步、指令、Status/Frame 两种同步规则
- `ecs.md` — ECS 框架（World/Entity/Component/System/Record）

### modules/ — 模块说明
- `player-controller.md` — 已删除的旧 TPC 文档（迁移映射）
- `player-framesync-ecs.md` — 帧同步版角色控制（TPBattleContext 启动入口 + 确定性逻辑层 ECS + 表现层 + PlayerWorld + 碰撞/斜坡/攀爬/动画配置 + 渲染插值）
- `animancer-fsm.md` — Animancer 动画 + TEngine FSM 状态机

### conventions/ — 规范
- `coding-style.md` — 命名、署名、注释、逻辑/表现分离

### framework/ — TEngine 框架文档收录
- `tengine-repowiki.md` — TEngine 框架百科（repowiki，137 篇）的导航页：按主题路由到 `UnityProject/repowiki/zh/content/` 下的具体文档。框架机制（模块/FSM/资源/UI/事件/热更新/配置/音频/性能/编辑器工具/API）都在这查。

### pitfalls/ — 踩坑记录（最值钱，调 Bug 前必看）
- `rollback-bugs.md` — 回滚相关的坑

### decisions/ — 架构决策记录（ADR）
- `0001-why-lockstep.md` — 为什么选帧同步而非状态同步
- `0004-tpc-migration-lessons.md` — TPC→FrameSync ECS 迁移踩坑与关键修复记录

## 阅读约定

- 每篇文档结构：一句话概述 → 关键文件 → 核心流程 → 注意事项/坑 → 相关代码位置。
- 文档里的「相关代码位置」是真实路径，读完文档可直接定位代码。
