# Phase 4-M2 Live Gateway and SignalR Design

Date: 2026-08-23  
Status: Approved by user on 2026-08-23
Repository: `D:\wpf_XM\IndustrialScadaPlatform`

## 1. Goal

Extend the Phase 4-M1 offline browser runtime with an authenticated,
cross-platform, read-only Gateway and SignalR transport. WPF Runtime and Web
Runtime must consume the same live variable contract, while PLC acquisition and
field commands remain outside this milestone.

M2 proves the boundary between a published SCADA project, a live data source,
authenticated clients and quality-aware rendering. It does not prove Siemens
communication and it must not create a path for browser clients to write to a
PLC.

## 2. Scope

Included:

- ASP.NET Core Gateway host for Windows and Linux.
- Cookie authentication with a separate SQLite `AuthStore`.
- `Viewer`, `Operator`, `Engineer` and `Admin` roles with explicit runtime and
  engineering permissions.
- SignalR read-only variable subscriptions and snapshot delivery.
- Initial full snapshots, incremental updates, sequence numbers and reconnect
  recovery.
- A provider lifecycle boundary with deterministic simulator and in-memory live
  provider implementations for tests and demonstrations.
- Published-project and variable-permission filtering.
- Data-source health, stale data, quality transitions and stable Chinese
  diagnostics.
- Loopback-first deployment configuration, explicit LAN mode and HTTPS rules.
- HTTP, SignalR, authentication, provider lifecycle, security and cross-runtime
  contract acceptance tests.

Excluded:

- Siemens S7, OPC UA, Modbus, PLCSIM or any PLC/network driver.
- Any field write, command endpoint, command hub method or browser-to-PLC path.
- Alarms, trends, audit reports, recipes, batches and PID modules.
- WinCC V8.1 deployment or Custom Web Control packaging.
- CAD/PDF recognition and legacy project/database migration.

## 3. Design Decisions

1. **Gateway is the only live-data boundary.** Browsers and WPF Runtime never
   connect directly to PLCs or data-source drivers.
2. **Read-only first.** M2 exposes no write API, even for controls that have
   command bindings in the project model. Command authorization and validated
   writes require a later, separately approved contract.
3. **Shared contracts.** M2 reuses `IRuntimeProjectSource`,
   `IRuntimeVariableSource`, `RuntimeVariableValue`, `VariableQuality` and the
   published `ProjectDocument` rules from M1. SignalR transport envelopes are
   additive and do not change project JSON.
4. **Published data only.** Runtime loads one immutable published project. Drafts,
   archived revisions and editor-only state are never exposed by the Gateway.
5. **Authentication is enabled by default.** An explicit loopback-only
   `--allow-anonymous` switch is available for automated previews and is always
   identified as development mode in startup diagnostics.
6. **Provider replacement is non-breaking.** M2 uses deterministic and in-memory
   providers. Phase 5 can add an S7 read-only provider by implementing the same
   provider interface without changing browser protocols or control renderers.

## 4. Architecture and Data Flow

```text
Published JSON / RevisionStore revision
                |
        IRuntimeProjectSource
                |
          RuntimeGateway
   +------------+-------------+----------------+
   |                          |                |
 REST project/health      AuthStore       SignalR Hub
   |                          |                |
   +-------------------- SnapshotCache ------+
                              |
                    IRuntimeDataProvider
                    +---------------------+
                    |                     |
             Deterministic simulator  In-memory live
             (tests/offline demo)      (M2 integration)
                              |
                    WPF Runtime / Web Runtime
```

The Gateway starts by validating the selected published project. It then starts
one data provider and maintains a latest-value cache. REST exposes project
metadata, health and diagnostics. SignalR authenticates the connection, checks
the requested screen and variables against the published project and streams
quality-aware snapshots. The runtime projection consumes those snapshots without
mutating the project document or changing control geometry.

Existing M1 boundaries remain the source of truth:

- `IRuntimeProjectSource` loads JSON or an immutable `RevisionStore` revision.
- `IRuntimeVariableSource` and `RuntimeVariableValue` represent typed values,
  timestamps and `VariableQuality`.
