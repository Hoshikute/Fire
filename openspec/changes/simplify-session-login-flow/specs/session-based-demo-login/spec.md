## ADDED Requirements

### Requirement: Demo login SHALL create an anonymous player from the server session
The system SHALL treat the demo login flow as an anonymous session bootstrap. When a client enters the game, the server MUST create or attach a temporary player context for the current connection session, and the client MUST NOT provide its own identity as the authoritative player identifier.

#### Scenario: Login button starts anonymous session bootstrap
- **WHEN** the player clicks the `LoginWindow` enter button after connecting to the demo server
- **THEN** the client sends a login request that does not contain a client-authored authoritative `playerID`
- **THEN** the server creates a temporary player context bound to the current session
- **THEN** the server returns the assigned player information required for later game flow

### Requirement: Assigned player identity SHALL come from the server
The system SHALL use a server-assigned temporary player identifier as the only authoritative identity for matching, room entry, and frame-sync command ownership in the demo flow.

#### Scenario: Server returns assigned identity after login
- **WHEN** the server accepts an anonymous demo login request
- **THEN** the login response includes the server-assigned temporary player identifier
- **THEN** the client stores that assigned identifier as runtime session state for the current play session
- **THEN** later gameplay messages and local logs use the assigned identifier instead of a locally generated account name as identity

### Requirement: Demo UI SHALL not imply registration or credential login
The system SHALL present the demo entry flow as a lightweight enter-game action rather than a full account system. UI and protocol behavior MUST avoid implying that account, password, or registration data is required for the demo.

#### Scenario: Enter game without account credentials
- **WHEN** the player opens `LoginWindow`
- **THEN** the flow does not require username-password validation or registration
- **THEN** clicking enter can proceed with anonymous session bootstrap
- **THEN** any local input shown in the window is treated only as optional display information and not as authoritative identity

### Requirement: Pure session demo login SHALL not guarantee reconnect recovery
The system SHALL define the pure-session implementation as a minimal demo-only flow and MUST NOT claim support for restoring player identity across disconnected sessions unless a separate reconnect token or durable identity mechanism is added later.

#### Scenario: Session is lost after login
- **WHEN** the connection session is closed and a new session is later established
- **THEN** the system does not guarantee that the new session maps back to the previous anonymous player
- **THEN** the player may be required to enter the game again as a new temporary player
