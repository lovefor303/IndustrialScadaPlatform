# Phase 4-M1 Offline Web Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task with verification checkpoints. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Serve one immutable published SCADA project through an offline ASP.NET Core runtime and render the same controls, pipes and text in a responsive browser without PLC or field-command access.

**Architecture:** Add a small `Scada.Runtime` class library above `Scada.Scene`, `Scada.Storage` and `Scada.Controls.Svg`. It loads only published project snapshots, evaluates bindings against an explicit deterministic variable source, and produces a runtime scene projection that preserves the retained model geometry. A separate `samples/Scada.Runtime.Preview` ASP.NET Core host exposes read-only JSON/SVG endpoints and a static Chinese browser shell; all runtime behavior is local and read-only.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, existing `Scada.Scene`/`Scada.Storage` contracts, existing `Scada.Controls` state resolver and `Scada.Controls.Svg` renderer, xUnit, `System.Text.Json`, HTML/CSS/vanilla JavaScript.

---

## Scope Guard

The implementation must stay inside `C:\Users\Administrator\.config\superpowers\worktrees\IndustrialScadaPlatform\phase3-m1-editor` (the authoritative repository is `D:\wpf_XM\IndustrialScadaPlatform`). Do not edit `D:\wpf_XM\配液系统湖南\配液系统2`, any TIA/PLC project, customer database, WinCC installation, or the permanent knowledge base. Phase 4-M1 does not add S7/OPC UA/Modbus, SignalR, authentication, alarms, trends, recipes, batches, PID, WinCC deployment or browser command endpoints.

## File Map

- Create `src/Scada.Runtime/Scada.Runtime.csproj`: runtime library references `Scada.Core`, `Scada.Scene`, `Scada.Storage`, `Scada.Controls` and `Scada.Controls.Svg`.
- Create `src/Scada.Runtime/RuntimeContracts.cs`: project-source, variable-source, quality and runtime-document contracts.
- Create `src/Scada.Runtime/JsonRuntimeProjectSource.cs`: validated JSON published-project loader.
- Create `src/Scada.Runtime/RevisionRuntimeProjectSource.cs`: immutable `RevisionStore` loader with explicit published-snapshot validation.
- Create `src/Scada.Runtime/SimulatedVariableSource.cs`: deterministic in-memory values, quality and timestamps; unknown keys return `Bad`.
- Create `src/Scada.Runtime/RuntimeSceneProjector.cs`: project/screen projection, binding evaluation, state metadata and SVG composition.
- Create `src/Scada.Runtime/RuntimeDiagnostics.cs`: stable diagnostic codes and Chinese operator-facing messages.
- Create `tests/Scada.Runtime.Tests/Scada.Runtime.Tests.csproj` and focused unit/acceptance tests for every runtime contract.
- Create `samples/Scada.Runtime.Preview/Scada.Runtime.Preview.csproj`, `Program.cs`, `RuntimeHostOptions.cs` and `wwwroot/index.html`, `wwwroot/app.css`, `wwwroot/app.js`.
- Modify `IndustrialScadaPlatform.sln`: include the runtime library, test project and preview host with the existing folder structure/configurations.
- Create `docs/phase4-m1-acceptance.md`: executable acceptance evidence and explicit exclusions.
- Modify `docs/handoff-current.md` and `docs/MASTER_ROADMAP.md`: record the active task, verification commands, artifacts and next gate.

### Task 1: Add Runtime Contracts and Test Project

**Files:**
- Create: `src/Scada.Runtime/Scada.Runtime.csproj`
- Create: `src/Scada.Runtime/RuntimeContracts.cs`
- Create: `src/Scada.Runtime/RuntimeDiagnostics.cs`
- Create: `tests/Scada.Runtime.Tests/Scada.Runtime.Tests.csproj`
- Create: `tests/Scada.Runtime.Tests/RuntimeContractTests.cs`
- Modify: `IndustrialScadaPlatform.sln`

- [x] **Step 1: Write failing contract tests.**

