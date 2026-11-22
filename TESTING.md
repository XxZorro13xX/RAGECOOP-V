# Testing — RAGECOOP-V + StorySync (Legacy)

Playtest checklist for the fork. Written from the setup as actually
verified on 2026-11-21 — if anything below doesn't match your machine,
fix the setup first, then test.

## 0. Pull first

Your local clone is behind `main`. Before building anything locally:

```
git pull
```

Recent commits you need: `00928f1` (StorySync `using` fix) and
`3b7cc76` (server retargeted `net6.0` → `net10.0`).

## 1. Prerequisites (verified)

| Item | Verified version / value |
|---|---|
| Game | Legacy `VER_1_0_3889_0` (check `GTA5.exe` → Properties → Details) |
| ScriptHookV | v3889.0 / 1158.13 (15 Jul 2026) — must match the game build |
| ScriptHookVDotNet | **3.7.0-nightly .206** (console showed `3.7.0.206` on a clean load). Stable 3.6.0 breaks on game 3258+ |
| LemonUI SHVDN3 | 2.2.0.0 |
| .NET Framework | 4.8 Developer Pack installed (client is net48) |
| .NET SDK | 10.0.404 (server is net10.0 since `3b7cc76`; the .NET 6 runtime is end-of-support and missing from the Actions runner) |
| `ScriptHookVDotNet.ini` | `ScriptTimeoutThreshold=15000` |

**Double-loader trap (bit us on 2026-11-21):** `ScriptHookVDotNet.asi`
(old name) and `ScriptHookVDotNet-Loader.asi` (new name) must not both
be in the GTA folder. Having both loads SHVDN twice and every
RAGECOOP script fails with `NullReferenceException` at
`GTA.Script..ctor()` line 41, plus garbled console text. Delete the
old `ScriptHookVDotNet.asi`, keep the Loader one.

## 2. Build to test — use the release zip, don't build locally

Download from the fork's **Legacy builds** release
(`https://github.com/XxZorro13xX/RAGECOOP-V/releases`):

```
Legacy_RageCOOP-V-StorySync-main-20261121-233552.zip  (8.7 MB)
```

It contains the client installer, the server, and both StorySync DLLs
already in the right resource folders
(`Resources/Server/StorySync/StorySync.dll`,
`Resources/Client/StorySync/StorySync.Client.dll`).
A fresh zip is published automatically on every push to `main`.

Install order (one-click, since installer commit `19ec6b1`):
extract the release zip anywhere except the GTA folder, run
`RageCoop.Client.Installer.exe` from the `Installer` folder, pick
`GTA5.exe` when asked. The installer now fetches anything missing or
too old itself — ScriptHookV matching your game build, the latest
SHVDN nightly, LemonUI 2.2 (embedded), .NET Framework 4.8 — removes
the old `ScriptHookVDotNet.asi` if both loaders are present, and sets
`ScriptTimeoutThreshold=15000`. It still deletes old `RageCoop.*`
files itself and keeps `RageCoop.Client.Settings.xml`. Before any of
that, the installer runs a **clean**: it silently deletes only known
mod files and folders (ScriptHookV, the ASI loader, ScriptHookVDotNet,
trainers, any `*.asi`, the `Scripts` / `mods` / `plugins` folders) so a
previous broken install can't survive into this one. Anything it
doesn't recognize is left alone, so it can never delete a default file
or break the game — and it never asks you to verify a list. Every
deletion is written to the installer log. The installer also writes
`RageCoop.Client.Installer.log` next to itself (game folder, OS and
installer version, versions found, downloads with sizes, every file
copied, every clean deletion) — send that file if an install goes
wrong. Manual
fallback (if a download fails): ScriptHookV from dev-c.com, SHVDN
nightly from
`github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases`,
LemonUI 2.2, `.NET Framework 4.8` runtime. Fixed in installer commit
`88131ef` (Nov 22): the ScriptHookV download used to fail with "End of
Central Directory record could not be found" because dev-c.com redirects
hotlinked downloads to its homepage — the installer now sends the right
Referer and checks the download is a real zip before extracting.

## 3. Test A — solo smoke test

- [ ] Start the server (`RageCoop.Server.exe`). Log shows
      `StorySync server loaded: mission lock ready.`
- [ ] Launch GTA V story mode, press **F9**, connect to
      `127.0.0.1:4499`.
- [ ] Welcome message appears; F9 menu opens.
- [ ] Run `/mission` → "no mission in progress".
- [ ] Walk into the Franklin intro trigger. You see
      "You are the mission host." Finish or fail it; both players
      (just you, here) see "Mission ended". `/mission` → no mission
      in progress.

## 4. Test B — two-player mission lock

Both players on identical builds (same game build, SHV, SHVDN
nightly, LemonUI, release zip). Do these in order.

- [ ] **B1 Connect:** both join, both see each other in the world and
      in the player list. Server log shows `StorySync server loaded`.
- [ ] **B2 Status:** either player runs `/mission` → "no mission in
      progress".
