# Phase 4-M2 Live Gateway and SignalR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (\`- [ ]\`) syntax for tracking.

**Goal:** Add an authenticated, cross-platform, read-only ASP.NET Core Gateway and SignalR transport that serves the same published project and variable-quality contract to Web Runtime and future WPF Runtime consumers.

**Architecture:** Keep project loading and control projection in Scada.Runtime. Add a small Scada.Gateway host library for authentication, REST, SignalR, provider coordination and snapshot caching. The Gateway receives values only through IRuntimeDataProvider; M2 providers are deterministic/in-memory, and no PLC or command path is added.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, SignalR, Cookie Authentication, SQLite via Microsoft.Data.Sqlite, Microsoft.Extensions.Identity.Core password hashing, System.Text.Json, xUnit, Microsoft.AspNetCore.TestHost, SignalR .NET client for integration tests, and the local Microsoft SignalR JavaScript client for the existing responsive runtime shell.

---

## Scope Guard

Work only in C:\Users\Administrator\.config\superpowers\worktrees\IndustrialScadaPlatform\phase3-m1-editor, whose authoritative repository is D:\wpf_XM\IndustrialScadaPlatform. Do not modify the legacy 配液项目, any PLC/TIA project, customer database, WinCC installation or the permanent knowledge base. Do not add S7, OPC UA, Modbus, PLCSIM, field writes, alarms, trends, recipes, batches, PID or WinCC deployment code in this plan.

## File Map

- Modify: Directory.Packages.props with centrally pinned SignalR client, ASP.NET Identity Core and TestHost versions.
- Modify: IndustrialScadaPlatform.sln to include the Gateway library and Gateway tests.
- Modify: src/Scada.Runtime/RuntimeContracts.cs or create src/Scada.Runtime/LiveRuntimeContracts.cs for provider, source-status, update, subscription and snapshot records.
- Modify: src/Scada.Runtime/SimulatedVariableSource.cs to implement the provider lifecycle without changing deterministic read behavior.
- Create: src/Scada.Runtime/RuntimeSnapshotApplier.cs and focused tests for shared WPF/Web snapshot application semantics.
- Create: src/Scada.Gateway/Scada.Gateway.csproj, GatewayOptions.cs, GatewayApplication.cs, RuntimeHub.cs, RuntimeSubscriptionRegistry.cs, RuntimeSnapshotCache.cs, RuntimeDataCoordinator.cs, AuthContracts.cs, AuthStore.cs, GatewayAuthorization.cs and GatewayDiagnostics.cs.
- Create: tests/Scada.Gateway.Tests/Scada.Gateway.Tests.csproj with unit and TestServer/SignalR integration tests.
- Modify: samples/Scada.Runtime.Preview project files and wwwroot assets to compose the Gateway and live browser client.
- Modify: tests/Scada.Runtime.Tests/RuntimeBrowserAssetTests.cs and create tests/Scada.Gateway.Tests/Phase4M2AcceptanceTests.cs.
- Modify: docs/handoff-current.md, docs/MASTER_ROADMAP.md and create docs/phase4-m2-acceptance.md.

### Task 1: Shared Live Runtime Contracts and Project Skeleton

**Files:**
- Modify: Directory.Packages.props
- Modify: IndustrialScadaPlatform.sln
- Modify: src/Scada.Runtime/Scada.Runtime.csproj
- Create: src/Scada.Runtime/LiveRuntimeContracts.cs
- Create: src/Scada.Runtime/RuntimeSnapshotApplier.cs
- Create: src/Scada.Gateway/Scada.Gateway.csproj
- Create: tests/Scada.Gateway.Tests/Scada.Gateway.Tests.csproj
- Create: tests/Scada.Gateway.Tests/LiveRuntimeContractTests.cs
- Create: tests/Scada.Runtime.Tests/RuntimeSnapshotApplierTests.cs

- [ ] Step 1: Write failing contract tests.

