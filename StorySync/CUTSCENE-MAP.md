# Cutscene map — static mission → cutscene map for StorySync v2

**Status: untested in-game.** Built 2026-11-21 from public sources (below).
No dotnet on the Linux VM, client is net48 / Windows-only, so nothing here
has been compiled or played. This map is the **fallback / complement to
the CutsceneWatcher**: if the watcher pattern breaks on a patch, the server
can still identify a cutscene from mission + character + position using
this map. If the watcher works, the map is the cross-check for what it
reads out of memory.

**13 missions, 62 cutscenes** — Prologue through Nervous Ron.

- **28 cross-checked** — name in the decompiled script, in the game's
  cutscene name list, and the autosplitter label agrees.
- **33 verified** — name in the decompiled script and in the game's
  cutscene name list (no autosplitter label for that scene).
- **1 script-only** — `family_2_mcs_4p2` (Daddy's Little Girl) is in the
  decompiled script but **not** in DurtyFree's cutscene name list. Do not
  use it for playback until it is verified in-game. `IsPlayableCutscene`
  in `CutsceneMap.cs` returns false for it on purpose.

## The map

Actors are cutscene entity handle strings from
`REGISTER_ENTITY_FOR_CUTSCENE` (e.g. `Lamar`, `Jimmys_Car`) — **not** ped
model names. "Per-scene" means the handles were in the same function that
requests that scene; "mission-level" means they were seen in the script
but could not be attributed to one scene. Origin and duration are
**unknown for every scene** (see "What is still missing").

| # | Mission (script) | Protagonist | Cutscenes |
|---|---|---|---|
| 1 | Prologue (`prologue1`, id 53) | Michael, Trevor (2004) | pro_mcs_1, pro_mcs_2, pro_mcs_3_pt1, pro_mcs_5, pro_mcs_6, pro_mcs_7_concat |
| 2 | Franklin and Lamar (`armenian1`, id 0) | Franklin | armenian_1_int, armenian_1_mcs_1, arm_1_mcs_2_concat |
| 3 | Repossession (`armenian2`, id 1) | Franklin | armenian_2_int, arm_2_mcs_4, armenian_2_mcs_6 |
| 4 | Chop (`franklin0`, id 40) | Franklin | fra_0_int, fra_0_mcs_1, fra_0_mcs_4_p2_t3, fra_0_mcs_5_p1 |
| 5 | Complications (`armenian3`, id 2) | Franklin (Michael introduced) | armenian_3_int, armenian_3_mcs_3, armenian_3_mcs_4, armenian_3_mcs_5, armenian_3_mcs_6, armenian_3_mcs_7, armenian_3_mcs_8, armenian_3_mcs_9_concat |
| 6 | Father/Son (`family1`, id 17) | Michael (+ Franklin) | family_1_int, fam_1_mcs_2, fam_1_ext_2, fam_1_ext_alt2, fam_1_ext_alt3 |
| 7 | Marriage Counseling (`family3`, id 19) | Michael (+ Franklin) | family_3_int, family_3_ext, fam_3_mcs_1 |
| 8 | Daddy's Little Girl (`family2`, id 18) | Michael | family_2_int, family_2_mcs_2, family_2_mcs_3, family_2_mcs_4, family_2_mcs_4p2 ⚠ script-only |
| 9 | Friend Request (`lester1`, id 44) | Michael | lester_1_int, les_1a_mcs_0, les_1a_mcs_1, les_1a_mcs_2, les_1a_mcs_3, les_1a_mcs_4, les_1b_mcs_1 |
| 10 | Casing the Jewel Store (`jewelry_setup1`, id 86) | Michael (+ Lester) | jh_1_int, jh_1_ig_3 |
| 11 | The Jewel Store Job (`jewelry_heist`, id 90) | Michael, Franklin (+ crew) | jh_2a_intp4 / jh_2b_int (one per approach), jh_2a_mcs_1 / jh_2b_mcs_1, jh_2_fin_a_mcs4_a1, jh2_fina_mcs4_a1a2, jh_2_arrest_fail_c, jh_2_celeb |
| 12 | Mr. Philips (`trevor1`, id 62) | Trevor | trevor_1_int, trv_1_mcs_1_p1, trv_1_mcs_3_concat, trv_1_mcs_4 |
| 13 | Nervous Ron (`trevor2`, id 63) | Trevor | trevor_2_int, trv_2_mcs_4_concat, trv_2_mcs_6, trv2_mcs_8 |

Mission IDs are the game's global mission IDs from the autosplitter's
`vars.missions` table (taken from `standard_global_init.c`). Play order
above follows the GTA Wiki mission list; Chop, Daddy's Little Girl and
Friend Request can be played in a different order in-game — the map is
keyed by mission **script**, not by a fixed sequence, so order does not
matter to the lookup.

Full detail per scene — description, condition, actors, playback-list
indices, flags, confidence, sources — is in
[`Shared/CutsceneMap.json`](Shared/CutsceneMap.json) and
[`Shared/CutsceneMap.cs`](Shared/CutsceneMap.cs). The two files are
generated from the same data and contain the same 62 names; if they ever
disagree, the JSON is the source of truth.

### Things the map deliberately does not claim

- **Loud vs Smart for the Jewel Store Job is unverified.** The script
  has approach A (`jh_2a_*`) and approach B (`jh_2b_*`); which one is
  Loud and which is Smart is not recorded anywhere fetched. The map
  records A / B only.
- **`jh_2_arrest_fail_c` and `jh_2_celeb`** — conditions ("played if
  arrested", "played on success") are inferred from the names only.
- **`les_1b_mcs_1`** (Friend Request, Tracy) is a branch/variant whose
  trigger condition is unknown.
- **`fam_1_ext_alt2` / `fam_1_ext_alt3`** (Father/Son) are alternate
  exterior variants; the condition that picks one is unknown.
- **In-mission order for Complications (`mcs_3`–`mcs_9`) is numeric
  order in the script**, not play-tested. The autosplitter only tracks
  the intro and the Simeon fight.
- **Cutscene names are case-insensitive in the game.** The map stores
  the lowercase canonical name plus `nameAsInScript` (the script's
  original case, e.g. `Armenian_2_int`).

## How it was built

1. **Ground truth: decompiled mission scripts.**
   `Sainan/GTA-V-Decompiled-Scripts`, `decompiled_scripts/<script>.c`
   (fetched 2026-11-21). Every cutscene name in the map was found as a
   `CUTSCENE::REQUEST_CUTSCENE("name", 8)` or
   `REQUEST_CUTSCENE_WITH_PLAYBACK_LIST("name", list, 8)` call in the
   mission's own script. Names passed through variables were resolved
   by tracing the assignment — e.g. `armenian1.c` sets
   `sLocal_2772 = "armenian_1_int"`, `sLocal_2773 =
   "armenian_1_mcs_1"`, `sLocal_2774 = "arm_1_mcs_2_concat"`;
   `jewelry_heist.c` sets `Local_3392.f_119`–`f_124` to the six
   approach/finale names. Two scripts use a proximity loader helper
   (`franklin0.c` func_77, `family2.c` func_98) that requests the name
   passed in by its caller — those names were resolved from the call
   sites.
2. **Name-list check.** Every name was checked against
   DurtyFree `cutsceneNames.json` (807 entries, 772 unique). 61 of 62
   are in it; the one exception is flagged `script-only` above.
3. **Cross-check: autosplitter.** `hoXyy/GTAV-AutoSplitter`
   `GTAV-Auto-Splitter.asl` — its `vars.missions` table gave the
   mission IDs, and its `vars.cutsceneNames` table gave a second
   source for 28 scenes (the labels are quoted verbatim in the JSON
   as `autosplitterLabel`, typos included — "Reposession"). The
   autosplitter only tracks scenes it splits on, so it is *not*
   complete: Complications, Friend Request, the Prologue and others
   have more cutscenes in their scripts than in its table.
4. **Mission identity / protagonists.** GTA Wiki, *Missions in GTA V*.
   Script-to-mission mapping is corroborated three ways: the script
   name itself (`armenian1` = Franklin & Lamar, etc.), the cutscene
   prefixes in it, and the autosplitter's mission table.
5. **Actors.** `REGISTER_ENTITY_FOR_CUTSCENE` lines were grouped by
   the function that requests each scene. Where the request and the
   registration live in different functions (Chop's post-intro scenes,
   Casing the Jewel Store, Marriage Counseling), the handles are listed
   at mission level and marked as such — not guessed per scene.

## Sources

- Decompiled scripts — https://github.com/Sainan/GTA-V-Decompiled-Scripts
  (`decompiled_scripts/prologue1.c`, `armenian1.c`, `armenian2.c`,
  `armenian3.c`, `franklin0.c`, `family1.c`, `family2.c`, `family3.c`,
  `lester1.c`, `jewelry_setup1.c`, `jewelry_heist.c`, `trevor1.c`,
  `trevor2.c`; master, fetched 2026-11-21)
- Autosplitter — https://raw.githubusercontent.com/hoXyy/GTAV-AutoSplitter/master/GTAV-Auto-Splitter.asl
- Cutscene names — https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/cutsceneNames.json
- Mission list / protagonists — https://gta.fandom.com/wiki/Missions_in_GTA_V
- Cutscene lookup (manual only, see below) — https://www.fivemad.com/docs/tools/cutscenes

## What is still missing

- **Origin XYZ + heading, and duration, for every scene.** No fetched
  source carries them. fivemad's Cutscene Browser lists them, but its
  pages are a JS app — the fetched HTML contains no cutscene data, so
  nothing could be extracted from it. Fill these in from (a) the
  CutsceneWatcher log: play the mission solo, note the player position
  and GameTime when the cutscene fires and ends; or (b) a manual
  fivemad lookup, one scene at a time, pasted into the JSON with
  source `fivemad-manual`.
- **Ped model names for the actors.** The map has cutscene handle
  strings (`Lamar`, `Jimmys_Car`), which is what
  `RegisterEntityForCutscene` wants. The actual ped model each handle
  maps to is determined by the entity the script passes in, not by the
  handle — resolving that needs tracing `CREATE_PED` calls in the
  script, which was not done.
- **Later missions.** Everything after Nervous Ron (Trevor Philips
  Industries, Crystal Maze, Friends Reunited, Fame or Shame, the FIB
  missions, the heists, the finales), plus Strangers & Freaks and the
  Lester assassinations. The autosplitter's table already lists
  cutscene names for most of these — expanding the map is the same
  procedure as below, one mission at a time. Not done here on purpose:
  add rows only as they are verified, and test one mission end-to-end
  before adding the next.
- **Missions with no cutscene in their script.** `jewelry_prep1a.c`
  and `jewelry_prep1b.c` (Jewel Store prep) contain no
  `REQUEST_CUTSCENE` calls at all — but which prep mission each file
  is (Carbine Rifles vs Bugstars Equipment) was not verified, so no
  "no cutscene" entry was added for them.
- **The V2 prototype map is superseded.** `V2-Prototype/CutsceneMap.cs`
  (5 missions, 9 cutscene names) is replaced by this map (13 missions,
  62 cutscenes). Do not compile both into the same assembly — both
  declare `StorySync.CutsceneMap`.

## How to add a new mission

1. Find the mission's script name. `all_script_names.txt` in the
   decompiled-scripts repo lists them (e.g. `franklin1` = Hood Safari,
   `lamar1` = The Long Stretch, `trevor3` = Trevor Philips Industries).
   Corroborate with the GTA Wiki mission list and the autosplitter's
   mission IDs.
2. Fetch the script:
   `https://raw.githubusercontent.com/Sainan/GTA-V-Decompiled-Scripts/master/decompiled_scripts/<name>.c`
3. Grep it for cutscene requests:
   `grep -n "REQUEST_CUTSCENE" <name>.c`
   Include `REQUEST_CUTSCENE_WITH_PLAYBACK_LIST` — several scenes are
   only requested that way (e.g. `pro_mcs_5`, `trevor_1_int`).
4. Resolve dynamic names: if a request passes a variable
   (`sLocal_*`, `sParam0`, `Local_*.f_*`), trace its assignments in
   the same file, or find the helper function's call sites and read
   the literal names passed in.
5. Cross-check every name against `cutsceneNames.json` (case-insensitive)
   and against the autosplitter's `cutsceneNames` table. A name in the
   script but not in the name list goes in as `script-only`, never as
   `verified`.
6. Get the actors: grep `REGISTER_ENTITY_FOR_CUTSCENE` in the same
   file and group the handles by the function that requests each scene.
   If the request and the registration are in different functions,
   list the handles at mission level with `actorsScope:
   "mission-level"`.
7. Add the entry to `Shared/CutsceneMap.json` (the source of truth),
   then regenerate `Shared/CutsceneMap.cs` from it — never edit the
   `.cs` by hand. Leave `origin`, `originHeading` and `durationMs`
   null until the watcher log or a manual fivemad lookup fills them
   in. Never invent coordinates, durations, or actor models.
8. Add the mission to the table at the top of this file.