Add tests that construct `RuntimeProjectMetadata`, `RuntimeScreenDocument`, `RuntimeObjectProjection` and `RuntimeVariableValue`, then verify required IDs, non-empty names, UTC timestamps, read-only interaction metadata and stable diagnostic codes. Assert that an unknown variable is represented by `VariableQuality.Bad` rather than a fabricated value.

- [x] **Step 2: Run the focused tests and verify they fail for missing types.**

Run:

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
```

Expected: compilation failure because the runtime project and contracts do not exist yet.

- [x] **Step 3: Implement the minimal contracts.**

Use explicit records/interfaces with these signatures (names may not drift between tasks):

```csharp
public interface IRuntimeProjectSource
{
    Task<ProjectDocument> LoadAsync(CancellationToken cancellationToken = default);
}

public interface IRuntimeVariableSource
{
    RuntimeVariableValue Read(string key, DateTimeOffset now);
}

public sealed record RuntimeVariableValue(
    string Key,
    object? Value,
    VariableDataType DataType,
    VariableQuality Quality,
    DateTimeOffset Timestamp);

public sealed record RuntimeObjectProjection(
    Guid Id,
    string Type,
    RectD Bounds,
    double Rotation,
    int ZIndex,
    bool IsVisible,
    string? Svg,
    ControlState State,
    VariableQuality Quality,
    bool ReadOnly,
    IReadOnlyList<RuntimeDiagnostic> Diagnostics);

public sealed record RuntimeScreenDocument(
    string Name,
    RectD DesignBounds,
    string LayoutProfile,
    IReadOnlyList<RuntimeObjectProjection> Objects,
    IReadOnlyList<RuntimeDiagnostic> Diagnostics);
```

`RuntimeDiagnostic` must carry `Code`, Chinese `Message`, `ObjectId` and `Severity`; `RuntimeDiagnostics` must expose fixed codes for missing project, invalid project, draft project, missing screen, unknown variable and unsupported control.

- [x] **Step 4: Run focused tests and confirm they pass.**

Run the same `dotnet test` command; expected: all contract tests pass with zero warnings.

- [x] **Step 5: Commit the isolated contract slice.**

```powershell
git add src\Scada.Runtime tests\Scada.Runtime.Tests IndustrialScadaPlatform.sln
git commit -m "feat: add offline runtime contracts"
```

### Task 2: Implement Published Project Sources

**Files:**
- Create: `src/Scada.Runtime/JsonRuntimeProjectSource.cs`
- Create: `src/Scada.Runtime/RevisionRuntimeProjectSource.cs`
- Create: `tests/Scada.Runtime.Tests/RuntimeProjectSourceTests.cs`
- Modify: `src/Scada.Storage/RevisionStore.cs` only if the published JSON snapshot needs an explicit `ProjectStatus.Published` representation; add/extend `tests/Scada.Storage.Tests/RevisionStoreTests.cs` for that behavior.

- [x] **Step 1: Write failing source tests.**

Cover: a valid published JSON package loads; a draft JSON package is rejected with diagnostic code `project.not-published`; missing files produce `project.missing`; malformed/schema-invalid JSON produces `project.invalid`; a revision source loads one selected immutable revision and rejects a missing revision. Use temporary files/databases only.

- [x] **Step 2: Run the focused tests and verify the expected failures.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeProjectSourceTests
```

Expected: failures for the unimplemented source classes.

- [x] **Step 3: Implement `JsonRuntimeProjectSource`.**

Read the absolute path asynchronously, call `JsonProjectSerializer.Deserialize`, and fail closed unless `project.Status == ProjectStatus.Published` and `project.Screens.Count > 0`. Convert all file, JSON and schema exceptions to `RuntimeSourceException` carrying one stable diagnostic code and Chinese message; never expose a stack trace through the public contract.

- [x] **Step 4: Implement `RevisionRuntimeProjectSource` and fix the revision status boundary if required.**

