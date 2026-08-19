# Industrial SCADA Platform Master Roadmap

Updated: 2026-08-19  
Status: Long-term direction approved; only Phase 1 is authorized for implementation.

## Product Outcome

Build a reusable industrial SCADA and process-visualization platform. Developers assemble projects, screens, industrial controls, variables, dynamics and events without changing C# source code. Operators use only published screens according to permissions.

This is not a one-off three-tank process picture and it is not a silent rewrite of the legacy medical-mixing application. PLC sequencing, interlocks, emergency-stop behavior and safety remain in the PLC. A future Gateway is the only component allowed to issue validated field commands.

## Fixed Architecture

1. Shared .NET 10 project, variable, scene and control contracts.
2. Windows WPF engineering editor with a WinCC-style workspace.
3. Cross-platform ASP.NET Core Gateway and Web Runtime for Windows/Linux hosts and browser clients.
4. Siemens S7 adapter developed read-only first, with commands added only behind validation, permissions and audit.
5. WinCC V8.1 Custom Web Control adapter for selected reusable controls.
6. Optional alarms, trends, audit, recipes, batches, reports and PID panels as separate modules.

Controls, valves, pipes and labels remain independently editable. The editor does not force port connections and does not automatically reroute pipes. Command variables and feedback variables are always separate.

## Delivery Sequence

### Phase 0 - Baseline and isolation

Purpose: preserve recovery paths and prevent the old and new products from contaminating each other.

Gate: the legacy baseline and data locations are documented; the new repository is independent.

### Phase 1 - Core project model (current)

Deliverables: .NET 10 solution, project/variable/scene contracts, JSON Schema and migrations, SQLite draft/publish/version/restore storage, import/export, deterministic offline simulator and acceptance tests.

Gate: create, validate, save, reopen, export, import, publish, list revisions, restore and simulate an empty project without PLC access; Release build has zero warnings and errors and all tests pass.

Excluded: WPF, Web, PLC, WinCC, alarms, trends, recipes, batches and legacy migration.

Detailed plan: `docs/superpowers/plans/2026-08-19-phase1-core-project-model.md`.

### Phase 2 - Industrial control SDK

Deliverables: semantic and versioned definitions for pumps, valves, vessels, agitators, filters, pipes, numeric displays, level bars and temperature/pressure instruments; state precedence; animations; WPF and SVG/Web render profiles.

Gate: one sample project renders equivalent equipment states in WPF and Web, with exact-size visual review evidence and command/feedback separation tests.

Before implementation: write and approve a separate detailed Phase 2 plan.

### Phase 3 - WPF engineering editor

Deliverables: project tree, toolbox, multi-screen workspace, dockable properties/variables/dynamics/events panels, canvas, rulers, grid, zoom, pan, selection, keyboard nudging, alignment, distribution, grouping, layers, resizing, rotation, undo/redo, draft, publish and restore.

Gate: a developer authors and reopens a small process screen without source-code edits or geometry drift. Operators cannot access engineering functions.

Before implementation: write and approve a separate detailed Phase 3 plan.

### Phase 4 - Web Runtime and Gateway shell

Deliverables: ASP.NET Core service, SignalR updates, published-project loading, local authentication, permissions, variable quality, responsive desktop/tablet/phone compositions and Windows/Linux deployment profiles.

Gate: Windows and Linux test hosts serve the same published project offline; browsers never connect directly to a PLC.

Before implementation: write and approve a separate detailed Phase 4 plan.

### Phase 5 - Siemens S7 adapter

Deliverables: address catalog, polling, type conversion, quality, reconnect and diagnostics. Read-only acquisition comes first. Commands require a separately approved contract covering whitelist, range, role, confirmation and audit.

Gate: PLCSIM proves read quality and reconnect behavior; no field command is enabled merely because reads work.

Before implementation: write and approve a separate detailed Phase 5 plan using an explicitly named offline test project.

### Phase 6 - WinCC V8.1 adapter

Deliverables: selected industrial controls packaged as Custom Web Controls with manifest, properties, events, deployment instructions and a compatibility test screen.

Gate: install, configure, run and remove the package on a clean WinCC V8.1 test machine.

Before implementation: archive supporting manuals under `E:\BaiduSyncdisk\旧电脑\知识库` and write an approved compatibility plan.

### Phase 7 - Optional business modules

Deliverables are separately selected modules: alarms, trends, audit, recipes, batches, reports and PID panels. Each module owns its schema, permissions, migrations and tests.

Gate: each selected module passes its own acceptance plan without making the generic scene core dependent on that module.

## Phase Rules

- Work on one numbered phase and one numbered task at a time.
- A later phase may be discussed but not implemented before the current gate passes.
- Every phase gets its own approved detailed implementation plan; this roadmap does not authorize later-phase code.
- Every schema change requires migration and export/import round-trip tests.
- Every release requires a reproducible build, artifacts, checksums and a handoff note.
- CAD, PDF and WinCC pictures are references for developers, not sources for automatic topology or pipe inference.
- Project-specific PLC addresses never belong inside reusable control definitions.

## Current Position

Current phase: Phase 1.  
Current task: Task 1 - create and verify the seven-project traditional `.sln` solution.  
Single operational checkpoint: `docs/handoff-current.md`.

No Phase 1 business code has been implemented yet, and no later phase has started.
