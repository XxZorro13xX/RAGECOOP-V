# CutsceneWatcher — memory-reader prototype (StorySync, Legacy)

**Status: UNTESTED. NOT wired in. NOT built.**

File: `Client/CutsceneWatcher.cs` (SHVDN C#, net48, C# 7.3 — same
style as `Client/MissionSyncClient.cs`). It is deliberately **not**
added to `Client/StorySync.Client.csproj` and **not** called from
`MissionSyncClient.cs` yet, so v1 keeps building and behaving
exactly as before. Wire it in only after the test below passes on
Windows — the wiring is 3 lines, in "Enable / disable" below.

This is **Recommendation 1** from the cutscene-connector work
(2026-11-21): a watcher that can see *which* cutscene is playing,
not just *that* one is playing. It is the missing piece for the
v2 vote flow — the server cannot ask "join this cutscene?" until
someone can name the cutscene.

It is **not** the whole v2. The vote flow, the static
mission → cutscene map, and synced playback live in
`V2-Prototype/` (`CutsceneMap.cs`, `CUTSCENE_VOTE_V2.md`). This
file's job is narrower: detect the name, log it, and (later) tell
the server. The polling-only watcher in
`V2-Prototype/CutsceneWatcher.cs` is the same idea with the memory
reader stubbed out (`ReadCutsceneNameFromMemory()` returns null
on purpose). This file is the real reader — it supersedes that
stub. If both ever get wired in, wire in **this** one, not the
V2 stub; do not run both at once or you will get duplicate logs.

## What it does

On every tick (script thread, called from `MissionSyncClient.OnTick`):

- Polls `Game.IsCutsceneActive` and detects the false → true /
  true → false edges.
- On **start**: records GameTime, player position, whether a
  mission is active, cutscene time and total duration from the
  natives, and reads the cutscene **name from memory** (if the
  reader is working). Logs one line, shows a GTA notification
  `~y~Cutscene detected: <name>`, and — only if `SendToServer`
  is turned on — sends `StorySync.CutsceneDetected`.
- While active: keeps the cutscene time / total duration fresh,
  latches `WAS_CUTSCENE_SKIPPED` (it may reset after the scene
  ends, so it is latched while the scene is still playing), and
  retries the name read if the string was empty at the start
  (the name can appear a few ticks after `IsCutsceneActive`
  flips). A name change mid-scene (concat scenes such as
  `pro_mcs_7_concat`) is logged.
- On **end**: logs duration (GameTime difference), final
  cutscene time, total duration, and `skipped=true/false`.

Natives used, with their hash names verified present in the
project's own `libs/ScriptHookVDotNet3.dll` (checked with
`strings` on 2026-11-21):

| Native | Present? |
|---|---|
| `GET_CUTSCENE_TIME` | yes |
| `GET_CUTSCENE_TOTAL_DURATION` | yes |
| `WAS_CUTSCENE_SKIPPED` | yes |
| `HAS_CUTSCENE_FINISHED` | yes (not used yet — see "Known limits") |
| `IS_CUTSCENE_ACTIVE` / `IS_CUTSCENE_PLAYING` | yes |
| `Game.IsCutsceneActive` property | per existing v1 code + V2-Prototype check |
| `Game.FindPattern(pattern, mask)` | yes — same 2-arg form `RageCoop.Client/Util/Memory.cs` uses |

One caution from that check: `START_CUTSCENE` did **not** show
up in the `strings` scan of `ScriptHookVDotNet3.dll`. This file
never plays a cutscene, so it does not need it — but the
playback helper in `V2-Prototype/CutsceneWatcher.cs` calls
`Hash.START_CUTSCENE`, and that name should be re-verified
before v2 playback is built on.

## Pattern research — the honest result

**No verified CutSceneManager pattern exists for Legacy 3411
(or 3258) in any public source checked on 2026-11-21.**

Sources checked:

| Source | What it gave |
|---|---|
| hoXyy `GTAV-AutoSplitter` (raw, read 2026-11-21) | v1.27 only. `string255 c: 0x01CB44A0, 0xB70` and `byte in_c: 0x1CB4472`. Header says "for version 1.27". |
| TheStonedTurtle `GTAV-AutoSplitter` (raw, same day) | Identical v1.27 file. |
| speedrun.com GTA V forum / autosplitter FAQ | "It only works on Patch 1.27 — make 100% sure you are ACTUALLY on patch 1.27." |
| Web search: CutSceneManager pattern, 3258 / 3411 | No pattern published. Only the v1.27 autosplitter and general pattern-scanning docs. |
| Menyoo / ChaosMod / SHVDN sources (web search) | Pattern-scanning machinery, no cutscene-manager pattern. Menyoo ships cutscene *name lists*, not the manager address. |
| `RageCoop.Client/Util/Memory.cs` + `Util.cs` | The *technique* (`Game.FindPattern` + RIP-relative resolve), not a cutscene pattern. |

So the code ships with the pattern slot **empty on purpose**
(`CutsceneManagerPattern = null`, `TODO-VERIFY`). That is not
an oversight — a guessed byte pattern that happens to match
somewhere else would read garbage and log it as a cutscene
name. The name validator in the code (`[a-z0-9_]` only,
≤ 64 chars, null-terminated within 255 bytes) is the second
line of defence, but it is not a substitute for a real pattern.

### Best candidate (TODO-VERIFY — not verified)

From the v1.27 autosplitter, the *structure* that is the best
starting point for the re-find:

- The loaded cutscene name is a 255-char buffer at **[pointer]+0xB70**,
  where the pointer lives at `GTA5.exe+0x01CB44A0` on v1.27.
  The code carries `CutsceneNameOffset = 0xB70` as a
  **candidate**: struct layout can shift between builds, so
  re-check it by playing a known cutscene and confirming the
  string reads back as that name. If it reads garbage, scan
  nearby offsets in steps of 8.
- The in-cutscene flag on v1.27 is a byte at `0x1CB4472`
  (0x2E *below* the name-pointer address, near the current-script
  string at `0x1CB4340`) — i.e. these globals sit close together
  in `.data`. That neighbourhood is a useful anchor when
  hunting the new base on 3411.
- The usual way to find the new base is a RIP-relative LEA to
  the manager global (e.g. `48 8B 05 ?? ?? ?? ??` — `mov rax,
  [rip+rel32]`, 7 bytes, rel32 at +3). The code's resolution
  follows exactly that shape, the same as `Memory.cs`
  (`address + *(int*)address + 4u`). `PatternRelOffset = 3`
  and `PatternInstructionLength = 7` are the standard LEA
  form, marked TODO-VERIFY — measure them from the actual
  disassembly when the pattern is found, do not assume them.

**Do not use the v1.27 addresses themselves.** `0x01CB44A0` /
`0x1CB4472` are wrong on Legacy 3411 and are never read by
this file.

### How to find the pattern on Windows (re-find procedure)

Do this on the Legacy build you actually play on, with a
known cutscene — Franklin & Lamar's first scene,
`armenian_1_mcs_1` ("Speak to Simeon", from the autosplitter's
`cutsceneNames` table, also in `V2-Prototype/CutsceneMap.cs`).

1. Start a fresh-ish save as Franklin, walk into the
   Franklin & Lamar trigger, and let the cutscene play.
   While it is playing, use Cheat Engine (or x64dbg) to scan
   process memory for the ASCII string `armenian_1_mcs_1`.
2. Note every address holding that string. The live one is
   the name buffer — it changes when the next cutscene
   starts. Confirm by letting the scene end and starting
   Repossession (`armenian_2_int`): the buffer should now
   read `armenian_2_int`.
3. In x64dbg, find what accesses that buffer address, and
   walk back to the code that loads the manager pointer
   (the RIP-relative LEA). That instruction is your pattern:
   copy its bytes, wildcard the 4 rel32 bytes (`??`), and
   record the rel32 offset and instruction length.
4. Put the pattern + mask + rel offset + instruction length
   into the four TODO-VERIFY constants in
   `CutsceneWatcher.cs`, and re-check `CutsceneNameOffset`
   (0xB70): read the string at manager+0xB70 during
   `armenian_1_mcs_1` and confirm it matches. If it does
   not, scan manager+0x00..manager+0xC00 in steps of 8 for
   the string and use the offset that matches.
5. Rebuild, play Franklin & Lamar → Repossession → Chop,
   and check the log (below). All three names must match
   the map: `armenian_1_mcs_1`, `armenian_2_int`,
   `arm_2_mcs_4`, `fra_0_int`, `fra_0_mcs_1`,
   `fra_0_mcs_4_p2_t3`.