Add tests for these exact contracts:

    public enum RuntimeSourceState { Stopped, Starting, Connected, Degraded, Disconnected }

    public sealed record RuntimeSourceStatus(
        RuntimeSourceState State,
        string Source,
        DateTimeOffset ChangedAt,
        DateTimeOffset? LastSuccessfulRead,
        IReadOnlyList<RuntimeDiagnostic> Diagnostics);

    public sealed record RuntimeVariableUpdate(
        RuntimeVariableValue Value,
        string Source,
        DateTimeOffset ReceivedAt);

    public interface IRuntimeDataProvider : IAsyncDisposable
    {
        RuntimeSourceStatus Status { get; }
        event EventHandler<RuntimeVariableUpdate>? Updated;
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        ValueTask<RuntimeVariableValue> ReadAsync(
            string key,
            DateTimeOffset now,
            CancellationToken cancellationToken = default);
    }

    public sealed record RuntimeSubscriptionRequest(
        string Screen,
        IReadOnlyList<string> Variables);

    public sealed record RuntimeSnapshotValue(
        string Key,
        VariableDataType DataType,
        object? Value,
        VariableQuality Quality,
        DateTimeOffset SourceTimestamp,
        long AgeMilliseconds);

    public sealed record RuntimeSnapshot(
        long Sequence,
        string Source,
        DateTimeOffset ServerTimestamp,
        IReadOnlyList<RuntimeSnapshotValue> Values,
        RuntimeSourceStatus SourceStatus);

Verify snapshots preserve null bad values, UTC timestamps, source status and monotonic sequence. Verify RuntimeSnapshotApplier sends the same values and quality to a sink without mutating project definitions.

- [ ] Step 2: Run focused tests and confirm the expected compile failures.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeSnapshotApplierTests

Expected: missing contract and project errors.

- [ ] Step 3: Add package and project references.

Pin Microsoft.AspNetCore.SignalR.Client 10.0.0, Microsoft.AspNetCore.TestHost 10.0.0 and Microsoft.Extensions.Identity.Core 10.0.0 in Directory.Packages.props. Add the Gateway project to the solution and reference Scada.Runtime, Scada.Core and Scada.Storage. Keep ASP.NET dependencies out of Scada.Core and Scada.Scene.

- [ ] Step 4: Implement contracts and snapshot applier.

RuntimeSnapshotApplier accepts an IRuntimeVariableSink:

    public interface IRuntimeVariableSink
    {
        void Apply(RuntimeSnapshot snapshot);
    }

It applies only known keys, preserves Bad/Uncertain quality and ignores duplicate or older sequence numbers. It never alters ProjectDocument or converts a command variable into confirmed feedback.

- [ ] Step 5: Run focused tests and commit the skeleton.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeSnapshotApplierTests
    git add Directory.Packages.props IndustrialScadaPlatform.sln src\Scada.Runtime src\Scada.Gateway tests\Scada.Gateway.Tests tests\Scada.Runtime.Tests
    git commit -m "feat: add live runtime transport contracts"

### Task 2: Provider Lifecycle and Snapshot Cache

**Files:**
- Modify: src/Scada.Runtime/SimulatedVariableSource.cs
- Create: src/Scada.Gateway/RuntimeSnapshotCache.cs
- Create: src/Scada.Gateway/RuntimeDataCoordinator.cs
- Create: tests/Scada.Gateway.Tests/RuntimeSnapshotCacheTests.cs
- Modify: tests/Scada.Runtime.Tests/SimulatedVariableSourceTests.cs

- [ ] Step 1: Write failing lifecycle and cache tests.

Cover Stopped to Starting to Connected, explicit Disconnected and recovery. Verify provider updates carry source timestamps; cache sequence starts at 1, increments for updates, produces full snapshots, rejects stale updates, and maps freshness to Good, Uncertain or Bad without fabricating values.

- [ ] Step 2: Run focused tests and verify failure.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeSnapshotCacheTests
    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~SimulatedVariableSourceTests

- [ ] Step 3: Make SimulatedVariableSource implement the provider contract.

Keep existing Read and Set semantics. StartAsync changes status to Starting then Connected; StopAsync becomes Stopped; Set raises Updated; SetQuality raises an update; a disconnected provider returns Bad values and does not throw from the coordinator.

- [ ] Step 4: Implement RuntimeSnapshotCache and RuntimeDataCoordinator.

The cache owns the sequence counter, source name, latest values, last-success timestamps and freshness thresholds. The coordinator starts/stops exactly one provider, subscribes to updates, converts stale values, and exposes GetFullSnapshot(keys) plus GetIncrementalSnapshot(keys). A provider failure changes source status and publishes a diagnostic but leaves the Gateway process alive.

- [ ] Step 5: Run focused and full runtime tests, then commit.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
    git add src\Scada.Runtime src\Scada.Gateway tests\Scada.Runtime.Tests tests\Scada.Gateway.Tests
    git commit -m "feat: add runtime provider lifecycle and snapshot cache"

### Task 3: AuthStore, First-Run Setup and Authorization Policies

