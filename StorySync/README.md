# StorySync — mission lock for RAGECOOP story co-op (Legacy)

**v1 prototype. Legacy only (`GTA5.exe`). Base: Draythianna/RAGECOOP-V at `5df09a5`.**

## The problem it fixes

RAGECOOP syncs mission peds and vehicles — but it has no idea a mission
is running. If two players each trigger the same mission locally, each
game spawns its own set of mission entities and they duplicate.

StorySync adds the missing piece: a **server-side mission lock**.
The first player whose mission flag goes true becomes the mission host.
Everyone else is told a mission is locked, so they know not to walk
into a trigger.

## What it does

- **Server** (`Server/MissionSyncServer.cs`) — state machine
  `Idle → Active → Ended → Idle`, held in memory only.
  - First `MissionStartRequest` wins and becomes mission host.
    The host gets `MissionGranted`; everyone else gets `MissionStarted`.
  - A request while locked gets `MissionDenied` (requester only).
  - Host's `MissionEnded` (or `/mission abort`, or host disconnect with
    nobody else in a mission) broadcasts `MissionEnded` and unlocks.
  - **Host disconnect:** the lock transfers to the next player who
    reported being in a mission and is still connected
    (`MissionHostTransferred`); if nobody qualifies, the mission is
    aborted and the lock clears.
  - **Late joiners** get the current lock state on connect
    (`MissionState` via `OnPlayerReady`).
  - Commands: `/mission` / `/mission status`, `/mission abort`
    (host or server console only).
- **Client** (`Client/MissionSyncClient.cs`) — polls
  `GTA.Game.IsMissionActive` on `API.Events.OnTick`, sends only on a
  real edge, and only after the new value has held for 500 ms
  (flickers are dropped, nothing is sent). Shows GTA notifications
  for granted / denied / started / ended / host-transferred.
- **Shared protocol** (`Shared/StorySyncEvents.cs`) — event names and
  payload layouts, hashed with `CustomEvents.Hash` so both sides agree
  without sharing an assembly. Never rename an event string after
  release — it changes the hash.

## Cutscene watcher (prototype, not wired in)

