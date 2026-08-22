# Phase 3-M3 Acceptance

Date: 2026-08-22
Repository: `D:\wpf_XM\IndustrialScadaPlatform`
Branch: `phase3-m1-editor`

## Delivered

- Toolbox leaf items retain stable control type IDs and start canvas drag-in.
- The property inspector edits selected-object position, size, rotation and visibility through `EditorSession`.
- Variable binding, dynamic-property and event/action panels use catalog-backed Chinese choices and strict validation.
- The preview shell injects `EditorCommands` backed by an isolated SQLite `RevisionStore`.
- “保存草稿” writes the internal SQLite draft; “载入最新草稿” restores it without a file path.
- JSON project files use the separate “打开项目”/“导出项目” flow, and invalid files are reported in the status bar instead of escaping the UI command.
- Preview drafts are stored under the current user's local application data and are loaded on preview startup when present.
- Save/reopen regression coverage preserves object IDs, control bounds, rotation and independent pipe start/end/bend geometry.
- Publish, modify, save and restore regression coverage returns the scene to the published geometry.

## Verification Evidence

| Command | Result |
|---|---|
| `dotnet test tests/Scada.Editor.Wpf.Tests/Scada.Editor.Wpf.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~EditorPersistenceTests` | Passed: 2 tests |
| `dotnet test tests/Scada.Editor.Wpf.Tests/Scada.Editor.Wpf.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~WindowCanUseInjectedPersistenceCommands` | Passed: 1 test |
| `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build` | Passed: 178 tests |
| `git diff --check` | Passed |

## Scope Confirmation

This milestone remains offline-only. PLC/S7/OPC UA communication, Gateway, Web Runtime,
WinCC deployment, alarms, trends, recipes, batches, CAD/PDF recognition and legacy
project migration remain excluded. The old配液 project, TIA/PLC projects and customer
databases are not used by the preview or its tests.

## Decision

M3 functional editor shell passed the Release build/test gate and is ready for
manual preview check. The next planned work is the separately scoped M4 runtime
boundary, not PLC or legacy-project integration.