Load exactly the requested `projectId`/`revisionId` through `RevisionStore.LoadRevisionAsync`. Ensure a published revision is represented as `ProjectStatus.Published` at the storage boundary, while drafts remain `ProjectStatus.Draft`. Do not make the runtime infer publication from a draft flag or from the presence of a row.

- [x] **Step 5: Run focused source and storage tests.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
dotnet test tests\Scada.Storage.Tests\Scada.Storage.Tests.csproj --configuration Release
```

Expected: all source/storage tests pass with zero warnings.

- [x] **Step 6: Commit the source boundary.**

```powershell
git add src\Scada.Runtime tests\Scada.Runtime.Tests src\Scada.Storage tests\Scada.Storage.Tests
git commit -m "feat: load published projects for runtime"
```

### Task 3: Implement the Deterministic Variable Source

**Files:**
- Create: `src/Scada.Runtime/SimulatedVariableSource.cs`
- Create: `tests/Scada.Runtime.Tests/SimulatedVariableSourceTests.cs`

- [x] **Step 1: Write failing simulator tests.**

Verify explicit bool, numeric and text values retain their declared data type; `Set` updates the timestamp and quality; `SetQuality` can produce `Uncertain` and `Bad`; repeated reads at the same timestamp are deterministic; unknown keys return `Bad` with a null value and never mutate the project variable definitions.

- [x] **Step 2: Run the focused tests and verify failure.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~SimulatedVariableSourceTests
```

- [x] **Step 3: Implement `SimulatedVariableSource`.**

Index only the variables supplied by the published project. Provide `Set(string key, object? value, VariableQuality quality, DateTimeOffset timestamp)` with data-type validation and a `Read` method that returns a stable `RuntimeVariableValue`. Include a deterministic demo profile factory that uses fixed values, not wall-clock random generation; optional timer updates must be injected and cancellable.

- [x] **Step 4: Run all runtime tests.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
```

Expected: PASS, zero warnings.

- [x] **Step 5: Commit the simulator slice.**

```powershell
git add src\Scada.Runtime tests\Scada.Runtime.Tests
git commit -m "feat: add deterministic runtime variable simulator"
```

### Task 4: Project and Render Runtime Screens

**Files:**
- Create: `src/Scada.Runtime/RuntimeSceneProjector.cs`
- Create: `tests/Scada.Runtime.Tests/RuntimeSceneProjectorTests.cs`
- Modify: `src/Scada.Controls.Svg/SvgControlRenderer.cs` only if a narrowly scoped composition hook is needed; preserve existing renderer behavior and tests.

- [x] **Step 1: Write failing projection tests.**

Create a published fixture containing a control, a text object and a pipe with bends. Assert that projection returns each object exactly once, retains ID/bounds/rotation/Z-index/visibility, preserves pipe start/end/bends, emits control SVG with `data-control-type`, `data-state` and `data-quality`, and produces a neutral diagnostic projection for an unsupported type without dropping the rest of the screen. Add tests for good/bad/unknown binding quality and read-only interaction metadata.

- [x] **Step 2: Run the focused tests and verify failure.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeSceneProjectorTests
```

- [x] **Step 3: Implement `RuntimeSceneProjector`.**

Project a named `ScreenDocument` into model coordinates. Sort objects by `ZIndex` using a stable ID tie-breaker. For `ControlObject`, build the existing `ControlRenderPlan` from its type, properties, bindings, dynamics and simulated values, then call `SvgControlRenderer.Render`. For `PipeObject`, serialize the independent pipe geometry as SVG without attaching or rerouting it. For `TextObject`, escape text and emit a text SVG fragment. Resolve state through the existing `ControlStateResolver`; bad or unknown quality must yield `ControlState.Unknown`. Return a `RuntimeScreenDocument` with design bounds and profile metadata; never mutate the source project.

- [x] **Step 4: Add layout profiles without changing model geometry.**

