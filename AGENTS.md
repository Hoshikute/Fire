# Fire 项目 — Agent 协作约定

> 本文件是「宪法层」，每次会话自动注入。只放最高频、违反就出错的规则。
> 详细知识在 `.knowledge/`，遇到具体任务先读 `.knowledge/INDEX.md` 找对应主题。

## 项目概况

Unity 第三人称角色控制器 + 帧同步（lockstep）网游，含预测回滚（rollback）。

- 引擎/框架：Unity + TEngine（模块化框架，提供 Fsm/Camera/Event 等 GameModule）
- 动画系统：Animancer（`com.kybernetik.animancer`）
- 架构：ECS（World / Entity / Component / System）驱动的帧同步
- 主命名空间：逻辑层 `GameLogic`，角色控制器 `ThirdPersonController`
- 作者署名：HuHu <3112891874@qq.com>

## 关键代码位置

| 模块 | 路径 |
|------|------|
| 帧同步核心 | `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/FrameSync/` |
| 玩家控制器 | `UnityProject/Assets/GameScripts/HotFix/GameLogic/Player/Controller/` |
| 玩家状态机 | `.../Player/Controller/Core/Player/State/` |

## 铁律（违反会导致同步不一致或回滚错乱）

1. **帧同步逻辑中禁止不确定性来源**：不要用 `float` 直接参与逻辑运算、`UnityEngine.Random`、`Time.deltaTime`、`DateTime.Now`。
   - 位置/向量用 `SyncVector3`（定点数，int + SCALE=1000）。
   - 逻辑帧步长固定 200ms，由 `FrameSyncModule` 累积驱动，不要用真实帧率。
2. **快照必须深拷贝**：所有参与回滚记录的 Component 实现 `DeepCopy()` 必须是真正的深拷贝，浅拷贝会导致回滚后状态污染。参考 `RecordSystem<T>.Record/RevertToFrame`。
3. **逻辑与表现分离**：`GameLogic`（确定性逻辑）与 `ThirdPersonController`（Unity 表现/动画/相机）不要互相污染。表现层读逻辑层状态，不要反向写。
4. **改 FrameSync 或 Player 模块前**：先读对应的 `.knowledge/` 文档（见下方导航），理解约束再动手。
5. **新增文件署名**：作者 HuHu <3112891874@qq.com>。

## 知识库导航

详细架构、模块、踩坑记录都在 `.knowledge/`。**先读 `.knowledge/INDEX.md`** 按任务类型定位到具体文档，再读对应文件。

> TEngine 框架机制（Module/FSM/资源/UI/事件/热更新/配置等通用框架问题）查 `.knowledge/framework/tengine-repowiki.md`，它会路由到完整框架文档 `UnityProject/repowiki/`。

## 维护约定

修改了架构、新增了模块、踩了新坑后，**同步更新对应的 `.knowledge/` 文档**，保持知识库与代码一致。
