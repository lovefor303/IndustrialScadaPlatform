# Phase 3-M1 WinCC-Style WPF Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** Build the first developer-only WPF editor that composes Phase 2 controls, edits the retained scene model independently, configures bindings/dynamics/events, and survives save/reopen without geometry drift.

**Architecture:** Add a `Scada.Editor.Wpf` application and a small editor-session layer over the existing `ProjectDocument`, `ScreenDocument`, `SceneGeometryOperations` and `RevisionStore`. Keep scene mutation immutable and command-driven; keep WPF rendering native through `Scada.Controls.Wpf`. The editor stores declarative bindings/interactions and does not add PLC or runtime ownership.

**Tech Stack:** .NET 10, WPF, existing `Scada.Core`, `Scada.Scene`, `Scada.Controls`, `Scada.Controls.Wpf`, `Scada.Storage`, xUnit, STA test helpers, temporary SQLite stores.

---

## File Map

- Create `src/Scada.Editor.Wpf/Scada.Editor.Wpf.csproj`: net10.0-windows WPF executable/library settings and project references.
- Create `src/Scada.Editor.Wpf/EditorSession.cs`: current project, screen, selection, dirty state and immutable mutation entry points.
- Create `src/Scada.Editor.Wpf/EditorAuthorization.cs`: local `EditorRole` enum and developer/engineer permission gate; no PLC or runtime permission logic.
- Create `src/Scada.Editor.Wpf/EditorCommands.cs`: new/open/save/publish/restore, selection and viewport commands.
- Create `src/Scada.Editor.Wpf/EditorShellWindow.xaml` and `.xaml.cs`: WinCC-style docked editor shell and bindings.
- Create `src/Scada.Editor.Wpf/EditorShellViewModel.cs`: observable shell state and command adapters.
- Create `src/Scada.Editor.Wpf/EditorCanvas.cs`: canvas rendering, selection adorners, drag/drop and pointer transforms.
- Create `src/Scada.Editor.Wpf/EditorViewport.cs`: grid, rulers, zoom/pan and screen-to-canvas transforms.
- Create `src/Scada.Editor.Wpf/PropertyPanelViewModel.cs`: selected-object properties and geometry editing.
- Create `src/Scada.Editor.Wpf/BindingPanelViewModel.cs`: valid variable-role binding editing.
- Create `src/Scada.Editor.Wpf/DynamicsPanelViewModel.cs`: declarative property dynamics editing.
- Create `src/Scada.Editor.Wpf/EventsPanelViewModel.cs`: Chinese event/action selection and parameter editing.
- Create `src/Scada.Editor.Wpf/ToolboxCatalog.cs`: toolbox groups and default object sizes.
- Modify `src/Scada.Scene/ProjectDocument.cs`: explicit immutable project replacement methods that update `UpdatedAt`.
- Modify `src/Scada.Scene/SceneContracts.cs`: explicit screen/object replacement helpers required by editor state.
- Modify `src/Scada.Controls/SceneGeometryOperations.cs`: alignment, distribution, group selection and pipe endpoint operations if not already present.
- Modify `IndustrialScadaPlatform.sln`: include editor and tests.
- Create `tests/Scada.Editor.Wpf.Tests/Scada.Editor.Wpf.Tests.csproj` and `StaThread.cs`.
- Create `tests/Scada.Editor.Wpf.Tests/EditorSessionTests.cs`: model, selection, dirty state and independent editing tests.
- Create `tests/Scada.Editor.Wpf.Tests/EditorPersistenceTests.cs`: save/reopen/publish/restore round-trip tests.
- Create `tests/Scada.Editor.Wpf.Tests/EditorAuthorizationTests.cs`: developer/operator permission tests.
- Create `tests/Scada.Editor.Wpf.Tests/WpfEditorVisualTests.cs`: loaded visual tree, selection adorners, handles, panels and viewport tests.
- Create `tests/Scada.Editor.Wpf.Tests/GeometryDriftTests.cs`: exact equality of IDs, bounds, rotations, bends and metadata after round-trip.
- Create `samples/Scada.Editor.Preview.Wpf/Scada.Editor.Preview.Wpf.csproj` and preview host for an offline sample screen.
- Create `docs/phase3/visual-review.md` and `docs/phase3/acceptance.md` after the gate.
- Update `docs/handoff-current.md` only at meaningful checkpoints with the branch, command, dirty files and exact next action.

### Task 1: Add immutable editor-facing project/session contracts

**Files:**
- Modify: `src/Scada.Scene/ProjectDocument.cs`, `src/Scada.Scene/SceneContracts.cs`
- Create: `src/Scada.Editor.Wpf/EditorSession.cs`
- Test: `tests/Scada.Editor.Wpf.Tests/EditorSessionTests.cs`

