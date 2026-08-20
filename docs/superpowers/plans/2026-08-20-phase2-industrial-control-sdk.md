# Phase 2 Industrial Control SDK Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable, versioned industrial-control SDK with mechanically recognizable 2.5D equipment, deterministic feedback-driven state and animation semantics, and equivalent native WPF and SVG preview rendering without PLC communication.

**Architecture:** `Scada.Controls` owns platform-neutral control definitions, state resolution, binding/action validation, geometry primitives and visual tokens. `Scada.Controls.Wpf` and `Scada.Controls.Svg` consume the same render plan through native platform renderers; small preview applications provide visual evidence without becoming the Phase 3 editor or Phase 4 Runtime. Phase 1 scene, storage and simulator contracts are extended through an explicit project-v2 migration and remain the only persisted project model.

**Tech Stack:** .NET 10, C# nullable reference types, WPF vector graphics and `VisualStateManager`, SVG 1.1/HTML5 output, `System.Text.Json`, JSON Schema Draft 2020-12, xUnit, deterministic offline simulator, `dotnet build`, `dotnet test`.

---

## Boundaries

- Work only in `D:\wpf_XM\IndustrialScadaPlatform`.
- Do not modify the legacy medical-mixing WPF repositories, TIA projects, PLC projects, customer databases or runtime data.
- Do not add S7, OPC UA, Modbus, SignalR, authentication or live command transport.
- Do not build the WinCC-style editor workspace. Preview applications may expose only state selection, size/orientation controls and artifact generation required for Phase 2 verification.
- Do not copy geometry, images or source from a repository whose compatible license has not been recorded.
- Controls, pipes, labels and instruments remain independent scene objects. Optional anchors are alignment metadata, not mandatory connectivity.

## File Map

### Platform-neutral SDK

- Create `src/Scada.Controls/Scada.Controls.csproj` - platform-neutral control SDK.
- Create `src/Scada.Controls/ControlContracts.cs` - identifiers, definitions, properties, binding roles and animations.
- Create `src/Scada.Controls/ControlCatalog.cs` - immutable first-party catalog.
- Create `src/Scada.Controls/ControlStateResolver.cs` - deterministic state precedence and staleness rules.
- Create `src/Scada.Controls/ControlValidator.cs` - binding, event, action and property validation.
- Create `src/Scada.Controls/InteractionCatalog.cs` - stable action/event identifiers and Chinese labels.
- Create `src/Scada.Controls/SceneGeometryOperations.cs` - catalog-aware immutable move/resize/rotate/nudge operations.
- Create `src/Scada.Controls/Rendering/RenderPrimitives.cs` - renderer-neutral vector geometry.
- Create `src/Scada.Controls/Rendering/VisualTokens.cs` - shared colors, strokes, typography and motion tokens.
- Create `src/Scada.Controls/Rendering/ControlRenderPlan.cs` - state-resolved render tree.
- Create `src/Scada.Controls/Geometry/PumpAndValveGeometry.cs` - pump and automated-valve morphology.
- Create `src/Scada.Controls/Geometry/VesselAgitatorFilterGeometry.cs` - vessel, agitator and filter morphology.
- Create `src/Scada.Controls/Geometry/PipeInstrumentOperatorGeometry.cs` - pipes, fittings, measurement and operator controls.
- Create `src/Scada.Controls/Geometry/ControlGeometryFactory.cs` - catalog type to geometry-builder dispatch.

### WPF and SVG renderers

- Create `src/Scada.Controls.Wpf/Scada.Controls.Wpf.csproj` - Windows WPF renderer.
- Create `src/Scada.Controls.Wpf/IndustrialControl.cs` - lookless WPF host control.
- Create `src/Scada.Controls.Wpf/WpfControlRenderer.cs` - render-plan to WPF visual conversion.
- Create `src/Scada.Controls.Wpf/Themes/Generic.xaml` - default WPF control template and state resources.
- Create `src/Scada.Controls.Svg/Scada.Controls.Svg.csproj` - platform-neutral SVG renderer.
- Create `src/Scada.Controls.Svg/SvgControlRenderer.cs` - deterministic SVG output.
- Create `src/Scada.Controls.Svg/SvgDocumentWriter.cs` - complete SVG/HTML preview document generation.

### Preview and review artifacts

- Create `samples/Scada.Controls.Preview.Wpf/Scada.Controls.Preview.Wpf.csproj`.
- Create `samples/Scada.Controls.Preview.Wpf/App.xaml` and `App.xaml.cs`.
- Create `samples/Scada.Controls.Preview.Wpf/MainWindow.xaml` and `MainWindow.xaml.cs`.
- Create `samples/Scada.Controls.Preview.Wpf/ReviewArtifactWriter.cs` - deterministic 1920x1080 PNG output.
- Create `samples/Scada.Controls.Preview.Svg/Scada.Controls.Preview.Svg.csproj`.
- Create `samples/Scada.Controls.Preview.Svg/Program.cs` - deterministic HTML/SVG artifact output.
- Create `samples/Scada.Controls.SampleProject/SampleProjectFactory.cs` - one shared generic sample project and state scenarios.
- Create `samples/Scada.Controls.SampleProject/SampleScenarios.cs` - stopped/active/transition/fault/unknown snapshots.
- Create `samples/Scada.Controls.SampleProject/Scada.Controls.SampleProject.csproj`.
- Create `docs/phase2/source-audit.md` - source, license and reuse decision log.
- Create `docs/phase2/visual-review.md` - magnified, exact-size and complete-screen review checklist/results.
- Create `docs/phase2-acceptance.md` and `docs/handoff-2026-08-20-phase2.md` at the final gate.

### Persistence and scene changes

- Modify `src/Scada.Core/ProjectContracts.cs` - central project format version.
- Modify `src/Scada.Scene/SceneContracts.cs` - control version and parameterized interactions.
- Modify `src/Scada.Scene/ProjectDocument.cs` - project-v2 creation.
- Create `schemas/project-v2.schema.json`.
- Modify `src/Scada.Storage/JsonProjectSerializer.cs` and `SchemaMigrations.cs` - v1-to-v2 migration.
- Modify `src/Scada.Storage/Scada.Storage.csproj` - embed v2 schema.

### Tests

- Create `tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj`.
- Create focused tests under `tests/Scada.Controls.Tests/` matching the production file names.
- Create `tests/Scada.Controls.Wpf.Tests/Scada.Controls.Wpf.Tests.csproj` and `WpfRenderingTests.cs`.
- Create `tests/Scada.Controls.Svg.Tests/Scada.Controls.Svg.Tests.csproj` and `SvgRenderingTests.cs`.
- Create `tests/Scada.Phase2.Acceptance.Tests/Scada.Phase2.Acceptance.Tests.csproj`, `SampleProjectTests.cs`, `CrossRendererAcceptanceTests.cs` and `Phase2AcceptanceTests.cs`.
- Modify `IndustrialScadaPlatform.sln` only to add the Phase 2 projects.
- Modify `.gitignore` to exclude `.superpowers/` visual-companion state and generated `artifacts/`.

