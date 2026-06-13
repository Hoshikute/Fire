## ADDED Requirements

### Requirement: TPBattleContext owns player FrameSync startup
`TPBattleContext` SHALL create and start the local player FrameSync world after the `Game` scene Player prefab has loaded, without requiring `PlayerFrameSyncEntry.Awake()`, `PlayerFrameSyncEntry.Start()`, or `AddComponent<PlayerFrameSyncEntry>()` to bootstrap gameplay.

#### Scenario: Game scene initializes local player FrameSync
- **WHEN** `TPBattleContext.InitializeGameScene()` finishes loading the Player prefab
- **THEN** the system SHALL create a `PlayerWorld`
- **AND** the system SHALL create the local player entity with `PlayerMoveComponent`, `PlayerStateComponent`, and `PlayerViewComponent`
- **AND** the system SHALL flush pending entity operations before setting the world to started

#### Scenario: Startup path does not depend on Entry MonoBehaviour
- **WHEN** the Player prefab does not contain `PlayerFrameSyncEntry`
- **THEN** `TPBattleContext` SHALL still complete Player FrameSync startup
- **AND** the system SHALL NOT add `PlayerFrameSyncEntry` at runtime as a required bridge

### Requirement: PlayerWorld remains an ECS world definition
`PlayerWorld` SHALL remain responsible for ECS system registration, snapshot component registration, and world-level simulation initialization only.

#### Scenario: PlayerWorld is constructed
- **WHEN** `PlayerWorld` is created by the startup flow
- **THEN** `PlayerWorld` SHALL provide its system order through `GetSystemTypes()`
- **AND** `PlayerWorld` SHALL provide rollback record component types through `GetRecordTypes()`
- **AND** `PlayerWorld` SHALL NOT load Player prefabs, find scene cameras, bind Cinemachine targets, or own scene shutdown logic

### Requirement: Startup injects explicit player view dependencies
`TPBattleContext` SHALL resolve the Unity view dependencies required by `PlayerViewComponent` during local player entity creation.

#### Scenario: Player view dependencies are available
- **WHEN** the loaded Player prefab provides a root `Transform`, an `AnimancerComponent`, and a configured `PlayerAnimConfig`
- **THEN** the local player entity's `PlayerViewComponent` SHALL receive those references before the world starts

#### Scenario: Player animation config is missing
- **WHEN** no valid `PlayerAnimConfig` can be resolved for the local player
- **THEN** the startup flow SHALL emit an explicit warning or error that names the missing dependency
- **AND** animation playback SHALL NOT fail only by silently skipping inside `PlayerAnimViewSystem`

### Requirement: TPBattleContext owns player FrameSync cleanup
`TPBattleContext` SHALL clean up the Player FrameSync world when the `Game` scene context shuts down or exits.

#### Scenario: Game scene context shuts down after startup
- **WHEN** `TPBattleContext.Shutdown()` is called after a Player FrameSync world was started
- **THEN** the system SHALL destroy or unregister that world from `FrameSyncModule`
- **AND** the system SHALL clear `TPBattleContext`'s local world reference
- **AND** no Player FrameSync world SHALL continue ticking for the destroyed Player instance

#### Scenario: Shutdown is called without completed startup
- **WHEN** `TPBattleContext.Shutdown()` is called before Player FrameSync startup completed
- **THEN** cleanup SHALL complete without throwing due to null world, null player instance, or missing camera target

### Requirement: Legacy Entry cannot create duplicate player worlds
The final startup path SHALL NOT allow `PlayerFrameSyncEntry` to create a second local `PlayerWorld` for the same `Game` scene player.

#### Scenario: Legacy Entry component exists during migration
- **WHEN** a Player instance still contains `PlayerFrameSyncEntry` during the migration window
- **THEN** only one local `PlayerWorld` SHALL be created for the `Game` scene player
- **AND** the active startup owner SHALL remain `TPBattleContext`

### Requirement: Knowledge documents reflect the new entry point
The project knowledge documents SHALL describe `TPBattleContext` as the Player FrameSync startup owner after this change.

#### Scenario: Developer searches for Player FrameSync entry point
- **WHEN** a developer reads `.knowledge` module documentation for Player FrameSync or Player controller startup
- **THEN** the documentation SHALL point to `TPBattleContext` for scene startup and cleanup
- **AND** the documentation SHALL NOT describe `PlayerFrameSyncEntry` as the required current runtime entry point