6. Only then consider the pattern verified for that build.
   Record the game build number next to it in the code
   comment. A Rockstar patch can move it again — the
   pattern lives in this one file so a patch breaks one
   place, not the sync core.

Alternative route if the string scan is noisy: the mission
`.ysc` scripts contain `REQUEST_CUTSCENE("name", 8)` — the
name is already known from the static map, so step 1's
search string is certain. The scan is for *where the game
stores it at runtime*, not what it is.

## Modes

| Mode | When | What you get |
|---|---|---|
| **Native-only** (default today) | No pattern configured, or `FindPattern` returns zero | Start/end, GameTime, cutscene time, total duration, skipped flag, position. Name is `(unknown — memory reader disabled)`. Still useful — it proves the watcher, the timing, and the log pipeline. |
| **Memory-reader** | Pattern filled in, verified per above | Everything above, plus the real cutscene name on start, and name-change logging mid-scene. |

The watcher **never throws** and **never disables the mission
lock**: resolution happens once, lazily, on the first tick
(not in a static constructor — a throwing static constructor
is what broke SHVDN's own `NativeMemory` on 3258, see
SHVDN issue #1451). If the pattern is missing or wrong, it
logs once and keeps running in native-only mode.

## Enable / disable

**Not wired in yet — v1 is untouched.** To enable on Windows:

1. `Client/StorySync.Client.csproj` — add one line inside the
   existing `<ItemGroup>` with `MissionSyncClient.cs`:
   ```xml
   <Compile Include="CutsceneWatcher.cs" />
   ```
2. `Client/MissionSyncClient.cs` — in `OnStart()`, after the
   existing `API.Events.OnTick += OnTick;` line, create the
   watcher (pass `Logger?.Info` as the log delegate so its
   lines go to the RAGECOOP log):
   ```csharp
   _cutsceneWatcher = new CutsceneWatcher(msg => Logger?.Info(msg));
   ```
   Add a `private CutsceneWatcher _cutsceneWatcher;` field.
   In `OnTick()`, after the mission-flag code, add
   `_cutsceneWatcher?.Tick();`. In `OnStop()`, add
   `_cutsceneWatcher?.Stop();`.
3. Build per `BUILD.md` and install per `README.md`.

To disable: remove those lines (or just don't wire it in).
There is no config flag and no server change. `SendToServer`
defaults to `false` — the watcher is log-only until the
server handler exists.

### Server event (reserved, not active)

- Name: `StorySync.CutsceneDetected`
  (hash via `CustomEvents.Hash`, same mechanism as
  `Shared/StorySyncEvents.cs`).
- Payload: `(string username, string cutsceneName,
  int totalDurationMs)` — `totalDurationMs` is `-1` when the
  total-duration native could not be read.
- The hash is defined in `CutsceneWatcher.cs` so the client
  is ready, but it is **not** added to
  `Shared/StorySyncEvents.cs` and the server has no handler.
  Do not turn `SendToServer` on or add the event to the
  shared file until the server handler exists — until then
  the event would be sent into the void. Adding it later
  does not break v1: old clients simply never send it.

## Where the log goes

Two places, both best-effort (a logging failure never throws):

1. **RAGECOOP log** — via the `Action<string>` passed in
   (wired as `Logger?.Info` above), same log that shows
   `StorySync client loaded`.
2. **Dedicated file** — `Scripts\RageCoop\StorySync-Cutscene.log`
   in the GTA V folder (same folder convention as RAGECOOP's
   own `Scripts\RageCoop\...` settings path). One line per
   event, e.g.:
   ```
   2026-11-21 14:02:11 [GameTime=123456] CutsceneWatcher: START name=armenian_1_mcs_1 gameTime=123456 cutsceneTimeMs=0 totalDurationMs=18400 missionActive=True pos={X:..., Y:..., Z:...}
   2026-11-21 14:02:30 [GameTime=141800] CutsceneWatcher: END name=armenian_1_mcs_1 durationMs~18344 cutsceneTimeMs=18200 totalDurationMs=18400 skipped=False startedPos={X:..., Y:..., Z:...}
   ```
   In native-only mode the START line reads
   `name=(unknown — memory reader disabled)` and the
   notification says the same — that is expected, not a bug.

## Test checklist (Windows, Legacy, solo first)

Do these with the watcher wired in, **solo** (no server
needed for the watcher itself — it logs locally even
offline; the mission lock is a separate test).

- [ ] **Build:** `dotnet build StorySync.sln -c Release` on
      Windows succeeds; `StorySync.Client.dll` contains the
      watcher (no `CutsceneWatcher` load errors in the SHVDN
      console). The client still shows
      `StorySync client loaded`.
- [ ] **No-pattern run:** with the pattern still null, start
      the game, open `Scripts\RageCoop\StorySync-Cutscene.log`.
      It should contain exactly one line:
      `CutsceneWatcher disabled: no CutSceneManager pattern
      configured for this build (TODO-VERIFY...)`. No crash,
      no spam, mission lock still works.
- [ ] **Franklin intro:** fresh save, play Franklin & Lamar
      as Franklin. The log should show a START line when the
      first cutscene plays and an END line when it finishes.
      **Pass (native-only):** START + END present, duration
      is a sane positive number close to `totalDurationMs`,
      `skipped=False`. **Pass (memory-reader):** as above,
      plus `name=armenian_1_mcs_1` and the notification
      `Cutscene detected: armenian_1_mcs_1`.
- [ ] **Second mission cutscene:** continue into Repossession.
      The log should show a second START/END pair.
      **Pass (memory-reader):** `name=armenian_2_int`, then
      later `arm_2_mcs_4` when the Vagos confrontation scene
      plays. If the first name is right and the second is
      wrong, the name buffer is being overwritten by something
      else — note both values, do not guess.
- [ ] **Skip:** replay a cutscene and skip it (the usual
      skip key). The END line must read `skipped=True` and
      `durationMs` much shorter than `totalDurationMs`.
      If `skipped` stays False after a real skip, the
      `WAS_CUTSCENE_SKIPPED` latch is not working — report
      it, do not work around it by guessing from duration.
- [ ] **Chop:** play Chop. **Pass (memory-reader):**
      `fra_0_int`, then `fra_0_mcs_1`, then
      `fra_0_mcs_4_p2_t3`, in that order — these are the
      three Chop names from the autosplitter table (also in
      `V2-Prototype/CutsceneMap.cs`).
- [ ] **Two players (only after solo passes):** both players
      on the same build, host plays Franklin & Lamar. Host's
      log shows the name; the other player's log shows
      whatever cutscene *they* see (probably nothing, until
      v2 playback exists — that is expected, not a failure).

### What "pass" looks like

Solo, memory-reader mode, Franklin & Lamar → Repossession →
Chop, in order, with the six names above, durations within
~1s of `totalDurationMs` when not skipped, and
`skipped=True` only on a real skip. Anything else is data,
not failure — save the whole log file, it is what fills in
the map's missing origins (the `pos=` on each START line is
where the player was standing when that cutscene fired).

### If the pattern is not found

That is the expected state on 3411 until the re-find
procedure above is done. It is **not** a failure of the
watcher: native-only mode still logs start/end/times, and
the static map (`V2-Prototype/CutsceneMap.cs`) still
supplies the *what* by mission + order. Do not:

- hard-code the v1.27 addresses (`0x01CB44A0`, `0x1CB4472`)
  — they are wrong on Legacy and reading them can return
  garbage or fault;
- paste a pattern from an unverified source — follow the
  re-find procedure and verify it against a known cutscene
  first;
- treat a wrong name in the log as real — if the name does
  not match the map for that mission, the pattern/offset is
  wrong; save the log and re-do the re-find.

## Known limits

- **Not built or tested.** No `dotnet` on the Linux VM,
  client is net48/Windows-only. The code was checked by
  reading only — no compiler ran on it. The first build on
  Windows may surface a typo; fix it there, do not assume
  the file as written compiles.
- **No pattern for 3411 yet** (see above). Native-only mode
  is the day-one reality.
- `HAS_CUTSCENE_FINISHED` is verified present but not used
  yet — the END edge comes from `IsCutsceneActive` going
  false, which is enough for logging. If a scene ever ends
  without that edge flipping, add it as a second END signal.
- The name read is a snapshot at START (plus retries while
  the name is resolving). Concat scenes that change name
  mid-play are logged on change, but only if the reader is
  active.
- `Game.GameTime` wraps after ~24 days of continuous play;
  a wrapped duration is reported as `-1`, not a huge number.
- The log file grows by a few lines per cutscene. It is not
  rotated — delete it between test sessions if you want a
  clean read.
- Legacy only. Enhanced has a different binary and the
  pattern, if found for Legacy, must be re-found there.
