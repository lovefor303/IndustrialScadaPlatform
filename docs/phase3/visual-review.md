# Phase 3-M1 Visual Review

- Preview host: `samples/Scada.Editor.Preview.Wpf`
- Target render: 1920x1080
- Canvas: dark industrial editor surface with optional grid
- Objects: vessel, centrifugal pump, automated valve, independent pipe and text
- Editing affordances: blue selection border, eight resize handles, orange rotation handle
- Navigation: Ctrl+mouse-wheel zoom around cursor; middle-button pan

The render assertion verifies non-empty pixels at 1920x1080. Object geometry is
stored in model coordinates; viewport zoom/pan is never serialized into the
project document.