Implement `RuntimeLayoutProfile` for `desktop`, `tablet` and `phone`. Each profile changes viewport chrome/fit metadata only; object coordinates and relative placement remain identical. Add a test comparing object-coordinate pairs across all profiles.

- [x] **Step 5: Run focused and existing renderer tests.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
dotnet test tests\Scada.Controls.Svg.Tests\Scada.Controls.Svg.Tests.csproj --configuration Release
```

- [x] **Step 6: Commit the projection slice.**

```powershell
git add src\Scada.Runtime tests\Scada.Runtime.Tests src\Scada.Controls.Svg
git commit -m "feat: project published scenes to runtime SVG"
```

### Task 5: Add the ASP.NET Core Preview Host

**Files:**
- Create: `samples/Scada.Runtime.Preview/Scada.Runtime.Preview.csproj`
- Create: `samples/Scada.Runtime.Preview/Program.cs`
- Create: `samples/Scada.Runtime.Preview/RuntimeHostOptions.cs`
- Create: `tests/Scada.Runtime.Tests/RuntimeHostTests.cs`
- Modify: `IndustrialScadaPlatform.sln`

- [x] **Step 1: Write failing host tests.**

Use an in-process test host or a loopback process fixture to assert:

```text
GET /api/runtime/health                  -> 200 and source/simulator status
GET /api/runtime/project                 -> 200, project ID/name/status/screens
GET /api/runtime/screens/Main            -> 200, runtime screen JSON or SVG payload
GET /api/runtime/screens/Missing         -> 404 with screen.not-found
GET /api/runtime/project when draft      -> 400 with project.not-published
```

Assert that no route accepts `POST`, `PUT` or `DELETE` commands and that responses never contain stack traces or PLC library names.

- [x] **Step 2: Run host tests and verify failure.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeHostTests
```

- [x] **Step 3: Implement the host and options.**

Use ASP.NET Core minimal APIs with dependency injection for `IRuntimeProjectSource`, `IRuntimeVariableSource` and `RuntimeSceneProjector`. Accept `--project <absolute-json-path>`, `--db <absolute-sqlite-path> --project-id <guid> --revision-id <guid>`, and `--urls`; require exactly one project source. Default Kestrel binding must be `http://127.0.0.1:0` (or an explicitly supplied loopback URL), print the resolved URL, and reject non-loopback binding unless an explicit opt-in flag is present. Map only the four GET routes from the design. Return `application/json` diagnostics and set stable HTTP status codes 400/404/500 as specified; startup without a valid source exits non-zero with a Chinese console error.

- [x] **Step 4: Run host tests and a real HTTP smoke test.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
dotnet run --project samples\Scada.Runtime.Preview\Scada.Runtime.Preview.csproj --configuration Release -- --project <published-fixture.json> --urls http://127.0.0.1:50741
```

Use `Invoke-WebRequest` against `/api/runtime/health`, `/api/runtime/project` and `/api/runtime/screens/Main`; stop the process after the assertions. Expected: all GET responses succeed, draft/missing cases are controlled, and the process remains alive after invalid requests.

- [x] **Step 5: Commit the host slice.**

```powershell
git add samples\Scada.Runtime.Preview tests\Scada.Runtime.Tests IndustrialScadaPlatform.sln
git commit -m "feat: add offline runtime preview host"
```

### Task 6: Build the Chinese Responsive Browser Shell

**Files:**
- Create: `samples/Scada.Runtime.Preview/wwwroot/index.html`
- Create: `samples/Scada.Runtime.Preview/wwwroot/app.css`
- Create: `samples/Scada.Runtime.Preview/wwwroot/app.js`
- Create: `tests/Scada.Runtime.Tests/RuntimeBrowserAssetTests.cs`

- [x] **Step 1: Write failing static-asset tests.**

Assert that the shell contains Chinese labels for project, screen, connection quality and read-only status; uses an SVG viewport with `preserveAspectRatio`; references the CSS/JS assets; has no command form or PLC write URL; and declares desktop/tablet/phone media rules.

- [x] **Step 2: Run the asset tests and verify failure.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeBrowserAssetTests
```