- `RuntimeSceneProjector` and the WPF/SVG renderers remain responsible for visual
  state; Gateway code does not redraw equipment.

## 5. Live Data Provider Contract

The provider boundary is asynchronous and lifecycle-aware. The exact C# names may
be finalized in the implementation plan, but the contract must provide these
operations and semantics:

```text
StartAsync(CancellationToken)
StopAsync(CancellationToken)
ReadSnapshotAsync(IReadOnlySet<string> keys, CancellationToken)
DataChanged event or async stream of typed variable updates
GetStatus() -> source state, last success, diagnostics
```

Provider states are:

```text
Stopped -> Starting -> Connected
                    -> Degraded
                    -> Disconnected
                    -> Stopped
```

The provider never owns authentication, project loading or browser connections.
It reports typed values and source timestamps; the Gateway applies project and
permission filtering, sequence numbers and stale-quality policy.

M2 providers:

- `DeterministicSimulator`: fixed values and timestamps for repeatable tests.
- `InMemoryLiveProvider`: controllable updates, disconnect/reconnect transitions
  and quality changes for integration tests and local demonstrations.

Phase 5's S7 read-only adapter will implement this same boundary. It may add
address polling internally, but S7 addresses must not appear in reusable control
definitions or browser payloads.

## 6. SignalR Contract

The Hub is read-only. It may expose only subscription operations equivalent to:

```text
Subscribe(screenName, variableKeys)
Unsubscribe(variableKeys)
RequestFullSnapshot()
```

There is no `WriteVariable`, `StartPump`, `StopPump`, `OpenValve`,
`CloseValve` or generic command method.

### 6.1 Subscription request

```json
{
  "screen": "Main",
  "variables": ["Tank.Level", "Pump.Run", "Pump.Fault"]
}
```

The server rejects unknown screens, unknown variables, variables not used by the
published project and requests over the configured subscription limit. Rejected
keys do not prevent valid keys in the same request from being subscribed.

### 6.2 Snapshot envelope

```json
{
  "sequence": 42,
  "source": "simulator",
  "serverTimestamp": "2026-08-23T12:00:00Z",
  "values": [
    {
      "key": "Tank.Level",
      "dataType": "float64",
      "value": 42.5,
      "quality": "good",
      "sourceTimestamp": "2026-08-23T11:59:59.900Z",
      "ageMs": 100
    }
  ]
}
```

`sequence` is monotonic per Gateway stream. A client that detects a gap requests
a full snapshot. A reconnect always re-authenticates, revalidates the screen and
subscription, receives a full snapshot, then resumes incremental updates.

### 6.3 Quality and freshness

- `Good` means the provider delivered a valid value within the freshness limit.
- `Uncertain` means the value is valid but older than the configured warning
  threshold.
- `Bad` means the provider is disconnected, the key is unknown, the value is
  type-invalid or no valid value exists.
- Unknown values carry `null`; retaining a last value is an explicit display
  policy and must not silently change quality to `Good`.

## 7. Authentication and Authorization

Authentication uses ASP.NET Core Cookie Authentication with an independent
SQLite `AuthStore`. Account records contain a password hash and role assignments;
passwords, cookies and PLC credentials are never stored in project JSON,
configuration logs or variable snapshots.

Roles and M2 permissions:

| Role | M2 permission |
| --- | --- |
| `Viewer` | View published screens, values, quality and diagnostics |
| `Operator` | Same read-only runtime permission; reserved for later validated commands |
| `Engineer` | Enter WPF engineering functions; no browser field writes |
| `Admin` | Manage users, roles and Gateway configuration |

Rules:

- Runtime REST and SignalR require authentication unless explicit development
  anonymous mode is enabled.
- Unauthenticated requests return `401`; authenticated users without the
  required permission return `403`.
- Cookies are HttpOnly and SameSite-restricted; Secure is required under HTTPS.
- SignalR connections bind the authenticated user to the published project,
  screen and variable permissions.
