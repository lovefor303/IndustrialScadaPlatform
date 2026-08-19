# Industrial SCADA Platform Project Control

## Purpose

This file prevents the platform from drifting when work is interrupted by PLC, legacy WPF, WinCC, documentation or other side tasks.

The product direction is a reusable industrial SCADA/visualization platform, not a one-off three-tank drawing and not a rewrite of the legacy medical-mixing application.

## Product Target

Developers create projects and process screens without changing C# source code. They compose reusable industrial controls, variables, dynamics and events, publish revisions and deploy the same project to the planned WPF editor, Web Runtime and WinCC V8.1 Custom Web Control adapter. Operators receive only the published runtime view according to permissions.

PLC sequencing, interlocks, emergency stops and field safety remain PLC-owned. A future Gateway is the only component allowed to issue validated field commands. Browser and mobile clients never connect directly to a PLC.

## Repository Boundaries

Authoritative new-platform repository:

`D:\wpf_XM\IndustrialScadaPlatform`

Separate legacy/reference projects:

- `D:\wpf_XM\配液系统湖南\配液系统2`
- `D:\wpf_XM\配液系统湖南\配液系统2\.worktrees\medical-mixing-system-next`
- Siemens TIA/PLC projects and legacy databases

The new platform must not silently read, migrate, overwrite or share any of those resources.

## Phase Roadmap

### Phase 0: Baseline and isolation

Freeze the old editor baseline, record artifacts and data locations, and keep the new repository separate.

### Phase 1: Core project model (current)

Build the .NET 10 SDK solution, project/variable/scene contracts, JSON Schema and migrations, SQLite draft/publish/restore/import/export storage, deterministic offline simulator and automated acceptance gate.

Phase 1 deliberately excludes WPF canvas, Web Runtime, PLC/S7/PLCSIM communication, WinCC deployment, recipes, batches, alarms, trends and legacy migration.

### Phase 2: Industrial control SDK

Add semantic controls and state matrices for pumps, valves, vessels, agitators, filters, pipes and measurement displays. Produce WPF and SVG/Web render profiles with exact-size review evidence.

### Phase 3: WPF engineering editor

Add the WinCC-like Windows editor: project tree, toolbox, canvas, properties, variables, dynamics, events, grid, zoom, pan, keyboard nudging, alignment, grouping, layers, undo/redo, draft, publish and restore.

### Phase 4: Web Runtime and Gateway shell

Add ASP.NET Core, SignalR, browser layouts, local authentication, quality states and Windows/Linux deployment profiles.

### Phase 5: Siemens S7 adapter

Implement read-only acquisition and quality/reconnect behavior first. Only after PLCSIM evidence and a separate command contract may validated commands be added.

### Phase 6: WinCC V8.1 adapter

Package and test selected controls as Custom Web Controls on a clean WinCC V8.1 test machine.

### Phase 7: Optional business modules

Add alarms, trends, audit, recipes, batches, reports and PID panels as separately scoped modules with their own schemas, permissions and tests.

## Task Switching Protocol

When the user asks for unrelated work:

1. Before leaving the platform task, update `docs/handoff-current.md` with the current phase, task, last verified command, dirty files and one exact next action.
2. Do the side task within its own repository and safety boundary. PLC/TIA, the legacy WPF application, the knowledge base and this platform are separate workstreams.
3. Do not copy side-task code, addresses, database records or assumptions into this repository unless an approved platform design explicitly requires an import contract.
4. Do not mark any platform phase complete because of side-task progress.
5. Before returning, read `AGENTS.md`, this file, `docs/handoff-current.md`, the approved design and the active phase plan.
6. Compare the working tree with the recorded dirty-file list. Investigate any difference before editing.
7. Continue only from the recorded `Next action`; rerun the recorded verification command when its result may be stale.
8. If the side task changes a shared assumption, update the design/plan and obtain approval before implementation.

## Workstream Isolation Register

| Workstream | Authoritative location | May change platform phase? |
| --- | --- | --- |
| New industrial SCADA platform | `D:\wpf_XM\IndustrialScadaPlatform` | Yes, only through an approved phase plan and gate |
| Legacy medical-mixing WPF software | `D:\wpf_XM\配液系统湖南\配液系统2` and its worktrees | No |
| PLC/TIA engineering | User-designated offline TIA project or export | No |
| WinCC/manual/reference research | `E:\BaiduSyncdisk\旧电脑\知识库` | No; findings require a design decision before implementation |

Only one workstream is the active implementation target at a time. A request to handle another workstream pauses the platform; it does not replace its roadmap or advance its phase.

## Resume Checklist

The platform may resume after an interruption only when all answers below are known from repository files, not from chat memory:

- Which phase and numbered task are active?
- What was the last command that passed?
- Which files are intentionally dirty?
- What is the single next action?
- Which features are explicitly excluded from the current phase?

If any answer is missing or contradictory, stop implementation and repair `docs/handoff-current.md` first.

## Phase Gate

Phase 1 is complete only when a new empty project can be created, validated, saved, reopened, exported, imported, published, listed, restored and simulated offline, with a Release build containing zero warnings and zero errors and all tests passing.

No later phase may be started before that gate is recorded in a handoff document.