- [x] **Step 3: Implement the shell.**

Use a restrained WinCC-like engineering layout: compact top status strip, screen selector, read-only quality indicator and a full-width SVG stage. `app.js` fetches project metadata and the selected screen, inserts returned SVG/object fragments, and shows Chinese diagnostics without throwing an uncaught exception. CSS must fit the same model scene on desktop/tablet/phone, use touch-sized controls on narrow screens, preserve object-relative placement, and keep all runtime controls read-only. Do not add marketing content, decorative cards or fake live controls.

- [x] **Step 4: Run asset tests and browser smoke checks.**

```powershell
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
```

Start the preview host and use the local browser/Playwright smoke path to verify the page is nonblank at desktop and mobile widths, the SVG is visible, screen selection works, and an invalid screen displays a Chinese diagnostic instead of blanking the page.

- [x] **Step 5: Commit the browser slice.**

```powershell
git add samples\Scada.Runtime.Preview tests\Scada.Runtime.Tests
git commit -m "feat: add responsive read-only runtime shell"
```

### Task 7: Phase 4-M1 Acceptance Gate

**Files:**
- Create: `tests/Scada.Runtime.Tests/Phase4M1AcceptanceTests.cs`
- Create: `docs/phase4-m1-acceptance.md`

- [x] **Step 1: Add the end-to-end acceptance fixture and tests.**

Exercise the same published JSON fixture through the project source, projector and HTTP host. Verify every retained object is present once, pipe points are byte-for-byte equivalent, supported controls carry SVG metadata, unsupported controls become placeholders, good/bad/unknown variables map to the expected state/quality, and draft/malformed/missing screen failures are stable.

- [x] **Step 2: Run the complete Release gate.**

```powershell
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
```

Expected: zero build warnings/errors, all existing tests plus Phase 4-M1 tests pass, and no diff whitespace errors.

- [x] **Step 3: Record acceptance evidence.**

Document commands, test counts, local URL smoke results, responsive screenshots/viewport checks, the published fixture identity and all explicitly excluded PLC/WebSocket/WinCC features in `docs/phase4-m1-acceptance.md`.

- [x] **Step 4: Commit the acceptance gate.**

```powershell
git add tests\Scada.Runtime.Tests docs\phase4-m1-acceptance.md
git commit -m "test: gate phase4 offline web runtime"
```

### Task 8: Update Handoff and Roadmap

**Files:**
- Modify: `docs/handoff-current.md`
- Modify: `docs/MASTER_ROADMAP.md`

- [x] **Step 1: Update the authoritative checkpoint.**

Record Phase 4-M1 status, the latest passing build/test/diff commands, intentionally dirty files (if any), preview executable path and one exact next action. State that PLC, SignalR, authentication, alarms, trends, recipes, batches, PID and WinCC deployment remain excluded until separately designed.

- [x] **Step 2: Run the final verification commands again.**

```powershell
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
```

- [x] **Step 3: Commit the handoff.**

```powershell
git add docs\handoff-current.md docs\MASTER_ROADMAP.md
git commit -m "docs: record phase4 m1 runtime handoff"
```

## Self-Review Against the Design

- Published-only loading: Tasks 2 and 7.
- JSON and immutable revision sources: Task 2.
- Deterministic variable values and quality: Task 3.
- Existing SVG controls, state metadata and independent pipes: Task 4.
- Desktop/tablet/phone profiles without model mutation: Tasks 4 and 6.
- Local read-only ASP.NET Core host and four GET routes: Task 5.
- Controlled diagnostics and unsupported-control placeholders: Tasks 2, 4, 5 and 7.
- Browser shell and HTTP smoke verification: Tasks 5, 6 and 7.
- Explicit PLC/SignalR/authentication/business-module exclusions: Scope Guard, Tasks 5 and 8.

No task depends on CAD/PDF recognition, legacy migration or field communication, so the implementation remains within Phase 4-M1.