**Files:**
- Create: src/Scada.Gateway/AuthContracts.cs
- Create: src/Scada.Gateway/AuthStore.cs
- Create: src/Scada.Gateway/GatewayAuthorization.cs
- Create: src/Scada.Gateway/GatewayDiagnostics.cs
- Create: tests/Scada.Gateway.Tests/AuthStoreTests.cs
- Create: tests/Scada.Gateway.Tests/AuthorizationTests.cs

- [ ] Step 1: Write failing authentication tests.

Use a temporary SQLite database and verify schema creation, first-admin creation only when no user exists, password hash verification, duplicate username rejection, disabled-user rejection, role assignment, and no password or cookie value appears in diagnostic text.

- [ ] Step 2: Run tests and confirm failure.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~AuthStoreTests

- [ ] Step 3: Implement the auth schema and store.

Create Users and UserRoles tables with parameterized SQL, UTC creation/update timestamps, unique usernames and disabled flags. Use PasswordHasher<AuthUser> from Microsoft.Extensions.Identity.Core; never store or log plaintext passwords. CreateFirstAdminAsync fails if any user exists.

- [ ] Step 4: Implement role policies.

Define Viewer, Operator, Engineer and Admin constants. Register Runtime.View for Viewer/Operator/Engineer/Admin and Gateway.Manage for Admin. Engineer permissions are consumed by the WPF editor and do not grant browser writes. Add stable AUTH_REQUIRED and ACCESS_DENIED diagnostics.

- [ ] Step 5: Run focused tests and commit.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    git add src\Scada.Gateway tests\Scada.Gateway.Tests
    git commit -m "feat: add local gateway authentication store"

### Task 4: Compose Gateway REST, Login and Setup Endpoints

**Files:**
- Create: src/Scada.Gateway/GatewayOptions.cs
- Create: src/Scada.Gateway/GatewayApplication.cs
- Modify: samples/Scada.Runtime.Preview/Scada.Runtime.Preview.csproj
- Modify: samples/Scada.Runtime.Preview/Program.cs
- Modify: samples/Scada.Runtime.Preview/RuntimeHostOptions.cs
- Modify: samples/Scada.Runtime.Preview/RuntimeHostApplication.cs
- Create: tests/Scada.Gateway.Tests/GatewayHttpTests.cs

- [ ] Step 1: Write failing HTTP integration tests.

Using WebApplicationFactory or TestServer, verify:

    GET  /api/runtime/health       -> 200 (authenticated or explicit test anonymous mode)
    GET  /api/runtime/project      -> 200 after login; 401 before login
    POST /api/auth/login           -> 200 for valid user, 401 for invalid user
    POST /api/auth/logout          -> 204 and subsequent runtime request is 401
    POST /api/auth/initialize      -> one-time success when AuthStore is empty, then 404
    GET  /api/runtime/project      -> 403 for an authenticated user without Runtime.View
    POST /api/runtime/write        -> 404; no write route exists

Assert JSON diagnostics are stable Chinese messages without stack traces.

- [ ] Step 2: Run tests and confirm failure.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~GatewayHttpTests

- [ ] Step 3: Implement Gateway options and composition.

GatewayOptions contains project source, auth database path, anonymous-development flag, freshness thresholds, subscription limit and listen URLs. GatewayApplication.Build registers project source, coordinator, cache, auth store, cookie authentication, policies and JSON enum options. Preserve M1 GET routes and map login/logout/setup as explicit POST routes.

- [ ] Step 4: Implement first-run setup safely.

When the auth database is empty, generate a one-time random setup token in memory and print only a short local setup instruction. Accept setup only on loopback, require the token plus username/password, invalidate the token after success and refuse LAN mode while setup is pending. --allow-anonymous is allowed only for loopback test previews and does not create a production account.

- [ ] Step 5: Implement login/logout and route authorization.

Use HttpOnly/SameSite-restricted cookies, Secure cookies when HTTPS is enabled, no wildcard CORS and no URL tokens. Return 401/403 according to the design. Keep health diagnostics minimal and do not include account or provider secrets.

- [ ] Step 6: Run HTTP tests and a loopback smoke test, then commit.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    dotnet run --project samples\Scada.Runtime.Preview\Scada.Runtime.Preview.csproj --configuration Release -- --project C:\Users\Administrator\.config\superpowers\worktrees\IndustrialScadaPlatform\phase3-m1-editor\tests\Scada.Runtime.Tests\Fixtures\runtime-published.json --auth-db C:\Users\Administrator\AppData\Local\Temp\scada-phase4-m2-auth.db --urls http://127.0.0.1:50741
    git add src\Scada.Gateway samples\Scada.Runtime.Preview tests\Scada.Gateway.Tests
    git commit -m "feat: add authenticated runtime gateway"

