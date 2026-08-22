# Phase 3-M1 Visual Review

- Preview host: `samples/Scada.Editor.Preview.Wpf`
- Target render: 1920x1080
- Canvas: dark industrial editor surface with optional grid
- Objects: vessel, centrifugal pump, automated valve, independent pipe and text
- Editing affordances: blue selection border, eight resize handles, blue rotation handle with a deep-blue arc cursor
- Navigation: Ctrl+mouse-wheel zoom around cursor; middle-button pan
- Engineering productivity: selected objects support alignment, equal-gap
  distribution, grouping and z-order commands; undo/redo does not change
  viewport zoom/pan and does not move unselected pipes.
- Command presentation: menu and toolbar labels are Chinese; unavailable
  commands are disabled when the current role or selection does not meet the
  operation requirements.

The render assertion verifies non-empty pixels at 1920x1080. Object geometry is
stored in model coordinates; viewport zoom/pan is never serialized into the
project document.