`Client/CutsceneWatcher.cs` + `CUTSCENE-WATCHER.md` — polls
`Game.IsCutsceneActive` and logs every cutscene start/end with
time, total duration, skipped flag and position, and reads the
cutscene **name from memory** when its pattern is verified.
**No verified pattern exists for Legacy 3411 yet** (the
autosplitter's addresses are v1.27-only), so it runs in
native-only mode (name unknown) until the pattern is re-found
on Windows — see `CUTSCENE-WATCHER.md` for the procedure and
the solo Franklin-intro test. Not wired into the build; v1 is
unaffected.

## What it does NOT do (read this before playtesting)

- **No cutscene sync, no interior sync, no shared save progress.**
  Mission completion is recorded in server memory only
  (`/mission status` shows the last host). Your own story save is
  still yours — the host is the one whose save advances.
- **Pass vs. fail is not known.** `MissionEnded` only means "the
  host's mission flag went false". The reason is `ended`, `aborted`,
  or `host-left` — never "passed".
- **It cannot hard-stop a mission already triggered locally.** If you
  walk into a trigger while someone else holds the lock, your game
  starts the mission anyway and entities may duplicate. The lock is
  coordination, not enforcement. If that happens, the denied
  notification says so — fail the mission and rejoin the host.
- **It does not sync mission objectives or dialogue.** You see the
  host's mission entities because RAGECOOP already syncs them; you
  do not see their mission UI.
- **No persistence across server restarts.** Restart the server and
  the lock (and the last-mission record) is gone.
- **Enhanced is out of scope.** Legacy only for v1.

## Cutscene map (v2 groundwork)

StorySync v1 has no cutscene sync. The static mission → cutscene map
for v2 lives in [`CUTSCENE-MAP.md`](CUTSCENE-MAP.md): **13 missions,
62 cutscenes** (Prologue through Nervous Ron), each name verified in
the decompiled mission script and cross-checked against the game's
cutscene name list and the LiveSplit autosplitter. Machine-readable
forms are in `Shared/CutsceneMap.json` (source of truth) and
`Shared/CutsceneMap.cs` (same data, generated from the JSON — do not
edit by hand). Origin and duration are unknown for every scene until
the CutsceneWatcher log or a manual fivemad lookup fills them in;
nothing in the map is guessed. This map supersedes the 5-mission
prototype in `V2-Prototype/CutsceneMap.cs` — do not compile both.

## Install (Legacy)

Build first — see [BUILD.md](BUILD.md). You need the two DLLs:
`StorySync.dll` (server) and `StorySync.Client.dll` (client).

**Everyone must run the same RAGECOOP build and the same StorySync
build.** A version mismatch is the #1 connect failure in RAGECOOP
generally, and it applies here too.

### Server (the machine hosting the RAGECOOP server)

```
<RAGECOOP server folder>\
  Resources\
    Server\
      StorySync\
        StorySync.dll          <- server build output, NOTHING else
    Client\
      StorySync\
        StorySync.Client.dll   <- client build output, NOTHING else
```

- Server side: the folder must be named `StorySync` and the DLL
  inside it must be named `StorySync.dll` — the loader builds the
  path as `<folder>\<folder>.dll`.
- Client side: the folder must be named `StorySync`. The server zips
  it to `StorySync.res` on startup and sends it to connecting
  clients. Do NOT put `RageCoop.*.dll` / `ScriptHookVDotNet*.dll` in
  either folder — the game/server already has them, and a second
  copy breaks type identity.
- Restart the server. You should see in the server log:
  `StorySync server loaded: mission lock ready.`

### Players (Legacy GTA V, every player)

1. Same Draythianna RAGECOOP Legacy build as the server.
2. `ScriptHookV` (matching your game build — see the root README) +
   `ScriptHookVDotNet` **v3.7.0-nightly or later** (no stable 3.7.0 yet; stable v3.6.0 breaks on game 1.0.3258.0+) + `LemonUI SHVDN3` 2.2 or later.
3. In `ScriptHookVDotNet.ini` (GTA V root folder):
   `ScriptTimeoutThreshold=15000` — RAGECOOP is killed on startup
   as a "blocking script" at the default 5000.
4. Join the server. The client resource downloads automatically on
   connect — there is nothing to install by hand on the player side.
   If it does not load, check the SHVDN console for
   `StorySync client loaded`.

## Commands

| Command | Who | What |
|---|---|---|
| `/mission` or `/mission status` | anyone | Lock state, host, start time, last mission |
| `/mission abort` | mission host, or server console | Ends the mission, unlocks, tells everyone |

## 2-player Legacy playtest checklist

Do these in order, with both players on the same builds.

- [ ] **Connect:** both join, both see each other in the world and in
      the player list. Server log shows `StorySync server loaded`.
- [ ] **Status:** either player runs `/mission` → "no mission in
      progress".
- [ ] **Franklin intro:** Player A (Franklin) walks into the first
      mission trigger. A sees "You are the mission host." B sees
      "[A] started a mission. Don't trigger one."
- [ ] **Second player joins:** B drives to A, fights / drives along.
      No duplicated mission peds or vehicles. B runs `/mission` →
      host is A.
- [ ] **Mission fail:** fail the mission on purpose. Both see
      "Mission ended (ended). You can start a mission now."
      `/mission` → "no mission in progress", last mission shows A.
- [ ] **Retry:** A restarts the mission, completes it. B sees the
      lock the whole time and never triggers a second copy.
- [ ] **Denied path:** while A is in a mission, B deliberately walks
      into a *different* mission trigger. B should get "denied" —
      and if B's mission actually started locally, the warning that
      it may duplicate. (This is the known limit, not a pass/fail
      of the lock — note what actually happened.)
- [ ] **Host disconnect:** while A hosts a mission, A Alt-F4s.
      If B is also in a mission, B becomes host ("You are now the
      mission host"). Otherwise everyone sees "Mission ended
      (host-left)" and the lock clears.
- [ ] **Late join:** C joins mid-mission. C immediately sees
      "[A] is already in a mission. Don't trigger one."
- [ ] **Abort:** A hosts, then runs `/mission abort`. Everyone sees
      "Mission ended (aborted)". B runs `/mission abort` while A
      hosts → "only the mission host can abort".
- [ ] **Reconnect:** a player disconnects and reconnects mid-session.
      No invisible-player or duplicate-ID issues from StorySync
      (that fix lives in the Draythianna base).

## Files

```
StorySync/
  README.md                  <- this file
  BUILD.md                   <- how to compile (Windows / CI only)
  CUTSCENE-MAP.md            <- static mission -> cutscene map (v2 groundwork)
  StorySync.sln
  Shared/StorySyncEvents.cs  <- wire protocol (linked into both DLLs)
  Shared/CutsceneMap.json    <- cutscene map, machine-readable (source of truth)
  Shared/CutsceneMap.cs      <- cutscene map, C# (generated from the JSON)
  Server/MissionSyncServer.cs
  Server/StorySync.Server.csproj   (net6.0 -> StorySync.dll)
  Client/MissionSyncClient.cs
  Client/CutsceneWatcher.cs        <- prototype, NOT wired into the csproj yet (see CUTSCENE-WATCHER.md)
  Client/StorySync.Client.csproj   (net48 -> StorySync.Client.dll)
  CUTSCENE-WATCHER.md              <- memory-reader prototype: pattern status, re-find procedure, test checklist
  V2-Prototype/                    <- cutscene vote + static mission->cutscene map (prototype, not wired in)
```

## Known limitations (v1)

1. Coordination, not enforcement (see "does NOT do" above).
2. Host transfer only goes to a player who reported being in a
   mission while locked. If nobody did, the mission aborts on host
   disconnect even if someone was standing right next to the host.
3. If a client joins in the tiny window before its resource loads,
   it may miss the `MissionState` sync — it will learn the lock on
   the next broadcast instead.
4. `Game.GameTime` wraps after ~24 days of continuous play; the
   500 ms guard treats a wrap as "guard met" rather than hanging.
5. Not built or tested on this Linux VM (no `dotnet`, client is
   net48/Windows-only). See BUILD.md. First live test must be the
   checklist above, on Legacy.