Exercise setup, login, /api/runtime/project and logout with Invoke-WebRequest; confirm an unauthenticated request is rejected and the host remains alive.

### Task 5: SignalR Read-Only Hub and Subscription Registry

**Files:**
- Create: src/Scada.Gateway/RuntimeHub.cs
- Create: src/Scada.Gateway/RuntimeSubscriptionRegistry.cs
- Modify: src/Scada.Gateway/GatewayApplication.cs
- Create: tests/Scada.Gateway.Tests/RuntimeHubTests.cs
- Create: tests/Scada.Gateway.Tests/SignalRIntegrationTests.cs

- [ ] Step 1: Write failing Hub tests.

Connect with HubConnection using an authenticated TestServer cookie. Verify Subscribe(screen, keys) sends an immediate full snapshot, valid updates send increasing sequences, invalid screens/keys produce SUBSCRIPTION_REJECTED, RequestFullSnapshot returns the complete cache, and disconnect/reconnect requires authentication and subscription validation again. Assert endpoint/reflection inspection finds no write method.

- [ ] Step 2: Run focused tests and confirm failure.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~SignalRIntegrationTests

- [ ] Step 3: Implement RuntimeSubscriptionRegistry.

Track connection ID, user ID, published project ID, screen and validated variable keys. Enforce a configured maximum key count and clear subscriptions when a connection closes. Never accept keys not present in the selected published screen/project.

- [ ] Step 4: Implement RuntimeHub.

Authorize with Runtime.View. Add only Subscribe, Unsubscribe and RequestFullSnapshot. Send snapshot and diagnostic client events. Use IHubContext<RuntimeHub> from the coordinator to publish updates to each connection's validated key set. Preserve per-stream sequence numbers and send a full snapshot after reconnect.

- [ ] Step 5: Run SignalR integration tests and commit.

    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    git add src\Scada.Gateway tests\Scada.Gateway.Tests
    git commit -m "feat: add read-only runtime signalr hub"

### Task 6: Shared Web Runtime Live Client and Diagnostics

**Files:**
- Modify: samples/Scada.Runtime.Preview/wwwroot/index.html
- Modify: samples/Scada.Runtime.Preview/wwwroot/app.js
- Modify: samples/Scada.Runtime.Preview/wwwroot/app.css
- Modify: tests/Scada.Runtime.Tests/RuntimeBrowserAssetTests.cs
- Create: tests/Scada.Gateway.Tests/RuntimeBrowserLiveTests.cs

- [ ] Step 1: Write failing asset and browser contract tests.

Assert that the shell references a local SignalR JavaScript client, contains login/setup states, displays connection state and quality, retries with bounded backoff, requests a full snapshot after reconnect, and contains no command URL or write method name.

- [ ] Step 2: Run tests and confirm failure.

    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeBrowserAssetTests
    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeBrowserLiveTests

- [ ] Step 3: Add the pinned local SignalR browser client.

Vendor the exact @microsoft/signalr 10.0.0 browser bundle into samples/Scada.Runtime.Preview/wwwroot/lib/signalr.min.js. Record package version 10.0.0 and its license in docs/phase4-m2-acceptance.md and do not load a CDN at runtime. The bundle is a dependency asset, not a project-specific control.

- [ ] Step 4: Implement login and setup UI.

The Chinese shell shows login required, first-run setup, authenticated project name, source state and read-only status. Login uses same-origin fetch POST with cookies; setup is available only when the Gateway reports pending setup. Do not display or persist passwords after the request.

- [ ] Step 5: Implement live subscription and rendering.

After project metadata loads, subscribe to variables used by the selected screen. Apply snapshots through the shared JSON shape, update SVG data-quality/state metadata, render Chinese diagnostics, and preserve the existing desktop/tablet/phone model-coordinate layout. On a sequence gap or reconnect call RequestFullSnapshot; never attempt a command call.

- [ ] Step 6: Run browser smoke tests and commit.

Start the Gateway with a temporary AuthStore and fixture, use Playwright at desktop and 390x844, verify login, nonblank SVG, live quality transition, reconnect diagnostic and read-only UI. Then run:

    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release
    git add samples\Scada.Runtime.Preview tests\Scada.Runtime.Tests tests\Scada.Gateway.Tests
    git commit -m "feat: connect web runtime to live read-only gateway"

### Task 7: Cross-Consumer Snapshot Parity and Security Regression Gate

