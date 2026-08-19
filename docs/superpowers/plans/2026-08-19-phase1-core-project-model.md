# Phase 1 Core Project Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first usable, PLC-independent project SDK that can create, validate, save, reopen, export, import, publish, restore, and simulate an empty industrial SCADA project without changing C# source code.

**Architecture:** Use a small .NET 10 SDK-style solution with platform-independent contracts in `Scada.Core`, retained scene/project types in `Scada.Scene`, persistence and revision logic in `Scada.Storage`, and deterministic offline values in `Scada.Simulator`. JSON is the interchange format; SQLite stores revision metadata and JSON payloads locally. No WPF UI, PLC adapter, gateway command path, or legacy-project migration is included in this phase.

**Tech Stack:** .NET 10, C# nullable reference types, `System.Text.Json`, JSON Schema Draft 2020-12, SQLite via `Microsoft.Data.Sqlite`, xUnit, `dotnet test`, `dotnet build`.

---

## File Map

Create the following files and projects. Keep each file focused on one responsibility.

- Create: `src/Scada.Core/Scada.Core.csproj` - SDK contracts and primitive value types.
- Create: `src/Scada.Core/ProjectContracts.cs` - project identity, revision, permission and validation contracts.
- Create: `src/Scada.Core/VariableContracts.cs` - variable data type, quality, engineering range and value contracts.
- Create: `src/Scada.Scene/Scada.Scene.csproj` - retained scene model.
- Create: `src/Scada.Scene/SceneContracts.cs` - project document, screens, scene objects, independent pipes and bindings.
- Create: `src/Scada.Storage/Scada.Storage.csproj` - JSON and SQLite persistence.
- Create: `src/Scada.Storage/JsonProjectSerializer.cs` - deterministic JSON serialization and schema checks.
- Create: `src/Scada.Storage/RevisionStore.cs` - draft/published/restore operations over SQLite.
- Create: `src/Scada.Storage/SchemaMigrations.cs` - explicit schema migration pipeline.
- Create: `src/Scada.Simulator/Scada.Simulator.csproj` - offline deterministic simulator.
- Create: `src/Scada.Simulator/OfflineSimulator.cs` - quality-aware simulated variable values.
- Create: `schemas/project-v1.schema.json` - JSON Schema for the first project format.
- Create: `tests/Scada.Core.Tests/Scada.Core.Tests.csproj` - contract test project.
- Create: `tests/Scada.Core.Tests/ContractTests.cs` - type and validation tests.
- Create: `tests/Scada.Storage.Tests/Scada.Storage.Tests.csproj` - persistence test project.
- Create: `tests/Scada.Storage.Tests/RevisionStoreTests.cs` - round-trip and revision tests.
- Create: `tests/Scada.Simulator.Tests/Scada.Simulator.Tests.csproj` - simulator test project.
- Create: `tests/Scada.Simulator.Tests/OfflineSimulatorTests.cs` - deterministic simulator tests.
- Create: `Directory.Build.props` - common compiler settings and warnings.
- Create: `Directory.Packages.props` - centrally pinned package versions.
- Create: `IndustrialScadaPlatform.sln` - solution containing all four libraries and three test projects.
- Create: `docs/superpowers/plans/phase1-test-fixtures/empty-project.json` - checked-in valid fixture.
- Create: `docs/superpowers/plans/phase1-test-fixtures/invalid-project.json` - checked-in invalid fixture used by schema tests.

The old repository at `D:\wpf_XM\配液系统湖南\配液系统2\.worktrees\medical-mixing-system-next` must not be modified by any task in this plan.

### Task 1: Create the solution skeleton

**Files:**
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `IndustrialScadaPlatform.sln`
- Create: all seven `.csproj` files listed in the file map

- [x] **Step 1: Write the project skeleton files**

Use SDK-style projects targeting `net10.0` with nullable enabled. Library projects reference only lower layers. Test projects reference the project under test and xUnit packages.

