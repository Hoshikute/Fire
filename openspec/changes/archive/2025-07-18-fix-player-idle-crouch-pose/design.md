## 背景

`TPBattleContext` 现在通过 TEngine/YooAsset 资源路径加载 `PlayerAnimConfig`，并把它注入 `PlayerViewComponent`。最新运行日志确认配置已经加载成功，`PlayerAnimViewSystem` 播放了 `PlayerLogicState.Idle`，同时 `PlayerStateSystem` 在收到移动输入前一直保持 `Idle`。

当前剩余症状是纯表现问题：进入 Game 场景后，角色可能立刻显示为蹲伏或偏低的待机姿态。资产检查显示 `PlayerAnimConfig.idle` 和 `lockIdle` 都引用了 `NoneLock/IdleLoop.asset`。这个过渡资源是 `LinearMixerTransition`，有两个子项：阈值 `0` 的 `IdleCrouch`，以及阈值 `1` 的 `IldeStand`；它的 `_DefaultParameter` 为 `0`，参数名是 `StandValue`。本地 Animancer 包源码也确认，mixer transition 会用 `_DefaultParameter` 初始化 state，而 `MixerState<T>.Parameter` 是运行时计算子动画权重的值。

因此，即使状态机完全正确，也仍然可能出现这个问题：表现层播放了正确的逻辑状态，但 Idle 过渡默认落到了 crouch 侧。

## 目标 / 非目标

**目标：**

- 让非锁定 `Idle` 状态在 Game 场景启动后可见地播放站立待机姿态。
- 把修复限制在表现层或资源配置层，不进入 FrameSync 确定性逻辑和回滚快照。
- 保留现有 `PlayerAnimConfig` 资源加载路径和当前 `PlayerLogicState` 状态机。
- 保留备用待机资源，供后续蹲伏或锁定状态使用，而不是直接删除。
- 提供足够的诊断或校验，能判断运行时是否使用了预期的站立 Idle 过渡或 mixer 参数。

**非目标：**

- 不新增蹲伏玩法状态或输入行为。
- 不把旧 `ThirdPersonController` 状态数据重新引入为运行时权威数据。
- 不修改 `PlayerMoveSystem`、`PlayerStateSystem`、固定步长、回滚数据或确定性移动数学。
- 不在本变更里一次性迁移所有 Jump/Fall/Vault/Climb 等可选动画。
- 不绕过 TEngine `GameModule.Resource` 去使用 `Resources.Load`、`AssetDatabase` 或硬编码绝对路径。

## 决策

### D1：把问题定位为表现层 Idle 选择错误

运行日志已经显示 `PlayerAnimConfig` 加载成功，并且 `PlayerAnimViewSystem` 调用了 `PlayAnim state=Idle prevState=Idle`。因此本设计只针对 `Idle` 使用的资产或 Animancer mixer 选择，不修改状态切换和输入处理。

备选方案：修改 `PlayerStateSystem`，让出生后进入另一个状态。这只是用新逻辑状态掩盖表现问题，而且会破坏确定性状态和视觉动画选择之间的边界。

### D2：优先在 `PlayerAnimViewSystem` 中显式选择站立 Idle

当 `PlayerLogicState.Idle` 被播放时，表现系统应确保最终选择的是站立待机。如果配置的过渡资源是使用 `StandValue` 的 `LinearMixerState`，系统可以在 `animancer.Play(...)` 后把参数设置到站立阈值。这让站立/蹲伏的选择留在视觉状态分发附近，也避免编辑确定性组件。

备选方案：只把 `NoneLock/IdleLoop.asset` 里的 `_DefaultParameter` 从 `0` 改成 `1`。这是一种更小的资产级修复，如果运行时 API 路径不稳定可以作为兜底；但它会全局改变共享过渡资源的默认值。实现前应先确认这个共享默认值是否会影响其他消费者。

### D3：继续让 `PlayerAnimConfig` 作为 FrameSync 动画映射的唯一入口

`PlayerAnimConfig.asset` 仍然是 `TPBattleContext` 消费的资源化映射。修复可以调整 `idle` 引用的过渡资源，也可以增加用于站立 Idle 选择的辅助元数据，但 FrameSync 启动链路仍应只加载一份配置资源并注入 `PlayerViewComponent`。

备选方案：运行时读取旧 `Player SO.asset` 来恢复动画参数。这会制造第二套动画权威数据，并把 FrameSync 路径重新耦合回旧控制器数据。

### D4：只在有诊断价值时记录 Idle 选择信息

诊断应帮助区分三类问题：配置缺失、`Idle` 没有播放、`Idle` 播放了但 mixer 侧选错。新增日志应保持简短，并限制为一次性或状态切换时输出，避免每个渲染帧刷屏。

备选方案：Idle 期间每帧输出日志。这会淹没 `PlayerMoveSystem` 和 `PlayerStateSystem` 已经提供的状态切换信息。

## 风险 / 权衡

- [风险] 设置嵌套 mixer 参数可能需要正确转换 `AnimancerState` 或遍历子 state -> 缓解：实现前对照本地 Animancer 包确认具体运行时 state 类型；如果路径不稳定，再退回资产默认值修复。
- [风险] 把 `NoneLock/IdleLoop.asset` 默认值从 crouch 改为 stand 可能影响旧消费者或编辑器工具 -> 缓解：搜索当前引用，并在变更后跑 Game 场景验证。
- [风险] 修复默认 Idle 后，`lockIdle` 仍可能临时复用同一个过渡资源 -> 缓解：锁定/蹲伏完整动画迁移不纳入本变更，只保留清晰的兜底说明。
- [风险] 用户提供的剪贴板图片已经丢失，无法直接复看原始视觉截图 -> 缓解：最终验证以运行日志和 Unity Editor Game 场景实际画面为准。

## 迁移计划

1. 确认 `PlayerAnimConfig.idle`、`NoneLock/IdleLoop.asset`、`IdleCrouch` 和 `IldeStand` 的当前引用。
2. 实现窄范围站立 Idle 选择：优先在 `PlayerAnimViewSystem` 播放 Idle 后处理；如果有充分理由，再调整配置的 Idle 过渡或默认参数。
3. 运行可用的 hotfix C# 编译或构建检查。
4. 在 Unity EditorSimulateMode 中进入 Game 场景，验证启动 Idle、移动切换和回到 Idle。
5. 如果资产级修复影响旧消费者，回退资产改动并优先采用表现层参数路径。

## 待确认问题

- 当前项目里是否还有 FrameSync 之外的活跃运行时路径依赖 `NoneLock/IdleLoop.asset` 默认选择 crouch？
- `lockIdle` 是否需要在本变更里绑定专用过渡资源，还是继续作为后续锁定模式动画优化处理？
