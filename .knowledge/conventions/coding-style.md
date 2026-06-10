# 代码规范

## 署名
新增文件作者署名：HuHu <3112891874@qq.com>。

## 命名空间
- 帧同步 / 确定性逻辑层：`GameLogic`（含 `Module/FrameSync/GameLogic/` + `Module/FrameSync/ClientLogic/`）
- 工具类 / 通用基础设施：`TEngine`（非 `TEngine.Utility`——与已有 `static partial class Utility` 冲突）
- 旧 `ThirdPersonController` 命名空间已**完全删除**，不要新增文件到该 namespace

## 命名约定（沿用现有代码）
- 私有字段：`m_xxx`（如 `m_world`、`m_entityDict`）或 `_xxx`（控制器层较多用，如 `_currentState`、`_pendingCameraBind`）。同一文件内保持与周边一致。
- 公开属性：PascalCase（`FrameCount`、`StateMachine`）。
- 类型/方法：PascalCase。

## 逻辑与表现分离（重要）
- `GameLogic`（帧同步、ECS、回滚）必须确定性，禁止浮点逻辑运算、`UnityEngine.Random`、`Time.deltaTime`、`DateTime.Now`（ClientTime 是受控例外）。
- `ThirdPersonController`（动画、相机、输入表现）可以用 Unity 那套，但只读逻辑层状态，不反向写入逻辑。

## 确定性数据
- 逻辑层坐标/向量用 `SyncVector3`（int 定点，SCALE=1000），不要用 `Vector3` 参与逻辑。
- 需要回滚记录的组件继承 `MomentComponentBase`，实现真正的深拷贝 `DeepCopy()`。

## 注释
- 类/公开成员用 `/// <summary>` 中文注释（与现有代码一致）。
- 关键约束（如"为什么延迟切换""为什么深拷贝"）写明原因，便于维护和面试讲解。

## 事件订阅
- 状态/组件里订阅事件必须成对退订（`AddEventListening`/`RemoveEventListening`、`+=`/`-=`），防止泄漏与回调错乱。