## Task 1: Create the Phase 2 project skeleton

**Files:**
- Create the ten Phase 2 `.csproj` files listed above.
- Modify `IndustrialScadaPlatform.sln`.
- Modify `docs/superpowers/specs/2026-08-20-phase2-industrial-control-sdk-design.md` status to approved.

- [ ] **Step 1: Add empty SDK, renderer, sample and test projects**

Use `Microsoft.NET.Sdk` for platform-neutral libraries and SVG sample. `Scada.Controls.Wpf` and its preview/tests must override the global framework with:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

`Scada.Controls` references `Scada.Core` and `Scada.Scene`; `Scada.Controls.Wpf` and `Scada.Controls.Svg` reference `Scada.Controls`; the shared sample-project library references `Scada.Controls`, `Scada.Scene` and `Scada.Simulator`. Each test project references only its system under test and the standard centrally managed test packages. Because the Phase 2 acceptance project exercises both renderers, it also targets `net10.0-windows` with WPF and Windows targeting enabled. Add `.superpowers/` and `artifacts/` to `.gitignore`; do not delete either local directory.

- [ ] **Step 2: Add projects to the solution and verify the graph**

Run:

```powershell
dotnet sln IndustrialScadaPlatform.sln add src/Scada.Controls/Scada.Controls.csproj src/Scada.Controls.Wpf/Scada.Controls.Wpf.csproj src/Scada.Controls.Svg/Scada.Controls.Svg.csproj samples/Scada.Controls.SampleProject/Scada.Controls.SampleProject.csproj samples/Scada.Controls.Preview.Wpf/Scada.Controls.Preview.Wpf.csproj samples/Scada.Controls.Preview.Svg/Scada.Controls.Preview.Svg.csproj tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj tests/Scada.Controls.Wpf.Tests/Scada.Controls.Wpf.Tests.csproj tests/Scada.Controls.Svg.Tests/Scada.Controls.Svg.Tests.csproj tests/Scada.Phase2.Acceptance.Tests/Scada.Phase2.Acceptance.Tests.csproj
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
```

Expected: restore succeeds; the solution lists 18 projects; Release build has 0 warnings and 0 errors.

- [ ] **Step 3: Commit the skeleton**

```powershell
git add IndustrialScadaPlatform.sln src samples tests docs/superpowers/specs/2026-08-20-phase2-industrial-control-sdk-design.md
git commit -m "build: create phase two control sdk projects"
```

## Task 2: Define control contracts and the first-party catalog

**Files:**
- Create `src/Scada.Controls/ControlContracts.cs`.
- Create `src/Scada.Controls/ControlCatalog.cs`.
- Test `tests/Scada.Controls.Tests/ControlCatalogTests.cs`.

- [ ] **Step 1: Write failing catalog tests**

```csharp
[Fact]
public void Catalog_contains_each_approved_control_family()
{
    var catalog = ControlCatalog.CreateDefault();
    var expected = new[]
    {
        ControlTypeIds.CentrifugalPump, ControlTypeIds.AutomatedValve,
        ControlTypeIds.Vessel, ControlTypeIds.Agitator, ControlTypeIds.Filter,
        ControlTypeIds.StraightPipe, ControlTypeIds.PipeElbow, ControlTypeIds.PipeTee,
        ControlTypeIds.NumericDisplay, ControlTypeIds.LevelBar,
        ControlTypeIds.TemperatureIndicator, ControlTypeIds.PressureIndicator,
        ControlTypeIds.FlowIndicator, ControlTypeIds.CommandButton
    };

    Assert.All(expected, id => Assert.NotNull(catalog.Get(id, version: 1)));
}

[Fact]
public void Pump_separates_command_and_feedback_roles()
{
    var pump = ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1);
    Assert.Equal(VariableDirection.Command, pump.GetBinding("StartCommand").Direction);
    Assert.Equal(VariableDirection.Feedback, pump.GetBinding("RunFeedback").Direction);
}
```

Run `dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore`. Expected: compile failure because the SDK contracts do not exist.

- [ ] **Step 2: Implement stable contracts**

Define these public types with constructor validation for blank IDs, version less than one, duplicate property/binding/animation names and invalid size:

```csharp
public enum ControlState { Neutral, Stopped, Active, Transition, Fault, Unknown }
public enum StateSignalRole { None, ActiveFeedback, InactiveFeedback, FaultFeedback, CommandRequest, ProcessValue }
public enum ResizePolicy { PreserveAspectRatio, StretchHorizontal, StretchVertical, Free }

public sealed record ControlSize(double Width, double Height);
public sealed record ControlPropertyDefinition(string Name, string DefaultValue, bool Required);
public sealed record ControlBindingRole(
    string Name,
    IReadOnlySet<VariableDataType> DataTypes,
    VariableDirection Direction,
    StateSignalRole StateRole,
    bool Required,
    string? UnitFamily = null);
public sealed record AnimationDefinition(string Name, string TriggerRole, bool StopsOnBadQuality, bool SupportsReducedMotion);
public sealed record ControlDefinition(
    string TypeId,
    int Version,
    string DisplayNameKey,
    ControlSize DefaultSize,
    ControlSize MinimumSize,
    ResizePolicy ResizePolicy,
    IReadOnlyList<ControlPropertyDefinition> Properties,
    IReadOnlyList<ControlBindingRole> Bindings,
    IReadOnlyList<AnimationDefinition> Animations);
```

`ControlTypeIds` contains the exact IDs `equipment.pump.centrifugal`, `equipment.valve.automated`, `equipment.vessel`, `equipment.agitator`, `equipment.filter`, `pipe.straight`, `pipe.elbow`, `pipe.tee`, `instrument.numeric`, `instrument.level-bar`, `instrument.temperature`, `instrument.pressure`, `instrument.flow`, and `operator.command-button`. Its `All` property returns those IDs once in declaration order. `ControlDefinition.GetBinding(string name)` returns the named role or throws a `KeyNotFoundException` containing the definition and role IDs.

`ControlCatalog.CreateDefault()` returns immutable version-1 definitions. `Get(typeId, version)` throws `KeyNotFoundException` with both values when absent. Do not add PLC addresses or project tag names.

