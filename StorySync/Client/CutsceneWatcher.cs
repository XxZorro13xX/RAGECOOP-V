// StorySync — CutsceneWatcher (memory-reader prototype).
//
// PROTOTYPE ONLY: NOT wired into StorySync.sln / the .csproj files
// yet, and NOT tested in-game. This VM has no dotnet and no GTA V.
// Do not wire it in until the test in CUTSCENE-WATCHER.md has been
// run on Windows, Legacy. Wiring is one line in
// Client/StorySync.Client.csproj plus two lines in
// MissionSyncClient.cs — see CUTSCENE-WATCHER.md.
//
// What it does:
//   Polls GTA.Game.IsCutsceneActive on the script thread (the same
//   thread MissionSyncClient already polls Game.IsMissionActive on)
//   and logs every cutscene edge: started / ended, name (if the
//   memory reader is working), Game.GameTime, cutscene time and
//   total duration from the natives, whether it was skipped, and
//   the player's position. It also shows a GTA notification with
//   the detected name and appends every event to
//   Scripts\RageCoop\StorySync-Cutscene.log in the GTA folder.
//
//   The name comes from game memory, the way the LiveSplit
//   autosplitter does it — NOT from any native, because no native
//   returns a cutscene name. See the pattern constants below.
//
// PATTERN STATUS — READ THIS BEFORE ANYTHING ELSE:
//   NO verified CutSceneManager pattern exists for Legacy 3411
//   (or 3258) in any public source checked 2026-11-21. The
//   hoXyy / TheStonedTurtle autosplitter is explicitly for game
//   v1.27 only ("It only works on Patch 1.27") and still carries
//   the v1.27 offsets: name string at [GTA5.exe+0x01CB44A0]+0xB70,
//   in-cutscene byte at 0x1CB4472. Those addresses are WRONG on
//   Legacy and are NEVER read by this file.
//   CutsceneManagerPattern is therefore null on purpose. With no
//   pattern the watcher runs in NATIVE-ONLY mode: it still logs
//   start/end, times, duration and skipped, but the name reads as
//   "(unknown — memory reader disabled)". That mode is useful on
//   day one and can never crash on a bad pointer. Fill the pattern
//   in only by following the re-find procedure in
//   CUTSCENE-WATCHER.md and verifying it reads back a known name
//   (e.g. "armenian_1_mcs_1") during a known cutscene.
//
// Safety contract (same as MissionSyncClient):
//   - No static constructor, no static state. All state is
//     instance-level, so a failure here cannot break RAGECOOP.
//   - Tick() never throws. Every native call and every memory
//     read is individually try/catch-guarded.
//   - If Game.FindPattern returns IntPtr.Zero, the watcher logs
//     once ("CutsceneWatcher disabled: pattern not found for this
//     build") and keeps running in native-only mode.
//   - Memory reads use System.Runtime.InteropServices.Marshal,
//     never unsafe pointers, so no AllowUnsafeBlocks is needed.
//     Every byte read is bounds-checked and every character of a
//     candidate name is validated (a-z, 0-9, underscore only) —
//     garbage from a wrong address is rejected, never logged as
//     a real cutscene name.
//   - Server send is OFF by default. The server has no handler
//     for CutsceneDetected yet; see the event note below.
//
// Threading: Tick() must be called from the script thread (from
//   MissionSyncClient.OnTick). GTA natives and
//   GTA.UI.Notification.Show are safe there. No background
//   threads are started.
//
// C# 7.3 (matches StorySync.Client, net48).

