# Phase 3-M2 Engineering Productivity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (recommended) to implement this plan task-by-task with review checkpoints. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Add the WinCC-style engineering productivity loop that is still missing after M1: alignment, distribution, grouping/layers, and reliable undo/redo without changing the retained scene contract or adding runtime/PLC behavior.

**Architecture:** Keep `EditorSession` as the single mutation boundary. Each edit records an immutable before/after `ProjectDocument` snapshot in a bounded history service; undo and redo restore snapshots without serializing viewport state. Alignment, distribution, grouping and layer operations remain explicit selection-scoped scene transformations, and pipes remain independently editable objects.

**Tech Stack:** .NET 10, C#, WPF, existing `Scada.Scene`, `Scada.Controls`, `Scada.Editor.Wpf`, xUnit and STA WPF tests.

---

## Scope Constraints

- Included: undo/redo, selection alignment, equal distribution, grouping/ungrouping, z-order/layer commands, keyboard shortcuts and editor toolbar/menu bindings.
- Excluded: PLC/S7 communication, Web Runtime, WinCC deployment, alarms, trends, recipes, batches, CAD/PDF recognition and automatic pipe routing.
- Moving a control must not move an unselected pipe. Grouping is an editor selection construct; it must not introduce port connections or rewrite pipe geometry.

## File Map

- Create `src/Scada.Editor.Wpf/EditHistory.cs` for bounded immutable project snapshots and undo/redo state.
- Modify `src/Scada.Editor.Wpf/EditorSession.cs` to route every scene mutation through history and expose `CanUndo`, `CanRedo`, `Undo`, `Redo`.
- Modify `src/Scada.Controls/SceneGeometryOperations.cs` with alignment, distribution and z-order operations over explicit IDs.
- Modify `src/Scada.Scene/SceneContracts.cs` only if grouping metadata needs a versioned, schema-safe field; add migration and round-trip tests if the persisted shape changes.
- Modify `src/Scada.Editor.Wpf/EditorShellViewModel.cs` and `EditorShellWindow.xaml` to bind Chinese menu/toolbar commands and keyboard gestures.
- Modify `src/Scada.Editor.Wpf/EditorCanvas.cs` to preserve selection after transforms and refresh only affected adorners.
- Add tests under `tests/Scada.Editor.Wpf.Tests/` for history, geometry operations, persistence and WPF command state.
- Update `docs/phase3/acceptance.md`, `docs/phase3/visual-review.md` and `docs/handoff-current.md` after the gate.

## Task 1: Immutable undo/redo history

**Files:** `EditHistory.cs`, `EditorSession.cs`, `EditorSessionTests.cs`

- [x] Write a failing test that performs two edits, undoes twice, redoes once, and verifies object IDs, geometry, metadata and dirty state.
- [x] Run the focused test and confirm it fails because history does not exist.
- [x] Implement a bounded history with a configurable capacity of 100 snapshots, clearing the redo stack after a new edit and preserving the initial project snapshot.
- [x] Route `ReplaceActiveScreen`, object add/remove and property edits through one history-aware mutation method; viewport zoom/pan must not enter history.
- [x] Add `Undo`/`Redo` tests for an independent pipe and a selected control to prove no unrelated object changes.
- [x] Run focused tests; full verification and commit are the Task 1 checkpoint below.

## Task 2: Alignment and distribution operations

**Files:** `SceneGeometryOperations.cs`, `EditorSession.cs`, `EditorSessionTests.cs`

- [x] Write failing tests for left/center/right and top/middle/bottom alignment using three controls, plus horizontal and vertical equal distribution.
- [x] Run the tests and confirm the operation APIs are missing.
- [x] Implement selection validation requiring at least two non-pipe objects; preserve widths, heights, rotations, IDs, metadata and all pipe endpoints.
- [x] Add session methods that record each operation as one undoable edit and keep the same selection.
- [x] Run focused geometry and history tests; full verification and commit are the Task 2 checkpoint below.

## Task 3: Grouping and layer order

**Files:** `SceneContracts.cs`, `SceneGeometryOperations.cs`, `EditorSession.cs`, serializer/schema tests if needed

- [x] First prefer an editor-only selection group map outside `ProjectDocument`; only persist group IDs if reopening a project must preserve groups, and then add a schema migration.
- [x] Write failing tests for group, ungroup, bring-forward, send-backward, bring-to-front and send-to-back while asserting stable object IDs and independent pipe geometry.
- [x] Implement layer operations by changing only `ZIndex`; group operations must not merge objects or change their bounds.
- [x] Add session coverage for the chosen group representation and reject malformed group references.
- [x] Run focused tests; full verification and commit are the Task 3 checkpoint below.

## Task 4: WPF command surface and shortcuts

**Files:** `EditorShellViewModel.cs`, `EditorShellWindow.xaml`, `EditorCanvas.cs`, `WpfEditorVisualTests.cs`

- [x] Write failing STA tests for command enablement and Chinese labels: `撤销`, `重做`, `左对齐`, `水平等距`, `组合`, `取消组合`, `置于顶层`, `置于底层`.
- [x] Bind commands to the shell and use `Ctrl+Z`, `Ctrl+Y`, `Ctrl+G`, `Ctrl+Shift+G`; arrow keys remain one-pixel nudges.
- [x] Add disabled states when selection/history does not satisfy the operation requirements.
- [x] Ensure clicking empty canvas clears selection and command state updates without rebuilding unrelated visuals.
- [x] Run WPF tests; full verification and commit are the Task 4 checkpoint below.

## Task 5: M2 acceptance gate

**Files:** `docs/phase3/acceptance.md`, `docs/phase3/visual-review.md`, `docs/handoff-current.md`

- [ ] Run `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` and require 0 warnings and 0 errors.
- [ ] Run `dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build` and record the exact total.
- [ ] Run `git diff --check` and verify no legacy, PLC or customer files changed.
- [ ] Review undo/redo, alignment, grouping and layer commands at 1920x1080 and exact selection states.
- [ ] Update the acceptance matrix and handoff with the next approved task; do not start Phase 4 or online adapters from this gate.
- [ ] Commit `test: complete phase3 m2 acceptance gate`.

## Verification Matrix

| Evidence | Command/artifact | Proves |
|---|---|---|
| Model | Editor session and geometry tests | immutable operations and independent pipes |
| WPF | STA visual/command tests | labels, shortcuts, selection and disabled states |
| Persistence | serializer/revision round-trip tests | no geometry or metadata drift |
| Full offline | Release build and complete test suite | repository integration without PLC/runtime |
