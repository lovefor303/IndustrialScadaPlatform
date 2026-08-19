# IndustrialScadaPlatform Workspace Rules

## Source Of Truth

Before changing this repository, read:

1. `docs/PROJECT_CONTROL.md` for the long-term product direction and phase boundaries.
2. `docs/handoff-current.md` for the current checkpoint and the single next action.
3. The approved design and implementation plan under `docs/superpowers/` when executing a phase.

## Isolation

- This repository is the new industrial SCADA platform.
- Never modify `D:\wpf_XM\配液系统湖南\配液系统2`, its worktrees, the legacy database, PLC projects, TIA projects or customer runtime data while working here.
- Do not import old application data or PLC addresses unless a later phase has an explicit migration contract.

## Phase Discipline

- Work on one numbered phase at a time.
- Do not start a later phase because an intermediate task is convenient.
- A side task does not change the platform phase. Record it separately and return to `docs/handoff-current.md`.
- Before resuming after any other task, re-read the control and handoff documents.
- Do not add WPF, Web Runtime, PLC communication, WinCC deployment or business modules during Phase 1.

## Verification

- Follow TDD for new behavior: write a failing test, observe the expected failure, implement the smallest change, then run the focused and full tests.
- Do not claim a task is complete without fresh build/test evidence.
- Keep generated output out of source control.
- Update `docs/handoff-current.md` at every meaningful checkpoint.
