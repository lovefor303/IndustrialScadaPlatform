# Phase 2 Industrial Control SDK Design

Date: 2026-08-20

Status: Approved by the user on 2026-08-20

Repository: `D:\wpf_XM\IndustrialScadaPlatform`

## 1. Purpose

Phase 2 establishes the reusable industrial-control foundation for the SCADA platform. It converts the Phase 1 project, variable and retained-scene contracts into recognizable industrial equipment with deterministic state, animation and interaction semantics.

The visual baseline is a restrained hybrid style:

- Equipment bodies use mechanically recognizable 2.5D construction.
- State and process motion use modern digital-twin animation where it improves operator understanding.
- Operator recognition at 100 percent Runtime size takes priority over decorative depth, glow or presentation-screen effects.

The first calibration artifact is a generic industrial equipment sample screen. A three-tank medical-mixing reference screen is used only as a dense-layout validation case. Neither sample may embed customer PLC addresses, recipe logic or three-tank sequence logic in a reusable control.

## 2. Scope

Phase 2 delivers:

- Versioned semantic definitions for the first control families.
- A shared state resolver and animation contract.
- WPF vector and SVG/Web render profiles over the same semantic definitions.
- Small WPF and SVG preview hosts used only for SDK demonstration and verification.
- Offline variable snapshots and deterministic state/animation simulation.
- Automated contract, serialization, validation and cross-render tests.
- Magnified, exact-size and complete-screen visual review artifacts.
- An asset and reference-source audit with license status.

Phase 2 does not deliver:

- The WinCC-style WPF engineering workspace, toolbox, docking panels or production canvas.
- Production Web Runtime, SignalR, authentication or responsive mobile screens.
- Siemens S7, OPC UA, Modbus or WinCC deployment.
- Live PLC commands.
- Alarms, trends, recipes, batches, reports or PID business panels.
- Automatic pipe routing, CAD topology recognition or mandatory port connections.

The preview hosts are verification tools, not early versions of the Phase 3 editor or Phase 4 Runtime.

## 3. Chosen Architecture

Phase 2 uses a semantic model with two native renderers.

### 3.1 Semantic control layer

`Scada.Controls` owns platform-independent definitions for:

- Stable control type identifiers and semantic versions.
- Public properties, defaults and validation rules.
- Logical component parts and optional visual anchors.
- Feedback, process-value and command binding roles.
- State precedence and transition rules.
- Named animations and their required signals.
- Supported events and declarative actions.
- Orientation, minimum size, aspect-ratio and hit-target rules.
- Migration rules for renamed, added or removed properties.

It does not own PLC addresses, WPF dependency properties, browser DOM nodes or customer-specific logic.

### 3.2 WPF render profile

The WPF profile uses native vector geometry, control templates, visual states and platform animation primitives. It is optimized for Windows rendering quality, design-time selection, hit testing and future Phase 3 editing.

### 3.3 SVG/Web render profile

The SVG profile emits stable SVG groups, paths, masks and named animation targets suitable for a future browser Runtime and WinCC Custom Web Control adapter. It does not force WPF to render through an SVG container.

### 3.4 Cross-render contract

Both profiles must preserve:

- Control identity and semantic version.
- Mechanical silhouette and orientation.
- Named parts relevant to state and animation.
- State precedence and binding meaning.
- Command/feedback separation.
- Equivalent stopped, active, transition, fault and unknown meanings.

Pixel-identical output is not required because WPF and SVG use different rendering engines. A visual difference is acceptable only when it does not change process meaning, device recognition or state interpretation.

## 4. Visual Construction Model

Every equipment control is composed from three conceptual layers.

### 4.1 Mechanical structure layer

This layer remains readable without state color. It includes the minimum geometry that identifies the equipment class and orientation: shell, casing, motor, coupling, shaft, actuator, jacket, flanges, nozzles and visible process ports as applicable.

Geometry uses a stable local coordinate system and preserves aspect ratio by default. Stretching one axis may be enabled only for explicitly stretchable families such as straight pipe and level bars. Resizing an equipment control scales its visible body, not merely its selection border.

### 4.2 Process-state layer

This layer applies feedback-derived state to selected parts without obscuring mechanical structure. It includes equipment status, process medium, liquid level, valve position, numeric value, unit and variable quality.

Normal stopped equipment remains visually quiet. Saturated colors are reserved for confirmed process state, attention and fault.

### 4.3 Animation layer

Animations express physical motion or process change:

- Pump and motor rotation.
- Agitator shaft and impeller rotation.
- Valve stem or actuator travel.
- Vessel and level-bar fill movement.
- Pipe flow direction.
- Controlled transition and fault attention.

Continuous decorative motion, strong glow, excessive gradients and whole-device flashing are excluded. Fault animation must not prevent reading the tag, state or process value.

## 5. Initial Control Families

### 5.1 Pump assembly

