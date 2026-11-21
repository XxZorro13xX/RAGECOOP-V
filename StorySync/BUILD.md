# Building StorySync

## Do not build here

This Linux VM has **no `dotnet`**, and the client project targets
**net48 (Windows-only)**. Do not try to build on this machine.
Build on **Windows** (Visual Studio 2022 or the **.NET 10 SDK** — do not install the .NET 6 SDK, it is end-of-support), or via
the RAGECOOP repo's GitHub Actions `windows-latest` workflow.

## Prerequisites (Windows)

- Draythianna/RAGECOOP-V cloned and **built first** — StorySync
  references its built assemblies. From the RAGECOOP clone:
  `dotnet build RageCoop.Client.Installer/RageCoop.Client.Installer.csproj -c Release`
  and `dotnet build RageCoop.Server/RageCoop.Server.csproj -c Release`
  (same commands the repo's `nightly-build.yaml` uses).
  After that build you must have:
  - `bin\Release\Client\Scripts\RageCoop\RageCoop.Client.dll`
  - `bin\Release\Server\RageCoop.Server.dll`
  - `libs\ScriptHookVDotNet3.dll` (already in the repo)
  - `RageCoop.Core` built from source (referenced as a project)
- .NET 10 SDK (server, `net10.0` — .NET 6 is end-of-support since 12 Nov 2024, so the server was retargeted to net10.0 in Nov 2026) and .NET Framework 4.8 targeting
  pack / Developer Pack (client, net48).
- ScriptHookVDotNet v3.7.0-nightly+ and LemonUI SHVDN3 2.2+ — these are
  runtime requirements for players, and the `IsMissionActive`
  property used by the client was confirmed present in the repo's
  `libs/ScriptHookVDotNet3.dll` (bundled 3.7.0.188).

## Build

From a Developer Command Prompt / PowerShell on Windows, in this
folder (`StorySync/` next to the `RAGECOOP-V-draythianna` clone):

```powershell
# Both projects, Release:
dotnet build StorySync.sln -c Release
```

If your RAGECOOP clone lives somewhere else, override the root:

```powershell
dotnet build StorySync.sln -c Release -p:RageCoopRoot=C:\path\to\RAGECOOP-V
```

Or build each project alone:

```powershell
dotnet build Server\StorySync.Server.csproj -c Release
dotnet build Client\StorySync.Client.csproj -c Release
```

### Outputs

| Project | Output | Goes to |
|---|---|---|
| `StorySync.Server` (net10.0) | `Server\bin\Release\StorySync.dll` | `<server>\Resources\Server\StorySync\StorySync.dll` |
| `StorySync.Client` (net48) | `Client\bin\Release\StorySync.Client.dll` | `<server>\Resources\Client\StorySync\StorySync.Client.dll` |

Copy **only** those two DLLs into the resource folders — nothing
else. In particular do not copy `RageCoop.Core.dll`,
`RageCoop.Client.dll`, `RageCoop.Server.dll` or any
`ScriptHookVDotNet*.dll` into them: the references are
`Private=false` for exactly this reason, and the resource loaders
would otherwise load duplicate assemblies and break type identity
(`Client`, `CustomEventReceivedArgs`, …).

## How the references work

- `Shared/StorySyncEvents.cs` is compiled into **both** DLLs via a
  `<Compile Include="..\Shared\..." Link="..." />` in each csproj.
  It references only `RageCoop.Core.Scripting.CustomEvents` (from the
  `RageCoop.Core` project reference) and the BCL.
- Server project: `net10.0`, references `RageCoop.Core` (project) and
  the built `RageCoop.Server.dll` (for `ServerScript`, `API`,
  `Client`). It needs no GTA/SHVDN reference — it never touches GTA
  types directly.
- Client project: `net48`, `LangVersion 7.3`, references
  `RageCoop.Core` (project), the built `RageCoop.Client.dll` (for
  `ClientScript` and `Scripting.API`), and
  `libs\ScriptHookVDotNet3.dll` (for `GTA.Game`, `GTA.UI`,
  `GTA.Math`).
- The `RageCoop.Client.dll` / `RageCoop.Server.dll` hint paths
  assume the standard RAGECOOP build layout
  (`bin\<Config>\Client\Scripts\RageCoop\...`,
  `bin\<Config>\Server\...`). If your build puts them elsewhere,
  fix the `HintPath` in the csproj — the path is marked
  `TODO: verify` in both files.

## Verifying the build without a game

1. **Server smoke test:** use the RAGECOOP repo's own harness —
   `tests/smoke-server.ps1 -ServerDir bin\Release\Server` — with the
   StorySync folder added under `Resources\Server\`. It starts the
   server with a resource loaded; check the log for
   `StorySync server loaded: mission lock ready.` and no
   `Failed to load resource: StorySync` error.
2. **Client:** there is no headless client test. The first real test
   is the playtest checklist in README.md, on Legacy, with two
   players on identical builds.

## Troubleshooting

- `RageCoop.Client.dll not found` / `RageCoop.Server.dll not found`
  at build time → build RAGECOOP first (see Prerequisites), or fix
  `RageCoopRoot` / the `HintPath`.
- `CustomEvents.Hash` collision at runtime → two different event
  names hashed to the same int. It throws `ArgumentException` naming
  the colliding name. Rename one event in `StorySyncEvents.cs`
  (and bump the version — old clients will no longer match).
- Resource loads but the client never gets `MissionState` → the
  server resource didn't start (check the server log), or the
  client folder on the server doesn't contain exactly
  `StorySync.Client.dll` (the server zips the folder; a stray DLL
  in there can break loading — see `CanBeIgnored` in
  `RageCoop.Core/CoreUtils.cs`, which only ignores the RAGECOOP /
  SHVDN DLLs).
- `Script RageCoop.Client.Main was terminated ... Blocking script!`
  on the player side → `ScriptHookVDotNet.ini`:
  `ScriptTimeoutThreshold=15000` (see README.md).