`Directory.Build.props` must contain:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props` must pin `Microsoft.Data.Sqlite` to `9.0.8`, `Newtonsoft.Json.Schema` is not allowed, and test projects must use `Microsoft.NET.Test.Sdk` `17.13.0`, `xunit` `2.9.2`, and `xunit.runner.visualstudio` `3.0.0`.

Each library `.csproj` must set `<OutputType>Library</OutputType>`. `Scada.Scene` references `Scada.Core`; `Scada.Storage` references both; `Scada.Simulator` references `Scada.Core`; tests reference their corresponding libraries. Do not add WPF or ASP.NET dependencies.

- [x] **Step 2: Generate the solution and verify project graph**

Run:

```powershell
dotnet sln IndustrialScadaPlatform.sln add src/Scada.Core/Scada.Core.csproj src/Scada.Scene/Scada.Scene.csproj src/Scada.Storage/Scada.Storage.csproj src/Scada.Simulator/Scada.Simulator.csproj tests/Scada.Core.Tests/Scada.Core.Tests.csproj tests/Scada.Storage.Tests/Scada.Storage.Tests.csproj tests/Scada.Simulator.Tests/Scada.Simulator.Tests.csproj
dotnet restore IndustrialScadaPlatform.sln
```

Expected: restore succeeds with no NU1900/NU1903/NU1904 vulnerability warning and `dotnet sln ... list` prints exactly seven projects.

- [x] **Step 3: Commit the skeleton**

```powershell
git add Directory.Build.props Directory.Packages.props IndustrialScadaPlatform.sln src tests
git commit -m "build: create phase one scada sdk solution"
```

### Task 2: Define core project and variable contracts

**Files:**
- Create: `src/Scada.Core/ProjectContracts.cs`
- Create: `src/Scada.Core/VariableContracts.cs`
- Test: `tests/Scada.Core.Tests/ContractTests.cs`

- [x] **Step 1: Write failing contract tests**

`ContractTests.cs` must include tests for stable IDs, invalid names, variable quality, and command separation. The test project references both `Scada.Core` and `Scada.Scene` because `ProjectDocument` is a scene-level aggregate:

```csharp
[Fact]
public void NewProject_has_stable_id_and_schema_version()
{
    var project = ProjectDocument.Create("Demo");
    Assert.NotEqual(Guid.Empty, project.ProjectId);
    Assert.Equal(1, project.SchemaVersion);
    Assert.Equal("Demo", project.Name);
}

[Theory]
[InlineData("")]
[InlineData("   ")]
public void Project_name_must_not_be_blank(string name)
{
    Assert.Throws<ArgumentException>(() => ProjectDocument.Create(name));
}

[Fact]
public void Command_and_feedback_variables_are_distinct()
{
    var command = VariableDefinition.Bool("Pump.StartCommand");
    var feedback = VariableDefinition.Bool("Pump.RunningFeedback");
    Assert.NotEqual(command.Key, feedback.Key);
    Assert.Equal(VariableDirection.Command, command.Direction);
    Assert.Equal(VariableDirection.Feedback, feedback.Direction);
}

[Fact]
public void Bad_quality_is_explicit_and_not_a_confirmed_value()
{
    var value = VariableValue.Unknown(VariableDataType.Bool, DateTimeOffset.UnixEpoch);
    Assert.Equal(VariableQuality.Bad, value.Quality);
    Assert.Null(value.Value);
}
```

Run `dotnet test tests/Scada.Core.Tests/Scada.Core.Tests.csproj --no-restore`. Expected: compile failure because the contracts do not exist yet.

- [x] **Step 2: Implement minimal contracts**

`ProjectContracts.cs` must define `ProjectIdentity`, `ProjectRevision`, `ProjectStatus`, `ProjectValidationError`, and the shared revision/validation records. `ProjectDocument` is defined in `Scada.Scene` because it owns screens and scene objects. Initialize collections to empty arrays and use UTC timestamps.

`VariableContracts.cs` must define:

```csharp
public enum VariableDataType { Bool, Int32, UInt32, Float64, String }
public enum VariableDirection { Feedback, Command, Parameter }
public enum VariableQuality { Good, Uncertain, Bad }
public sealed record VariableDefinition(string Key, VariableDataType DataType, VariableDirection Direction, string? Unit, double? Minimum, double? Maximum);
public sealed record VariableValue(VariableDataType DataType, object? Value, VariableQuality Quality, DateTimeOffset Timestamp)
{
    public static VariableValue Unknown(VariableDataType type, DateTimeOffset timestamp) => new(type, null, VariableQuality.Bad, timestamp);
}
```

Validate keys with `^[A-Za-z0-9_.-]+$`; reject empty keys, duplicate project variable keys, and minimum values greater than maximum values. `VariableDefinition.Bool` must create a feedback/command definition from an explicit direction rather than guessing it.

- [x] **Step 3: Run the contract tests**

Run:

```powershell
dotnet test tests/Scada.Core.Tests/Scada.Core.Tests.csproj --no-restore
```

Expected: all tests pass and no warnings are emitted.

- [x] **Step 4: Commit the contracts**

```powershell
git add src/Scada.Core tests/Scada.Core.Tests
git commit -m "feat: add core project and variable contracts"
```

### Task 3: Add the retained scene model and JSON Schema

**Files:**
- Create: `src/Scada.Scene/SceneContracts.cs`
- Create: `schemas/project-v1.schema.json`
- Create: `docs/superpowers/plans/phase1-test-fixtures/empty-project.json`
- Create: `docs/superpowers/plans/phase1-test-fixtures/invalid-project.json`
- Modify: `tests/Scada.Core.Tests/Scada.Core.Tests.csproj` to reference `src/Scada.Scene/Scada.Scene.csproj`.
- Test: `tests/Scada.Core.Tests/ContractTests.cs`

- [x] **Step 1: Write scene model tests**

Add these tests before implementation:

```csharp
[Fact]
public void Pipe_is_independent_from_equipment_objects()
{
    var valve = SceneObject.Create("Valve", new RectD(10, 20, 40, 40));
    var pipe = PipeObject.Create(new PointD(0, 40), new PointD(100, 40));
    var scene = ScreenDocument.Create("Main", new[] { valve, pipe });

    var moved = valve with { Bounds = valve.Bounds with { X = 30 } };
    var updated = scene.ReplaceObject(moved);
    Assert.Equal(0, Assert.IsType<PipeObject>(updated.Objects.Single(o => o.Id == pipe.Id)).Start.X);
}