The pump control identifies the pump casing, suction, discharge, motor and coupling. Orientation is explicit. Its semantic inputs cover run command, run feedback, stop feedback when available, fault feedback, transition timing and communication quality.

### 5.2 Automated valve

The valve control identifies body direction, actuator and stem/travel. It supports open command, close command, open feedback, closed feedback, optional continuous position, fault feedback and quality. Horizontal and vertical orientations use the same semantics.

### 5.3 Vessel

The vessel control includes shell, top and bottom form, configurable nozzles, optional jacket, liquid fill and bottom outlet. Level and temperature are process bindings rather than hard-coded labels.

### 5.4 Agitator

The agitator includes motor, shaft and impeller. It may be placed independently or visually composed with a vessel. It remains an independent scene object unless the developer explicitly groups it with the vessel.

### 5.5 Filter

The filter includes a recognizable housing and verified inlet/outlet orientation. It supports normal, active, differential-pressure attention, blocked/fault and unknown states.

### 5.6 Pipe and fittings

Straight pipe, elbow, tee, direction arrow and flow animation are independent scene objects. Pipe geometry owns its own endpoints and bends. Moving equipment does not move or reroute an unselected pipe.

Ports and anchors are optional alignment metadata. They may assist snapping and validation but do not create a mandatory connection graph and do not block saving solely because two objects are not logically connected.

### 5.7 Measurement controls

The first measurement controls are:

- Numeric process display with value, unit, quality and range state.
- Vertical level bar with value, limits and quality.
- Temperature, pressure and flow indicators using one shared measurement contract and family-specific units/presentation.

### 5.8 Operator controls

The first declarative operator controls cover start/stop, open/close, manual/automatic, acknowledge, reset and navigation. They expose command intent but never render a command as confirmed equipment feedback.

## 6. State Model

State resolution is deterministic and shared by both renderers. The precedence from highest to lowest is:

1. Invalid or stale communication quality.
2. Confirmed fault or contradictory feedback.
3. Transition or command awaiting feedback.
4. Confirmed running/open/active feedback.
5. Confirmed stopped/closed feedback.
6. Neutral state when no stronger state can be proved.

The default visual meanings are:

- Stopped or closed: neutral gray with readable structure.
- Confirmed running or open: restrained green on the relevant active parts.
- Transition or attention: amber with limited motion where useful.
- Fault: red local emphasis plus a stable fault marker.
- Unknown or bad quality: muted gray with a consistent striped or unknown marker; misleading process animation stops.

A command changes command-pending state only. It cannot directly establish running, open or closed state. Confirmed state requires feedback.

State definitions are data-driven and versioned. Renderer-specific brushes and animation primitives implement the state but do not redefine its meaning.

## 7. Variables, Bindings and Events

### 7.1 Semantic bindings

Controls declare binding roles such as `RunFeedback`, `FaultFeedback`, `OpenFeedback`, `ClosedFeedback`, `ProcessValue`, `LevelValue`, `TemperatureValue`, `PressureValue`, `FlowValue`, `StartCommand` and `ResetCommand`.

Scene instances bind those roles to project variable keys. PLC addresses remain in future communication configuration, not in control definitions. Binding validation checks data type, engineering unit where required, writable capability for commands, range and quality availability.

Bindings may drive text, color, visibility, rotation, position/opening, level, flow direction, animation state and alarm presentation.

### 7.2 Events

The editor-facing display names are Chinese selections rather than free-form English identifiers. Initial events are:

- Left-button press and release.
- Double click.
- Right click.
- Pointer enter and leave.
- Variable change.
- Fault activation and recovery.

Initial declarative actions are:

- Write a validated command value.
- Toggle a Boolean command.
- Open an equipment panel.
- Open a trend view.
- Acknowledge an alarm.
- Reset an allowed condition.
- Navigate to a screen.
- Show a message.

The stored contract uses stable language-neutral identifiers; localization maps them to Chinese labels. Users select valid identifiers and parameters rather than typing arbitrary action names.

Phase 2 defines and validates these action contracts but does not implement the trend, alarm, navigation-target or equipment-panel modules. When a preview project references an unavailable target capability, validation reports it explicitly and the preview performs no side effect. The owning modules are implemented only in their approved later phases.

### 7.3 Offline preview

Phase 2 preview actions update only the deterministic offline simulator. They never connect to or write a PLC. Preview values are marked as simulated and are reproducible for automated tests.

## 8. Editing Contract

Phase 2 defines and tests the geometry behavior required by the future editor:

- Equipment, pipes, labels, instruments and agitators are independently selectable scene objects.
- Move, resize, rotate and keyboard-nudge operations modify the retained scene model.
- Equipment preserves aspect ratio unless its definition explicitly permits free-axis stretch.
- Straight pipes and supported fittings allow length or geometry changes without scaling unrelated objects.
- Group movement affects only explicitly selected or grouped objects.
- Optional alignment to an anchor does not create forced connectivity or automatic rerouting.

