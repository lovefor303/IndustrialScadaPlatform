# Industrial SCADA Platform Design

Date: 2026-08-19  
Status: Design approved by user on 2026-08-19
Scope: New platform only. The legacy medical-mixing applications and PLC projects are out of scope for modification.

## 1. Product Decision

Build a reusable industrial visualization platform with a shared control contract, a Windows engineering editor, a cross-platform Runtime, and host adapters for Siemens WinCC V8.1. The platform is intended for internal reuse first and possible commercial delivery later.

This is not a replacement for every WinCC option. It is a focused platform for process visualization, device controls, variables, alarms, trends, permissions, audit, and project deployment. PLC sequencing, interlocks, safety and field execution remain PLC-owned.

## 2. Goals

- Create a new process project without changing C# source code.
- Let developers compose process screens from reusable industrial controls.
- Keep equipment, pipes, labels, groups and interactions independently editable.
- Run the same published project on Windows, Linux gateways, compatible touch panels, tablets and phones through a browser Runtime.
- Provide a WinCC V8.1 Custom Web Control export path using the same semantic control contract.
- Keep the platform usable offline on a single industrial PC or Linux panel.
- Preserve a stable project format with migrations, drafts, published revisions and rollback.
- Allow later commercial packaging without redesigning the core.

## 3. Non-goals for the Initial Platform

- Reimplementing WinCC redundancy, distributed servers, WebNavigator, every historical archive option or hundreds of device drivers.
- Replacing PLC sequence logic, safety interlocks or emergency-stop behavior.
- Direct PLC connections from phones or touch clients.
- ActiveX as the primary control delivery mechanism.
- Native iOS/Android applications in the first release.
- Public internet exposure in the first release.
- Automatic CAD recognition, automatic port inference or automatic pipe rerouting.

## 4. Platform Shape

```text
                         +--------------------------+
                         | Windows Engineering App  |
                         | .NET 10 + WPF            |
                         +------------+-------------+
                                      |
                         project package / JSON model
                                      |
              +-----------------------+-----------------------+
              |                                               |
   +----------v-----------+                         +---------v----------+
   | Cross-platform Core   |                         | WinCC V8.1 Adapter |
   | .NET 10 libraries     |                         | Custom Web Control |
   +----------+-----------+                         +--------------------+
              |
   +----------v-----------------------------------------------+
   | Runtime and Gateway: ASP.NET Core + SignalR + SQLite     |
   | Siemens S7 first; OPC UA and Modbus later               |
   +----------+-----------------------------------------------+
              |
       PLC / field network
              |
   Windows PC | Linux gateway | Linux/Android touch panel
                         browser Runtime clients
```

The Runtime service can be installed on an existing Windows industrial PC, on a Linux industrial computer, or directly on a Linux panel that allows third-party services. A separate Windows Server is not required.

## 5. Project Boundaries

The new repository is:

`D:\wpf_XM\IndustrialScadaPlatform`

The existing editor baseline remains separate:

`D:\wpf_XM\配液系统湖南\配液系统2\.worktrees\medical-mixing-system-next`

The new platform must not read, migrate, overwrite or silently share the legacy application database, recipe database, PLC addresses or runtime data. Migration is opt-in and will use explicit import packages after the core format is stable.

## 6. Solution Layers

### 6.1 Scada.Core

Platform-independent .NET 10 contracts for project identity, schema versions, revisions, permissions, variables, quality, commands, alarms, trends and audit records.

### 6.2 Scada.Scene

The retained scene model for screens and objects. Every object has a stable ID, type, bounds, rotation, z-order, visibility, properties, bindings, dynamics and interactions. Pipes are independent objects with their own endpoints and bends. Moving a valve never moves a pipe unless both are explicitly selected and grouped.

### 6.3 Scada.Controls

Semantic equipment definitions and state matrices for pumps, valves, motors, vessels, agitators, filters, instruments, level bars, temperature/pressure indicators, numeric displays, buttons and pipes. Geometry, state brushes, animations, bindings and event capabilities are versioned separately.

### 6.4 Scada.Editor.Wpf

Windows-only engineering editor inspired by WinCC V8.1 Graphics Designer: project tree, screen tabs, toolbox, docking panels, property/variable/dynamics/events inspectors, rulers, grid, zoom, pan, alignment, distribution, layer order, grouping, keyboard nudging, undo/redo, draft save, publish and restore.

### 6.5 Scada.Runtime.Web

HTML5/SVG Runtime client for process screens, values, equipment states, alarms, trends, dialogs and touch interaction. Desktop, tablet and phone layouts are separate compositions over the same semantic project, not a blind scale of one 1920x1080 canvas.

### 6.6 Scada.Gateway

ASP.NET Core service for PLC communication, data quality, caching, command validation, permission checks, rate limits, audit, alarm evaluation and SignalR updates. The gateway is the only component allowed to issue field commands.

### 6.7 Scada.Adapters

Pluggable communication and host adapters. Siemens S7 is the first production adapter. OPC UA and Modbus TCP are later adapters. WinCC V8.1 Custom Web Control is the first external-host adapter. Legacy .NET/WPF control packaging is optional and must be proven with a compatibility sample before investment.

## 7. Shared Control Contract

Each control must define:

- Stable control type and semantic version.
- Visible properties and defaults.
- Input/output ports only as internal rendering metadata; ports are not a forced user connection model.
- Command variables and feedback variables separately.
- State precedence: communication invalid, fault, transition, confirmed running/open, stopped/closed.
- Animation names and required feedback signals.
- Allowed events and actions.
- Size, rotation, hit-target and accessibility rules.
- WPF rendering profile, Web/SVG rendering profile and WinCC Web profile.
- Migration behavior for renamed or removed properties.

Initial state policy: neutral gray for stopped/closed, green for confirmed running/open, amber for transition/attention, red or flashing red for confirmed fault, and an explicit gray/striped unknown state for bad communication quality. Command state is never shown as confirmed feedback.

## 8. Variable and Command Model

Variables have a stable key, data type, engineering unit, source, quality, timestamp and optional range. Bindings can drive text, color, visibility, rotation, fill level, animation and alarm state.

Commands are declarative and server-validated. A command must pass role policy, object capability, data type/range validation, whitelist checks, optional confirmation and audit logging before the gateway sends it. The mobile and browser clients never connect to a PLC directly.

## 9. Deployment Profiles

### Profile A: Windows all-in-one

WPF editor/maintenance tools and Runtime/Gateway run on one Windows industrial PC. Suitable for the current projects and offline delivery.

### Profile B: Linux all-in-one panel

Runtime/Gateway, SQLite and a kiosk browser run on one Linux panel that permits third-party services.

### Profile C: Edge gateway plus panel

A small Linux or Windows gateway connects to the PLC; an otherwise closed panel accesses the Runtime through its browser. Use this when the panel cannot install our service.

### Profile D: WinCC host

Package the selected controls as WinCC V8.1 Custom Web Controls. The package includes manifest, property/event contract, version, deployment instructions and a compatibility test screen.

The product must explicitly report unsupported panels that lack a modern browser, WebSocket support or permission to install a gateway. It must not promise compatibility with closed vendor systems without an adapter proof.

## 10. Delivery Phases and Gates

### Phase 0: Baseline and isolation

Freeze the existing WPF editor, record its final tests and artifacts, back up process-screen data, and document the exact recovery path. No new-platform code is added to the old repository.

Gate: old project opens from its recorded baseline and the new repository contains only platform documentation.

### Phase 1: Core project model

Create the SDK-style solution, JSON Schema, schema migration rules, draft/published revision store, offline simulator and contract tests.

Gate: create, reopen, export, import, publish, restore and validate an empty project without PLC access.

### Phase 2: Control SDK

Implement pump, valve, vessel, agitator, filter, pipe and three measurement controls. Produce WPF and SVG/Web prototypes, state matrices and exact-size review images.

Gate: the same sample project shows equivalent states and animations in WPF and Web Runtime.

### Phase 3: WPF editor

Implement WinCC-style docking workspace and independent object editing. Add keyboard nudging, alignment, grouping, property/variable/dynamics/events panels, undo/redo and publish workflow.

Gate: a developer can author a small process screen without changing source code and reopen it without geometry drift.

### Phase 4: Web Runtime and gateway shell

Add ASP.NET Core Runtime, SignalR, local authentication, quality states, browser layouts, touch hit targets and local deployment profiles.

Gate: Windows and Linux test hosts can serve the same published project offline.

### Phase 5: Siemens S7 adapter

Implement read-only S7 acquisition first, then commands behind the full validation/audit path. Validate against PLCSIM before any field use.

Gate: read-only data quality, reconnect and address catalog tests pass; command tests require explicit project authorization.

### Phase 6: WinCC adapter

Package and test Custom Web Controls in WinCC V8.1. Only after this proof, decide whether a .NET Framework/WPF package has enough customer value.

Gate: install, configure, run and remove a control on a clean WinCC test machine with documented dependencies.

### Phase 7: Optional business modules

Add alarms, trends, audit, recipes, batches, reports and PID panels as separate modules. The generic scene core remains usable without them.

Gate: each module has its own schema, tests, permissions and deployment notes.

## 11. Long-Term Anti-Drift Rules

- Every phase has a written scope, non-goals, demo and acceptance matrix.
- No feature enters implementation without a design decision and stable owner layer.
- No new PLC protocol without a real project requirement and a test device/simulator.
- No control may embed recipe or project-specific PLC addresses.
- Every schema change requires a migration test and an export/import round trip.
- Every release has a reproducible build, artifact directory, checksum and handoff note.
- Manual acceptance is recorded by project ID and published revision.
- The roadmap is reviewed at every phase gate; deferred items are not silently reintroduced.
- Technical manuals and reference materials are archived under the permanent Obsidian knowledge base according to the workspace `AGENTS.md` rule.

## 12. Definition of Platform MVP

The MVP is complete only when a new Siemens S7 process project can be created, edited, published and run without changing C# code; the same project can be displayed in WPF and browser Runtime; the first controls expose states, animations, bindings and events; offline Linux/Windows deployment works; and the WinCC Web Control sample is installable with documented limits.

The MVP does not claim to replace all WinCC functions.

## 13. Open Risks

- Closed touch panels may not allow our Runtime or a modern browser.
- WinCC V8.1 legacy .NET/WPF containers may not load modern .NET 10 assemblies.
- PLC driver behavior and address semantics require device-specific validation.
- Mobile control increases safety, permissions and audit requirements.
- A complete commercial product will require support, licensing, documentation and upgrade policy beyond the internal MVP.