[Fact]
public void Duplicate_scene_object_ids_are_rejected()
{
    var id = Guid.NewGuid();
    var first = SceneObject.Create("Valve", new RectD(0, 0, 10, 10), id);
    var second = SceneObject.Create("Pump", new RectD(20, 0, 10, 10), id);
    Assert.Throws<ArgumentException>(() => ScreenDocument.Create("Main", new[] { first, second }));
}
```

- [x] **Step 2: Implement scene contracts**

`SceneContracts.cs` must define immutable records `PointD`, `RectD`, `ProjectDocument`, `ScreenDocument`, abstract `SceneObject`, concrete `PipeObject`, `TextObject`, and `ControlObject`, plus `BindingDefinition` and `InteractionDefinition`. `ProjectDocument` contains `Guid ProjectId`, `int SchemaVersion`, `string Name`, `DateTimeOffset CreatedAt`, `DateTimeOffset UpdatedAt`, `ProjectStatus Status`, `IReadOnlyList<ScreenDocument> Screens`, and `IReadOnlyList<VariableDefinition> Variables`. `SceneObject` has `Guid Id`, `string Type`, `RectD Bounds`, `double Rotation`, `int ZIndex`, `bool IsVisible`, and read-only dictionaries for properties/bindings/interactions. `PipeObject` has independent `PointD Start`, `PointD End`, and `IReadOnlyList<PointD> Bends`; moving a scene object must never mutate a pipe.

`ScreenDocument.Create` must reject blank names and duplicate object IDs. `ReplaceObject` must replace by ID and return a new screen. JSON polymorphism must use a required discriminator property named `$type` with values `control`, `pipe`, and `text`.

- [x] **Step 3: Write and validate the schema fixtures**

`project-v1.schema.json` must require `schemaVersion`, `projectId`, `name`, `status`, `screens`, and `variables`; require every scene object to have `$type`, `id`, `bounds`, `zIndex`, and `isVisible`; require pipes to have `start`, `end`, and `bends`; forbid unknown top-level fields with `additionalProperties: false`.

`empty-project.json` must be a valid project generated by the serializer in Task 4. `invalid-project.json` must omit `schemaVersion` and use a blank project name. Tests must assert the first fixture validates and the second returns at least two validation errors.

- [x] **Step 4: Run scene tests and commit**

Run:

```powershell
dotnet test tests/Scada.Core.Tests/Scada.Core.Tests.csproj --no-restore
```

Expected: all core and scene tests pass. Then commit:

```powershell
git add src/Scada.Core src/Scada.Scene schemas docs/superpowers/plans/phase1-test-fixtures tests/Scada.Core.Tests
git commit -m "feat: add retained scene model and project schema"
```

### Task 4: Implement deterministic JSON serialization and migrations

**Files:**
- Create: `src/Scada.Storage/JsonProjectSerializer.cs`
- Create: `src/Scada.Storage/SchemaMigrations.cs`
- Create: `tests/Scada.Storage.Tests/JsonProjectSerializerTests.cs`

- [x] **Step 1: Write failing serializer tests**

Test that serialization is deterministic, round-trips polymorphic objects, rejects an unsupported schema version, and migrates a version-0 fixture to version 1. The deterministic test must serialize the same document twice and compare the exact UTF-8 strings.

- [x] **Step 2: Implement serializer and migration pipeline**

`JsonProjectSerializer` must expose:

```csharp
string Serialize(ProjectDocument project);
ProjectDocument Deserialize(string json);
IReadOnlyList<ProjectValidationError> Validate(string json);
```

Use `System.Text.Json` with camelCase, indented output, stable property ordering through declaration order, and a custom converter for `$type`. `Deserialize` must call `SchemaMigrations.UpgradeToCurrent` before materializing objects. Reject future schema versions with a typed `UnsupportedSchemaVersionException`.

`SchemaMigrations` must expose `const int CurrentVersion = 1` and `string UpgradeToCurrent(string json, int sourceVersion)`. Version 0 migration must add `status: "draft"`, `screens: []`, and `variables: []`; it must not invent PLC addresses or control bindings.

- [x] **Step 3: Run storage tests**

Run:

```powershell
dotnet test tests/Scada.Storage.Tests/Scada.Storage.Tests.csproj --no-restore
```

Expected: serializer, schema validation and migration tests pass.

- [x] **Step 4: Commit**

```powershell
git add src/Scada.Storage tests/Scada.Storage.Tests
git commit -m "feat: add project json serialization and schema migrations"
```

### Task 5: Add draft, publish, restore and import/export storage

**Files:**
- Create: `src/Scada.Storage/RevisionStore.cs`
- Create: `tests/Scada.Storage.Tests/RevisionStoreTests.cs`

- [x] **Step 1: Write failing revision tests**

Cover these exact behaviors:

```csharp
[Fact] public async Task SaveDraft_then_reopen_returns_same_project() { }
[Fact] public async Task Publish_creates_immutable_revision_and_runtime_reads_published_copy() { }
[Fact] public async Task Restore_copies_selected_revision_into_draft_without_changing_history() { }
[Fact] public async Task Import_export_round_trip_preserves_scene_geometry_and_bindings() { }
[Fact] public async Task Publishing_invalid_project_fails_without_creating_revision() { }
```

Each test must use a unique temporary SQLite file and delete it in `finally`; no test may touch `C:\MedicalMixingSystem.Next` or the legacy database.

- [x] **Step 2: Implement the SQLite store**

`RevisionStore` must create tables `Projects(ProjectId TEXT PRIMARY KEY, Name TEXT NOT NULL, DraftJson TEXT NOT NULL, UpdatedAt TEXT NOT NULL)` and `Revisions(ProjectId TEXT NOT NULL, RevisionId TEXT PRIMARY KEY, RevisionNumber INTEGER NOT NULL, Json TEXT NOT NULL, CreatedAt TEXT NOT NULL, PRIMARY KEY(ProjectId, RevisionNumber))` using parameterized commands. Expose:

```csharp
Task InitializeAsync(CancellationToken cancellationToken = default);
Task SaveDraftAsync(ProjectDocument project, CancellationToken cancellationToken = default);
Task<ProjectDocument?> LoadDraftAsync(Guid projectId, CancellationToken cancellationToken = default);
Task<ProjectRevision> PublishAsync(Guid projectId, string author, CancellationToken cancellationToken = default);
Task<IReadOnlyList<ProjectRevision>> ListRevisionsAsync(Guid projectId, CancellationToken cancellationToken = default);
Task RestoreToDraftAsync(Guid projectId, Guid revisionId, CancellationToken cancellationToken = default);
Task ExportAsync(Guid projectId, string filePath, CancellationToken cancellationToken = default);
Task<Guid> ImportAsync(string filePath, CancellationToken cancellationToken = default);
```

`PublishAsync` must validate JSON before inserting a revision, assign a monotonically increasing revision number, and never update the draft. `RestoreToDraftAsync` copies the selected immutable JSON into `Projects.DraftJson` and never deletes or edits `Revisions`.

- [x] **Step 3: Run persistence tests and commit**

Run:

```powershell
dotnet test tests/Scada.Storage.Tests/Scada.Storage.Tests.csproj --no-restore
```

Expected: all revision tests pass. Commit:

```powershell
git add src/Scada.Storage tests/Scada.Storage.Tests
git commit -m "feat: add draft publish restore and project import"
```

### Task 6: Implement the deterministic offline simulator

**Files:**
- Create: `src/Scada.Simulator/OfflineSimulator.cs`
- Create: `tests/Scada.Simulator.Tests/OfflineSimulatorTests.cs`

- [x] **Step 1: Write failing simulator tests**

Test that a seeded simulator returns repeatable values, unknown variables return `Bad` quality, and simulated values never become confirmed commands. Test a numeric range and a bool feedback separately.

- [x] **Step 2: Implement simulator**

`OfflineSimulator` must accept an `IReadOnlyList<VariableDefinition>` and a `uint seed`. Expose:

```csharp
VariableValue Read(string key, DateTimeOffset now);
void SetFeedback(string key, object? value, VariableQuality quality = VariableQuality.Good);
IReadOnlyDictionary<string, VariableValue> Snapshot(DateTimeOffset now);
```

Use a deterministic PRNG seeded by the constructor. Numeric values must stay within the definition range; bool feedback defaults to `false`; missing keys return `VariableValue.Unknown`. Reject writes to variables whose direction is `Command` or whose data type/range does not match. Do not add PLC communication dependencies.

- [x] **Step 3: Run simulator tests and commit**

Run:

```powershell
dotnet test tests/Scada.Simulator.Tests/Scada.Simulator.Tests.csproj --no-restore
dotnet test IndustrialScadaPlatform.sln --no-restore
```

Expected: all tests pass. Commit:

```powershell
git add src/Scada.Simulator tests/Scada.Simulator.Tests
git commit -m "feat: add deterministic offline simulator"
```

### Task 7: Add the Phase 1 acceptance harness and documentation

**Files:**
- Create: `tests/Scada.Acceptance.Tests/Scada.Acceptance.Tests.csproj`
- Create: `tests/Scada.Acceptance.Tests/Phase1AcceptanceTests.cs`
- Modify: `IndustrialScadaPlatform.sln`
- Create: `docs/phase1-acceptance.md`
- Create: `docs/handoff-2026-08-19-phase1.md`

- [ ] **Step 1: Write the end-to-end acceptance test**

The test must create an empty project, add one screen with one pipe and one control, save a draft, export, import into a second temporary store, publish, list the revision, restore it to draft, and compare IDs, bounds, pipe endpoints, bindings and schema version. It must not instantiate WPF or contact a PLC.

- [ ] **Step 2: Document the acceptance matrix**

`docs/phase1-acceptance.md` must record commands and expected results for:

```text
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
```

It must state that the gate passes only with 0 warnings, 0 errors, all tests passing, valid fixture accepted, invalid fixture rejected, and no files changed under the legacy repository.

- [ ] **Step 3: Run the complete gate**

Run:

```powershell
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
git status --short --branch
```

Expected: Release build has 0 warnings and 0 errors; every test passes; `git diff --check` is clean; only the new platform repository has changes.

- [ ] **Step 4: Commit the phase gate**

```powershell
git add IndustrialScadaPlatform.sln tests/Scada.Acceptance.Tests docs/phase1-acceptance.md docs/handoff-2026-08-19-phase1.md
git commit -m "test: add phase one acceptance gate"
```

## Self-Review Checklist

- Spec coverage: Phase 1 requirements map to Tasks 1-7: SDK solution, project model, JSON Schema, migrations, revisions, offline simulator and contract/acceptance tests. WPF editor, Web Runtime, gateway, S7 and WinCC adapters remain intentionally deferred to later phase plans.
- Placeholder scan: no step uses `TBD`, `TODO`, `FIXME`, or an unspecified implementation request; every implementation step names files, APIs, validation behavior and commands.
- Type consistency: `ProjectDocument`, `ScreenDocument`, `SceneObject`, `VariableDefinition`, `VariableValue`, `JsonProjectSerializer`, `RevisionStore` and `OfflineSimulator` are defined before use and their method signatures are repeated consistently.
- Boundary check: no task reads or modifies the legacy database, PLC project, WPF editor source or customer runtime data.
- Safety check: Phase 1 has no field commands and no PLC dependency; the simulator rejects command writes and uses explicit quality states.

Plan complete and saved to `docs/superpowers/plans/2026-08-19-phase1-core-project-model.md`. Two execution options:

1. Subagent-Driven (recommended) - dispatch a fresh implementation worker per task with review checkpoints.
2. Inline Execution - execute this plan in the current session with checkpoints.