using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Client.Scripting;
using RageCoop.Core.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace StorySync.Client
{
    public sealed class CutsceneWatcher
    {
        // ------------------------------------------------------------------
        // Pattern / layout constants — TODO-VERIFY, see header.
        // ------------------------------------------------------------------

        // Best candidate, from the LiveSplit autosplitter (v1.27 only):
        //   https://raw.githubusercontent.com/hoXyy/GTAV-AutoSplitter/master/GTAV-Auto-Splitter.asl
        //   "current loaded cutscene"  string255 c: 0x01CB44A0, 0xB70
        //   "in cutscene"              byte in_c: 0x1CB4472
        // The 0xB70 is the offset of the name buffer inside the
        // CutSceneManager struct on v1.27. Struct layout can shift
        // between builds, so treat it as a candidate to re-check,
        // not a fact: when the pattern is found, play a known
        // cutscene and confirm the string at manager+0xB70 reads
        // back as that cutscene's name. If it reads garbage, the
        // offset moved — scan nearby offsets in steps of 8.
        private const int CutsceneNameOffset = 0xB70; // TODO-VERIFY (v1.27)

        // TODO-VERIFY: no byte pattern for the CutSceneManager
        // global is known for Legacy 3411 / 3258 (searched
        // 2026-11-21: autosplitter, Menyoo, ChaosMod, SHVDN
        // sources — none publish one). Leave null until the
        // re-find procedure in CUTSCENE-WATCHER.md produces one.
        // When filled in, use the classic pattern+mask form that
        // Game.FindPattern(pattern, mask) and
        // RageCoop.Client.Util.Memory already use, e.g. a
        // RIP-relative LEA to the manager global. Do NOT paste a
        // guessed pattern here — a wrong pattern that happens to
        // match somewhere else reads garbage, and the name
        // validator below is the only thing standing between that
        // garbage and the log.
        private const string CutsceneManagerPattern = null; // TODO-VERIFY
        private const string CutsceneManagerMask = null;    // TODO-VERIFY

        // TODO-VERIFY: how to turn the pattern match into the
        // manager address. The usual form is a RIP-relative
        // instruction (e.g. 48 8B 05 ?? ?? ?? ?? — mov rax,
        // [rip+rel32], 7 bytes, rel32 at +3): manager =
        // match + rel32 + instructionLength. These defaults are
        // the standard LEA form, but they are INERT while the
        // pattern above is null — measure them from the actual
        // disassembly when the pattern is found, do not assume.
        private const int PatternRelOffset = 3;         // TODO-VERIFY
        private const int PatternInstructionLength = 7; // TODO-VERIFY

        private const int MaxNameLength = 255; // LiveSplit reads string255
        private const int MaxValidNameLength = 64; // real names are far shorter

        // Log file, relative to the GTA folder (same convention as
        // RageCoop's "Scripts\RageCoop\..." settings path).
        public const string LogFileName = "Scripts\\RageCoop\\StorySync-Cutscene.log";

        // ------------------------------------------------------------------
        // Server event (NOT wired up yet — log-only by default).
        // ------------------------------------------------------------------
        // Event name reserved for the future server handler:
        //   "StorySync.CutsceneDetected"
        //   payload: (string username, string cutsceneName, int totalDurationMs)
        //   totalDurationMs is -1 when the total duration native
        //   could not be read. Do NOT add this to
        //   Shared/StorySyncEvents.cs or turn SendToServer on until
        //   the server has a handler that does something with it;
        //   the hash is defined here so the client side is ready.
        //   Sending is off by default and never breaks v1: the
        //   mission lock does not depend on this event in any way.
        public const string CutsceneDetectedName = "StorySync.CutsceneDetected";
        public static readonly int CutsceneDetected = CustomEvents.Hash(CutsceneDetectedName);

        private readonly Action<string> _log;
        private readonly Func<string> _usernameProvider;
        private readonly HashSet<string> _loggedOnce = new HashSet<string>();
        private readonly object _fileLock = new object();

        private bool _stopped;
        private bool _resolveTried;
        private IntPtr _nameAddress = IntPtr.Zero;

        private bool _wasInCutscene;
        private string _lastName; // null while the name is still resolving
        private bool _namePending;
        private int _startedGameTime;
        private int _startedCutsceneTimeMs;
        private int _lastCutsceneTimeMs;
        private int _lastTotalDurationMs = -1;
        private bool _wasSkipped;
        private Vector3 _startedPosition = Vector3.Zero;

        /// <summary>True once the pattern resolved and the name address is set.</summary>
        public bool MemoryReaderActive
        {
            get { return _nameAddress != IntPtr.Zero; }
        }

        /// <summary>Last cutscene name detected, or null if none yet.</summary>
        public string LastDetectedName
        {
            get { return _lastName; }
        }

        /// <summary>
        /// Send CutsceneDetected to the server. Default false.
        /// Only set true after the server handler exists.
        /// </summary>
        public bool SendToServer { get; set; }

        public CutsceneWatcher(Action<string> log, Func<string> usernameProvider)
        {
            _log = log ?? delegate (string s) { };
            _usernameProvider = usernameProvider;
        }

        public CutsceneWatcher(Action<string> log) : this(log, null)
        {
        }

        public void Stop()
        {
            _stopped = true;
        }

        /// <summary>
        /// One-line status for debugging, e.g. in the client log.
        /// </summary>
        public string Status
        {
            get
            {
                return "CutsceneWatcher: memoryReaderActive=" + MemoryReaderActive
                    + " inCutscene=" + _wasInCutscene
                    + " lastName=" + (_lastName ?? "(none)")
                    + " sendToServer=" + SendToServer;
            }
        }

        // ------------------------------------------------------------------
        // Tick — call once per tick from MissionSyncClient.OnTick
        // (script thread). Never throws.
        // ------------------------------------------------------------------

        public void Tick()
        {
            if (_stopped)
            {
                return;
            }

            try
            {
                if (!_resolveTried)
                {
                    _resolveTried = true;
                    TryResolve();
                }

                bool inCutscene;
                try
                {
                    inCutscene = Game.IsCutsceneActive;
                }
                catch (Exception ex)
                {
                    LogOnce("iscutsceneactive", "CutsceneWatcher: Game.IsCutsceneActive failed: " + ex.Message);
                    return;
                }

                // While a cutscene is active, keep the time / duration /
                // skipped flags fresh — they are only meaningful now.
                if (inCutscene)
                {
                    _lastCutsceneTimeMs = ReadCutsceneTime();
                    int total = ReadTotalDuration();
                    if (total >= 0)
                    {
                        _lastTotalDurationMs = total;
                    }
                    if (ReadWasSkipped())
                    {
                        _wasSkipped = true; // latch: the flag may reset after the end
                    }
                }

                if (inCutscene == _wasInCutscene)
                {
                    // No edge. If the name was still resolving when the
                    // cutscene started, keep trying — the string may
                    // appear a few ticks after IsCutsceneActive flips.
                    if (inCutscene && _namePending)
                    {
                        string resolved = ReadName();
                        if (resolved != null)
                        {
                            _namePending = false;
                            _lastName = resolved;
                            Log("CutsceneWatcher: name resolved after start: " + resolved);
                            Notify("~y~Cutscene detected: ~s~" + resolved);
                            SendDetected(resolved, _lastTotalDurationMs);
                        }
                    }
                    return;
                }

                _wasInCutscene = inCutscene;

                if (inCutscene)
                {
                    OnCutsceneStarted();
                }
                else
                {
                    OnCutsceneEnded();
                }
            }
            catch (Exception ex)
            {
                // A tick handler must never throw: it would break the
                // whole RAGECOOP tick for every resource.
                LogOnce("tick", "CutsceneWatcher: Tick failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Edges
        // ------------------------------------------------------------------

        private void OnCutsceneStarted()
        {
            _startedGameTime = Game.GameTime;
            _startedCutsceneTimeMs = ReadCutsceneTime();
            _lastCutsceneTimeMs = _startedCutsceneTimeMs;
            _lastTotalDurationMs = ReadTotalDuration();
            _wasSkipped = ReadWasSkipped();
            _lastName = null;
            _namePending = false;

            try
            {
                Ped character = Game.Player != null ? Game.Player.Character : null;
                _startedPosition = (character != null && character.Exists())
                    ? character.Position : Vector3.Zero;
            }
            catch (Exception)
            {
                _startedPosition = Vector3.Zero; // position is best-effort
            }

            bool missionActive = false;
            try { missionActive = Game.IsMissionActive; } catch (Exception) { /* best-effort */ }

            string name = ReadName();
            string line;
            if (name != null)
            {
                _lastName = name;
                line = "CutsceneWatcher: START name=" + name
                    + " gameTime=" + _startedGameTime
                    + " cutsceneTimeMs=" + _startedCutsceneTimeMs
                    + " totalDurationMs=" + _lastTotalDurationMs
                    + " missionActive=" + missionActive
                    + " pos=" + _startedPosition;
                Log(line);
                Notify("~y~Cutscene detected: ~s~" + name);
                SendDetected(name, _lastTotalDurationMs);
            }
            else if (MemoryReaderActive)
            {
                // Reader resolved but the string is empty right now:
                // the cutscene is still loading. Retry on later ticks.
                _namePending = true;
                line = "CutsceneWatcher: START name=(resolving…) gameTime=" + _startedGameTime
                    + " totalDurationMs=" + _lastTotalDurationMs
                    + " missionActive=" + missionActive
                    + " pos=" + _startedPosition;
                Log(line);
            }
            else
            {
                // Native-only mode: no pattern, so no name — expected
                // until the TODO-VERIFY pattern is filled in.
                line = "CutsceneWatcher: START name=(unknown — memory reader disabled) gameTime="
                    + _startedGameTime
                    + " totalDurationMs=" + _lastTotalDurationMs
                    + " missionActive=" + missionActive
                    + " pos=" + _startedPosition;
                Log(line);
                Notify("~y~Cutscene detected ~s~(name unknown — memory reader disabled)");
            }
        }

        private void OnCutsceneEnded()
        {
            int durationMs = Game.GameTime - _startedGameTime;
            if (durationMs < 0)
            {
                durationMs = -1; // Game.GameTime wrapped; don't report nonsense
            }

            // The name may have changed mid-cutscene (concat scenes
            // such as pro_mcs_7_concat): log the last one seen.
            string name = _lastName ?? "(unknown)";
            string line = "CutsceneWatcher: END name=" + name
                + " durationMs~" + durationMs
                + " cutsceneTimeMs=" + _lastCutsceneTimeMs
                + " totalDurationMs=" + _lastTotalDurationMs
                + " skipped=" + _wasSkipped
                + " startedPos=" + _startedPosition;
            Log(line);

            _lastName = null;
            _namePending = false;
            _wasSkipped = false;
            _lastTotalDurationMs = -1;
        }

        // ------------------------------------------------------------------
        // Memory reader
        // ------------------------------------------------------------------

        private void TryResolve()
        {
            try
            {
                if (string.IsNullOrEmpty(CutsceneManagerPattern)
                    || string.IsNullOrEmpty(CutsceneManagerMask))
                {
                    LogOnce("no-pattern",
                        "CutsceneWatcher disabled: no CutSceneManager pattern configured for this build "
                        + "(TODO-VERIFY, see CUTSCENE-WATCHER.md). Running in native-only mode: "
                        + "start/end, times and skipped are still logged, but the cutscene name is unknown.");
                    _nameAddress = IntPtr.Zero;
                    return;
                }

                IntPtr match;
                try
                {
                    match = Game.FindPattern(CutsceneManagerPattern, CutsceneManagerMask);
                }
                catch (Exception ex)
                {
                    LogOnce("findpattern", "CutsceneWatcher: Game.FindPattern failed: " + ex.Message);
                    _nameAddress = IntPtr.Zero;
                    return;
                }

                if (match == IntPtr.Zero)
                {
                    LogOnce("pattern-not-found",
                        "CutsceneWatcher disabled: pattern not found for this build. "
                        + "Running in native-only mode (name unknown).");
                    _nameAddress = IntPtr.Zero;
                    return;
                }

                // RIP-relative resolution, same shape as
                // RageCoop.Client.Util.Memory: manager = match +
                // rel32 + instructionLength. All three inputs are
                // TODO-VERIFY — if any is wrong the validator in
                // ReadName() rejects the garbage instead of logging it.
                int rel32 = Marshal.ReadInt32(match + PatternRelOffset);
                IntPtr manager = match + rel32 + PatternInstructionLength;
                if (manager == IntPtr.Zero)
                {
                    LogOnce("manager-zero", "CutsceneWatcher: resolved manager pointer is zero; native-only mode.");
                    _nameAddress = IntPtr.Zero;
                    return;
                }

                IntPtr managerStruct = Marshal.ReadIntPtr(manager);
                if (managerStruct == IntPtr.Zero)
                {
                    LogOnce("struct-zero", "CutsceneWatcher: CutSceneManager struct pointer is zero; native-only mode.");
                    _nameAddress = IntPtr.Zero;
                    return;
                }

                _nameAddress = managerStruct + CutsceneNameOffset;
                Log("CutsceneWatcher: memory reader resolved, name address=" + _nameAddress.ToString("X")
                    + " (TODO-VERIFY: confirm it reads a real cutscene name during a known cutscene).");
            }
            catch (Exception ex)
            {
                // Resolution must never take the watcher (or the
                // client) down with it.
                LogOnce("resolve", "CutsceneWatcher: TryResolve failed: " + ex.Message);
                _nameAddress = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Reads the current cutscene name from memory, or null if
        /// the reader is disabled, the string is empty, or what is
        /// there fails validation (garbage from a wrong address).
        /// Never throws.
        /// </summary>
        public string ReadName()
        {
            if (_nameAddress == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                return ReadNameFromAddress(_nameAddress);
            }
            catch (Exception ex)
            {
                LogOnce("readname", "CutsceneWatcher: ReadName failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Safe null-terminated ASCII string read, max 255 bytes.
        /// Returns null for an empty string or anything that is not
        /// a plausible cutscene name (lowercase letters, digits,
        /// underscore). Static so the validation can be reasoned
        /// about without an instance.
        /// </summary>
        public static string ReadNameFromAddress(IntPtr address)
        {
            if (address == IntPtr.Zero)
            {
                return null;
            }

            char[] chars = new char[MaxNameLength];
            int len = 0;
            try
            {
                for (int i = 0; i < MaxNameLength; i++)
                {
                    byte b = Marshal.ReadByte(address, i);
                    if (b == 0)
                    {
                        break; // null terminator
                    }
                    // Cutscene names are ASCII [a-z0-9_]. Anything
                    // else means this is not a cutscene name — it is
                    // garbage from a wrong address. Reject it all.
                    if (!((b >= (byte)'a' && b <= (byte)'z')
                        || (b >= (byte)'0' && b <= (byte)'9')
                        || b == (byte)'_'))
                    {
                        return null;
                    }
                    chars[len++] = (char)b;
                }
            }
            catch (Exception)
            {
                // Reading an unmapped address: not a name, not a crash.
                return null;
            }

            if (len == 0 || len > MaxValidNameLength)
            {
                return null;
            }
            return new string(chars, 0, len);
        }

        // ------------------------------------------------------------------
        // Cutscene natives (hash names verified present in
        // libs/ScriptHookVDotNet3.dll, 2026-11-21 — see
        // CUTSCENE-WATCHER.md. Each call is individually guarded.)
        // ------------------------------------------------------------------

        private static int ReadCutsceneTime()
        {
            try { return Function.Call<int>(Hash.GET_CUTSCENE_TIME); }
            catch (Exception) { return -1; }
        }

        private static int ReadTotalDuration()
        {
            try { return Function.Call<int>(Hash.GET_CUTSCENE_TOTAL_DURATION); }
            catch (Exception) { return -1; }
        }

        private static bool ReadWasSkipped()
        {
            try { return Function.Call<bool>(Hash.WAS_CUTSCENE_SKIPPED); }
            catch (Exception) { return false; }
        }

        // ------------------------------------------------------------------
        // Output: RAGECOOP logger, GTA notification, log file, server
        // ------------------------------------------------------------------

        private void SendDetected(string cutsceneName, int totalDurationMs)
        {
            if (!SendToServer || string.IsNullOrEmpty(cutsceneName))
            {
                return;
            }
            try
            {
                string username = "";
                try
                {
                    username = _usernameProvider != null ? _usernameProvider() : "";
                }
                catch (Exception)
                {
                    username = "";
                }
                API.SendCustomEvent(CutsceneDetected, username ?? "", cutsceneName, totalDurationMs);
            }
            catch (Exception ex)
            {
                LogOnce("send", "CutsceneWatcher: sending CutsceneDetected failed: " + ex.Message);
            }
        }

        private void Notify(string message)
        {
            try
            {
                GTA.UI.Notification.Show(message);
            }
            catch (Exception ex)
            {
                LogOnce("notify", "CutsceneWatcher: notification failed: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            try
            {
                if (_log != null)
                {
                    _log(message);
                }
            }
            catch (Exception)
            {
                // Logging must never throw.
            }
            WriteToFile(message);
        }

        private void LogOnce(string key, string message)
        {
            try
            {
                lock (_loggedOnce)
                {
                    if (_loggedOnce.Count > 200 || !_loggedOnce.Add(key))
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
            Log(message);
        }

        private void WriteToFile(string message)
        {
            try
            {
                lock (_fileLock)
                {
                    string dir = Path.GetDirectoryName(LogFileName);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        + " [GameTime=" + Game.GameTime + "] " + message + Environment.NewLine;
                    File.AppendAllText(LogFileName, line);
                }
            }
            catch (Exception)
            {
                // File logging is best-effort. Never throw, never
                // spam the RAGECOOP log about it.
            }
        }
    }
}
