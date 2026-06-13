## Context

FrameSync 玩家当前通过 `TPBattleContext -> PlayerAnimConfig -> PlayerAnimViewSystem` 播放动画。最近几轮修复已经让 `PlayerAnimConfig` 可以加载和校验，并补齐了 Idle 站立姿态与 Jump/Fall/Land 空中链路；最新 Console 也显示地面状态仍会正常切换 `MoveStart -> MoveLoop -> MoveEnd -> Idle`。

剩余现象集中在地面 locomotion 的 mixer 语义：角色逻辑可以移动、速度档位也会因为 Shift 写入 `speedGear=2`，但动画没有进入跑步分支；站立 Idle 被强制修正后，下蹲分支也无法再被选择。资源对照显示旧 `Player SO.asset` 使用外层资源，例如 `PlayerIdleLoop.asset`、`PlayerMoveLoop.asset`、`PlayerMoveEnd_L.asset` / `PlayerMoveEnd_R.asset`。这些外层资源会继续通过 `LockValue`、`StandValue`、`SpeedValue`、`RotationValue` 分发到非锁定/锁定、站立/下蹲、走/跑和方向子动画。当前 `PlayerAnimConfig.asset` 多处直接引用 `NoneLock/*` 内层资源，且 `PlayerAnimViewSystem` 只有 Idle 播放后设置了 `StandValue=1`，没有持续驱动其它 locomotion 参数。

该问题属于表现层和动画资源迁移缺口。FrameSync 逻辑层仍需要保持确定性，动画参数不能写入回滚快照。

## Goals / Non-Goals

**Goals:**

- 恢复地面移动动画的旧 mixer 层级，使站立、下蹲、走、跑和必要的方向分支可以被同一套 `PlayerAnimConfig` 正确引用。
- 在表现层根据当前输入和逻辑状态驱动 Animancer 参数：至少包括 `StandValue` 与 `SpeedValue`，并按资源需要处理 `RotationValue`、`LockValue`。
- 让 Shift 跑步的逻辑速度和动画速度分支一致：`speedGear=1` 播走路，`speedGear=2` 播跑步。
- 明确下蹲恢复边界：优先复用现有输入/状态；如果当前 FrameSync 输入层没有下蹲接口，先补齐清晰的输入/状态契约，再驱动 `StandValue=0`。
- 保持动画资源、mixer 参数和 Animancer state 只在表现层或配置层使用，不进入 `PlayerMoveComponent`、`PlayerStateComponent` 的回滚快照，除非新增的下蹲本身被确认为 gameplay 逻辑状态。

**Non-Goals:**

- 不重新接回旧 `ThirdPersonController` 运行链路，也不让旧 `Player SO.asset` 成为运行时第二套权威配置。
- 不重复处理 Jump/Fall/Land 空中动画配置；这些属于 `fix-player-air-animation-config` 的范围。
- 不完整迁移 Vault、Climb、LedgeClimb 等交互动画。
- 不修改帧同步固定步长、重力、移动速度常量或碰撞/斜坡确定性算法。
- 不凭空发明输入接口名称；如果找不到现有下蹲输入，需要先在实现中暴露为设计问题并更新 artifacts。

## Decisions

### D1：以旧外层 TransitionAsset 作为资源层级对照基准

实现时先建立当前 `PlayerAnimConfig.asset` 与旧 `Player SO.asset` 的地面动画映射表。凡旧配置使用外层 `Player*` 资源表达 `LockValue` 或其它高层 mixer 分支，而当前配置错误地直接接到 `NoneLock/*` 内层资源的字段，应优先恢复到等价外层资源，或在设计中明确为什么当前字段必须保持内层资源。

备选方案：继续保留当前内层资源，只在代码里手动遍历并选子 state。这样能局部修补某些分支，但会复制旧 mixer 已经表达过的资源层级，后续锁定、下蹲、方向分支会继续扩散成代码特例。

### D2：mixer 参数由 `PlayerAnimViewSystem` 统一驱动

`StandValue`、`SpeedValue`、`RotationValue`、`LockValue` 都是视觉动画选择参数，应在 `PlayerAnimViewSystem` 读取 `PlayerInputComponent`、`PlayerMoveComponent`、`PlayerStateComponent` 后设置。参数设置可以在状态切换播放动画前后执行，也可以在同状态持续更新时刷新；关键是不能只在 Idle 首帧写一次。

建议的第一版参数来源：

- `LockValue`: `st.isLocked ? 1 : 0`。
- `StandValue`: 站立为 `1`，下蹲为 `0`；下蹲来源必须来自已确认的输入/状态。
- `SpeedValue`: `input.speedGear >= 2 ? 2 : 1`，并在无移动输入时回到适合 Idle 的值。
- `RotationValue`: 根据 `input.moveDir` 和 `move.faceDir` 的角度映射到旧资源使用的方向阈值；若第一版只验证正向移动，可先记录为需要覆盖的后续任务，但不能破坏现有 8 向 `MoveStart` 选择。