- [ ] **Step 3: Run tests and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls tests/Scada.Controls.Tests
git commit -m "feat: add versioned industrial control catalog"
```

Expected: focused tests pass with no warnings.

## Task 3: Implement deterministic state resolution

**Files:**
- Create `src/Scada.Controls/ControlStateResolver.cs`.
- Test `tests/Scada.Controls.Tests/ControlStateResolverTests.cs`.

- [ ] **Step 1: Write the complete precedence tests**

Create one theory covering the required precedence plus a command/feedback test:

```csharp
[Theory]
[InlineData(VariableQuality.Bad, false, false, false, false, ControlState.Unknown)]
[InlineData(VariableQuality.Good, true, false, false, false, ControlState.Fault)]
[InlineData(VariableQuality.Good, false, true, true, false, ControlState.Fault)]
[InlineData(VariableQuality.Good, false, false, false, true, ControlState.Transition)]
[InlineData(VariableQuality.Good, false, true, false, false, ControlState.Active)]
[InlineData(VariableQuality.Good, false, false, true, false, ControlState.Stopped)]
public void Resolver_applies_required_precedence(
    VariableQuality quality, bool fault, bool active, bool inactive, bool commandPending, ControlState expected)
{
    var snapshot = CreatePumpSnapshot(quality, fault, active, inactive, commandPending);
    Assert.Equal(expected, ControlStateResolver.Resolve(PumpDefinition, snapshot, SampleTime).State);
}

[Fact]
public void Start_command_without_run_feedback_is_not_active()
{
    var result = ControlStateResolver.Resolve(PumpDefinition,
        CreatePumpSnapshot(VariableQuality.Good, fault: false, active: false, inactive: false, commandPending: true),
        SampleTime);
    Assert.Equal(ControlState.Transition, result.State);
}
```

In the test class, define `SampleTime`, obtain `PumpDefinition` from `ControlCatalog.CreateDefault()`, and implement `CreatePumpSnapshot` as a local helper returning a dictionary with `FaultFeedback`, `RunFeedback`, `StopFeedback`, and `StartCommand` `VariableValue` entries timestamped at `SampleTime`. Also test that a `Good` feedback value timestamped more than five seconds before `SampleTime` resolves to `Unknown`.

Use this helper exactly, with `BoolValue` returning `new VariableValue(VariableDataType.Bool, value, quality, SampleTime)`:

```csharp
private static IReadOnlyDictionary<string, VariableValue> CreatePumpSnapshot(
    VariableQuality quality, bool fault, bool active, bool inactive, bool commandPending) =>
    new Dictionary<string, VariableValue>
    {
        ["FaultFeedback"] = BoolValue(fault, quality),
        ["RunFeedback"] = BoolValue(active, quality),
        ["StopFeedback"] = BoolValue(inactive, quality),
        ["StartCommand"] = BoolValue(commandPending, VariableQuality.Good)
    };

private static VariableValue BoolValue(bool value, VariableQuality quality) =>
    new(VariableDataType.Bool, value, quality, SampleTime);
```

- [ ] **Step 2: Implement the resolver**

Add:

```csharp
public sealed record ControlStateResult(
    ControlState State,
    string ReasonCode,
    IReadOnlySet<string> ActiveAnimations);

public static class ControlStateResolver
{
    public static ControlStateResult Resolve(
        ControlDefinition definition,
        IReadOnlyDictionary<string, VariableValue> roleValues,
        DateTimeOffset now,
        TimeSpan? staleAfter = null);
}
```

The method must apply this exact order: missing/bad/stale required feedback => `Unknown`; fault or contradictory active/inactive feedback => `Fault`; command request without matching feedback => `Transition`; active feedback => `Active`; inactive feedback => `Stopped`; otherwise `Neutral`. Animation names come only from the definition and are omitted when quality is bad or reduced-motion handling later disables them.

- [ ] **Step 3: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls/ControlStateResolver.cs tests/Scada.Controls.Tests/ControlStateResolverTests.cs
git commit -m "feat: resolve feedback driven control states"
```

## Task 4: Add binding, event and action validation with Chinese labels

**Files:**
- Create `src/Scada.Controls/InteractionCatalog.cs`.
- Create `src/Scada.Controls/ControlValidator.cs`.
- Test `tests/Scada.Controls.Tests/ControlValidatorTests.cs`.
- Test `tests/Scada.Controls.Tests/InteractionCatalogTests.cs`.

- [ ] **Step 1: Write failing validation tests**

```csharp
[Fact]
public void Numeric_level_role_rejects_bool_feedback()
{
    var projectVariables = new[] { VariableDefinition.Bool("Tank.Level", VariableDirection.Feedback) };
    var instance = ControlObject.Create(ControlTypeIds.LevelBar, new RectD(0, 0, 40, 120)) with
    {
        Bindings = new Dictionary<string, BindingDefinition>
        {
            ["LevelValue"] = new("Tank.Level", "LevelValue")
        }
    };
    var errors = new ControlValidator().Validate(
        instance, ControlCatalog.CreateDefault().Get(ControlTypeIds.LevelBar, 1), projectVariables,
        new HashSet<string>());
    Assert.Contains(errors, e => e.Code == "control.binding.type" && e.Path.Contains("LevelValue"));
}

[Fact]
public void Command_role_rejects_feedback_direction()
{
    var projectVariables = new[] { VariableDefinition.Bool("Pump.Start", VariableDirection.Feedback) };
    var instance = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(0, 0, 120, 72)) with
    {
        Bindings = new Dictionary<string, BindingDefinition>
        {
            ["StartCommand"] = new("Pump.Start", "StartCommand")
        }
    };
    var errors = new ControlValidator().Validate(
        instance, ControlCatalog.CreateDefault().Get(ControlTypeIds.CentrifugalPump, 1), projectVariables,
        new HashSet<string>());
    Assert.Contains(errors, e => e.Code == "control.binding.direction");
}

[Fact]
public void Editor_labels_are_chinese_but_ids_are_stable()
{
    Assert.Equal("左键按下", InteractionCatalog.Event("pointer.left.press").ChineseLabel);
    Assert.Equal("写入命令", InteractionCatalog.Action("command.write").ChineseLabel);
}
```

- [ ] **Step 2: Implement the catalogs and validator**

Use stable event IDs `pointer.left.press`, `pointer.left.release`, `pointer.double-click`, `pointer.right-click`, `pointer.enter`, `pointer.leave`, `variable.changed`, `fault.activated`, `fault.recovered`. Use action IDs `command.write`, `command.toggle-bool`, `panel.open-equipment`, `trend.open`, `alarm.acknowledge`, `condition.reset`, `screen.navigate`, `message.show`. `InteractionCatalog.Event(string id)` and `Action(string id)` return `LocalizedCapability` or throw a key-specific `KeyNotFoundException`.

Expose:

