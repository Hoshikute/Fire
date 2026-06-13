## ADDED Requirements

### Requirement: 空中动画映射存在
`PlayerAnimConfig` SHALL provide non-null animation transitions for the FrameSync player states `Jump`, `JumpInPlace`, `Fall`, and `Land`.

#### Scenario: 原地跳播放起跳动画
- **WHEN** `PlayerStateComponent.state` changes from `Idle` to `JumpInPlace`
- **THEN** `PlayerAnimViewSystem` MUST play the configured `jumpInPlace` transition instead of leaving the previous Idle or movement animation active

#### Scenario: 前跳播放起跳动画
- **WHEN** `PlayerStateComponent.state` changes from a ground movement state to `Jump`
- **THEN** `PlayerAnimViewSystem` MUST play the configured `jumpForward` transition instead of leaving the previous movement animation active

#### Scenario: 下落播放空中循环
- **WHEN** `PlayerStateComponent.state` changes to `Fall`
- **THEN** `PlayerAnimViewSystem` MUST play a configured fall transition, using `fallStart` followed by `fallLoop` when both exist, or `fallLoop` as the direct fallback when `fallStart` is absent

#### Scenario: 落地播放落地动画
- **WHEN** `PlayerStateComponent.state` changes from `Jump`, `JumpInPlace`, or `Fall` to `Land`
- **THEN** `PlayerAnimViewSystem` MUST play the configured `land` transition before returning to Idle

### Requirement: 空中动画来源可追溯
The air animation transitions SHALL be migrated from existing project animation resources rather than guessed or replaced with unrelated placeholder clips.

#### Scenario: 迁移旧跳跃资源
- **WHEN** `jumpInPlace`, `jumpForward`, `fallStart`, `fallLoop`, or `land` is assigned in `PlayerAnimConfig`
- **THEN** each assigned transition MUST reference an existing project animation clip that can be traced back to the old `Player SO.asset` jump/fall/land configuration or an explicitly documented equivalent resource

#### Scenario: 保留旧资源不删除
- **WHEN** new `TransitionAsset` wrappers are created for jump/fall/land clips
- **THEN** the original animation clips and old source configuration assets MUST remain available for comparison and rollback

### Requirement: 空中动画仍属于表现层
Air animation playback SHALL remain presentation-only and MUST NOT write Unity animation resources, Animancer runtime state, or visual-only flags into deterministic FrameSync logic components.

#### Scenario: 播放跳跃动画不写逻辑组件
- **WHEN** `PlayerAnimViewSystem` plays `jumpForward`, `jumpInPlace`, `fallStart`, `fallLoop`, or `land`
- **THEN** it MUST NOT modify `PlayerMoveComponent`, `PlayerStateComponent`, `PlayerInputComponent`, or any rollback snapshot component

### Requirement: 缺失空中映射可提前诊断
The system SHALL diagnose missing Jump/Fall/Land mappings before or at the moment they affect visible animation playback.

#### Scenario: 配置加载后发现空中映射缺失
- **WHEN** `PlayerAnimConfig` is loaded and validated for the Game scene
- **THEN** the validation MUST identify missing `jumpForward`, `jumpInPlace`, `fallLoop`, or `land` mappings as air animation configuration gaps

#### Scenario: 运行时仍遇到空中映射缺失
- **WHEN** the player enters `Jump`, `JumpInPlace`, `Fall`, or `Land` and the corresponding transition is null
- **THEN** Console warning MUST name the missing `PlayerAnimConfig` field and state, and it MUST NOT repeat every render frame for the same missing field

### Requirement: 跳跃链路运行时可验证
The Game scene SHALL provide a verifiable visible chain for jumping, falling, landing, and returning to standing Idle.

#### Scenario: 原地跳完整链路
- **WHEN** the local player starts from standing Idle and the user presses Jump without movement input
- **THEN** the visible animation MUST transition through in-place jump, fall or airborne loop, land, and then the same standing Idle pose

#### Scenario: 移动中跳跃完整链路
- **WHEN** the local player is moving and the user presses Jump
- **THEN** the visible animation MUST leave the movement loop, play a forward jump or airborne animation, land, and then return to the appropriate ground movement or standing Idle animation