- [ ] **B3 Franklin intro:** Player A (Franklin) walks into the first
      mission trigger. A sees "You are the mission host." B sees
      "[A] started a mission. Don't trigger one."
- [ ] **B4 Second player joins:** B drives to A, fights / drives
      along. No duplicated mission peds or vehicles. B runs
      `/mission` → host is A.
- [ ] **B5 Mission fail:** fail the mission on purpose. Both see
      "Mission ended (ended). You can start a mission now."
      `/mission` → "no mission in progress", last mission shows A.
- [ ] **B6 Retry:** A restarts the mission, completes it. B sees the
      lock the whole time and never triggers a second copy.
- [ ] **B7 Denied path (known weak spot):** while A is in a mission,
      B deliberately walks into a *different* mission trigger. B
      should get "denied" — and if B's mission actually started
      locally, the warning that it may duplicate. This is the known
      limit (coordination, not enforcement), not a pass/fail of the
      lock — note what actually happened.
- [ ] **B8 Host disconnect:** while A hosts a mission, A Alt-F4s.
      If B is also in a mission, B becomes host ("You are now the
      mission host"). Otherwise everyone sees "Mission ended
      (host-left)" and the lock clears.
- [ ] **B9 Late join:** C joins mid-mission. C immediately sees
      "[A] is already in a mission. Don't trigger one."
- [ ] **B10 Abort:** A hosts, then runs `/mission abort`. Everyone
      sees "Mission ended (aborted)". B runs `/mission abort` while
      A hosts → "only the mission host can abort".
- [ ] **B11 Reconnect:** a player disconnects and reconnects
      mid-session. No invisible-player or duplicate-ID issues from
      StorySync (that fix lives in the Draythianna base).

## 5. Test C — CutsceneWatcher solo run (v2 groundwork, NOT wired in yet)

`Client/CutsceneWatcher.cs` is a prototype: it is **not** in the
client `.csproj`, so the release zip does not include it. Don't try
this until the watcher is wired into the build (open v2 choice).

When it is wired in, the run is: solo, Franklin intro mission.
The watcher log (`Scripts\RageCoop\StorySync-Cutscene.log`) should
record the intro cutscene, expected name `armenian_1_mcs_1`, plus
position and duration. That fills in the origin/duration blanks in
the mission→cutscene map (`StorySync/Shared/CutsceneMap.json`).

## 6. If the game crashes loading into story mode

The logs often don't name the cause: `ScriptHookV.log` ends right
after launching the ASIs, and the `ScriptHookVDotNet.log` on disk can
be stale — check its timestamps match the crashing run before trusting
it. Isolate in order (re-add after each step):

1. Move `Scripts\RageCoop\` out of the game folder and retry. Loads
   fine → the crash is in the RAGECOOP scripts; still crashes → it
   isn't.
2. Remove `NativeTrainer.asi` from the game folder and retry.
3. Check Windows Event Viewer → Windows Logs → Application for the
   crash entry at that time — it names the faulting module.
4. Remove `ScriptHookVDotNet-Loader.asi` (SHV only, no .NET scripts)
   and retry.
5. Rename `dinput8.dll` to `dinput8.dll.bak` (fully vanilla, no ASI
   loading) and retry. If vanilla also crashes, verify the game
   files in Steam — that's a game-install problem, not a mod one.

Known Nov 22 (test system): blackscreen then straight to desktop,
no Event Viewer entry, story mode worked before RAGECOOP and with
the OG mod — and steps 1–2 did NOT fix it, so the crash is not in
the RAGECOOP scripts or the trainer. Deleting
`ScriptHookVDotNet-Loader.asi` DID fix it: game boots fully with SHV
only. So the crash is SHVDN-side. Next: delete the rest of SHVDN
(`ScriptHookVDotNet3.dll`, `ScriptHookVDotNet2.dll`,
`ScriptHookVDotNet.ini`), re-run the installer to re-download the
.206 nightly fresh, and reboot if .NET Framework 4.8 was just
installed. Fallback if it still crashes: nightly .188 (the version
the client was built against) —
`github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases/download/v3.7.0-nightly.188/ScriptHookVDotNet-v3.7.0-nightly.188.zip` —
copy its `ScriptHookVDotNet3.dll` + loader `.asi` into the game
folder (delete the old `ScriptHookVDotNet.asi` if both loaders end
up present).

Note (Nov 22): a `Could not load file or assembly 'Lidgren.Network'`
line while SHVDN scans `RageCoop.Core.dll` is most likely a benign
scan artifact — Lidgren is embedded in both RAGECOOP DLLs by Costura
and the scripts start fine after it. Don't chase it unless the
scripts actually fail to start.

## 7. What to record

For every failure, keep: game build, SHV / SHVDN / LemonUI versions,
the exact log lines (`ScriptHookVDotNet.log`,
`Scripts\RageCoop\Data\RageCoop.Client.log`,
`RageCoop.Server.log`), and what each player saw — especially the
denied-path result in B7.
