# Phase 3-M3 Functional Editor Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the current offline WPF editor shell from a visual prototype into a usable engineering editor whose toolbox and inspector panels edit the retained project model without geometry drift.

**Architecture:** Keep `EditorSession` as the single editing boundary and keep scene objects immutable. WPF view models expose session-backed state; every successful edit goes through `EditorSession.UpdateSelectedObject` or another session operation so history, dirty state, project-change notifications and canvas refresh remain consistent. XAML controls are data-bound to these view models instead of carrying independent UI-only state.

**Tech Stack:** .NET 10, WPF, C# records, xUnit STA tests, existing `Scada.Scene`, `Scada.Controls` and `Scada.Storage` contracts.

## Scope Boundary

Included: toolbox catalog display and drag-in, selection-aware property editing, variable binding editing, dynamic editing, event/action editing, save/reopen geometry regression, Chinese labels and command-state refresh.

Excluded: PLC/S7 communication, Gateway, Web Runtime, WinCC deployment, alarms, trends, recipes, batches, CAD/PDF recognition and legacy project migration.

### Task 1: Make the toolbox functional

**Files:** `src/Scada.Editor.Wpf/EditorShellWindow.xaml`, `src/Scada.Editor.Wpf/EditorShellWindow.xaml.cs`, `src/Scada.Editor.Wpf/EditorShellViewModel.cs`, `tests/Scada.Editor.Wpf.Tests/WpfEditorVisualTests.cs`

- [x] Add stable `ToolboxCatalog` type IDs to each visible leaf and expose a drag-start handler that calls `EditorCanvas.BeginToolboxDrag` with the entry type ID.
- [x] Keep the existing grouped TreeView labels while wiring leaf items to the catalog's stable Chinese control IDs.
- [x] Add a failing STA test that finds a catalog entry in the window, verifies its type ID is retained in the item container, and verifies the shell remains usable after a drag-start event.
- [x] Run the focused test and then implement the minimum XAML/code-behind wiring.

### Task 2: Make the property panel edit the selected object

**Files:** `src/Scada.Editor.Wpf/PropertyPanelViewModel.cs`, `src/Scada.Editor.Wpf/EditorShellViewModel.cs`, `src/Scada.Editor.Wpf/EditorShellWindow.xaml`, `tests/Scada.Editor.Wpf.Tests/WpfEditorVisualTests.cs`

- [x] Add selection-aware properties for X, Y, width, height, rotation and visibility, plus an `ApplyGeometry` command that validates finite positive dimensions and uses `PropertyPanelViewModel.SetGeometry`.
- [x] Add an explicit no-selection state and disable Apply when selection count is not one.
- [x] Add a failing test that changes a valve through the panel command and asserts the retained object and canvas geometry change immediately.
- [x] Verify invalid dimensions are rejected without changing the object.

### Task 3: Wire variables, dynamics and events panels

**Files:** existing `BindingPanelViewModel.cs`, `DynamicsPanelViewModel.cs`, `EventsPanelViewModel.cs`, `EditorShellViewModel.cs`, `EditorShellWindow.xaml`, focused tests

- [x] Expose Chinese option lists from the existing catalogs and project variables.
- [x] Add panel commands that call the existing `TrySet` methods and surface validation errors in panel error text.
- [x] Disable these commands unless exactly one object is selected and required values are present; existing type/direction validation remains authoritative.
- [x] Add tests for a valid binding, a dynamic rule, and an event/action pair.

### Task 4: Persistence and reopen acceptance

**Files:** `EditorShellWindow.xaml.cs`, `EditorShellViewModel.cs`, `EditorCommands.cs`, `tests/Scada.Editor.Wpf.Tests/WpfEditorVisualTests.cs`, `docs/phase3-m3-acceptance.md`

- [x] Inject a temporary `RevisionStore` and `EditorCommands` into the preview shell instead of leaving save/open commands detached.
- [x] Add a save/reopen test that edits geometry, saves draft, reloads, and compares object IDs, bounds, rotation and pipe endpoints.
- [x] Keep all files in a temporary test directory and never touch legacy databases.

### Task 5: M3 gate and handoff

- [x] Run Release build, full solution tests, editor focused tests and `git diff --check`.
- [x] Produce `docs/phase3-m3-acceptance.md` with the exact commands and results.
- [x] Update `docs/handoff-current.md` with the M3 checkpoint and remaining exclusions.
- [ ] Commit each task separately with focused messages.

## Verification Commands

```powershell
dotnet test tests/Scada.Editor.Wpf.Tests/Scada.Editor.Wpf.Tests.csproj --configuration Release --no-restore
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
```