```csharp
public sealed record LocalizedCapability(string Id, string ChineseLabel, IReadOnlySet<string> RequiredParameters);

public sealed class ControlValidator
{
    public IReadOnlyList<ProjectValidationError> Validate(
        ControlObject instance,
        ControlDefinition definition,
        IReadOnlyList<VariableDefinition> variables,
        IReadOnlySet<string> availableCapabilities);
}
```

Validation codes must be stable: `control.binding.required`, `control.binding.missing-variable`, `control.binding.type`, `control.binding.direction`, `control.binding.unit`, `control.property.required`, `control.event.unknown`, `control.action.unknown`, `control.action.parameter`, and `control.action.capability-unavailable`. Error paths include screen/object context supplied by the caller. Unavailable later-phase capabilities are diagnosed and produce no preview side effect.

- [ ] **Step 3: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls tests/Scada.Controls.Tests
git commit -m "feat: validate control bindings and interactions"
```

## Task 5: Introduce project-v2 control versions and action parameters

**Files:**
- Modify `src/Scada.Core/ProjectContracts.cs`.
- Modify `src/Scada.Scene/SceneContracts.cs`.
- Modify `src/Scada.Scene/ProjectDocument.cs`.
- Create `schemas/project-v2.schema.json`.
- Modify `src/Scada.Storage/JsonProjectSerializer.cs`.
- Modify `src/Scada.Storage/SchemaMigrations.cs`.
- Modify `src/Scada.Storage/Scada.Storage.csproj`.
- Test `tests/Scada.Storage.Tests/ProjectV2MigrationTests.cs`.

- [ ] **Step 1: Write failing migration and round-trip tests**

```csharp
[Fact]
public void Version_one_control_migrates_to_control_version_one()
{
    var upgraded = SchemaMigrations.UpgradeToCurrent(VersionOneProjectJson, 1);
    using var json = JsonDocument.Parse(upgraded);
    var control = json.RootElement.GetProperty("screens")[0].GetProperty("objects")[0];
    Assert.Equal(2, json.RootElement.GetProperty("schemaVersion").GetInt32());
    Assert.Equal(1, control.GetProperty("controlVersion").GetInt32());
}

[Fact]
public void Interaction_parameters_round_trip_without_loss()
{
    var interaction = new InteractionDefinition(
        "pointer.left.release", "command.write",
        new Dictionary<string, string> { ["role"] = "StartCommand", ["value"] = "true" });
    var control = ControlObject.Create(ControlTypeIds.CentrifugalPump, new RectD(0, 0, 120, 72)) with
    {
        Interactions = new Dictionary<string, InteractionDefinition> { ["release"] = interaction }
    };
    var project = ProjectDocument.Create("v2", screens: new[] { ScreenDocument.Create("Main", new[] { control }) });
    var serializer = new JsonProjectSerializer();
    var restored = serializer.Deserialize(serializer.Serialize(project));
    Assert.Equal("true", restored.Screens[0].Objects[0].Interactions["release"].Parameters["value"]);
}
```

- [ ] **Step 2: Implement versioned scene instances**

Add `ProjectFormat.CurrentVersion = 2` in `Scada.Core`. Add `int ControlVersion` to `ControlObject`, defaulting to one in `ControlObject.Create`. Replace the positional `InteractionDefinition` with a validated record exposing `EventName`, `ActionName`, and immutable `Parameters`, with an optional empty parameter dictionary for existing call sites.

`project-v2.schema.json` is based on v1 and additionally requires `controlVersion` for `$type: control` and `parameters` for every interaction. `SchemaMigrations.UpgradeToCurrent` must add `controlVersion: 1` to each v1 control, change a legacy pipe object's semantic `type` value from `pipe` to `pipe.straight`, and add `parameters: {}` to each v1 interaction, then set schema version 2. The serializer embeds and validates only the current v2 schema while preserving its existing v0-to-v1 migration step. `PipeObject.Create` gains an overload accepting a semantic pipe type; its existing overload creates `pipe.straight` so old callers remain source-compatible.

- [ ] **Step 3: Run storage and acceptance regression tests**

```powershell
dotnet test tests/Scada.Storage.Tests/Scada.Storage.Tests.csproj --no-restore
dotnet test tests/Scada.Acceptance.Tests/Scada.Acceptance.Tests.csproj --no-restore
```

Expected: v0, v1 and v2 inputs deserialize to v2; v2 round trips exactly; existing Phase 1 behavior remains passing.

- [ ] **Step 4: Commit**

```powershell
git add src/Scada.Core src/Scada.Scene src/Scada.Storage schemas tests/Scada.Storage.Tests tests/Scada.Acceptance.Tests
git commit -m "feat: add versioned control scene contracts"
```

## Task 6: Define render primitives, anchors and visual tokens

**Files:**
- Create `src/Scada.Controls/Rendering/RenderPrimitives.cs`.
- Create `src/Scada.Controls/Rendering/ControlRenderPlan.cs`.
- Create `src/Scada.Controls/Rendering/VisualTokens.cs`.
- Test `tests/Scada.Controls.Tests/RenderContractTests.cs`.

- [ ] **Step 1: Write failing render-contract tests**

```csharp
[Fact]
public void Anchor_outside_design_bounds_is_rejected()
{
    Assert.Throws<ArgumentOutOfRangeException>(() => new ControlRenderPlan(
        "test.control", 1, new RenderSize(100, 60), Array.Empty<RenderPrimitive>(),
        new Dictionary<string, RenderPoint> { ["outside"] = new(101, 30) },
        ControlState.Neutral, new HashSet<string>(), new Dictionary<string, string>()));
}

[Fact]
public void Equipment_preserves_aspect_ratio_but_straight_pipe_stretches_horizontally()
{
    var catalog = ControlCatalog.CreateDefault();
    Assert.Equal(ResizePolicy.PreserveAspectRatio, catalog.Get(ControlTypeIds.CentrifugalPump, 1).ResizePolicy);
    Assert.Equal(ResizePolicy.StretchHorizontal, catalog.Get(ControlTypeIds.StraightPipe, 1).ResizePolicy);
}

