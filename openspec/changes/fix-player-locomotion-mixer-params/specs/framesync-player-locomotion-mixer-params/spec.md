## ADDED Requirements

### Requirement: Ground locomotion config preserves required mixer hierarchy
The FrameSync player animation configuration SHALL reference ground locomotion TransitionAssets at the resource layer that preserves the required Animancer mixer parameters for lock state, stance, speed, and direction. The system MUST NOT replace an old outer locomotion mixer with an inner `NoneLock/*` child resource unless the implementation provides equivalent parameter selection behavior elsewhere.

#### Scenario: Move loop uses complete mixer semantics
- **WHEN** `PlayerAnimConfig` is loaded for the FrameSync player
- **THEN** the move-loop animation path preserves access to the old walking/running and stance branches represented by `StandValue`, `SpeedValue`, and related locomotion parameters

#### Scenario: Config validation catches broken ground mapping
- **WHEN** a required ground locomotion TransitionAsset is missing or mapped to a resource that cannot reach required walk/run branches
- **THEN** configuration validation or diagnostics report the exact missing or invalid field before the user has to infer it from a wrong visible pose

### Requirement: Locomotion mixer parameters are driven by presentation state
The FrameSync animation presentation system SHALL set locomotion mixer parameters from existing FrameSync input and state data without storing Animancer resources, Animancer state, or visual-only mixer parameters in rollback snapshots.

#### Scenario: Standing locomotion selects standing branches
- **WHEN** the local player is not crouching and enters `Idle`, `MoveStart`, `MoveLoop`, or `MoveEnd`
- **THEN** the animation presentation sets the stance mixer so the selected branch is standing rather than crouched

#### Scenario: Locked locomotion selects lock branch
- **WHEN** `PlayerStateComponent.isLocked` is true and the player enters a ground locomotion state
- **THEN** the animation presentation sets the lock mixer to the locked branch when the active TransitionAsset exposes that parameter

#### Scenario: Visual parameters stay out of rollback state
- **WHEN** locomotion mixer parameters are updated during render-frame animation playback
- **THEN** `PlayerMoveComponent`, `PlayerStateComponent`, and rollback snapshot data do not contain Animancer objects, TransitionAssets, or visual-only parameter values

### Requirement: Running animation follows speed gear
The FrameSync player animation presentation SHALL select the walking or running branch according to the current speed gear used by the movement logic.

#### Scenario: Shift run selects run animation
- **WHEN** the player holds the project run input and `PlayerInputComponent.speedGear` is `2` while moving on the ground
- **THEN** the movement logic uses run speed and the animation mixer receives the run speed parameter value so the visible animation is a run

#### Scenario: Walk selects walk animation
- **WHEN** the player moves on the ground without the run input and `PlayerInputComponent.speedGear` is `1`
- **THEN** the movement logic uses walk speed and the animation mixer receives the walk speed parameter value so the visible animation is a walk

#### Scenario: Speed gear changes while moving
- **WHEN** the player toggles between walking and running while remaining in `MoveLoop`
- **THEN** the animation presentation updates the speed mixer during the ongoing state without requiring a logic-state transition

### Requirement: Crouch animation uses confirmed input and state contract
The FrameSync player SHALL restore crouch stance animation only through a confirmed project input and state contract. The implementation MUST NOT hard-code an unregistered key or invent an input name without verifying the existing input module and old controller behavior.

#### Scenario: Existing crouch input drives crouch stance
- **WHEN** the project input system exposes a crouch action and the player activates it while grounded
- **THEN** the FrameSync input/state path represents crouch intent and the animation presentation sets the stance mixer to the crouched branch

#### Scenario: Crouch affects gameplay facts
- **WHEN** old controller evidence shows crouch changes capsule height, movement speed, collision passability, or another deterministic gameplay fact
- **THEN** the FrameSync implementation represents that fact in deterministic logic state or components with rollback-safe copying

#### Scenario: Crouch input is not found
- **WHEN** implementation cannot find an existing crouch input binding or old-controller contract
- **THEN** the change remains incomplete and reports the missing contract instead of marking crouch animation restoration complete

### Requirement: Diagnostics identify locomotion mixer issues without warning spam
The FrameSync animation presentation SHALL provide targeted diagnostics for locomotion mixer configuration and parameter selection while avoiding per-frame Console spam.

#### Scenario: Missing parameter or transition is reported once
- **WHEN** a required locomotion TransitionAsset, mixer parameter, or branch cannot be resolved
- **THEN** the system logs a concise warning with the entity, logic state, field or parameter name, and does not repeat the same warning every render frame

#### Scenario: Runtime validation has clean Console after fix
- **WHEN** the user validates standing idle, walking, running, crouch idle, and crouch movement in EditorSimulateMode
- **THEN** the Console contains no repeated `Missing PlayerAnimConfig transition` or locomotion mixer parameter warnings for the validated ground states
