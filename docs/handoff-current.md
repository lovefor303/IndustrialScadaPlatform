# Current Handoff

Updated: 2026-08-22

## Authoritative Scope

Repository: `D:\wpf_XM\IndustrialScadaPlatform`

Current phase: **Phase 3-M2 engineering productivity gate passed; visual acceptance next**

Approved design: `docs/superpowers/specs/2026-08-19-industrial-scada-platform-design.md`

Approved implementation plan: `docs/superpowers/plans/2026-08-19-phase1-core-project-model.md`

Phase 2 approved design: `docs/superpowers/specs/2026-08-20-phase2-industrial-control-sdk-design.md`

Phase 2 draft implementation plan: `docs/superpowers/plans/2026-08-20-phase2-industrial-control-sdk.md`

## Completed In This Repository

- Product design and Phase 1 implementation plan were approved and committed before implementation.
- The initial `src` and `tests` directory skeletons were created.
- `Directory.Build.props` and `Directory.Packages.props` were created with the planned .NET 10 and package constraints.
- Task 1 solution skeleton was verified with exactly seven projects before the separate acceptance-test project was added.
- `dotnet restore IndustrialScadaPlatform.sln` passed after pinning the vulnerable SQLitePCLRaw transitive packages to `2.1.13`.
- `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` passed with 0 warnings and 0 errors.
- Task 2 core contracts are implemented and verified by 10 passing tests.
- The full Release solution build passes with 0 warnings and 0 errors.
- Task 3 retained scene model and project-v1 Schema are implemented and verified by 15 passing tests.
- Valid and invalid Schema fixtures are checked automatically; independent pipe geometry is covered.
- Task 4 deterministic JSON serializer and version-0 migration are implemented and verified by 5 passing tests.
- Full Release build remains at 0 warnings and 0 errors.
- Task 5 SQLite draft, publish, revision, restore and import/export storage is implemented.
- Storage is verified by 10 passing tests using isolated temporary database files.
- Task 6 deterministic offline simulator is implemented and verified by 7 passing tests.
- Commands remain unconfirmed/Bad in simulation; feedback type and range checks are enforced.
- Task 7 acceptance harness is implemented; the complete Phase 1 gate passed with 33 tests, 0 build warnings and 0 build errors.

## Phase 2 Completion

- The Phase 2 Industrial Control SDK gate passed on 2026-08-20.
- See `docs/phase2-acceptance.md`, `docs/phase2/source-audit.md`, `docs/phase2/visual-review.md` and `docs/handoff-2026-08-20-phase2.md`.
- Final verification: Release build 0 warnings/0 errors; 128 tests passed across the solution; 17 Phase 2 acceptance tests passed; generated WPF/SVG artifacts are non-empty and dimension-checked.

## Current Incomplete State

