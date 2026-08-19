# Phase 1 Acceptance Matrix

Date: 2026-08-19  
Repository: `D:\wpf_XM\IndustrialScadaPlatform`

## Scope

Phase 1 proves the PLC-independent project core only. It covers project and variable contracts, retained scene geometry, JSON Schema and migration, SQLite draft/publish/restore/import/export, and the deterministic offline simulator.

It does not include WPF, Web Runtime, Gateway, PLC/S7/PLCSIM communication, WinCC deployment, alarms, trends, recipes, batches, PID panels or legacy migration.

## Automated Checks

Run from the repository root:

```powershell
dotnet restore IndustrialScadaPlatform.sln
dotnet build IndustrialScadaPlatform.sln --configuration Release --no-restore
dotnet test IndustrialScadaPlatform.sln --configuration Release --no-build
git diff --check
git status --short --branch
```

Expected gate result:

- Restore succeeds without NU1900/NU1903/NU1904. SQLitePCLRaw transitive packages resolve to the security-fixed `2.1.13` line.
- Release build has 0 warnings and 0 errors.
- Core, storage, simulator and acceptance tests all pass.
- `empty-project.json` is accepted by `schemas/project-v1.schema.json`.
- `invalid-project.json` is rejected with at least two validation errors.
- Scene objects and pipes round-trip without geometry drift; moving an equipment object does not mutate independent pipe geometry.
- Draft save/reopen, immutable publish, revision listing, restore-to-draft and import/export round-trip pass against temporary SQLite files.
- Offline simulation is deterministic; command variables are never presented as confirmed feedback.
- No file under `D:\wpf_XM\配液系统湖南\配液系统2` or any PLC/TIA project is changed by this gate.

## Acceptance Test

`tests/Scada.Acceptance.Tests/Phase1AcceptanceTests.cs` executes one complete lifecycle: create project, add variables and independent scene objects, save draft, export, import into a second database, publish, list revisions, restore, compare IDs/bounds/bends/bindings, and verify simulator quality behavior.

## Evidence Rule

Do not mark Phase 1 complete from a focused test alone. The handoff must contain fresh output for the complete commands above, the final solution list, the test count and the clean legacy-boundary check.
