# Phase 3-M1 Acceptance

## Scope

This gate covers the offline developer/editor milestone only:

- project/session contracts and stable scene IDs;
- toolbox drop-in for Phase 2 controls, pipes and text;
- independent control and pipe editing;
- properties, variable bindings, declarative dynamics and events;
- draft save, publish, revision listing, restore and dirty-discard protection;
- viewport zoom/pan/grid behavior, selection handles and geometry drift.

PLC/S7/OPC UA/Modbus, Web Runtime, mobile runtime, WinCC deployment, alarms,
trends, recipes, batches, reports and CAD/PDF recognition are explicitly out of
this gate.

## Evidence

Run from the `phase3-m1-editor` worktree:

```text
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
```

The editor test project separately covers permissions, toolbox rendering,
property/binding/dynamics/event editing, persistence, zoom/pan, selection
handles and a non-empty 1920x1080 WPF render. Persistence tests use temporary
SQLite files only.

The offline preview is `samples/Scada.Editor.Preview.Wpf` and contains no
customer PLC addresses or legacy project/database references.

## Phase 3-M2 Engineering Productivity Gate

The M2 extension is verified by the same offline Release gate and adds:

- 100-step immutable project undo/redo history; a new edit clears redo history.
- Selection-scoped left/center/right/top/middle/bottom alignment.
- Horizontal and vertical equal-gap distribution.
- Editor-session grouping/ungrouping without changing the retained scene model.
- Explicit z-order commands that change only selected object `ZIndex` values.
- Chinese menu/toolbar commands and `Ctrl+Z`, `Ctrl+Y`, `Ctrl+G`,
  `Ctrl+Shift+G` shortcuts.

M2 verification on 2026-08-22:

```text
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
  0 warnings, 0 errors
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
  165 passed, 0 failed
git diff --check
  clean
```

The M2 gate remains offline-only. PLC/S7, Web Runtime, WinCC deployment,
alarms, trends and business modules remain outside the milestone.