- Phase 2 SDK implementation is complete and gated.
- Phase 3 Task 1 editor session contracts are complete (`a4a859b`).
- Phase 3 Task 2 WPF shell and authorization gate are complete (`7162977`).
- Phase 3 Task 3 toolbox drag-in and independent scene editing are complete (`a8e4dbe`).
- Phase 3 Task 4 properties, variable bindings, dynamics and events are complete (`e4f19bb`).
- Phase 3 Task 5 persistence core, dirty-discard confirmation and WPF command binding are implemented (`2d01f16`, `96b3a56`, `92c44f3`): `EditorCommands` covers new/open/export/save/publish/list/restore against an injected `RevisionStore`; menu/toolbar commands and status messages are bound; dirty sessions require an injected confirmation before new/open; temporary SQLite round-trip test passes.
- Phase 3 Task 6 viewport and selection ergonomics are implemented (`2fedf35`, `b0cb221`): cursor-anchored zoom, pan, optional grid snapping/grid rendering, Ctrl+wheel zoom, middle-button pan, eight resize handles, a rotation handle, byte-for-byte scene JSON drift gate, and a non-empty 1920x1080 render assertion.
- The follow-up handle correction is implemented in the current worktree: resize handles use directional double-arrow cursors, the object selection surface keeps the four-way move cursor, the rotation handle uses a generated arc cursor, and resize/rotation drag deltas update the retained visual in place until `DragCompleted` so both positive and negative directions remain usable.
- Rotation handle styling and sensitivity were corrected: the rotation handle now uses the same white fill and DeepSkyBlue border as resize handles, the custom cursor is deep blue, and horizontal drag input maps to 0.01 degrees per pixel for very fine adjustments.
- Phase 3-M2 Task 1 is implemented in the working tree: immutable 100-step project history, undo/redo session APIs, redo invalidation after new edits, and independent-pipe history regression tests.
- Phase 3-M2 Task 2 is implemented in the working tree: selection-scoped left/center/right/top/middle/bottom alignment, horizontal/vertical equal-gap distribution, non-pipe validation and one-step session undo integration.
- Phase 3-M2 Task 3 is implemented in the working tree: editor-session-only groups, group selection/ungrouping, and explicit layer-order operations that modify only selected `ZIndex` values.
- Phase 3-M2 Task 4 is implemented in the working tree: Chinese productivity commands, WPF menu/toolbar bindings, engineering-role enablement and Ctrl+Z/Ctrl+Y/Ctrl+G/Ctrl+Shift+G shortcuts.
- Phase 3-M2 selection ergonomics are implemented and verified: normal left-click single-selects, Ctrl+left-click toggles selection, and dragging on blank canvas performs intersecting-object frame selection. Frame cleanup removes the owned rectangle by reference, preserving existing scene visuals and handles.
- Selection-dependent WPF commands now refresh when the session selection changes, so alignment, distribution, grouping and layer commands no longer remain disabled after Ctrl-click or frame selection. The toolbar exposes all alignment and distribution directions.
- The canvas now subscribes to session project-change notifications, so alignment, distribution, undo/redo, grouping and layer commands redraw immediately without requiring a blank-canvas click. Live resize/rotation drags suppress full redraw during each delta and keep their active handle stable, then refresh on drag completion.
- Object dragging is implemented: normal left-button drag moves the current selection in model coordinates, clicking an unselected object first selects it, and Ctrl-click remains selection-only. Pipes remain independent objects and move only when selected.
- Phase 3 Task 7 acceptance assets are in the working tree: offline preview host, scope/acceptance notes and visual review notes. The sample contains no PLC addresses or legacy project references.
- The editor remains offline-only; no PLC, Web Runtime, WinCC adapter or legacy migration has started.

## Next Action

Phase 3-M2 Tasks 1 through 4 and the acceptance gate are complete. Perform the
user-facing visual acceptance of the latest offline preview. After acceptance,
write and approve the next Phase 3 milestone plan; do not start PLC/Web/WinCC
deployment work from this gate.

Phase 1, Phase 2, Phase 3-M1 and Phase 3-M2 gates are complete. Phase 3-M3 has
not been planned or started.

## Interruption Checkpoint

- Active workstream: new industrial SCADA platform.
- Active unit: Phase 3-M2 visual acceptance; M2 implementation and gate are complete.
- Last verified commands: `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` (0 warnings, 0 errors); `dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build` (169 tests passed); editor focused tests (34 passed); `git diff --check` clean. The offline preview is running from `samples/Scada.Editor.Preview.Wpf/bin/Release/net10.0-windows/Scada.Editor.Preview.Wpf.exe`.
- Legacy boundary note: the legacy repository had pre-existing dirty files when inspected; no command in this task targeted or modified that repository.
- Intentionally untracked local visual-companion files: `.superpowers/`; these are not product source and must not be committed without an explicit decision.
- Resume action after any side task: re-read `AGENTS.md`, `docs/PROJECT_CONTROL.md`, this handoff and the Phase 3 plan; continue only from user-facing Phase 3-M2 visual acceptance.
- Current exclusions: production Web Runtime, Gateway, PLC communication, WinCC adapter, business modules, CAD/PDF recognition and legacy migration.

## Verification Status

- Phase 1, Phase 2, Phase 3-M1 and Phase 3-M2 final gates passed. PLC/Web/WinCC/runtime features remain explicitly out of scope.
- The old配液项目 and PLC projects have not been modified by this repository task.

## Return Rule After Side Tasks

Before resuming platform work after any unrelated task, read `AGENTS.md`, `docs/PROJECT_CONTROL.md` and this file. Continue only from `Next Action`; do not jump to a later phase or start a convenient UI feature.
