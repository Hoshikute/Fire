## ADDED Requirements

### Requirement: MoveLoop 奔跑转向不得误入侧跑分支
FrameSync 玩家在非锁定 `MoveLoop` 状态下奔跑左右转时，系统 SHALL 以表现层策略控制 `RotationValue`，避免因 `PlayerMoveSystem.faceDir` 的逻辑帧转向滞后而长期选择左右跑或斜跑动画分支。

#### Scenario: 非锁定奔跑左转保持自然前进跑姿态
- **WHEN** 玩家在非锁定状态按住跑步输入并持续向左调整移动方向
- **THEN** `MoveLoop` 的可见动画 SHALL 保持自然前进跑转向姿态，不出现由左右跑分支误选导致的明显侧倾

#### Scenario: 非锁定奔跑右转保持自然前进跑姿态
- **WHEN** 玩家在非锁定状态按住跑步输入并持续向右调整移动方向
- **THEN** `MoveLoop` 的可见动画 SHALL 保持自然前进跑转向姿态，不出现由左右跑分支误选导致的明显侧倾

### Requirement: RotationValue 修正不得污染确定性逻辑
系统 MUST 将 `RotationValue` 的限幅、归零、平滑或诊断缓存限制在表现层，不得写入 `PlayerMoveComponent`、`PlayerStateComponent` 或其它参与回滚快照的逻辑组件。

#### Scenario: 表现层参数不进入回滚快照
- **WHEN** `PlayerAnimViewSystem` 为奔跑转向计算并写入 Animancer `RotationValue`
- **THEN** `PlayerMoveComponent.DeepCopy()` 和 `PlayerStateComponent.DeepCopy()` 的回滚数据 MUST 不包含 Animancer 参数、平滑缓存或 TransitionAsset 引用

### Requirement: 起步方向选择保持可用
系统 SHALL 保留 `MoveStart` 的起步方向选择能力，不得为了稳定 `MoveLoop` 奔跑转向而让起步动画全部退化为正前方向。

#### Scenario: 斜向起步仍播放对应 MoveStart
- **WHEN** 玩家从 Idle 输入斜向移动并进入 `MoveStart`
- **THEN** 系统 SHALL 继续根据 `faceDir` 与 `moveDir` 的夹角选择相应的 `moveStart_*` 动画

### Requirement: 临时运行诊断验证完成后必须清理
系统 MAY 在验证期使用可搜索的 locomotion 参数诊断证明 `MoveLoop` 奔跑转向不再频繁越过跑步左右阈值，但在修复确认后 MUST 清理普通 Info 诊断，避免 Console 长期刷屏。

#### Scenario: 侧倾诊断日志不进入最终常规运行
- **WHEN** 奔跑转向侧倾已通过 Unity Editor 复测确认消失
- **THEN** Console MUST 不再输出 `Player locomotion mixer params` 或 `side-tilt diag` 普通 Info 日志

#### Scenario: 异常告警仍然保留
- **WHEN** 动画配置、相机初始化或其它关键运行依赖异常
- **THEN** 系统 SHALL 继续输出 warning 或 error 级别告警

### Requirement: Unity 复测必须区分参数问题与资源问题
实现完成后，验证流程 MUST 包含 `RotationValue=0` 对照或等价诊断，以区分侧倾是否来自 mixer 参数误选、跑步 clip 本身或 Transform 旋转。

#### Scenario: RotationValue 归零对照
- **WHEN** 本地验证将非锁定 `MoveLoop` 的 `RotationValue` 临时固定为 `0`
- **THEN** 验证记录 MUST 明确说明奔跑左右转侧倾是否消失，并据此判断后续修复方向
