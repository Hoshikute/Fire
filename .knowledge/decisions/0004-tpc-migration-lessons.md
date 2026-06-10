# ADR 0004: TPC → FrameSync ECS 迁移踩坑记录

状态：**已记录**
作者：HuHu
日期：2026-06
关联：0002-tpc-to-framesync-migration.md, 0003-framesync-movement-full-replication.md

## 踩坑 1：`namespace TEngine.Utility` 与已有 `TEngine.Utility` 静态类冲突

**现象**：编译错误 `The namespace 'TEngine' already contains a definition for 'Utility'`

**原因**：TEngine 已有 `static partial class Utility`（`Runtime/Core/Utility/Utility.cs` 等 14 个文件），在 `namespace TEngine` 下定义了 `Utility` 类。创建 `Runtime/Utility/` 目录并把工具类放 `namespace TEngine.Utility` 时，C# 编译器将 `TEngine.Utility` 解析为 `TEngine.Utility` 命名空间，与 `TEngine.Utility` 类型冲突。

**修复**：工具类（`MonoSingleton`/`NoMonoSingleton`/`BindableProperty`/`ToolFunction`）直接放 `namespace TEngine`，不在尾缀加 `.Utility`。

**教训**：在已有 C# 项目中新增命名空间时，先确认同名类是否已存在。TEngine 作者在 `namespace TEngine` 下定义了大量 partial class（`Utility`、`Log` 等），任何 `TEngine.<name>` 命名的 namespace 都可能冲突。

---

## 踩坑 2：动画首帧不播放——`state == prevState` 永久跳过

**现象**：角色启动后一直 T-pose，`PlayerAnimViewSystem` 从不触发 `PlayAnim`。

**原因**：
```csharp
// PlayerAnimViewSystem.Update (旧代码)
if (st.state == st.prevState)  // Idle == Idle → true
    continue;                   // 永久跳过！
```
`PlayerFrameSyncEntry.SpawnPlayer` 创建 `PlayerStateComponent` 时：
```csharp
state = PlayerLogicState.Idle,    // 当前 = Idle
prevState = PlayerLogicState.Idle // 上一帧 = Idle
```
首帧 `state == prevState`（均为 `Idle`），条件永真，`continue` 跳过播放。

**修复**：
- `PlayerViewComponent` 新增 `bool animInitialized`（默认 `false`）
- `PlayerAnimViewSystem.Update` 首先检测 `!view.animInitialized` → 无条件 `PlayAnim(st, view)` + 设 `true`
- 后续帧才按 `state != prevState` 门控播放

**教训**：用"状态是否变化"作为触发条件的系统，必须在首次执行时做特殊处理。典型模式：
```csharp
if (!initialized) { Play(curState); initialized = true; return; }
if (curState != prevState) { Play(curState); }
```

---

## 踩坑 3：`ClipTransition` vs `TransitionAsset` 类型选择

**现象**：创建 `PlayerAnimConfig` ScriptableObject 时，无法将旧 `.asset` 文件中的 `ClipTransition` 提取出来赋值。

**原因**：旧动画资源用 Animancer `TransitionAsset`（`CreateAssetMenu` 生成的预配置资产，包含嵌套的 `_Transition` managedReference）。原本 `PlayerAnimConfig` 字段类型是 `ClipTransition`（值/引用类型无法直接对 ScriptableObject 中的嵌套 managedReference 做 `as` 转换和序列化赋值）。

**修复**：`PlayerAnimConfig` 字段改为 `TransitionAsset`（Animancer 可直接 `animancer.Play(asset)`），创建 SO 时直接 `AssetDatabase.LoadAssetAtPath<TransitionAsset>(path)` 赋值引用。

**教训**：Animancer 资产管线中，`TransitionAsset` 是"可拖入 Inspector 的打包资产"，`ClipTransition` 是"实际播放数据"。字段类型用 `TransitionAsset` 后在 Editor 中直接拖资产到 Inspector 即可，运行时 `Play(asset)` 自动解包。

---

## 踩坑 4：`PlayerAnimConfig` SO 未创建导致 null check 跳过

**现象**：日志显示 `[PlayerFrameSyncEntry] animConfig 未赋值，动画将无法播放`。

**原因**：`PlayerAnimConfig` 只是 `.cs` 类定义，从未创建 `.asset` 实例。`Player.prefab` 上 `PlayerFrameSyncEntry._animConfig` 字段为 null。`PlayerAnimViewSystem.Update` 中：
```csharp
if (view.animConfig == null) continue;
```
直接跳过所有动画播放。

**修复**：通过 `execute_script` 执行编辑器脚本，用 `ScriptableObject.CreateInstance<PlayerAnimConfig>()` + `AssetDatabase.CreateAsset()` + `PrefabUtility.EditPrefabContentsScope` 创建 SO 并挂接到 `Player.prefab`。

**教训**：凡是建立在 ScriptableObject 上的配置系统，必须在代码之外创建 `.asset` 实例。`[CreateAssetMenu]` 仅是"可手动创建"的提示，流水线构建时需自动化。

---

## 踩坑 5：场景 Missing Script 残留

**现象**：启动时 15 条警告 `The referenced script (Unknown) on this Behaviour is missing!`

**原因**：删除 `ThirdPersonController` 所有 .cs 文件后，场景 `Game.unity` 中 10 个 GameObject（含 `CameraController`）仍引用了已不存在的脚本。

**修复**：编辑器脚本 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go)` 批量清理。

**教训**：删除模块 .cs 文件后，必须在 Unity Editor 中走一遍场景/预制体，清理 Missing Script。可做成 Editor 菜单工具一键执行。

---

## 踩坑 6：渲染插值从"指数追逐"改为"两快照 Lerp"

**现象**：逻辑帧 200ms 一跳时，画面用 `Lerp(currentTransformPos, logicPos, dt*12)` 追不上逻辑位置，看起来"拖泥带水"。

**原因**：指数衰减插值 `current = Lerp(current, target, t)` 的逼近速度随距离缩小而减慢。如果 target 每 200ms 一跳快于 lerp 追赶速率，画面永远落后。

**修复**：改为在两个逻辑快照之间线性过渡：
```
prevLogicPos ← 上一逻辑帧位置
lastLogicPos ← 当前逻辑帧位置
interpT      ← 每个渲染帧累加 dt/0.2s（0→1）
position = Lerp(prevLogicPos, lastLogicPos + extrap, interpT)
```
t 归一时必然到达，不 overshoot。额外加了 3m 跳变 snap 和 30% 速度外推。

**教训**：帧同步的渲染层插值公式不要用指数衰减（`Lerp(current, target, dt*speed)`），因其永远追赶。用"逻辑快照 A→B 之间 `t` 进度线性过渡"才是标准做法。

---

## 踩坑 7：`PlayerInputCollectSystem` 写 `PlayerMoveComponent.speedGear`

**状态**：当前可接受，真实回滚前需处理

**分析**：`PlayerInputCollectSystem` 是渲染帧系统（`ViewSystemBase`），但它直接写入 `PlayerMoveComponent.speedGear`（`MomentComponentBase`，可回滚组件）。回滚重算时 `FixedUpdate` 会重复执行逻辑帧，但渲染帧写入的 `speedGear` 不会同步重算。当前单机本地路径（`SyncRule.Frame`，无预测回滚）没问题。接真实多人回滚时需将 `speedGear` 移入 `PlayerInputComponent`（单例，随指令同步）或在回滚时从 authority input 恢复。
