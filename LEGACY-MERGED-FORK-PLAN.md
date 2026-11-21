# RAGECOOP Legacy — Merged-Fork Plan
**Focus: Legacy only (GTA5.exe). Enhanced is out of scope for v1.**
**Date audited: 2026-11-21 — clones in `~/workspace/ragecoop-fork/`**

## 1. Verified base — do not start from clean upstream

|  | Upstream | Draythianna fork (recommended base) |
|---|---|---|
| Repo | `RAGECOOP/RAGECOOP-V` | `Draythianna/RAGECOOP-V` |
| Head | `fcd7e18` — Docker conversion of server (#54) | `5df09a5` — Merge claude/port-fixes-main: invisible players and compatibility fixes |
| Commits | 980 | 1,004 (24 ahead) |
| Last push | 2026-02-04 | 2026-10-16 |
| License | MIT | MIT (same) |
| Open issues | 31 open issues (+ 4 open PRs = 35 total items — earlier "35 issues" was wrong) | — |

**Clone commands used:**
```
git clone https://github.com/Draythianna/RAGECOOP-V.git RAGECOOP-V-draythianna
git clone https://github.com/RAGECOOP/RAGECOOP-V.git RAGECOOP-V-upstream
git -C RAGECOOP-V-draythianna diff --stat fcd7e18..HEAD
# 48 files changed, 906 insertions, 138 deletions
```

## 2. Correction to the earlier correction

Draythianna **does** address issue #91 in code, even though its README doesn't say so:

`RageCoop.Client/Util/Memory.cs` (diff `fcd7e18..HEAD`):
- Old: `NativeMemory.FindPattern(...)` → `throw new NotSupportedException("Can't find memory pattern to patch weapon/radio slow-mo")` in the static constructor — one missing pattern killed *every* Memory call.
- New: `Game.FindPattern(...)` → if `IntPtr.Zero`, `return` (skip the patch), and `ApplyPatches()` / `RestorePatches()` are now null-conditional (`VignettingPatch?.Install()`).
- `ReadPosition()` / `ReadVelocity()` no longer read `e.MemoryAddress + PositionOffset` / `VelocityOffset`. They now guard `e == null || !e.Exists()` and return `e.Position` / `e.Velocity` via natives — no fixed offset that differs between builds.

So on Enhanced the crash becomes a skipped slow-mo patch, not a startup crash. That is a mitigation, not a proven Enhanced fix — still needs a live Enhanced test. **On Legacy it is a non-issue: the pattern is present, the patch applies as before.**

## 3. What Draythianna already fixes (verified in diff)

### a) Invisible players — the #1 Legacy bug, root-caused in 3 places
1. `RageCoop.Client/Networking/Networking.cs` — `Main.LocalPlayerID` was 0 at handshake because `AddPlayer()` hadn't run yet. Now pre-assigns `EntityPool.RequestNewID()` before sending the handshake, so the server registers the real ped ID.
2. `RageCoop.Client/Sync/EntityPool.cs` — reuses that pre-assigned ID when creating the local `SyncedPed` instead of minting a second one.
3. `RageCoop.Client/PlayerList.cs` — `UpdatePlayer()` used to silently skip a player the server knew but the client never saw connect. Now adds them if `Username` is present.
4. `RageCoop.Server/Networking/Server.Connections.cs` — stale sessions by name / ped ID used to throw on duplicate keys *after* approval, leaving a player connected but invisible. Now cleans up stale sessions on reconnect, denies only a genuinely live duplicate ped ID.

### b) SHVDN 3.7 / 3.9 compatibility
- `c884077 SHDN3.7 and LemonUI2.2 compatibility`, `ef9d7bb 1.5.8 SHDN 3.7`
- README now requires **ScriptHookVDotNet 3.7.0 + LemonUI SHVDN3 2.2+** (was 3.6.0), and adds the SHVDN timeout fix: `ScriptHookVDotNet.ini` → `ScriptTimeoutThreshold=15000` (was 5000) or RageCoop is killed on startup as a "blocking script".
- New `RageCoop.Client/Util/ShvdnWorkarounds.cs` — SHVDN Enhanced 3.9.0.6 reads the projectile pool wrong on Legacy, so `World.GetAllProjectiles()` throws every tick. Workaround clears `SHVDN.NativeMemory.s_projectilePoolAddress` by reflection so it returns an empty list. `EntityPool.cs` now wraps that call in try/catch, and `SyncedProjectile.cs` marks `_untracked` and stops re-firing when the list is empty.
- Costura no longer embeds SHVDN into Core/Server (`FodyWeavers.xml`: `ExcludeAssemblies="ScriptHookVDotNet|ScriptHookVDotNet3"`), plus a new pure-IL `RageCoop.ShvdnStub` so `GTA.Math` types load on the server. Fixes the Race resource server crash (`ee49523`).

### c) Crash / null-safety hardening
- `Main.cs`: `DoSync` errors now logged once via `ShvdnWorkarounds.LogOnce` instead of swallowed in release; `Connected()` wraps `Memory.ApplyPatches()` in try/catch; `KillMessage()` never throws while handling death.
- `SyncEvents.cs`: `HandleVehicleBulletShot` guards missing ped / vehicle and bone 35 (`v.Bones.Count > 35 ? ... : v.UpVector`).
- `SyncedPed.cs`: blip fallback to White when `ShowPlayerBlip` is on but colour is 255 ("no blip"); fixed `SET_PED_IS_IGNORED_BY_AUTO_OPEN_DOORS` missing handle arg; reload animation flag `AllowRotation` → `Secondary`.

### d) Server / release infra
- `AnnouncedAddress` setting + fallback in `Server.Background.cs` / `ServerInfo.cs` (use a No-IP hostname instead of detected public IP for master-server listing).
- CI (`nightly-build.yaml`): builds on `windows-latest`, publishes one zip per branch to a family release — `main / dev-nightly / last-rel-nightly` → **Legacy**, `enhanced_*` → **Enhanced**. Includes installer in the zip, runs `tests/ShvdnStubCheck` and `tests/smoke-server.ps1` (server smoke test with a resource).
- **Build note:** this Linux VM has no `dotnet`. Client is `net48`, Server is `net6.0`, Core is `netstandard2.0`. Build on Windows or via the GitHub Actions workflow, not here.

## 4. What is NOT in Draythianna — the real work

### No mission / story sync system exists, in either repo
Grep for `mission|story|savegame|cutscene|interior` across Client/Core/Server:
- Only hits: `Settings.cs:52` — "Disable world NPC traffic, mission entities won't be affected"; `WorldThread.cs:120,138` — traffic cleanup *exempts* `EntityPopulationType.Mission` peds/vehicles (i.e. it deletes local traffic but leaves mission entities alone); `TaskType.cs` — `CTaskCutScene`, `CTaskTargetUnreachableInInterior` enum values only.
- **There is no code that syncs mission state, mission objectives, cutscenes, interiors, or save progress.** The server is a relay only — it holds no world and no save.

That confirms the known rule, still true on this base: **only one player starts a mission, otherwise entities duplicate; other players need a 100% save if they haven't done it yet.** Any "better story co-op" has to be built, not merged.

### RageCoop+ — treat as unverified, do not merge blind
- gta5-mods page (Apr 16–21, 2026) could not be re-verified this run. Its listed wins — keyboard-layout switching in chat, detailed server browser, custom Tab colour, RU/EN localisation, custom master server, auto-update, Legacy+Enhanced support — sound useful, but there is no GitHub source linked from that page to diff.
- Note: **server password already exists upstream** (`Settings.Password`, `CoopMenu` password item, SHA256-hashed handshake in `Server.Connections.cs`). Do not count that as a RageCoop+ exclusive.
- Action: only cherry-pick RageCoop+ if you can get its source / diff it. Otherwise re-implement the two cheapest wins yourself (server-browser detail, auto-update via the existing installer + CI zip).

### Other small forks worth cherry-picking (verify source first)
- `RhockPereira/RAGECOOP-Motorcycle-Passenger-Fix` (2026-08-29) — G key picks driver seat on motorcycles instead of passenger. Small, testable, Legacy-relevant.
- DoctorZhou Gunshot Sound Sync Fix (gta5-mods, based on v1.5.4.7 source) — fixes missing/out-of-sync gunshots, client-side only, all players must install it. Merge the *source change*, not just the compiled DLL.
- `oldnapalm` Race resource — already referenced in Draythianna's server-crash fix; use it as the smoke-test resource, not as a feature to merge.

## 5. Merged-fork plan — Legacy v1

### Phase 0 — Freeze the base (1 evening)
1. Fork `Draythianna/RAGECOOP-V` at `5df09a5` (main). Do not rebase onto upstream — upstream is stale.
2. Pin versions in README + CI: **Legacy GTA V, ScriptHookV (matching game build), SHVDN 3.7.0 minimum (test 3.9.x separately — the projectile workaround exists for a reason), LemonUI SHVDN3 2.2+, .NET Framework 4.8, .NET 6 SDK for the server.**
3. Keep MIT licence + attribution to EntenKoeniq / Sardelka9515 / Draythianna / oldnapalm.

### Phase 1 — Verify the base actually plays on Legacy (before writing new code)
- Build via CI (windows-latest) or local Windows `dotnet build`, take the Legacy zip from the family release.
- 2-player Legacy test: both on same SHVDN + same RAGECOOP build, `ScriptTimeoutThreshold=15000`, connect via direct IP first (skip master server).
- Pass criteria: both players visible in list + in world, blips show, no invisible-player repro, no projectile-pool crash, kill message shows killer, reconnect after unclean disconnect works (stale-session cleanup).
- If any of those fail, fix in this fork — do not start Phase 2.

### Phase 2 — Mission sync MVP (the differentiator)
Build as a **server resource + client resource** using the existing API (`RageCoop.Client/Scripting/API.cs`, `RageCoop.Server/Scripting/`), not by rewriting the sync core:
1. **Host-authoritative mission lock:** first player to start a mission becomes host; others get a "joining mission" state and do *not* trigger the same mission script. This kills the entity-duplication bug at the source.
2. **Shared mission-entity ownership:** mission peds/vehicles already exempt from traffic cleanup — extend that to an explicit owner map, so a mission ped killed by Player B is dead for Player A too (fixes issue #88: teammates becoming enemies in the other player's mission).
3. **Checkpoint / fail sharing:** on mission fail / pass, broadcast the result; on pass, record it server-side per player so the group can see who still needs it — a stepping stone to replacing the "everyone needs a 100% save" workaround.
4. **Interior gate:** missions that go inside buildings need explicit testing; log which ones break and document InteriorsV as the interim workaround.

Do *not* attempt full cutscene sync or shared save files in v1 — those are the hardest parts and the most likely to desync.

### Phase 3 — Sync polish (cherry-picks)
1. Motorcycle passenger fix (RhockPereira).
2. Gunshot sound sync (DoctorZhou source).
3. Melee / ragdoll: re-test after Phase 1; only fix what repros on Legacy — do not port FiveM-style prediction wholesale.
4. Performance: keep the existing ped/vehicle/projectile limits (`NpcStreamingDistance` default 500, `PlayerStreamingDistance` -1 = unlimited) tunable in `Settings.xml`; add a one-line FPS guard if 2-player sessions with traffic on still dip.

### Phase 4 — UX / server (cheap wins only)
1. Server-browser detail + `AnnouncedAddress` already in Draythianna — expose it in the installer UI.
2. Auto-update: reuse the CI Legacy zip + installer; do not build a custom master server in v1.
3. Admin: stock server has no admin commands / ban list, only `KickGodMode` / `KickSpamming`. Add a minimal resource-based kick/ban + whitelist via the existing handshake event — do not add it to the core server.

## 6. What to test, in order (Legacy playtest checklist)
- [ ] Both players spawn, see each other, blips + nametags correct
- [ ] Drive together, passenger seat (G) incl. motorcycle
- [ ] Shoot together — gunshots heard, kill message correct
- [ ] Franklin intro mission, host-only start, second player joins
- [ ] Mission fail → both see fail, retry works, no duplicated mission peds
- [ ] Reconnect after Alt-F4 / crash → no invisible player, no duplicate ped ID deny
- [ ] 1-hour freeroam, traffic on, no projectile-pool crash, no SHVDN timeout kill

## 7. Out of scope for Legacy v1
- Enhanced (`GTA5_Enhanced.exe`) — needs live re-patterning of `Memory.cs` + a full SHVDN 3.9 test matrix. Draythianna soft-fails the pattern, but that is not a verified Enhanced fix.
- Shared save files, cutscene sync, full story-progress sync.
- Custom master server, new networking (Lidgren stays).
- GTA Online — never load this mod there.

## 8. Next decision
Pick one:
1. **Ship Phase 0+1 as-is** — publish the Draythianna Legacy build as your fork's v1 and play on it.
2. **Build Phase 2** — I start on the host-authoritative mission-lock resource.
3. **Cherry-pick Phase 3 first** — motorcycle + gunshot fixes, then playtest.
