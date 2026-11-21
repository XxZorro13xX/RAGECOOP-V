// StorySync — static mission -> cutscene map (Shared).
//
// GENERATED from CutsceneMap.json in this same folder — do not edit this
// file by hand. Edit the JSON, re-run the generator, and keep the two
// in sync. Generated 2026-11-21.
//
// Scope: 13 missions, 62 cutscenes (Prologue through Nervous Ron).
// Legacy only (GTA5.exe). Base: Draythianna/RAGECOOP-V at 5df09a5.
// UNTESTED in-game: no dotnet on the Linux VM, client is net48 /
// Windows-only. See CUTSCENE-MAP.md for sources, confidence levels,
// and what is still missing (actors are cutscene handle strings,
// origins and durations are all null until the CutsceneWatcher log
// or a manual fivemad lookup fills them in).
//
// This file supersedes V2-Prototype/CutsceneMap.cs (5 missions, 9
// cutscene names, actors/origin null). Do NOT compile both files into
// the same assembly — both declare StorySync.CutsceneMap.
//
// Plain BCL only, C# 7.3, no GTA / SHVDN / RAGECOOP references, so it
// compiles into both the client (net48) and the server (net6.0).

using System;
using System.Collections.Generic;

namespace StorySync
{
    /// <summary>
    /// One cutscene inside one mission.
    /// Name is the canonical lowercase name REQUEST_CUTSCENE wants.
    /// Actors are cutscene entity handle strings from
    /// REGISTER_ENTITY_FOR_CUTSCENE — e.g. "Lamar", "Jimmys_Car" —
    /// NOT ped model names. ActorsScope is "per-cutscene" when the
    /// handles were in the same function that requests this scene,
    /// "mission-level" when they were seen in the script but could
    /// not be attributed to one scene, and null when none were found.
    /// Origin / OriginHeading / DurationMs are null until filled in
    /// from the CutsceneWatcher log or fivemad — never guess them.
    /// </summary>
    public sealed class CutsceneInfo
    {
        public string Name;
        public string NameAsInScript;
        public int Order;
        public string Description;
        public string Condition;
        public string[] Actors;
        public string ActorsScope;
        public float[] Origin; // XYZ, or null when unknown
        public float OriginHeading; // NaN when unknown
        public int DurationMs; // 0 when unknown
        public int[] PlaybackLists; // null when the script only requests it plain
        public int[] FlagsSeen;
        public string Confidence; // cross-checked | verified | script-only
    }

    /// <summary>
    /// One story mission and every cutscene its script requests.
    /// Keyed by mission script name (e.g. "armenian1"). MissionId is
    /// the game's global mission id from the autosplitter's table.
    /// </summary>
    public sealed class MissionCutscene
    {
        public int MissionId;
        public string MissionScript;
        public string DisplayName;
        public string Protagonist;
        public int PlayOrder;
        public string Confidence; // high | medium (see CUTSCENE-MAP.md)
        public CutsceneInfo[] Cutscenes;
    }