- [ ] Write tests that replace a screen, replace a scene object, add a toolbox object and mark the project dirty while preserving all unrelated object IDs.
- [ ] Run the focused tests and confirm they fail because the editor project and replacement APIs do not exist.
- [ ] Add `ProjectDocument.ReplaceScreen`, `ProjectDocument.ReplaceVariables`, and `ProjectDocument.TouchDraft` returning `ProjectDocument.FromStorage` with a new `UpdatedAt` and `ProjectStatus.Draft`.
- [ ] Add `ScreenDocument.AddObject`, `RemoveObject`, `ReplaceObjects` and `FindObject` with duplicate-ID validation.
- [ ] Implement `EditorSession` with `Project`, `ActiveScreenName`, `SelectedObjectIds`, `IsDirty`, `ReplaceActiveScreen`, `AddObject`, `RemoveSelected`, `Move`, `Resize`, `Rotate`, `Nudge` and `SetSelectedObjectMetadata`; every method replaces immutable records and sets `IsDirty`.
- [ ] Run the focused tests and confirm pass.
- [ ] Commit `feat: add phase3 editor session contracts`.

### Task 2: Create the WPF editor project and shell baseline

**Files:**
- Create: `src/Scada.Editor.Wpf/Scada.Editor.Wpf.csproj`, `EditorShellWindow.xaml`, `EditorShellWindow.xaml.cs`, `EditorShellViewModel.cs`, `EditorAuthorization.cs`.
- Modify: `IndustrialScadaPlatform.sln`.
- Test: `tests/Scada.Editor.Wpf.Tests/EditorAuthorizationTests.cs` and STA setup.

- [ ] Add a WPF project targeting `net10.0-windows`, `UseWPF=true`, nullable and warning-as-error inherited from `Directory.Build.props`, with references to `Scada.Core`, `Scada.Scene`, `Scada.Controls`, `Scada.Controls.Wpf` and `Scada.Storage`.
- [ ] Add a shell with menu/toolbar, left project tree, center canvas host, right dock tabs (`属性`, `变量`, `动态`, `事件`) and bottom status bar. Use neutral industrial colors and no decorative dashboard cards.
- [ ] Add local `EditorRole { Developer, Engineer, Operator, Viewer }` and `EditorAuthorization.CanEdit(EditorRole role)` returning true only for `Developer` and `Engineer`; operator/viewer receives a disabled/hidden engineering surface. This is a Phase 3 editor boundary and is not a legacy WPF role import.
- [ ] Write STA tests that load the window, assert the four panel headers and verify operator access is denied.
- [ ] Build the project and run the focused tests.
- [ ] Commit `feat: add wpf editor shell and authorization gate`.

### Task 3: Implement toolbox drag-in and independent scene editing

**Files:**
- Create: `ToolboxCatalog.cs`, `EditorCanvas.cs`, `EditorViewport.cs`.
- Modify: `src/Scada.Controls/SceneGeometryOperations.cs` only for missing alignment/distribution/group helpers.
- Test: `EditorSessionTests.cs`, `GeometryDriftTests.cs`, `WpfEditorVisualTests.cs`.

- [ ] Define toolbox entries from `ControlCatalog.CreateDefault().All` plus `pipe.straight` and `text`, with default bounds taken from the control definition and a stable type ID.
- [ ] Write a failing test that drags a valve and a pipe into a screen, moves only the valve, and expects the pipe endpoints and bends to remain byte-for-byte unchanged.
- [ ] Add WPF drag/drop data with type ID and preview size; on drop call `EditorSession.AddObject` using canvas coordinates transformed by `EditorViewport`.
- [ ] Render each `ControlObject` through `IndustrialControl`/`WpfControlRenderer`, render `PipeObject` with its own path, and render `TextObject` with its stored text.
- [ ] Add selection adorners, multi-select, click-empty-to-clear, direction-key nudging, delete and explicit pipe endpoint handles. Do not infer or enforce connections.
- [ ] Add Ctrl-click alignment/distribution commands that operate only on the current selection.
- [x] Run focused model and STA tests.
- [x] Commit `feat: add toolbox and independent scene editing`.

### Task 4: Add properties, variables, dynamics and events panels

**Files:**
- Create: `PropertyPanelViewModel.cs`, `BindingPanelViewModel.cs`, `DynamicsPanelViewModel.cs`, `EventsPanelViewModel.cs`.
- Modify: `src/Scada.Scene/SceneContracts.cs` only if a typed dynamic definition is missing; add a schema migration when persisted shape changes.
- Test: `tests/Scada.Editor.Wpf.Tests/EditorSessionTests.cs`, `WpfEditorVisualTests.cs`.