[Fact]
public void Normal_tokens_are_quieter_than_fault_tokens()
{
    Assert.NotEqual(VisualTokens.EquipmentBody, VisualTokens.Fault);
    Assert.Equal("#D64045", VisualTokens.Fault);
}
```

- [ ] **Step 2: Implement a small vector DSL**

Define `RenderPoint`, `RenderSize`, `RenderRect`, `RenderTransform`, `RenderGroup`, `RenderLine`, `RenderRectangle`, `RenderEllipse`, and `RenderPath`. Paths use typed commands `MoveTo`, `LineTo`, `CubicTo`, and `ClosePath`, not free-form SVG strings. Every primitive has a stable `PartId`, visual-role token and optional animation name.

`ControlRenderPlan` contains `TypeId`, `Version`, 100x100-or-family-specific `DesignSize`, ordered primitives, named anchors, resolved `ControlState`, active animations and semantic diagnostics. It rejects duplicate part IDs and anchors outside design bounds.

Define `ControlRenderContext` with `ControlState State`, `IReadOnlyDictionary<string, double> NumericValues`, `IReadOnlyDictionary<string, string> TextValues`, `VariableQuality Quality`, and `bool ReducedMotion`. Provide `ForState(ControlState state)` and clamped numeric-value accessors so later geometry tests use one defined context type.

Create centralized tokens with these initial values:

```csharp
public static class VisualTokens
{
    public const string Canvas = "#202428";
    public const string EquipmentBody = "#D4D9DC";
    public const string EquipmentDepth = "#929BA1";
    public const string Outline = "#343B40";
    public const string Stopped = "#7E878D";
    public const string Active = "#2E9D62";
    public const string Transition = "#D79B2B";
    public const string Fault = "#D64045";
    public const string Unknown = "#697278";
    public const double EquipmentStroke = 1.5;
    public const double PipeStroke = 4.0;
    public static readonly TimeSpan StandardMotion = TimeSpan.FromMilliseconds(900);
}
```

Keep letter spacing zero and include a reduced-motion flag in render context.

- [ ] **Step 3: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls/Rendering tests/Scada.Controls.Tests/RenderContractTests.cs
git commit -m "feat: add shared control render contract"
```

## Task 7: Build pump and automated-valve morphology

**Files:**
- Create `src/Scada.Controls/Geometry/PumpAndValveGeometry.cs`.
- Test `tests/Scada.Controls.Tests/PumpAndValveGeometryTests.cs`.

- [ ] **Step 1: Write morphology tests before geometry**

```csharp
[Fact]
public void Pump_has_casing_motor_coupling_and_boundary_ports()
{
    var plan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));
    Assert.Contains(plan.Primitives, p => p.PartId == "pump.casing");
    Assert.Contains(plan.Primitives, p => p.PartId == "pump.motor");
    Assert.Contains(plan.Primitives, p => p.PartId == "pump.coupling");
    Assert.Equal(0, plan.Anchors["suction"].X);
    Assert.Equal(plan.DesignSize.Width, plan.Anchors["discharge"].X);
}

[Fact]
public void Valve_travel_animation_targets_stem_not_entire_body()
{
    var plan = PumpAndValveGeometry.BuildValve(ControlRenderContext.ForState(ControlState.Transition));
    Assert.Contains(plan.Primitives, p => p.PartId == "valve.stem" && p.AnimationName == "valve.travel");
    Assert.DoesNotContain(plan.Primitives, p => p.PartId == "valve.body" && p.AnimationName is not null);
}
```

- [ ] **Step 2: Implement original vector geometry**

Use a 120x72 pump design grid. Draw a volute-like casing, left suction flange, top/right discharge according to orientation, motor body, cooling fins and coupling guard. Place anchors exactly on visible flange boundaries. Animate only shaft/impeller-related parts for `pump.rotate`.

Use a 100x80 valve grid. Draw the inline valve body centered on the pipe axis, actuator above the body, stem between them and boundary anchors at both pipe ends. Open/closed feedback changes the relevant state layer; continuous position moves the stem within a clamped 0-100 percent range. Rotation applies to the complete control through the scene transform, not by redrawing a separate vertical asset.

Do not import images or third-party SVG paths.

- [ ] **Step 3: Verify all state variants and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls/Geometry/PumpAndValveGeometry.cs tests/Scada.Controls.Tests/PumpAndValveGeometryTests.cs
git commit -m "feat: add pump and valve control morphology"
```

## Task 8: Build vessel, agitator and filter morphology

**Files:**
- Create `src/Scada.Controls/Geometry/VesselAgitatorFilterGeometry.cs`.
- Test `tests/Scada.Controls.Tests/VesselAgitatorFilterGeometryTests.cs`.

- [ ] **Step 1: Write component and animation tests**

```csharp
[Fact]
public void Vessel_exposes_jacket_fill_and_bottom_outlet_as_distinct_parts()
{
    var context = ControlRenderContext.ForState(ControlState.Active) with
    {
        NumericValues = new Dictionary<string, double> { ["LevelValue"] = 62 }
    };
    var plan = VesselAgitatorFilterGeometry.BuildVessel(context);
    Assert.Contains(plan.Primitives, p => p.PartId == "vessel.shell");
    Assert.Contains(plan.Primitives, p => p.PartId == "vessel.jacket");
    Assert.Contains(plan.Primitives, p => p.PartId == "vessel.liquid");
    Assert.Equal(plan.DesignSize.Height, plan.Anchors["bottom-outlet"].Y);
}

[Fact]
public void Agitator_rotates_impeller_and_shaft_without_moving_motor_or_vessel()
{
    var plan = VesselAgitatorFilterGeometry.BuildAgitator(
        ControlRenderContext.ForState(ControlState.Active));
    Assert.Equal("agitator.rotate", plan.Primitives.Single(p => p.PartId == "agitator.impeller").AnimationName);
    Assert.Null(plan.Primitives.Single(p => p.PartId == "agitator.motor").AnimationName);
}
```

Also assert that filter inlet/outlet anchors are on opposite visible housing boundaries and the blocked state uses the fault/attention layer without changing port geometry.

- [ ] **Step 2: Implement the three families**

Use original vector shapes with shallow surface-depth planes. The vessel shell, jacket and liquid clip region must remain separate. Clamp liquid to 0-100 percent and preserve a visible empty headspace at 100 percent. The agitator is an independent render plan with motor, shaft and bottom impeller; composition with a vessel is done by scene placement/grouping. The filter must remain recognizable at its catalog minimum size and expose verified inlet/outlet orientation.

- [ ] **Step 3: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
git add src/Scada.Controls/Geometry/VesselAgitatorFilterGeometry.cs tests/Scada.Controls.Tests/VesselAgitatorFilterGeometryTests.cs
git commit -m "feat: add vessel agitator and filter morphology"
```

## Task 9: Build pipes, fittings, measurements and operator controls

**Files:**
- Create `src/Scada.Controls/Geometry/PipeInstrumentOperatorGeometry.cs`.
- Test `tests/Scada.Controls.Tests/PipeInstrumentOperatorGeometryTests.cs`.

- [ ] **Step 1: Write tests for stretch, values and quality**

