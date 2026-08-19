# Current Handoff

Updated: 2026-08-19

## Authoritative Scope

Repository: `D:\wpf_XM\IndustrialScadaPlatform`

Current phase: **Phase 1 - Core project model**

Approved design: `docs/superpowers/specs/2026-08-19-industrial-scada-platform-design.md`

Approved implementation plan: `docs/superpowers/plans/2026-08-19-phase1-core-project-model.md`

## Completed In This Repository

- Product design and Phase 1 implementation plan were approved and committed before implementation.
- The initial `src` and `tests` directory skeletons were created.
- `Directory.Build.props` and `Directory.Packages.props` were created with the planned .NET 10 and package constraints.
- Task 1 solution skeleton is verified: `IndustrialScadaPlatform.sln` contains exactly seven projects.
- `dotnet restore IndustrialScadaPlatform.sln` passed after pinning the vulnerable SQLitePCLRaw transitive packages to `2.1.13`.
- `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` passed with 0 warnings and 0 errors.

## Current Incomplete State

- No core contracts, scene model, serializer, revision store, simulator or acceptance tests have been implemented.
- No WPF canvas, Web Runtime, PLC communication, WinCC adapter or legacy migration has started.

## Next Action

Start Task 2 only: write the failing core contract tests in `tests/Scada.Core.Tests/ContractTests.cs`, then run the focused test to confirm the expected compile failure before implementing contracts.

Task 1 is complete, but the Phase 1 gate is not complete. Do not start Task 3 until Task 2 tests and implementation are complete and verified.

## Interruption Checkpoint

- Active workstream: new industrial SCADA platform.
- Active unit: Phase 1, Task 2 - core project and variable contracts.
- Last verified commands: `dotnet restore IndustrialScadaPlatform.sln`; `dotnet sln IndustrialScadaPlatform.sln list`; `dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore` (7 projects, 0 warnings, 0 errors).
- Intentionally uncommitted files: `AGENTS.md`, `Directory.Build.props`, `Directory.Packages.props`, `IndustrialScadaPlatform.sln`, `docs/MASTER_ROADMAP.md`, `docs/PROJECT_CONTROL.md`, `docs/handoff-current.md`, `src/`, and `tests/`.
- Resume action after any side task: re-read the five controlling documents, compare `git status --short` with this list, then write the Task 2 failing tests.
- Current exclusions: WPF editor, Web Runtime, Gateway, PLC communication, WinCC adapter, business modules and legacy migration.

## Verification Status

- Task 1 build/restore checkpoint has passed; no Phase 1 functional or test gate has passed.
- Do not claim the platform or Task 1 is complete.
- The old配液项目 and PLC projects have not been modified by this repository task.

## Return Rule After Side Tasks

Before resuming platform work after any unrelated task, read `AGENTS.md`, `docs/PROJECT_CONTROL.md` and this file. Continue only from `Next Action`; do not jump to a later phase or start a convenient UI feature.