- CORS is disabled by default; no wildcard origin is allowed.
- First-run initialization creates an administrator through a local setup flow;
  there is no universal default password.
- Login failure, logout, rejected access and source disconnect are recorded as
  minimal security events without passwords, cookies or complete value dumps.

## 8. Gateway API and Diagnostics

M2 keeps the M1 read routes and adds authentication/session behavior:

- `GET /api/runtime/health`
- `GET /api/runtime/project`
- `GET /api/runtime/screens/{screenName}`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- SignalR hub at `/hubs/runtime`

No HTTP or SignalR write route is registered.

Stable diagnostic codes include:

- `AUTH_REQUIRED` - login is required;
- `ACCESS_DENIED` - role lacks the requested permission;
- `PROJECT_NOT_PUBLISHED` - source is not an immutable published project;
- `VARIABLE_UNKNOWN` - variable key does not exist in the published project;
- `SOURCE_DISCONNECTED` - provider is disconnected;
- `SOURCE_STALE` - value exceeds the freshness threshold;
- `SUBSCRIPTION_REJECTED` - screen, key, permission or limit rejection;
- `RUNTIME_READ_ONLY` - a write operation is not supported in M2.

Diagnostics are Chinese operator-readable messages plus stable machine codes.
Stack traces and sensitive data are never sent to clients.

## 9. Deployment and Configuration

Windows and Linux use the same ASP.NET Core host and configuration schema. M2
provides a command-line host and deployment notes; Windows Service and systemd
wrappers may be added without changing the protocol.

Configuration contains only:

- published JSON path or `RevisionStore` project/revision identifiers;
- authentication database path;
- listen URLs and loopback/LAN mode;
- freshness thresholds and subscription limits;
- provider selection and simulator settings.

The default listener is `http://127.0.0.1:<port>`. LAN mode is explicit and
requires HTTPS plus authentication. Mobile and tablet clients connect to the
Gateway URL; they never install or load a PLC driver.

## 10. Error and Recovery Behavior

- Invalid or missing published projects fail startup with a concise Chinese
  console diagnostic and non-zero exit code.
- Provider startup failure leaves the Gateway available for authenticated status
  and diagnostics; runtime values are `Bad` until recovery.
- A single malformed variable update is rejected and diagnosed without stopping
  the provider or other subscriptions.
- Recovery publishes a complete snapshot and resets each client's sequence base.
- Client-side reconnect backoff is bounded and visible in the status strip; it
  never retries a write because no write exists.

## 11. Testing and Acceptance Gate

Required automated coverage:

1. Login, logout, expired cookie, `401`, `403`, role permissions and first-run
   administrator initialization.
2. Published-project filtering, draft rejection, screen/key validation and
   subscription limits.
3. Full snapshot, incremental sequence, type preservation, timestamps and
   `Good`/`Uncertain`/`Bad` quality.
4. Sequence gap detection, reconnect re-authentication, resubscription and full
   snapshot recovery.
5. Provider lifecycle transitions, stale values, disconnect/reconnect and
   per-variable failure isolation.
6. WPF/Web consumers rendering equivalent states from the same snapshot contract.
7. No command route or hub method exists; CORS and loopback defaults are enforced.
8. Windows and Linux-compatible host configuration tests using the same fixtures.

The M2 gate requires:

```powershell
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
```

In addition, an authenticated HTTP/SignalR smoke test must prove login, project
load, subscription, quality transition, disconnect/reconnect and rejection of a
write attempt. Release build must have zero warnings and errors.

## 12. Phase Boundaries

M2 authorizes implementation of the authenticated live Gateway and read-only
SignalR transport only after this specification and a separate implementation
plan are approved. It does not authorize S7 code, PLC commands, WinCC adapter
work or optional business modules.

Phase 5 will define the Siemens S7 read-only adapter and its address/quality
mapping. Any command path requires a later contract covering whitelist, range,
role, confirmation, audit and PLC-owned safety interlocks.

The legacy medical-mixing project, legacy databases, TIA/PLC projects and the
technical knowledge base remain outside this repository and are not read or
modified by M2.