```csharp
[Fact]
public void Straight_pipe_length_changes_without_moving_other_scene_objects()
{
    var pipe = PipeObject.Create(new PointD(10, 50), new PointD(110, 50));
    var valve = ControlObject.Create(ControlTypeIds.AutomatedValve, new RectD(50, 30, 40, 40));
    var resized = pipe.WithEnd(new PointD(160, 50));
    Assert.Equal(50, valve.Bounds.X);
    Assert.Equal(160, resized.End.X);
}

[Fact]
public void Level_bar_clamps_fill_and_preserves_bad_quality_marker()
{
    var highContext = ControlRenderContext.ForState(ControlState.Active) with
    {
        NumericValues = new Dictionary<string, double> { ["ProcessValue"] = 125 }
    };
    var badContext = ControlRenderContext.ForState(ControlState.Unknown) with
    {
        Quality = VariableQuality.Bad,
        NumericValues = new Dictionary<string, double> { ["ProcessValue"] = 50 }
    };
    var high = PipeInstrumentOperatorGeometry.BuildLevelBar(highContext);
    var bad = PipeInstrumentOperatorGeometry.BuildLevelBar(badContext);
    Assert.Equal("100", high.Metadata["display.percent"]);
    Assert.Contains(bad.Primitives, p => p.PartId == "quality.unknown");
}
```

Test numeric formatting with unit, temperature/pressure/flow family identity, elbow and tee branch geometry, and command buttons rendering pending separately from confirmed feedback.

- [ ] **Step 2: Implement the approved controls**

Pipes use independent start/end/bend geometry from `PipeObject`; flow markers translate along the verified path only when feedback and quality allow it. Elbows and tees have explicit paths and no inferred topology. Numeric displays use invariant model values and localized display formatting. Level bars and indicators clamp visual fill/needle positions while retaining the raw value for diagnostics. Operator controls emit only declarative action intent and never mutate feedback state. `ControlGeometryFactory.Build(typeId, context)` dispatches to the explicit builders from Tasks 7-9 and rejects unknown IDs without a fallback drawing.

Add `PipeObject WithEnd(PointD end)` and `WithStart(PointD start)` helpers that recompute bounds from start, bends and end without modifying any other object.

- [ ] **Step 3: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
dotnet test tests/Scada.Core.Tests/Scada.Core.Tests.csproj --no-restore
git add src/Scada.Controls/Geometry src/Scada.Scene/SceneContracts.cs tests/Scada.Controls.Tests tests/Scada.Core.Tests
git commit -m "feat: add pipe measurement and operator controls"
```

## Task 10: Implement the native WPF renderer and preview host

**Files:**
- Create `src/Scada.Controls.Wpf/IndustrialControl.cs`.
- Create `src/Scada.Controls.Wpf/WpfControlRenderer.cs`.
- Create `src/Scada.Controls.Wpf/Themes/Generic.xaml`.
- Create WPF preview files under `samples/Scada.Controls.Preview.Wpf/`.
- Test `tests/Scada.Controls.Wpf.Tests/WpfRenderingTests.cs`.
- Test support `tests/Scada.Controls.Wpf.Tests/StaThread.cs`.

- [ ] **Step 1: Write WPF rendering tests**

Run WPF tests in an STA thread and assert:

```csharp
[Fact]
public Task Pump_plan_creates_named_native_wpf_parts() => StaThread.RunAsync(() =>
{
    var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));
    var visual = WpfControlRenderer.Render(pumpPlan);
    Assert.NotNull(VisualSearch.ByPartId(visual, "pump.casing"));
    Assert.NotNull(VisualSearch.ByPartId(visual, "pump.motor"));
});

[Fact]
public Task Resize_scales_body_and_not_only_selection_bounds() => StaThread.RunAsync(() =>
{
    var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Stopped));
    var control = new IndustrialControl { RenderPlan = pumpPlan, Width = 240, Height = 144 };
    control.Measure(new Size(240, 144));
    control.Arrange(new Rect(0, 0, 240, 144));
    Assert.Equal(2d, control.GeometryScaleX, precision: 3);
    Assert.Equal(2d, control.GeometryScaleY, precision: 3);
});
```

`StaThread.RunAsync(Action action)` starts a background `Thread`, calls `SetApartmentState(ApartmentState.STA)`, executes the action, completes a `TaskCompletionSource`, propagates any exception and joins the thread. `VisualSearch.ByPartId(DependencyObject root, string partId)` recursively walks `VisualTreeHelper` children and reads the renderer's attached `PartId` property. Keep both helpers inside the WPF test project.

- [ ] **Step 2: Implement native rendering and visual states**

`IndustrialControl` derives from `Control` and exposes dependency properties for `RenderPlan`, `State`, `ReducedMotion` and `Orientation`. `WpfControlRenderer` maps lines, rectangles, ellipses and typed paths to `Shape`, `StreamGeometry` and `Canvas` elements; it attaches stable part IDs without parsing arbitrary XAML. Use `VisualStateManager` groups `EquipmentState` and `QualityState`. Apply a `Viewbox` or equivalent uniform transform for preserve-aspect equipment and directional transforms for stretchable pipes.

Animations target named moving parts only. On unknown quality, stop storyboards and show the stable unknown marker. All brushes come from `Generic.xaml` token resources.

- [ ] **Step 3: Implement the preview window and artifact mode**

The preview window displays the generic equipment sheet, a state selector and reduced-motion toggle. Command-line argument `--render-review <absolute-directory>` renders deterministic magnified sheets and a 1920x1080 exact-size PNG using `RenderTargetBitmap`, writes the files, then exits. It must not open authentication, databases or PLC connections.

- [ ] **Step 4: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Wpf.Tests/Scada.Controls.Wpf.Tests.csproj --configuration Release --no-restore
$reviewRoot = [IO.Path]::GetFullPath('artifacts/phase2/wpf')
dotnet run --project samples/Scada.Controls.Preview.Wpf/Scada.Controls.Preview.Wpf.csproj --configuration Release -- --render-review $reviewRoot
git add src/Scada.Controls.Wpf samples/Scada.Controls.Preview.Wpf tests/Scada.Controls.Wpf.Tests
git commit -m "feat: add native wpf industrial control renderer"
```

Expected: WPF tests pass and review PNG files are non-empty. `artifacts/` remains ignored and uncommitted.

## Task 11: Implement deterministic SVG rendering and preview output

**Files:**
- Create `src/Scada.Controls.Svg/SvgControlRenderer.cs`.
- Create `src/Scada.Controls.Svg/SvgDocumentWriter.cs`.
- Create `samples/Scada.Controls.Preview.Svg/Program.cs`.
- Test `tests/Scada.Controls.Svg.Tests/SvgRenderingTests.cs`.

- [ ] **Step 1: Write SVG output tests**

