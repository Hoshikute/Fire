## ADDED Requirements

### Requirement: SceneLauncher prepares local server before Play Mode

`SceneLauncher` SHALL prepare the local server environment before it enters Play Mode from the editor toolbar `Launcher` button.

#### Scenario: Server is already ready
- **WHEN** the user clicks the editor toolbar `Launcher` button and the configured local server endpoint is already listening
- **THEN** `SceneLauncher` opens the `main` scene and enters Play Mode without starting another server process

#### Scenario: Server is not ready
- **WHEN** the user clicks the editor toolbar `Launcher` button and the configured local server endpoint is not listening
- **THEN** `SceneLauncher` starts the repository server bootstrap script before entering Play Mode

#### Scenario: Server startup succeeds
- **WHEN** the server bootstrap script starts the local server and the configured endpoint becomes ready before timeout
- **THEN** `SceneLauncher` enters Play Mode

#### Scenario: Server startup fails
- **WHEN** the server bootstrap script fails or the configured endpoint does not become ready before timeout
- **THEN** `SceneLauncher` MUST cancel the automatic Play Mode entry and log an actionable editor error

### Requirement: Server bootstrap uses repository-relative paths

The server bootstrap script SHALL locate the `Server` directory relative to the current checkout or an explicit argument, and MUST NOT depend on a hard-coded absolute checkout path.

#### Scenario: Repository path differs from old local path
- **WHEN** the repository is checked out at `E:\EUGIT\Fire`
- **THEN** the server bootstrap script locates `Server/LockStepDemo.sln` under that checkout instead of using `D:\UGitD\Fire\Server`

#### Scenario: Script is launched from SceneLauncher
- **WHEN** `SceneLauncher` invokes the server bootstrap script
- **THEN** the script runs with a working directory or argument that resolves to the current repository's `Server` directory

### Requirement: SceneLauncher reports server preparation status

`SceneLauncher` SHALL report server preparation progress and failure reasons through Unity Editor logs.

#### Scenario: Existing server is reused
- **WHEN** the configured endpoint is already listening
- **THEN** Unity Console logs that the existing local server environment is being reused

#### Scenario: Script path is missing
- **WHEN** the expected server bootstrap script cannot be found
- **THEN** Unity Console logs the missing path and cancels automatic Play Mode entry

#### Scenario: Dependency is missing
- **WHEN** MySQL, MSBuild, the server solution, or the server executable cannot be found or started
- **THEN** Unity Console logs the failing dependency or script failure context

### Requirement: Client and server protocol configuration is validated

The launcher server preparation flow SHALL account for the configured client protocol and server listening mode before claiming the local server environment is ready.

#### Scenario: Protocol configuration is aligned
- **WHEN** the client network initialization protocol matches the local server listening mode and the configured endpoint is ready
- **THEN** `SceneLauncher` may proceed into Play Mode

#### Scenario: Protocol configuration is mismatched
- **WHEN** the client network initialization protocol does not match the local server listening mode
- **THEN** `SceneLauncher` MUST log a clear protocol mismatch warning or error before Play Mode entry

### Requirement: Runtime startup flow remains unchanged

The server environment preparation SHALL be limited to the Unity Editor `SceneLauncher` path and MUST NOT change the runtime TEngine Procedure startup flow.

#### Scenario: Normal Unity Play button is used
- **WHEN** the user enters Play Mode without clicking the `SceneLauncher` toolbar `Launcher` button
- **THEN** the server bootstrap script is not started by this feature

#### Scenario: ProcedureLaunch runs
- **WHEN** `ProcedureLaunch` runs after the `main` scene enters Play Mode
- **THEN** it continues to initialize launcher UI, language, and audio without starting the server environment itself

#### Scenario: Player build runs
- **WHEN** the project runs as a player build outside the Unity Editor
- **THEN** no `SceneLauncher` server bootstrap code is included or executed
