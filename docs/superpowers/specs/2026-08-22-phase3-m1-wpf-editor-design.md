# Phase 3-M1 WinCC-Style WPF Editor Design

Date: 2026-08-22  
Status: Scope confirmed by the user  
Baseline: Phase 2 Industrial Control SDK gate commit `32e0c8d`

## Goal

Deliver the first usable developer-only WPF engineering editor for the reusable
industrial SCADA platform. A developer can create or open a project, compose a
small process screen from toolbox controls, configure properties, variable
bindings, dynamics and events, save a draft, close/reopen it, and prove that
object geometry does not drift. Operators must not reach engineering functions.

## Scope

Included:

- Windows WPF editor shell with a WinCC-style engineering workspace.
- New/open project and screen tree.
- Toolbox drag-in for the Phase 2 control catalog and text/pipe objects.
- Independent selection, move, resize, rotate and keyboard nudge.
- Free independent pipe editing; moving a valve never moves an unselected pipe.
- Properties, variables, dynamics and events panels with Chinese display labels.
- Grid, rulers, zoom, pan, alignment and selection feedback needed for the first
  editing loop.
- Draft save, close/reopen, publish and restore commands using `RevisionStore`.
- Developer permission gate; runtime/operator view is not part of this milestone.
- STA WPF tests, model round-trip tests and an exact-size editor review artifact.

Excluded:

- PLC/S7/OPC UA/Modbus communication or field commands.
- Production Web Runtime, SignalR, Gateway, mobile layout and Linux host.
- WinCC deployment or Custom Web Control packaging.
- Alarm, trend, recipe, batch, report and PID business modules.
- CAD/PDF recognition, automatic topology inference, mandatory ports or
  automatic pipe routing.
- Runtime execution of later-phase actions such as opening a real trend or alarm
  window. The editor stores and validates declarative event definitions.

## Editing Semantics

The retained `ScreenDocument` is the source of truth. Selection and transforms
produce immutable scene replacements through `SceneGeometryOperations`. Every
scene object has a stable ID. Pipes own their own endpoints and bends. A move
operation affects only explicitly selected IDs; a pipe endpoint changes only
through an explicit pipe-endpoint operation. No connection graph is invented.

The editor has two modes:

- **工程编辑模式**: available only to a developer/engineer role; toolbox,
  properties, bindings, dynamics, events and save/publish/restore are enabled.
- **运行预览模式**: available for local preview only in this milestone; no
  engineering mutation commands are exposed.

Bindings and interactions use the stable IDs already defined by `Scada.Controls`.
Chinese labels are presentation only; serialized IDs remain language-neutral.
Unsupported later-phase actions are shown as unavailable validation results and
are not executed.

## Acceptance Statement

Using the WPF editor with a temporary SQLite store, a developer can:

1. Create a project and a screen.
2. Drag a pump, valve, vessel, pipe and text object from the toolbox onto the
   canvas.
3. Select and edit each object independently, including keyboard nudging and
   pipe endpoint editing.
4. Configure a property, a valid variable binding, a dynamic rule and a
   Chinese-labeled event/action pair.
5. Save the draft, close the editor, reopen the project and observe identical
   object IDs, bounds, rotations, pipe endpoints/bends, properties, bindings and
   interactions.
6. Publish the draft, list the revision, restore it to a new draft and reopen it
   without geometry drift.
7. Verify that an operator role cannot access engineering commands or panels.

The acceptance run must not access a PLC, Web Runtime, legacy database or TIA
project. Release build and tests must pass with zero warnings and zero errors.

