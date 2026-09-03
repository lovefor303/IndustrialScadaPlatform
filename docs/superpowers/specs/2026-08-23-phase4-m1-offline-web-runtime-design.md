# Phase 4-M1 Offline Web Runtime Design

Date: 2026-08-23  
Status: Proposed for user review  
Repository: `D:\wpf_XM\IndustrialScadaPlatform`

## 1. Goal

Add the first browser runtime boundary for the new industrial SCADA platform. A
published project created by the WPF editor must be viewable offline in a modern
browser on Windows or Linux, using the same project and semantic control contracts
as the WPF renderer. This milestone is a runtime proof, not a PLC integration.

## 2. Scope

Included:

- A new ASP.NET Core runtime host that serves a local browser application.
- Loading one immutable published project from a JSON package or an explicitly
  selected `RevisionStore` revision.
- Rendering screens, controls, pipes and text through the existing SVG control
  renderer and shared control geometry.
- A deterministic simulated variable source for feedback values and quality states.
- Control state resolution for stopped, active, transition, fault and unknown states.
- Responsive desktop, tablet and phone compositions over the same scene model.
- Read-only runtime behavior: command controls may display their disabled state but
  cannot write a PLC or issue a field command.
- Local diagnostics for missing projects, invalid schemas and unsupported control
  types.
- Automated HTTP, project-loading, rendering and responsive-layout acceptance tests.

Excluded:

- Siemens S7, OPC UA, Modbus, PLCSIM or any PLC/network driver.
- SignalR live transport; the runtime may expose an internal provider interface,
  but M1 uses deterministic in-memory simulation only.
- User authentication, roles, audit, alarms, trends, recipes, batches and PID.
- Browser-to-PLC communication or browser command execution.
- WinCC V8.1 deployment and Custom Web Control packaging.
- CAD/PDF recognition and legacy project/database migration.

## 3. Architecture

```text
Published JSON / RevisionStore revision
                |
        RuntimeProjectSource
                |
        RuntimeSceneProjection
          /                 \
 SimulatedVariableSource   LayoutProfile
          |                 |
       StateResolver   Desktop/Tablet/Phone composition
                \         /
             SVG Runtime Document
                    |
             ASP.NET Core host
                    |
                Browser
```

The runtime does not mutate `ProjectDocument`. It reads a published snapshot and
projects it into a render model containing object ID, model bounds, rotation,
visibility, SVG markup, state metadata and read-only interaction metadata. The
existing `Scada.Controls.Svg` renderer remains the only component that emits control
SVG primitives; the runtime host is responsible for composition, not for redrawing
equipment geometry.

### 3.1 Project source

Define an `IRuntimeProjectSource` boundary with two implementations:

- `JsonRuntimeProjectSource`: reads one `.json` project package, deserializes it with
  `JsonProjectSerializer`, validates the schema, and requires a published status.
- `RevisionRuntimeProjectSource`: reads a selected immutable revision from an
  initialized `RevisionStore`, validates it, and returns the same project contract.

The first preview executable may use a JSON path argument for deterministic manual
testing. The host must fail closed when the file is missing, malformed, draft-only,
or contains no screen.

### 3.2 Variable source and state

Define an `IRuntimeVariableSource` that returns a timestamped value and
`VariableQuality` for a stable variable key. M1 provides an in-memory simulator with
explicit values for the sample project and a timer-driven demo profile. It must not
invent values for unknown keys: unknown variables produce bad quality and controls
render the existing unknown state marker.

Bindings and dynamics remain declarative records from `Scada.Scene`. Runtime M1
evaluates only the existing state-driving roles needed by the control catalog and
numeric/level display values. Unsupported dynamic targets are reported in diagnostics
and do not change the rendered project.

### 3.3 Browser composition

The browser receives a full-screen runtime document with a model-coordinate canvas.
CSS transforms scale the document into a viewport; the project model is never
rewritten for a device size. Layout profiles define:

- desktop: full engineering canvas with navigation and status strip;
- tablet: canvas with compact status strip and touch-sized read-only controls;
- phone: fit-to-screen composition with a screen selector and compact status strip.

The runtime must preserve object relative positions and z-order in every profile.
Controls are independently rendered; pipes are never automatically rerouted or
attached to moved controls.

### 3.4 Read-only interaction policy

The browser runtime exposes no command endpoint in M1. A command-button control is
rendered with a disabled/read-only affordance and an explanatory local status message
when activated. No HTTP endpoint accepts a field command, and no runtime code imports
PLC libraries. This makes the M1 proof safe to run on an isolated machine.

## 4. HTTP surface

The initial host exposes only local read operations:

- `GET /` - browser runtime shell.
- `GET /api/runtime/project` - published project metadata and screen catalog.
- `GET /api/runtime/screens/{screenName}` - composed read-only screen document.
- `GET /api/runtime/health` - source and simulator status.

Responses use `application/json` for metadata and `image/svg+xml` or an HTML/SVG
document for rendered screens. The host binds to localhost by default and must print
the actual URL on startup. Binding to a non-loopback interface is an explicit command
line option and is outside the default acceptance path.

## 5. Error handling

- Invalid project packages return HTTP 400 with a stable error code and a Chinese
  operator-readable message; stack traces are never returned.
- Missing screens return HTTP 404.
- Unsupported control types render a neutral placeholder with object ID and a
  diagnostic entry instead of taking down the whole screen.
- Invalid or bad-quality variables render the existing unknown-quality visual state.
- Runtime startup fails with a concise console error and non-zero exit code when no
  valid published project source is configured.
- Browser-side errors are shown in a compact status strip; no exception is allowed
  to blank the entire application.

## 6. Testing and acceptance

The M1 gate requires:

1. A valid published JSON project loads and is rejected when its status is draft.
2. Project metadata and screen catalog endpoints return stable IDs and names.
3. A screen response contains every retained object exactly once, preserves model
   bounds/rotation/z-order, and contains SVG `data-control-type`, `data-state` and
   `data-quality` metadata for supported controls.
4. Pipes remain independent objects and retain their endpoints and bends.
5. Simulated good, bad and unknown variable quality produces the expected control
   states and does not mutate the project JSON.
6. Desktop, tablet and phone layout profiles preserve object-relative placement and
   return the expected viewport metadata.
7. Invalid project, missing screen and unsupported control requests return controlled
   diagnostics without process termination.
8. The host starts on Windows and Linux test hosts using the same published JSON
   fixture, with no PLC or customer project dependencies.

Required verification commands will include a Release build, full solution tests,
runtime-focused tests, an HTTP smoke test and `git diff --check`.

## 7. Data and repository boundaries

Runtime fixtures and temporary databases live under the test output or a temporary
directory. The implementation must not read or modify:

- `D:\wpf_XM\配液系统湖南\配液系统2`;
- any legacy worktree or customer database;
- Siemens TIA/PLC projects;
- the permanent technical knowledge base, except for downloaded technical manuals
  that must follow the workspace archive rule.

## 8. Gate decision

Phase 4-M1 passes only when the same published project is served and rendered in a
browser on Windows and a Linux-compatible test host, with deterministic simulation,
responsive profiles and no command/PLC path. Passing M1 authorizes a separate design
for live SignalR transport and a separate Phase 5 design for Siemens S7 acquisition;
it does not authorize either implementation automatically.