- [x] Write tests for editing bounds/rotation/visibility, selecting a valid binding role and rejecting an unknown variable or wrong data direction.
- [x] Implement property editing against immutable `SceneObject` copies; equipment uses the Phase 2 resize policy, while pipes use endpoint/bend operations.
- [x] Implement dynamics as declarative records `{TargetProperty, VariableKey, Condition/Mapping}` with validation and no runtime side effect.
- [x] Implement events using `InteractionCatalog` stable IDs and Chinese labels; action parameters are selected from known schemas rather than free-form English names.
- [x] Display validation errors with object ID and field path; preserve invalid drafts only in the view model and block publish until validation passes.
- [x] Run focused tests and verify serialized metadata survives a round-trip.
- [x] Commit `feat: add editor property binding dynamics and event panels`.

### Task 5: Add project/new-open/save/publish/restore workflow

**Files:**
- Modify: `EditorCommands.cs`, `EditorShellViewModel.cs`.
- Test: `EditorPersistenceTests.cs`.

- [x] Write a failing temporary-SQLite test for create project, add screen/object metadata, save draft, dispose session, load draft, publish, list revision, restore and load again.
- [x] Implement `RevisionStore` adapter calls with cancellation tokens and file dialogs abstracted behind `IProjectFileDialog` so tests never use the real user database.
- [x] Require confirmation before discarding dirty changes; save draft updates only the new platform temporary store.
- [x] Add toolbar commands `新建`, `打开`, `保存草稿`, `发布`, `恢复版本` and status messages; show revision number after publish.
- [x] Verify loaded project status is draft after restore and all stable IDs/metadata are identical.
- [x] Run focused persistence tests.
- [x] Commit `feat: add editor project persistence workflow`.

### Task 6: Add viewport ergonomics and geometry drift gates

**Files:**
- Modify: `EditorViewport.cs`, `EditorCanvas.cs`, `EditorShellWindow.xaml`.
- Test: `GeometryDriftTests.cs`, `WpfEditorVisualTests.cs`.

- [x] Write tests for zoom around cursor, pan, grid snapping toggle, ruler coordinates and one-pixel direction-key nudge in model coordinates.
- [x] Implement a uniform `MatrixTransform` from screen coordinates to model coordinates; never serialize the viewport transform into scene object bounds.
- [x] Add grid/ruler rendering, Ctrl+mouse-wheel zoom, middle-button pan, selection handles, rotation handle and stable minimum hit targets.
- [x] Add the round-trip drift assertion comparing every object ID, type, bounds, rotation, z-index, visibility, pipe endpoints, bends, properties, bindings and interactions exactly.
- [x] Render a 1920x1080 editor review artifact and assert non-empty content and in-canvas object extents.
- [x] Run all focused WPF and geometry tests.
- [x] Commit `feat: add editor viewport and geometry drift gates`.

### Task 7: Complete the Phase 3-M1 acceptance gate and handoff

**Files:**
- Create: `samples/Scada.Editor.Preview.Wpf/*`, `docs/phase3/acceptance.md`, `docs/phase3/visual-review.md`.
- Modify: `docs/handoff-current.md`, `docs/MASTER_ROADMAP.md` only after all evidence passes.

- [x] Add an offline sample screen containing a vessel, pump, valve, pipe, numeric display and text label with no customer PLC addresses.
- [x] Run `dotnet restore IndustrialScadaPlatform.sln`.
- [x] Run `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` and require 0 warnings and 0 errors.
- [x] Run `dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build` and record the exact pass/skip counts.
- [x] Run the Phase 3 editor acceptance test project and record project round-trip, permission, independent-editing and no-drift evidence separately from any future online evidence.
- [x] Review the editor at magnified morphology, exact 1920x1080 and full-screen density levels; reject clipping, stale selection handles, incorrect control scaling and any pipe mutation caused by moving an unselected object.
- [x] Update handoff with the branch, commit, commands, intentionally dirty files and the next approved Phase 3 task. Do not claim Phase 3 complete until the gate passes.
- [x] Commit `test: complete phase3 m1 editor acceptance gate`.

## Verification Matrix

| Evidence | Command/Artifact | Proves |
|---|---|---|
| Source/unit | editor/session tests | immutable editing and validation contracts |
| WPF visual | STA visual tests and 1920x1080 PNG | loaded layout, hit targets, panels and viewport |
| Offline integration | temporary SQLite persistence tests | save/reopen/publish/restore and no drift |
| Full offline | Release build and complete test suite | repository integration only |
| Online/PLC | none in M1 | explicitly not executed |

## Self-Review

- Phase 2 control semantics remain stable; no new PLC address or runtime command is introduced.
- Independent pipes and equipment are preserved; no port graph or auto-routing is added.
- Dynamics/events are persisted declaratively and validated; later modules own side effects.
- Project data uses isolated temporary SQLite files during tests; legacy databases and TIA projects are untouched.
- The milestone has no hidden Web, PLC, WinCC deployment, alarm, trend, recipe or batch dependency.