**Files:**
- Modify: src/Scada.Runtime/RuntimeSnapshotApplier.cs
- Modify: src/Scada.Editor.Wpf/EditorShellViewModel.cs only if a transport-neutral runtime snapshot adapter is required; do not add PLC access to the editor.
- Create: tests/Scada.Runtime.Tests/CrossConsumerSnapshotTests.cs
- Create: tests/Scada.Gateway.Tests/GatewaySecurityTests.cs

- [ ] Step 1: Write failing parity/security tests.

Feed the same RuntimeSnapshot into the shared applier and Web JSON fixture. Assert equal key/type/value/quality/state outcomes, command variables remain unconfirmed, pipes remain independent, bad quality renders unknown state and project JSON is unchanged. Assert CORS is not wildcard, default URLs reject non-loopback, cookies are HttpOnly/SameSite, unauthenticated SignalR is rejected and no endpoint/method can write a variable.

- [ ] Step 2: Run focused tests and confirm failure.

    dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~CrossConsumerSnapshotTests
    dotnet test tests\Scada.Gateway.Tests\Scada.Gateway.Tests.csproj --configuration Release --filter FullyQualifiedName~GatewaySecurityTests

- [ ] Step 3: Implement only the shared adapter and security fixes required by the tests.

Keep the adapter transport-neutral. Do not add a WPF editor command, PLC library, command endpoint or field address. Any security failure must be fixed at the Gateway boundary, not hidden in the browser.

- [ ] Step 4: Run the complete solution tests and commit.

    dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
    git add src\Scada.Runtime src\Scada.Editor.Wpf tests\Scada.Runtime.Tests tests\Scada.Gateway.Tests
    git commit -m "test: verify live snapshot parity and gateway security"

### Task 8: Phase 4-M2 Acceptance Evidence and Handoff

**Files:**
- Create: tests/Scada.Gateway.Tests/Phase4M2AcceptanceTests.cs
- Create: docs/phase4-m2-acceptance.md
- Modify: docs/handoff-current.md
- Modify: docs/MASTER_ROADMAP.md

- [ ] Step 1: Add the end-to-end acceptance test.

Run one published fixture through AuthStore setup, login, REST project load, SignalR subscription, live update, stale/Bad quality, provider disconnect/reconnect, sequence recovery and an attempted write that returns controlled 404/RUNTIME_READ_ONLY. Assert the original project JSON is byte-for-byte unchanged.

- [ ] Step 2: Run the complete Release gate.

    dotnet restore IndustrialScadaPlatform.sln
    dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
    dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
    git diff --check

Expected: zero build warnings/errors, all existing tests plus M2 tests pass, no write endpoint exists and no whitespace errors are reported.

- [ ] Step 3: Run authenticated HTTP/SignalR smoke checks.

Start the preview host on loopback with temporary project/auth paths. Record login, project metadata, SignalR snapshot, quality transition, reconnect and read-only write rejection. Repeat host configuration tests with a Linux-compatible dotnet invocation when available; do not require a PLC or customer project.

- [ ] Step 4: Record evidence and boundaries.

docs/phase4-m2-acceptance.md must record test counts, build output, smoke URLs, the local SignalR client package/version/license, screenshots or viewport evidence and all exclusions. Update handoff-current.md and MASTER_ROADMAP.md so the single next action becomes a separate Phase 5 S7 read-only adapter design; commands remain excluded.

- [ ] Step 5: Commit the gate and push the PR branch.

    git add tests\Scada.Gateway.Tests docs\phase4-m2-acceptance.md docs\handoff-current.md docs\MASTER_ROADMAP.md
    git commit -m "test: gate phase4 live gateway"
    git push origin phase3-m1-editor

## Plan Self-Review

- Published-only project loading remains the M1 source boundary; Tasks 4 and 8 test it.
- Provider lifecycle, freshness, quality and recovery are covered by Tasks 2 and 8.
- AuthStore, first-run setup, roles, cookies and 401/403 are covered by Tasks 3 and 4.
- SignalR full snapshots, deltas, sequence gaps and reconnects are covered by Task 5 and the end-to-end gate.
- Web Runtime receives the same snapshot shape and stays read-only in Task 6.
- WPF/Web parity is transport-neutral and covered by Task 7; no PLC code is introduced.
- Security, loopback defaults, CORS and absence of write methods are covered by Tasks 4, 5, 7 and 8.
- No task reads legacy applications, databases, TIA projects, customer data or the knowledge base.

The plan does not authorize Phase 5 S7 acquisition, any command write, WinCC deployment or optional business modules.
