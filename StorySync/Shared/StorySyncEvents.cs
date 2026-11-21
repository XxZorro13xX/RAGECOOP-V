// StorySync — shared event protocol (client + server).
//
// This file is compiled into BOTH assemblies (linked from each .csproj).
// It must not reference RageCoop.Client or RageCoop.Server — only
// RageCoop.Core (CustomEvents.Hash) and the .NET base library.
//
// Why hashes, not raw ints: CustomEvents.Hash("StorySync....") is an MD5
// truncated to Int32, computed independently in each process. Both sides
// hashing the same string get the same int, so client and server agree
// without sharing an assembly. Do NOT change a name string after release —
// it changes the hash and breaks compatibility with older clients.
//
// Supported arg types on the wire (see CustomEventReceivedArgs docs):
// byte, short, ushort, int, uint, long, ulong, float, bool, string,
// Vector3, Quaternion (client) + Vector2 (server). This protocol uses
// only string, int, bool and Vector3.

using RageCoop.Core.Scripting;

namespace StorySync
{
    /// <summary>
    /// Wire protocol for the StorySync mission lock, v1.
    ///
    /// Flow:
    ///   1. A client's Game.IsMissionActive flips false -> true.
    ///      Client sends MissionStartRequest (username, position).
    ///   2. Server: if no mission is active, that client becomes the
    ///      mission host. It replies MissionGranted to the host and
    ///      broadcasts MissionStarted to everyone else.
    ///      If a mission is already active, it replies MissionDenied to
    ///      the requester only (and records them as "also in mission" so
    ///      a host disconnect can transfer to them — see server code).
    ///   3. When the host's flag flips true -> false, its client sends
    ///      MissionEnded. Server broadcasts MissionEnded and unlocks.
    ///   4. A player who connects mid-mission gets MissionState on
    ///      OnPlayerReady so they know a lock is held.
    ///
    /// What this protocol does NOT carry: mission name, objectives,
    /// cutscene state, interior state, or save progress. Pass vs. fail
    /// is not knowable from the mission flag alone, so MissionEnded
    /// only ever means "the flag went false", with a reason string of
    /// "ended", "aborted" or "host-left". Never claim a pass.
    /// </summary>
    public static class StorySyncEvents
    {
        // ---- Client -> Server ----
        public const string MissionStartRequestName = "StorySync.MissionStartRequest";
        public const string MissionEndedName = "StorySync.MissionEnded";

        // ---- Server -> Client ----
        public const string MissionGrantedName = "StorySync.MissionGranted";
        public const string MissionDeniedName = "StorySync.MissionDenied";
        public const string MissionStartedName = "StorySync.MissionStarted";
        public const string MissionEndedBroadcastName = "StorySync.MissionEndedBroadcast";
        public const string MissionHostTransferredName = "StorySync.MissionHostTransferred";
        public const string MissionStateName = "StorySync.MissionState";

        public static readonly int MissionStartRequest = CustomEvents.Hash(MissionStartRequestName);
        public static readonly int MissionEnded = CustomEvents.Hash(MissionEndedName);
        public static readonly int MissionGranted = CustomEvents.Hash(MissionGrantedName);
        public static readonly int MissionDenied = CustomEvents.Hash(MissionDeniedName);
        public static readonly int MissionStarted = CustomEvents.Hash(MissionStartedName);
        public static readonly int MissionEndedBroadcast = CustomEvents.Hash(MissionEndedBroadcastName);
        public static readonly int MissionHostTransferred = CustomEvents.Hash(MissionHostTransferredName);
        public static readonly int MissionState = CustomEvents.Hash(MissionStateName);

        // ---- Payload layouts (arg order matters) ----
        // MissionStartRequest:  (string username, Vector3 position)
        // MissionEnded (client -> server): (string username)
        // MissionGranted:       (string hostUsername, int hostPlayerId)
        // MissionDenied:        (string hostUsername, int hostPlayerId)
        // MissionStarted:       (string hostUsername, int hostPlayerId)
        // MissionEndedBroadcast:(string hostUsername, int hostPlayerId, string reason)
        // MissionHostTransferred:(string oldHostUsername, string newHostUsername, int newHostPlayerId)
        // MissionState:         (string state, string hostUsername, int hostPlayerId)
        //   state is "Idle" or "Active". hostUsername is "" and hostPlayerId is 0 when Idle.

        // ---- MissionEnded reason strings ----
        public const string ReasonEnded = "ended";       // host's mission flag went false
        public const string ReasonAborted = "aborted";   // /mission abort (host or console)
        public const string ReasonHostLeft = "host-left"; // host disconnected mid-mission

        // ---- State strings (MissionState payload) ----
        public const string StateIdle = "Idle";
        public const string StateActive = "Active";

        // ---- Defensive arg readers ----
        // Custom-event args arrive boxed in their wire types, but be liberal:
        // an int may arrive as long/short, a float as double. Never throw here.

        public static bool TryGetString(object[] args, int index, out string value)
        {
            value = "";
            if (args == null || index < 0 || index >= args.Length || args[index] == null)
            {
                return false;
            }
            if (args[index] is string)
            {
                value = (string)args[index];
                return true;
            }
            value = args[index].ToString();
            return true;
        }

        public static bool TryGetInt(object[] args, int index, out int value)
        {
            value = 0;
            if (args == null || index < 0 || index >= args.Length || args[index] == null)
            {
                return false;
            }
            object o = args[index];
            if (o is int)
            {
                value = (int)o;
                return true;
            }
            if (o is long)
            {
                value = (int)(long)o;
                return true;
            }
            if (o is short)
            {
                value = (short)o;
                return true;
            }
            if (o is ushort)
            {
                value = (ushort)o;
                return true;
            }
            if (o is byte)
            {
                value = (byte)o;
                return true;
            }
            return false;
        }
    }
}