    public static class CutsceneMap
    {
        public static readonly Dictionary<string, MissionCutscene> ByScript =
            new Dictionary<string, MissionCutscene>(StringComparer.OrdinalIgnoreCase)
            {
                { "prologue1", new MissionCutscene
                    {
                        MissionId = 53,
                        MissionScript = "prologue1",
                        DisplayName = "Prologue",
                        Protagonist = "Michael, Trevor (2004)",
                        PlayOrder = 1,
                        Confidence = "high",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_1",
                                NameAsInScript = "pro_mcs_1",
                                Order = 1,
                                Description = "Bank interior: hostages tied up, security guard subdued.",
                                Condition = "intro — bank robbery in progress",
                                Actors = new string[] { "Michaels_weapon", "PRO_Male_Hostage_1", "PRO_Male_Hostage_2", "PRO_SecurityGuard_Tiedup", "Trevor", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 4, 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_2",
                                NameAsInScript = "pro_mcs_2",
                                Order = 2,
                                Description = "Bank interior, continued.",
                                Condition = "after pro_mcs_1",
                                Actors = new string[] { "Michaels_weapon", "Trevor", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_3_pt1",
                                NameAsInScript = "pro_mcs_3_pt1",
                                Order = 3,
                                Description = "Security guard grabs Michael.",
                                Condition = "after pro_mcs_2",
                                Actors = new string[] { "Michaels_weapon", "PRO_SecurityGuard_Grabs_mike", "Trevor", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_5",
                                NameAsInScript = "pro_mcs_5",
                                Order = 4,
                                Description = "Getting in the getaway vehicle.",
                                Condition = "getaway — requested with playback lists 57 and 62",
                                Actors = new string[] { "Brad", "Michael", "PRO_COP_Shooting_car", "PRO_Cop_Driving_Car", "PRO_Getaway_driver", "Trevor", "Trevors_weapon", "chase_cop_car", "getaway_car" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 57, 62 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_6",
                                NameAsInScript = "pro_mcs_6",
                                Order = 5,
                                Description = "The train crossing; Brad is shot.",
                                Condition = "after the getaway",
                                Actors = new string[] { "Brad", "Brads_Shotgun", "Daves_Rifle", "Michael", "Michaels_weapon", "Trevor", "Trevors_Heist_Bag", "Trevors_weapon", "getaway_car" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 4, 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "pro_mcs_7_concat",
                                NameAsInScript = "PRO_MCS_7_Concat",
                                Order = 6,
                                Description = "Trevor flees Ludendorff.",
                                Condition = "outro",
                                Actors = new string[] { "1st_car", "2nd_car", "3rd_car", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 2, 8 },
                                Confidence = "cross-checked",
                            },
                        }
                    }
                },
                { "armenian1", new MissionCutscene
                    {
                        MissionId = 0,
                        MissionScript = "armenian1",
                        DisplayName = "Franklin and Lamar",
                        Protagonist = "Franklin",
                        PlayOrder = 2,
                        Confidence = "high",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "armenian_1_int",
                                NameAsInScript = "armenian_1_int",
                                Order = 1,
                                Description = "Intro at Simeon's dealership.",
                                Condition = "mission start",
                                Actors = new string[] { "Lamar" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_1_mcs_1",
                                NameAsInScript = "armenian_1_mcs_1",
                                Order = 2,
                                Description = "Speak to Simeon.",
                                Condition = "after the intro",
                                Actors = new string[] { "Franklin_stealcar", "Jimmy", "Jimmys_Car", "Lamar", "Siemon", "bjxl^1", "ninef^1" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "arm_1_mcs_2_concat",
                                NameAsInScript = "arm_1_mcs_2_concat",
                                Order = 3,
                                Description = "Franklin at home, changes shirt (closet doors, shirt).",
                                Condition = "outro",
                                Actors = new string[] { "Closet_Door_L", "Closet_Door_R", "Franklins_shirt" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "armenian2", new MissionCutscene
                    {
                        MissionId = 1,
                        MissionScript = "armenian2",
                        DisplayName = "Repossession",
                        Protagonist = "Franklin",
                        PlayOrder = 3,
                        Confidence = "high",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "armenian_2_int",
                                NameAsInScript = "Armenian_2_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start",
                                Actors = new string[] { "Lamar", "Siemon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "arm_2_mcs_4",
                                NameAsInScript = "Arm_2_mcs_4",
                                Order = 2,
                                Description = "Confronted by Vagos.",
                                Condition = "mid-mission",
                                Actors = new string[] { "Franklins_weapon", "Lamar", "Lamars_weapon", "Mexgoon_1" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_2_mcs_6",
                                NameAsInScript = "Armenian_2_mcs_6",
                                Order = 3,
                                Description = "Bike repossession scene.",
                                Condition = "after the Vagos confrontation",
                                Actors = new string[] { "Armenian_bike", "Lamar" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "franklin0", new MissionCutscene
                    {
                        MissionId = 40,
                        MissionScript = "franklin0",
                        DisplayName = "Chop",
                        Protagonist = "Franklin",
                        PlayOrder = 4,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "fra_0_int",
                                NameAsInScript = "fra_0_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start — requested with playback lists 13 and 14",
                                Actors = new string[] { "Chop", "Franklin", "Lamar" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 13, 14 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "fra_0_mcs_1",
                                NameAsInScript = "fra_0_mcs_1",
                                Order = 2,
                                Description = "Confront D in the alley.",
                                Condition = "mid-mission (proximity-loaded)",
                                Actors = new string[] { "Ballas_OG", "BallasOG_Bike", "Chop", "Franklin", "Hoodrat_girl", "Kidnapping_van", "Lamar" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "fra_0_mcs_4_p2_t3",
                                NameAsInScript = "FRA_0_MCS_4_P2_T3",
                                Order = 3,
                                Description = "Put D in the van.",
                                Condition = "after confronting D",
                                Actors = new string[] { "Ballas_OG", "BallasOG_Bike", "Chop", "Franklin", "Hoodrat_girl", "Kidnapping_van", "Lamar" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "fra_0_mcs_5_p1",
                                NameAsInScript = "fra_0_mcs_5_p1",
                                Order = 4,
                                Description = "Unknown scene — in the script via the proximity loader, no autosplitter label.",
                                Condition = "unknown",
                                Actors = new string[] { "Ballas_OG", "BallasOG_Bike", "Chop", "Franklin", "Hoodrat_girl", "Kidnapping_van", "Lamar" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "armenian3", new MissionCutscene
                    {
                        MissionId = 2,
                        MissionScript = "armenian3",
                        DisplayName = "Complications",
                        Protagonist = "Franklin (Michael introduced)",
                        PlayOrder = 5,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "armenian_3_int",
                                NameAsInScript = "Armenian_3_int",
                                Order = 1,
                                Description = "Start of the mission at the dealership.",
                                Condition = "mission start",
                                Actors = new string[] { "Showroom_Car", "Siemon", "customer" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_3",
                                NameAsInScript = "Armenian_3_mcs_3",
                                Order = 2,
                                Description = "House interior sequence (tennis coach).",
                                Condition = "after the intro — shared function with mcs_4/mcs_5",
                                Actors = new string[] { "Amanda", "Jimmy", "TennisCoach", "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_4",
                                NameAsInScript = "Armenian_3_mcs_4",
                                Order = 3,
                                Description = "House interior sequence (tennis coach).",
                                Condition = "after mcs_3 — shared function",
                                Actors = new string[] { "Amanda", "Jimmy", "TennisCoach", "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_5",
                                NameAsInScript = "Armenian_3_mcs_5",
                                Order = 4,
                                Description = "House interior sequence (tennis coach).",
                                Condition = "after mcs_4 — shared function",
                                Actors = new string[] { "Amanda", "Jimmy", "TennisCoach", "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_6",
                                NameAsInScript = "Armenian_3_mcs_6",
                                Order = 5,
                                Description = "Chase/escape with Michael in the car.",
                                Condition = "after the house sequence",
                                Actors = new string[] { "Franklin", "Jimmys_Car", "Michael" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_7",
                                NameAsInScript = "Armenian_3_mcs_7",
                                Order = 6,
                                Description = "Chase/escape, continued.",
                                Condition = "after mcs_6",
                                Actors = new string[] { "Franklins_Car", "Michael" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_8",
                                NameAsInScript = "Armenian_3_mcs_8",
                                Order = 7,
                                Description = "Start of the Simeon fight.",
                                Condition = "after the chase",
                                Actors = new string[] { "Franklin", "Jimmys_Car", "Siemon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 4, 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "armenian_3_mcs_9_concat",
                                NameAsInScript = "Armenian_3_MCS_9_concat",
                                Order = 8,
                                Description = "Outro (Simeon, Jimmy's car).",
                                Condition = "mission end",
                                Actors = new string[] { "Jimmys_Car", "Siemon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "family1", new MissionCutscene
                    {
                        MissionId = 17,
                        MissionScript = "family1",
                        DisplayName = "Father/Son",
                        Protagonist = "Michael (+ Franklin)",
                        PlayOrder = 6,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "family_1_int",
                                NameAsInScript = "family_1_int",
                                Order = 1,
                                Description = "Intro — Michael and Franklin at the house.",
                                Condition = "mission start — also requested with playback list 254",
                                Actors = new string[] { "Franklin", "Michael" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 254 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "fam_1_mcs_2",
                                NameAsInScript = "FAM_1_MCS_2",
                                Order = 2,
                                Description = "Arrive at Los Santos Customs.",
                                Condition = "mid-mission",
                                Actors = new string[] { "Amandas_car", "Franklin", "Jimmy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "fam_1_ext_2",
                                NameAsInScript = "FAM_1_EXT_2",
                                Order = 3,
                                Description = "Exterior shot (Amanda's car, Jimmy).",
                                Condition = "exterior variant — condition unknown",
                                Actors = new string[] { "Amandas_car", "Jimmy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "fam_1_ext_alt2",
                                NameAsInScript = "FAM_1_EXT_ALT2",
                                Order = 4,
                                Description = "Alternate exterior shot.",
                                Condition = "alternate variant — condition unknown",
                                Actors = new string[] { "AMANDAS_CAR", "JIMMY" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "fam_1_ext_alt3",
                                NameAsInScript = "FAM_1_EXT_ALT3",
                                Order = 5,
                                Description = "Alternate exterior shot.",
                                Condition = "alternate variant — condition unknown",
                                Actors = new string[] { "AMANDAS_CAR", "JIMMY" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "family3", new MissionCutscene
                    {
                        MissionId = 19,
                        MissionScript = "family3",
                        DisplayName = "Marriage Counseling",
                        Protagonist = "Michael (+ Franklin)",
                        PlayOrder = 7,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "family_3_int",
                                NameAsInScript = "family_3_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start — requested plain and with playback lists 1 and 24",
                                Actors = new string[] { "Franklin", "Michael", "TennisCoach", "Racket_1", "Racket_2", "Madrazos_Car", "Fam3_tow_truck" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 1, 24 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "fam_3_mcs_1",
                                NameAsInScript = "fam_3_mcs_1",
                                Order = 2,
                                Description = "Pull down the house.",
                                Condition = "mid-mission",
                                Actors = new string[] { "Franklin", "Michael", "TennisCoach", "Racket_1", "Racket_2", "Madrazos_Car", "Fam3_tow_truck" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "family_3_ext",
                                NameAsInScript = "family_3_ext",
                                Order = 3,
                                Description = "Exterior shot — scene unknown.",
                                Condition = "unknown",
                                Actors = new string[] { "Franklin", "Michael", "TennisCoach", "Racket_1", "Racket_2", "Madrazos_Car", "Fam3_tow_truck" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "family2", new MissionCutscene
                    {
                        MissionId = 18,
                        MissionScript = "family2",
                        DisplayName = "Daddy's Little Girl",
                        Protagonist = "Michael",
                        PlayOrder = 8,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "family_2_int",
                                NameAsInScript = "family_2_int",
                                Order = 1,
                                Description = "Intro — Michael and Jimmy.",
                                Condition = "mission start",
                                Actors = new string[] { "Jimmy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "family_2_mcs_2",
                                NameAsInScript = "family_2_mcs_2",
                                Order = 2,
                                Description = "Finish the bike ride.",
                                Condition = "after the bike ride — requested with playback lists 5 and 6",
                                Actors = new string[] { "FAM_2_Jimmys_bike", "Jimmy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 5, 6 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "family_2_mcs_3",
                                NameAsInScript = "family_2_mcs_3",
                                Order = 3,
                                Description = "Finish swimming.",
                                Condition = "after swimming",
                                Actors = new string[] { "Girl_being_chatted_up", "Girl_on_sofa", "Guy_on_sofa", "Man_getting_wood", "Michaels_seashark", "Porn_Dude", "Porn_Dude^1", "Porn_Dude_^1_seashark", "Porn_Dude_seashark", "Prop_MP3_Dock", "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "family_2_mcs_4",
                                NameAsInScript = "family_2_mcs_4",
                                Order = 4,
                                Description = "Tracey on the boat (with Jimmy and the seashark).",
                                Condition = "after swimming — also requested with playback list 2",
                                Actors = new string[] { "Jimmy", "Michaels_seashark", "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 2 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "family_2_mcs_4p2",
                                NameAsInScript = "family_2_mcs_4p2",
                                Order = 5,
                                Description = "Part 2 of the boat scene — script-only, NOT in the game's cutscene name list.",
                                Condition = "after mcs_4 (inferred from the name)",
                                Actors = new string[] { "Jimmy", "Michaels_seashark", "Tracy" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "script-only",
                            },
                        }
                    }
                },
                { "lester1", new MissionCutscene
                    {
                        MissionId = 44,
                        MissionScript = "lester1",
                        DisplayName = "Friend Request",
                        Protagonist = "Michael",
                        PlayOrder = 9,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "lester_1_int",
                                NameAsInScript = "lester_1_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start",
                                Actors = null,
                                ActorsScope = null,
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1a_mcs_0",
                                NameAsInScript = "LES_1A_MCS_0",
                                Order = 2,
                                Description = "Clothes store.",
                                Condition = "after the intro",
                                Actors = new string[] { "Michael", "Shop_Assistant" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1a_mcs_1",
                                NameAsInScript = "LES_1A_MCS_1",
                                Order = 3,
                                Description = "Enter Lifeinvader.",
                                Condition = "after the clothes store",
                                Actors = new string[] { "Life_invader_Engineer" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1a_mcs_2",
                                NameAsInScript = "LES_1A_MCS_2",
                                Order = 4,
                                Description = "Lifeinvader interior.",
                                Condition = "after entering Lifeinvader",
                                Actors = new string[] { "LifeInvad_Chair", "Life_invader_Engineer" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1a_mcs_3",
                                NameAsInScript = "LES_1A_MCS_3",
                                Order = 5,
                                Description = "Finish the popups.",
                                Condition = "after the interior scenes",
                                Actors = new string[] { "LifeInvad_Chair", "Life_invader_Engineer" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1a_mcs_4",
                                NameAsInScript = "LES_1A_MCS_4",
                                Order = 6,
                                Description = "Lifeinvader, continued.",
                                Condition = "after mcs_3",
                                Actors = null,
                                ActorsScope = null,
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "les_1b_mcs_1",
                                NameAsInScript = "LES_1B_MCS_1",
                                Order = 7,
                                Description = "Variant scene with Tracy.",
                                Condition = "branch/variant — condition unknown",
                                Actors = new string[] { "Tracy" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "jewelry_setup1", new MissionCutscene
                    {
                        MissionId = 86,
                        MissionScript = "jewelry_setup1",
                        DisplayName = "Casing the Jewel Store",
                        Protagonist = "Michael (+ Lester)",
                        PlayOrder = 10,
                        Confidence = "high",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "jh_1_int",
                                NameAsInScript = "Jh_1_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start",
                                Actors = new string[] { "Lester", "Michael", "Michaels_car", "Jewellery_Assitance" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_1_ig_3",
                                NameAsInScript = "JH_1_IG_3",
                                Order = 2,
                                Description = "Greet the employee.",
                                Condition = "inside the store",
                                Actors = new string[] { "Lester", "Michael", "Michaels_car", "Jewellery_Assitance" },
                                ActorsScope = "mission-level",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                        }
                    }
                },
                { "jewelry_heist", new MissionCutscene
                    {
                        MissionId = 90,
                        MissionScript = "jewelry_heist",
                        DisplayName = "The Jewel Store Job",
                        Protagonist = "Michael, Franklin (+ crew)",
                        PlayOrder = 11,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "jh_2a_intp4",
                                NameAsInScript = "JH_2A_INTp4",
                                Order = 1,
                                Description = "Heist intro, approach A.",
                                Condition = "mission start — approach A chosen; also requested with playback lists 71 and 120",
                                Actors = new string[] { "Driver_selection", "Franklin", "Hacker_selection", "Lester", "WalkingStick_lester", "gunman_selection_1", "seamstress_near_door", "seamstress_near_michael" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 71, 120 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2b_int",
                                NameAsInScript = "jh_2b_int",
                                Order = 1,
                                Description = "Heist intro, approach B.",
                                Condition = "mission start — approach B chosen",
                                Actors = new string[] { "Driver_selection", "Franklin", "Hacker_selection", "Lester", "WalkingStick_lester", "gunman_selection_1", "seamstress_near_door", "seamstress_near_michael" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2a_mcs_1",
                                NameAsInScript = "JH_2A_MCS_1",
                                Order = 2,
                                Description = "Inside the store, approach A.",
                                Condition = "after the approach A intro",
                                Actors = new string[] { "HC_GUNMAN1_2_HANDEDWEAPON", "HC_GUNMAN_2_HANDEDWEAPON", "Michael", "Michaels_2_handedweapon", "customer_1_withassist_1", "customer_2_withassist_2", "driver_selection", "gunman_selection_1", "jewellery_Assitance", "security", "shop_assistant" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2b_mcs_1",
                                NameAsInScript = "JH_2B_MCS_1",
                                Order = 2,
                                Description = "Inside the store, approach B.",
                                Condition = "after the approach B intro",
                                Actors = new string[] { "BURRITO3", "Franklin", "HC_GUNMAN1_2_HANDEDWEAPON", "HC_GUNMAN_2_HANDEDWEAPON", "Jewellery_Assitance", "Michaels_weapon", "angry_customer", "customer", "driver_selection", "gunman_selection_1", "security_guard", "shop_assistant" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2_fin_a_mcs4_a1",
                                NameAsInScript = "Jh_2_fin_a_mcs4_a1",
                                Order = 3,
                                Description = "Escape, approach A.",
                                Condition = "after the store — shared function with the A1A2 finale",
                                Actors = new string[] { "Franklins_Heist_Bike", "Michael", "Traffic_Warden" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh2_fina_mcs4_a1a2",
                                NameAsInScript = "Jh2_fina_mcs4_a1a2",
                                Order = 3,
                                Description = "Leave the jewelry store (finale).",
                                Condition = "after the store",
                                Actors = new string[] { "Franklins_Heist_Bike", "Michael", "Traffic_Warden" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2_arrest_fail_c",
                                NameAsInScript = "JH_2_ARREST_FAIL_C",
                                Order = 4,
                                Description = "Arrest / fail scene.",
                                Condition = "played if arrested during the heist (inferred from the name — unverified)",
                                Actors = new string[] { "Franklin", "driver_selection", "drivers_bati", "drivers_bike", "franklins_getaway_bati", "franklins_getaway_bike", "gunman_selection_1", "gunmans_bati", "gunmans_bike" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "jh_2_celeb",
                                NameAsInScript = "JH_2_CELEB",
                                Order = 5,
                                Description = "Celebration scene.",
                                Condition = "played on heist success (inferred from the name — unverified)",
                                Actors = new string[] { "gunman_selection_1" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "trevor1", new MissionCutscene
                    {
                        MissionId = 62,
                        MissionScript = "trevor1",
                        DisplayName = "Mr. Philips",
                        Protagonist = "Trevor",
                        PlayOrder = 12,
                        Confidence = "high",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "trevor_1_int",
                                NameAsInScript = "TREVOR_1_INT",
                                Order = 1,
                                Description = "Intro — Trevor, Johnny, Michael, Franklin, Ron, Wade, Ashley.",
                                Condition = "mission start — requested with playback lists 8183, 8180 and 8176",
                                Actors = new string[] { "Ashley", "Franklin", "Johnny", "Michael", "Ron", "Wade" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 8183, 8180, 8176 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv_1_mcs_1_p1",
                                NameAsInScript = "TRV_1_MCS_1_P1",
                                Order = 2,
                                Description = "Finish the first drive (Lost MC / Wade / Ron).",
                                Condition = "after the intro",
                                Actors = new string[] { "Clay", "Clays_bike", "Lost_Van", "Lost_biker", "Other_Lost_biker", "Ron", "Terry", "Terrys_bike", "Trevor", "Trevors_car", "Wade" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv_1_mcs_3_concat",
                                NameAsInScript = "TRV_1_MCS_3_CONCAT",
                                Order = 3,
                                Description = "Smash the trailer (Ortega).",
                                Condition = "after the first drive",
                                Actors = new string[] { "Ortega", "Ortega_Trailer", "Ron", "Trevors_car", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv_1_mcs_4",
                                NameAsInScript = "TRV_1_MCS_4",
                                Order = 4,
                                Description = "Outro (Ron, Trevor's car).",
                                Condition = "mission end",
                                Actors = new string[] { "Ron", "Trevors_car" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
                { "trevor2", new MissionCutscene
                    {
                        MissionId = 63,
                        MissionScript = "trevor2",
                        DisplayName = "Nervous Ron",
                        Protagonist = "Trevor",
                        PlayOrder = 13,
                        Confidence = "medium",
                        Cutscenes = new CutsceneInfo[]
                        {
                            new CutsceneInfo
                            {
                                Name = "trevor_2_int",
                                NameAsInScript = "trevor_2_int",
                                Order = 1,
                                Description = "Start of the mission.",
                                Condition = "mission start",
                                Actors = new string[] { "Ron" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv_2_mcs_4_concat",
                                NameAsInScript = "trv_2_mcs_4_concat",
                                Order = 2,
                                Description = "Shoot down the chopper.",
                                Condition = "mid-mission — requested with playback lists 15 and 23",
                                Actors = null,
                                ActorsScope = null,
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 15, 23 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv_2_mcs_6",
                                NameAsInScript = "TRV_2_MCS_6",
                                Order = 3,
                                Description = "Board the plane.",
                                Condition = "after the chopper",
                                Actors = new string[] { "Lost_biker", "Rons_Plane", "Trevors_Plane", "Trevors_weapon" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = null,
                                FlagsSeen = new int[] { 8 },
                                Confidence = "cross-checked",
                            },
                            new CutsceneInfo
                            {
                                Name = "trv2_mcs_8",
                                NameAsInScript = "TRV2_MCS_8",
                                Order = 4,
                                Description = "Plane sequence (Ron, Trevor, planes).",
                                Condition = "unknown — requested with playback list 3",
                                Actors = new string[] { "Ron", "Rons_plane", "Trevor", "Trevors_plane" },
                                ActorsScope = "per-cutscene",
                                Origin = null,
                                OriginHeading = float.NaN,
                                DurationMs = 0,
                                PlaybackLists = new int[] { 3 },
                                FlagsSeen = new int[] { 8 },
                                Confidence = "verified",
                            },
                        }
                    }
                },
            };

        public static MissionCutscene FindByScript(string missionScript)
        {
            if (string.IsNullOrEmpty(missionScript))
            {
                return null;
            }
            MissionCutscene mission;
            if (ByScript.TryGetValue(missionScript, out mission))
            {
                return mission;
            }
            return null;
        }

        public static CutsceneInfo FindByName(string cutsceneName)
        {
            if (string.IsNullOrEmpty(cutsceneName))
            {
                return null;
            }
            foreach (MissionCutscene mission in ByScript.Values)
            {
                if (mission.Cutscenes == null)
                {
                    continue;
                }
                for (int i = 0; i < mission.Cutscenes.Length; i++)
                {
                    if (string.Equals(mission.Cutscenes[i].Name, cutsceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        return mission.Cutscenes[i];
                    }
                }
            }
            return null;
        }

        public static bool IsKnownCutscene(string cutsceneName)
        {
            return FindByName(cutsceneName) != null;
        }

        /// <summary>
        /// True only for cutscenes safe to play back by name: the name
        /// was verified in the decompiled script AND in the game's
        /// cutscene name list. "script-only" entries (in the script
        /// but not in the name list) return false — verify in-game
        /// before relying on them.
        /// </summary>
        public static bool IsPlayableCutscene(string cutsceneName)
        {
            CutsceneInfo info = FindByName(cutsceneName);
            return info != null && !string.Equals(info.Confidence, "script-only", StringComparison.Ordinal);
        }
    }
}