```csharp
[Fact]
public void Svg_contains_stable_part_ids_state_and_view_box()
{
    var pumpPlan = PumpAndValveGeometry.BuildPump(ControlRenderContext.ForState(ControlState.Active));
    var svg = new SvgControlRenderer().Render(pumpPlan);
    var document = XDocument.Parse(svg);
    var root = document.Root!;
    Assert.Equal("0 0 120 72", root.Attribute("viewBox")!.Value);
    Assert.Equal("active", root.Attribute("data-state")!.Value);
    Assert.Single(root.Descendants().Where(e => (string?)e.Attribute("data-part") == "pump.casing"));
}

[Fact]
public void Svg_escapes_text_and_has_no_script_or_external_asset()
{
    var context = ControlRenderContext.ForState(ControlState.Active) with
    {
        TextValues = new Dictionary<string, string>
        {
            ["DisplayValue"] = "<unsafe>",
            ["Unit"] = "°C"
        }
    };
    var svg = new SvgControlRenderer().Render(
        PipeInstrumentOperatorGeometry.BuildNumericDisplay(context));
    Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("href=\"http", svg, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("&lt;unsafe&gt;", svg);
}
```

- [ ] **Step 2: Implement SVG and HTML writers**

Use `XmlWriter` with invariant numeric formatting and deterministic attribute/primitive ordering. Map the typed path commands directly to SVG path data. Every SVG root includes `data-control-type`, `data-control-version`, `data-state` and `data-quality`; every named primitive includes `data-part`. Use CSS classes generated from shared visual tokens. Motion is CSS/SVG animation scoped to named parts and disabled by `prefers-reduced-motion` or render context.

Reject or escape arbitrary markup in labels. Do not emit scripts, network URLs, base64 images or third-party assets.

- [ ] **Step 3: Add deterministic preview generation**

`Scada.Controls.Preview.Svg` accepts `--output <absolute-directory>`, writes `index.html` plus one standalone SVG per state/control, and exits. The HTML may switch state examples locally but must have no server, SignalR or PLC code.

- [ ] **Step 4: Verify and commit**

```powershell
dotnet test tests/Scada.Controls.Svg.Tests/Scada.Controls.Svg.Tests.csproj --configuration Release --no-restore
$reviewRoot = [IO.Path]::GetFullPath('artifacts/phase2/svg')
dotnet run --project samples/Scada.Controls.Preview.Svg/Scada.Controls.Preview.Svg.csproj --configuration Release -- --output $reviewRoot
git add src/Scada.Controls.Svg samples/Scada.Controls.Preview.Svg tests/Scada.Controls.Svg.Tests
git commit -m "feat: add deterministic svg industrial renderer"
```

## Task 12: Build the shared sample project and deterministic scenarios

**Files:**
- Create `samples/Scada.Controls.SampleProject/SampleProjectFactory.cs`.
- Create `samples/Scada.Controls.SampleProject/SampleScenarios.cs`.
- Modify both preview applications to consume the shared sample.
- Test `tests/Scada.Phase2.Acceptance.Tests/SampleProjectTests.cs`.

- [ ] **Step 1: Write sample-contract tests**

```csharp
[Fact]
public void Generic_sample_contains_every_initial_family_without_plc_addresses()
{
    var project = SampleProjectFactory.Create();
    Assert.All(ControlTypeIds.All, id =>
        Assert.Contains(project.Screens.SelectMany(s => s.Objects), o => o.Type == id));
    Assert.DoesNotContain(project.Screens.SelectMany(s => s.Objects)
        .SelectMany(o => o.Properties.Values), value => value.Contains("DB", StringComparison.OrdinalIgnoreCase));
}

[Theory]
[InlineData("stopped")]
[InlineData("active")]
[InlineData("transition")]
[InlineData("fault")]
[InlineData("unknown")]
public void Every_review_scenario_is_deterministic(string scenarioName)
{
    Assert.Equal(SampleScenarios.Get(scenarioName), SampleScenarios.Get(scenarioName));
}
```

- [ ] **Step 2: Implement the 1920x1080 sample**

Create one generic process screen with semantic zones: source vessel and valve, pump/motor, filter, receiving vessel with agitator, straight/elbow/tee pipes, numeric/level/temperature/pressure/flow controls and operator buttons. Use fixed scene IDs and deterministic timestamps for review output. Include all five states without embedding customer addresses or sequence logic.

Add a second dense-layout screen using only generic three-tank-like placement to test crowding and independent routing. Label it as layout validation, not verified process topology.

- [ ] **Step 3: Drive scenarios through `OfflineSimulator`**

Use `SetFeedback` for feedback/parameter variables only. Represent pending command intent in a separate preview action-state object; never write command variables as confirmed feedback. Both preview applications consume the same scene and scenario snapshot.

- [ ] **Step 4: Verify and commit**

```powershell
dotnet test tests/Scada.Phase2.Acceptance.Tests/Scada.Phase2.Acceptance.Tests.csproj --no-restore
git add samples tests/Scada.Phase2.Acceptance.Tests
git commit -m "feat: add phase two industrial sample project"
```

## Task 13: Verify the editing contract and cross-render equivalence

**Files:**
- Create `src/Scada.Controls/SceneGeometryOperations.cs`.
- Create `tests/Scada.Controls.Tests/SceneGeometryOperationTests.cs`.
- Create `tests/Scada.Phase2.Acceptance.Tests/CrossRendererAcceptanceTests.cs`.
- Create `tests/Scada.Phase2.Acceptance.Tests/StaThread.cs`.

- [ ] **Step 1: Write scene-operation tests**

```csharp
[Fact]
public void Moving_valve_does_not_move_unselected_pipe()
{
    var valveId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var pipeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    var valve = ControlObject.Create("equipment.valve.automated", new RectD(40, 30, 40, 40), valveId);
    var pipe = PipeObject.Create(new PointD(0, 50), new PointD(120, 50), id: pipeId);
    var screen = ScreenDocument.Create("Main", new SceneObject[] { valve, pipe });
    var moved = SceneGeometryOperations.Move(screen, new[] { valveId }, dx: 1, dy: 0);
    Assert.Equal(pipe, Assert.IsType<PipeObject>(moved.Objects.Single(o => o.Id == pipeId)));
    Assert.Equal(41, Assert.IsType<ControlObject>(moved.Objects.Single(o => o.Id == valveId)).Bounds.X);
}

[Fact]
public void Group_move_changes_only_explicit_ids()
{
    var pump = ControlObject.Create("equipment.pump.centrifugal", new RectD(10, 20, 120, 72));
    var valve = ControlObject.Create("equipment.valve.automated", new RectD(160, 30, 80, 64));
    var pipe = PipeObject.Create(new PointD(130, 56), new PointD(160, 56));
    var label = TextObject.Create("P-101", new RectD(10, 96, 80, 24));
    var screen = ScreenDocument.Create("Main", new SceneObject[] { pump, valve, pipe, label });
    var moved = SceneGeometryOperations.Move(screen, new[] { pump.Id, label.Id }, dx: 4, dy: -2);
    Assert.Equal(pipe, moved.Objects.Single(o => o.Id == pipe.Id));
    Assert.Equal(valve, moved.Objects.Single(o => o.Id == valve.Id));
}
```

