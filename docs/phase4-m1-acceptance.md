# Phase 4-M1 Acceptance

Date: 2026-08-23  
Scope: offline ASP.NET Core + SVG/Web Runtime only  
Repository: `D:\wpf_XM\IndustrialScadaPlatform`

## Acceptance Fixture

`tests/Scada.Runtime.Tests/Fixtures/runtime-published.json` is a published,
self-contained project with one screen containing a vessel, an independent
straight pipe and a Chinese text object. It has no PLC address, customer
database, TIA project or network dependency.

## Automated Gate

The runtime-focused test project covers:

- published JSON and immutable revision loading;
- draft/missing/malformed project diagnostics;
- deterministic values, timestamps and `Good`/`Uncertain`/`Bad` quality;
- unknown variables returning `Bad` with a null value;
- control SVG metadata (`data-control-type`, `data-state`, `data-quality`);
- independent pipe endpoints and bends;
- unsupported-control placeholders;
- desktop/tablet/phone coordinate preservation;
- read-only HTTP routes, missing-screen 404 and draft-project 400;
- responsive Chinese browser assets without command URLs.

Verification commands:

```powershell
dotnet build IndustrialScadaPlatform.sln --configuration Release
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release
dotnet test tests\Scada.Runtime.Tests\Scada.Runtime.Tests.csproj --configuration Release --filter FullyQualifiedName~Phase4M1AcceptanceTests
git diff --check
```

The latest focused run passed 22 runtime tests and 2 Phase 4-M1 acceptance
tests. The Release build passed with 0 warnings and 0 errors before the final
handoff verification.

## HTTP Smoke Path

Start the host with the fixture on a loopback port:

```powershell
dotnet run --project samples\Scada.Runtime.Preview\Scada.Runtime.Preview.csproj --configuration Release -- --project <absolute-path>\tests\Scada.Runtime.Tests\Fixtures\runtime-published.json --urls http://127.0.0.1:50741
```

Expected read-only requests:

```powershell
Invoke-WebRequest http://127.0.0.1:50741/api/runtime/health
Invoke-WebRequest http://127.0.0.1:50741/api/runtime/project
Invoke-WebRequest http://127.0.0.1:50741/api/runtime/screens/Main
```

The browser smoke review verified a nonblank desktop scene with visible vessel,
pipe and text, a 390x844 phone layout without overlap, and no browser console
errors. The default browser endpoint remains loopback-only and no command route
is registered.

## Explicit Exclusions

Phase 4-M1 does not include Siemens S7, OPC UA, Modbus, PLCSIM, SignalR,
authentication, roles, alarms, trends, recipes, batches, PID, WinCC V8.1
deployment, CAD/PDF recognition, legacy migration or browser-to-PLC commands.
Those items require separate designs and acceptance gates.