The Phase 2 preview host may expose minimal controls needed to verify these contracts. Full editing tools, docking and production undo/redo belong to Phase 3.

## 9. Data Flow and Safety Boundary

The eventual read path is:

```text
PLC -> Gateway -> value/quality/timestamp -> semantic state resolver -> WPF or SVG renderer
```

The eventual command path is:

```text
control event -> declarative action -> Gateway authorization, whitelist,
type/range validation, optional confirmation and audit -> PLC
```

PLC sequencing, interlocks, emergency stops and safety remain PLC-owned. The control SDK does not duplicate or bypass them. Phase 2 substitutes deterministic offline snapshots for both paths and does not implement the Gateway or a PLC adapter.

## 10. Error Handling and Publication Validation

- A missing optional binding uses the documented neutral default.
- A missing required binding is shown as unbound in design preview and blocks publication.
- A binding type, unit, range or access mismatch identifies the screen, object, property and variable and blocks publication.
- Bad or stale quality selects unknown state and stops misleading animation.
- Contradictory feedback selects the defined abnormal state and records diagnostic context.
- One renderer failure is contained by a diagnostic placeholder so the remaining sample screen can render.
- A semantic mismatch between WPF and SVG state output blocks Phase 2 acceptance.
- Invalid control versions or properties fail validation without silently discarding data.

Diagnostics use stable codes plus localized messages so tests do not depend on translated text.

## 11. Visual Tokens and Calibration

Phase 2 defines shared named tokens for:

- Background, equipment body, outline and surface-depth colors.
- Stopped, active, transition, fault and unknown states.
- Pipe widths and medium colors.
- Primary and secondary text sizes.
- Outline, separator and focus thickness.
- Shallow depth/shadow treatment.
- Animation duration and reduced-motion behavior.
- Minimum pointer/touch hit targets.

Token values are centralized and mapped to WPF and CSS/SVG resources. Normal equipment remains neutral and process state provides the strongest color. Letter spacing is zero. Text does not scale directly with viewport width.

The Phase 2 desktop calibration canvas is 1920 by 1080 logical pixels at 100 percent scale. This is a review baseline, not a promise that later Runtime pages will be uniformly scaled to every panel. Later panel profiles receive their own composition and physical-size review.

## 12. Reference and Asset Policy

Reference sources include complete SCADA screens, industrial control libraries, industrial HMI training material, mechanical-animation examples and restrained digital-twin examples. References are scored for silhouette clarity, port clarity, state restraint, alarm salience, label legibility and whole-screen density.

Public projects may inform architecture and visual principles. Code, geometry or images may be reused only when the license is verified as compatible and attribution obligations are recorded. Repositories without a clear license are study-only. Generated or original geometry is preferred for the production SDK.

Downloaded manuals, specifications, application notes and sample archives must be verified and archived under the permanent Obsidian knowledge base at `E:\BaiduSyncdisk\旧电脑\知识库` using the matching vendor or subject hierarchy. Project source, build output, customer data and working assets remain in the repository, not the knowledge base.

## 13. Verification Strategy

### 13.1 Automated tests

Tests cover:

- Control type and property versioning.
- State precedence for every family.
- Command/feedback separation.
- Binding type, range, unit and access validation.
- Deterministic offline simulation.
- Serialization and migration round trips.
- Stable independent pipe geometry.
- Geometry scaling, rotation and aspect-ratio policy.
- WPF/SVG semantic render-description equivalence.
- Missing, stale, invalid and contradictory inputs.
- Renderer failure isolation.

### 13.2 Visual review

Each equipment family produces:

1. A magnified morphology sheet with named mechanical parts and anchors.
2. Stopped/closed, active/open, transition, fault and unknown states.
3. A 100 percent actual-size placement on the calibration background.
4. Placement on the complete generic equipment sample screen.

The dense three-tank reference screen verifies composition pressure, independent placement and route readability. It is not accepted as evidence of process correctness unless its topology is separately verified from source drawings.

### 13.3 Build gate

The Release solution build must complete with zero warnings and zero errors, all automated tests must pass, and visual review must record hard failures separately from refinements.

## 14. Phase 2 Acceptance Gate

Phase 2 is complete only when all conditions are satisfied:

1. Every initial control family has a mechanically recognizable 2.5D body, versioned semantic contract, state matrix and necessary process animation.
2. One shared sample project produces equivalent WPF and SVG equipment meaning for all required states.
3. Magnified, 100 percent actual-size, complete-screen and editing-contract reviews pass.
4. Commands and feedback remain separate in contracts, previews and tests.
5. Invalid bindings and renderer failures are contained and diagnosed as designed.
6. Release build and all automated tests pass with zero warnings and zero errors.
7. Reference and production-asset sources have documented license status.
8. No WPF production editor, Web Runtime, PLC adapter, WinCC deployment or business module is silently included.

After this specification is approved, a separate Phase 2 implementation plan will divide the work into numbered tasks with review checkpoints. No Phase 2 production code is authorized by this design document alone.