Also test one-unit keyboard nudging, rotation about object center, aspect-preserving equipment resize, free pipe endpoint resize and serialization without geometry drift.

- [ ] **Step 2: Implement immutable geometry operations**

Expose `Move`, `Resize`, `Rotate` and `Nudge` methods in `Scada.Controls` that accept a `ControlCatalog`, explicit scene IDs and return a new `ScreenDocument`. Apply `ControlDefinition.ResizePolicy`; never discover adjacent pipes or alter unselected objects. Recompute pipe bounds after endpoint/bend changes. Reject non-finite coordinates, negative sizes and missing IDs with stable exceptions. Keeping this catalog-aware service above `Scada.Scene` avoids a reverse project reference from the scene model to the control SDK.

- [ ] **Step 3: Write and pass cross-render semantic tests**

For every control and required state, render WPF and SVG from the same `ControlRenderPlan`. Run WPF creation through the same `StaThread.RunAsync` implementation used by the WPF test project. Normalize output to `{type, version, state, partIds, activeAnimations, anchors}` and assert equality. Additionally assert WPF and SVG each contain visible non-background primitives and no diagnostic placeholder for valid input.

Run:

```powershell
dotnet test tests/Scada.Controls.Tests/Scada.Controls.Tests.csproj --no-restore
dotnet test tests/Scada.Phase2.Acceptance.Tests/Scada.Phase2.Acceptance.Tests.csproj --no-restore
```

- [ ] **Step 4: Commit**

```powershell
git add src/Scada.Controls tests/Scada.Controls.Tests tests/Scada.Phase2.Acceptance.Tests
git commit -m "test: verify independent editing and renderer parity"
```

## Task 14: Produce visual evidence, source audit and the Phase 2 gate

**Files:**
- Create `docs/phase2/source-audit.md`.
- Create `docs/phase2/visual-review.md`.
- Create `docs/phase2-acceptance.md`.
- Create `docs/handoff-2026-08-20-phase2.md`.
- Modify `docs/handoff-current.md`.
- Modify `docs/MASTER_ROADMAP.md` only after the gate passes.
- Complete `tests/Scada.Phase2.Acceptance.Tests/Phase2AcceptanceTests.cs`.

- [ ] **Step 1: Complete the asset/source audit before visual acceptance**

For every reviewed source, record URL, repository/document title, license, accessed date, what principle was learned, whether code/assets were reused, and any attribution obligation. Mark no-license repositories as `study-only`. If a manual, PDF, specification or sample archive is downloaded, verify it and archive it under the matching folder inside `E:\BaiduSyncdisk\旧电脑\知识库`, with an adjacent Markdown source note containing the required metadata and SHA-256.

- [ ] **Step 2: Generate and inspect all review artifacts**

```powershell
$wpfReview = [IO.Path]::GetFullPath('artifacts/phase2/wpf')
$svgReview = [IO.Path]::GetFullPath('artifacts/phase2/svg')
dotnet run --project samples/Scada.Controls.Preview.Wpf/Scada.Controls.Preview.Wpf.csproj --configuration Release -- --render-review $wpfReview
dotnet run --project samples/Scada.Controls.Preview.Svg/Scada.Controls.Preview.Svg.csproj --configuration Release -- --output $svgReview
```

Record in `visual-review.md` the magnified morphology result, stopped/active/transition/fault/unknown state result, 100 percent 1920x1080 result, full sample-screen result and dense-layout result. List hard failures separately from refinements. A hard failure includes unrecognizable device silhouette, wrong orientation, anchor off the visible boundary, clipped label/value, malformed geometry, full-device decorative flashing, command shown as confirmed feedback, nonblank mismatch between WPF/SVG semantics, or any unlicensed copied asset.

- [ ] **Step 3: Add the end-to-end acceptance test**

The test creates the v2 sample, validates bindings/actions, serializes and restores it, runs all deterministic scenarios, builds all render plans, renders both profiles, verifies semantic equivalence and confirms invalid bindings produce stable diagnostics without crashing another control. It must assert there are no strings resembling Siemens DB addresses in reusable definitions.

- [ ] **Step 4: Run the complete Phase 2 gate**

```powershell
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
git status --short --branch
```

Expected: restore succeeds; Release build has 0 warnings and 0 errors; every test passes; `git diff --check` is clean; review artifacts exist and are non-empty; only intended new-platform source/docs are staged for the final commit.

- [ ] **Step 5: Record the gate and commit**

Only after Step 4 and visual review pass, mark Phase 2 complete in `MASTER_ROADMAP.md`, write exact commands/results and remaining risks in both handoff documents, and commit:

```powershell
git add docs tests/Scada.Phase2.Acceptance.Tests
git commit -m "test: complete phase two control sdk gate"
```

## Self-Review Checklist

- Spec coverage: Tasks 2-4 implement semantic definitions, state precedence, binding validation, Chinese events/actions and feedback-driven behavior. Task 5 implements versioning and migration. Tasks 6-9 implement shared visual language and every approved first control family. Tasks 10-11 implement native WPF and SVG profiles. Tasks 12-14 implement offline samples, independent editing, cross-render parity, visual review, source audit and the final gate.
- Scope check: WPF and SVG preview hosts are verification-only. The WinCC-style editor, production Web Runtime, Gateway, PLC adapters, WinCC deployment and business modules remain excluded.
- Placeholder scan: no placeholder marker, unspecified error-handling request or unassigned test remains in the plan.
- Type consistency: `ControlDefinition`, `ControlStateResult`, `ControlRenderPlan`, `ControlCatalog`, `ControlValidator`, `ControlStateResolver`, `InteractionDefinition`, `ProjectFormat.CurrentVersion` and renderer APIs are introduced before later tasks use them.
- Safety check: commands remain intent only; only feedback drives confirmed state; Phase 2 has no live communication path.
- Persistence check: project-v2 has an explicit v1 migration and serializer/schema round-trip tests.
- Visual check: every equipment family is reviewed magnified, at 100 percent size and on a complete page; dense three-tank placement is not treated as verified process topology.
- Licensing check: no third-party geometry or image enters production without compatible license evidence.

Plan complete. Tasks 1 through 14 were implemented and verified on 2026-08-20. Phase 3 requires a separate approved plan.