备选方案：把这些参数存到逻辑组件。这样会让视觉 mixer 细节污染回滚快照，并把纯表现状态引入确定性逻辑。

### D3：下蹲是否进入逻辑层取决于现有输入和 gameplay 影响

当前快速搜索没有发现 FrameSync 层已接入 `Crouch` / `Duck` 类输入，`PlayerLogicState` 也没有 Crouch 状态。实现时必须先查 `InputButtonType`、输入配置和旧 TPC 下蹲语义：

- 如果旧下蹲只影响动画姿态，不影响碰撞胶囊、移动速度、可通过高度等 gameplay 事实，则可以作为表现层姿态参数接入，并保持逻辑状态不变。
- 如果旧下蹲影响胶囊高度、移动速度、可通过空间或其它确定性判断，则必须补齐输入意图和逻辑状态/组件字段，并确保参与快照的字段可回滚。

备选方案：直接在表现层读取某个硬编码按键控制 `StandValue`。这会绕过项目输入系统，也会让多人/回滚路径不可控。

### D4：诊断只覆盖状态切换和参数变更，不每帧刷屏

为了确认问题是否来自配置层级、参数未设置或输入缺失，应保留少量一次性或变更时日志，例如当前播放的 TransitionAsset 字段、`StandValue/SpeedValue` 的值和是否找到了下蹲输入。日志必须可搜索，但不能在每个渲染帧刷屏。

备选方案：直接删除现有 `[CODEX_LOG]` 或每帧打印全部参数。前者降低排查能力，后者会淹没 Console。

## Risks / Trade-offs

- [Risk] 恢复外层 `Player*` TransitionAsset 后，现有手写的 MoveStart 8 向选择可能和旧外层 mixer 的参数选择重叠。Mitigation：先逐项对照旧 `Player SO.asset`，只替换确认由旧配置直接引用的字段，不做全量重构。
- [Risk] `StandValue=1` 解决了站立 Idle，但会掩盖下蹲输入缺失。Mitigation：把下蹲输入/状态确认列为前置任务，不能只靠改默认值。
- [Risk] `SpeedValue` 只根据 Shift 设置为 1/2，可能和旧 TPC 的连续速度/加速曲线不完全一致。Mitigation：第一版对齐当前 FrameSync 已有 `speedGear` 离散档位，后续再考虑连续混合。
- [Risk] 如果下蹲影响碰撞体高度，仅恢复动画会造成视觉和逻辑体积不一致。Mitigation：实现前确认旧 TPC 下蹲是否改变 capsule；若改变，更新设计并扩展逻辑组件。
- [Risk] 手动改 Unity YAML 资源 GUID 容易接错。Mitigation：优先复用已有 `.asset` 和 `.meta`，改动后做 GUID 扫描、Unity 加载验证和 Game 场景视觉验证。

## Migration Plan

1. 建立旧 `Player SO.asset` 到新 `PlayerAnimConfig.asset` 的地面动画映射表，重点核对 `idle`、`moveLoop`、`moveEnd_L/R`、`moveStart_*`、`lockIdle`。
2. 扫描 `PlayerAnimacer/Parameter` 下的 `StandValue`、`SpeedValue`、`RotationValue`、`LockValue`，确认资源阈值和字段语义。
3. 修正 `PlayerAnimConfig.asset` 中需要恢复外层资源的字段引用。
4. 在 `PlayerAnimViewSystem` 增加统一的 locomotion 参数刷新逻辑，并确保 Idle、MoveStart、MoveLoop、MoveEnd、LockIdle 都使用同一套参数来源。
5. 查明现有输入系统是否支持下蹲；若支持，接入 `StandValue=0`；若不支持且下蹲是本变更必要目标，先更新 artifacts 再补最小输入/逻辑契约。
6. 运行 C# 构建检查、OpenSpec strict 校验，并在 Unity Game 场景验证站立/走/跑/下蹲/停止回 Idle。

## Open Questions

- 当前 TEngine `InputButtonType` 或输入配置中是否存在已命名的下蹲按键？如果没有，应由哪个输入绑定承担下蹲？
- 旧 TPC 的下蹲是否只影响动画，还是也修改 capsule 高度、移动速度或碰撞通过性？
- `PlayerAnimConfig.moveStart_*` 是否应继续保留当前 8 向字段，还是恢复到旧外层 `MoveStart` 资源后统一用 mixer 参数驱动？
- `RotationValue` 的第一版是否必须覆盖侧移/后退跑，还是先恢复正向走/跑并把完整方向混合作为后续任务？
