// StorySync — server side (RAGECOOP resource, net6.0).
//
// Single mission lock for a whole server. First MissionStartRequest wins
// and becomes the mission host; everyone else is told a mission is locked
// so they don't trigger the same mission locally (which is what creates
// duplicated mission entities — EntityPool syncs mission peds/vehicles,
// but two players each starting the same mission locally each spawn
// their own set).
//
// State machine: Idle -> Active -> Ended -> Idle.
// "Ended" is a real state but transient: EndMission() records the last
// mission, broadcasts, then immediately returns to Idle. It is NOT a
// "passed" state — pass vs. fail cannot be known from the mission flag
// alone, so MissionEnded only ever means "the host's flag went false".
//
// Threading: RAGECOOP invokes custom-event handlers and player events
// from network/listener threads, and InvokeCustomEventReceived does NOT
// wrap handlers in try/catch — a throw here would propagate into the
// network code. Every handler therefore takes the lock, validates its
// args, and try/catches everything. No background threads are started
// by this script, so OnStop() has nothing to terminate.
//
// NOTE: API.Host is the weather/time-sync host. It is a different
// concept from the StorySync mission host. This script never reads or
// writes API.Host.

using RageCoop.Core.Scripting;
using RageCoop.Server.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorySync.Server
{
    public class MissionSyncServer : ServerScript
    {
        // Keep the enum the task asked for. Ended is transient (see above).
        private enum MissionState { Idle, Active, Ended }

        private readonly object _lock = new object();

        private MissionState _state = MissionState.Idle;
        private Client _host;               // null unless Active
        private string _hostName = "";
        private int _hostId;                // Client.Player.ID
        private DateTime _startedAtUtc;

        // Players whose own mission flag went true while the lock was held.
        // They already triggered a mission locally (Deny could not stop it),
        // so they are the only sane transfer candidates if the host leaves:
        // "still in mission" = in this set and still connected. A bystander
        // who never reported a mission is never made host.
        private readonly HashSet<int> _alsoInMission = new HashSet<int>();

        // Last mission, kept for "/mission status" after the lock clears.
        private string _lastHostName = "";
        private DateTime _lastEndedAtUtc;
        private string _lastEndedReason = "";

        public override void OnStart()
        {
            try
            {
                API.RegisterCustomEventHandler(StorySyncEvents.MissionStartRequest, OnMissionStartRequest);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionEnded, OnMissionEnded);

                API.Events.OnPlayerReady += OnPlayerReady;
                API.Events.OnPlayerDisconnected += OnPlayerDisconnected;

                // One command with subcommands: /mission, /mission status, /mission abort
                API.RegisterCommand("mission", OnMissionCommand);

                Logger?.Info("StorySync server loaded: mission lock ready. Use /mission status.");
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnStart failed: " + ex);
            }
        }

        public override void OnStop()
        {
            // No background threads/tasks were started, so there is
            // nothing to terminate. Just drop the lock state.
            try
            {
                lock (_lock)
                {
                    _state = MissionState.Idle;
                    _host = null;
                    _hostName = "";
                    _hostId = 0;
                    _alsoInMission.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnStop failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // Client -> server events
        // ------------------------------------------------------------------

        private void OnMissionStartRequest(CustomEventReceivedArgs e)
        {
            try
            {
                if (e == null || e.Client == null)
                {
                    return;
                }

                string username;
                if (!StorySyncEvents.TryGetString(e.Args, 0, out username) || string.IsNullOrEmpty(username))
                {
                    username = e.Client.Username ?? "Unknown";
                }
                // Position (args[1], Vector3) is accepted for logging/debugging
                // only. The server is authoritative on WHO the host is, not where.

                lock (_lock)
                {
                    if (_state == MissionState.Active)
                    {
                        // Duplicate request from the host itself (re-send / race):
                        // re-grant, do not broadcast, do not change state.
                        if (SamePlayer(e.Client, _hostId))
                        {
                            SendTo(e.Client, StorySyncEvents.MissionGranted, _hostName, _hostId);
                            return;
                        }

                        // Someone else is already running a mission. Deny, and
                        // remember them as "also in mission" — their local flag
                        // is already true, the deny is a warning, not a stop.
                        _alsoInMission.Add(PlayerIdOf(e.Client));
                        SendTo(e.Client, StorySyncEvents.MissionDenied, _hostName, _hostId);
                        Logger?.Info("StorySync: denied mission start from " + username
                            + " — mission already hosted by " + _hostName + ".");
                        return;
                    }

                    // Idle: first request wins.
                    _host = e.Client;
                    _hostName = username;
                    _hostId = PlayerIdOf(e.Client);
                    _startedAtUtc = DateTime.UtcNow;
                    _alsoInMission.Clear();
                    _state = MissionState.Active;

                    SendTo(e.Client, StorySyncEvents.MissionGranted, _hostName, _hostId);
                    BroadcastExcept(_host, StorySyncEvents.MissionStarted, _hostName, _hostId);
                    API.SendChatMessage("Mission started — host is " + _hostName
                        + ". Don't trigger a mission or entities will duplicate.",
                        null, "StorySync");
                    Logger?.Info("StorySync: mission started, host=" + _hostName
                        + " (id " + _hostId + ").");
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnMissionStartRequest failed: " + ex);
            }
        }

        private void OnMissionEnded(CustomEventReceivedArgs e)
        {
            try
            {
                if (e == null || e.Client == null)
                {
                    return;
                }

                lock (_lock)
                {
                    if (_state != MissionState.Active)
                    {
                        // Stale end (e.g. after an abort already cleared the lock).
                        _alsoInMission.Remove(PlayerIdOf(e.Client));
                        return;
                    }

                    if (SamePlayer(e.Client, _hostId))
                    {
                        EndMissionLocked(StorySyncEvents.ReasonEnded);
                        return;
                    }

                    // A non-host participant's own mission ended. Just forget them.
                    _alsoInMission.Remove(PlayerIdOf(e.Client));
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnMissionEnded failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // Player lifecycle
        // ------------------------------------------------------------------

        private void OnPlayerReady(object sender, Client client)
        {
            try
            {
                if (client == null)
                {
                    return;
                }
                lock (_lock)
                {
                    // Late joiner: tell them the current lock state so they
                    // don't walk into a mission trigger blind.
                    if (_state == MissionState.Active)
                    {
                        SendTo(client, StorySyncEvents.MissionState,
                            StorySyncEvents.StateActive, _hostName, _hostId);
                    }
                    else
                    {
                        SendTo(client, StorySyncEvents.MissionState,
                            StorySyncEvents.StateIdle, "", 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnPlayerReady failed: " + ex);
            }
        }

        private void OnPlayerDisconnected(object sender, Client client)
        {
            try
            {
                if (client == null)
                {
                    return;
                }

                lock (_lock)
                {
                    int id = PlayerIdOf(client);
                    _alsoInMission.Remove(id);

                    if (_state != MissionState.Active || !SamePlayer(client, _hostId))
                    {
                        return; // not the host — nothing else to do.
                    }

                    // Host left mid-mission. Prefer transfer to someone who
                    // reported being in a mission and is still connected.
                    Client next = null;
                    string nextName = "";
                    int nextId = 0;
                    Dictionary<int, Client> all = null;
                    try
                    {
                        all = API.GetAllClients();
                    }
                    catch (Exception ex)
                    {
                        Logger?.Error("StorySync: GetAllClients failed during host transfer: " + ex);
                    }
                    if (all != null)
                    {
                        foreach (int candidateId in _alsoInMission.ToList())
                        {
                            Client c;
                            if (all.TryGetValue(candidateId, out c) && c != null)
                            {
                                next = c;
                                nextId = candidateId;
                                nextName = c.Username ?? "Unknown";
                                break;
                            }
                        }
                    }

                    if (next != null)
                    {
                        string oldHostName = _hostName;
                        _host = next;
                        _hostName = nextName;
                        _hostId = nextId;
                        _alsoInMission.Remove(nextId);
                        // _state stays Active; _startedAtUtc stays as the
                        // original start — the mission itself never ended.
                        BroadcastAll(StorySyncEvents.MissionHostTransferred,
                            oldHostName, nextName, nextId);
                        API.SendChatMessage("Mission host left. " + nextName
                            + " is now the mission host.", null, "StorySync");
                        Logger?.Info("StorySync: host left, transferred mission to "
                            + nextName + " (id " + nextId + ").");
                    }
                    else
                    {
                        EndMissionLocked(StorySyncEvents.ReasonHostLeft);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: OnPlayerDisconnected failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // Commands: /mission [status|abort]
        // ------------------------------------------------------------------

        private void OnMissionCommand(CommandContext ctx)
        {
            try
            {
                string sub = "";
                if (ctx != null && ctx.Args != null && ctx.Args.Length > 0
                    && !string.IsNullOrEmpty(ctx.Args[0]))
                {
                    sub = ctx.Args[0].Trim().ToLowerInvariant();
                }

                if (sub == "abort")
                {
                    AbortCommand(ctx);
                    return;
                }

                // "status", no args, and anything unknown all show status.
                StatusCommand(ctx);
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: /mission failed: " + ex);
            }
        }

        private void StatusCommand(CommandContext ctx)
        {
            string msg;
            lock (_lock)
            {
                if (_state == MissionState.Active)
                {
                    msg = "StorySync: mission ACTIVE — host " + _hostName
                        + " (id " + _hostId + "), started "
                        + _startedAtUtc.ToString("HH:mm:ss") + " UTC."
                        + " Don't trigger a mission.";
                }
                else if (!string.IsNullOrEmpty(_lastHostName))
                {
                    msg = "StorySync: no mission in progress. Last mission: host "
                        + _lastHostName + ", ended "
                        + _lastEndedAtUtc.ToString("HH:mm:ss") + " UTC ("
                        + _lastEndedReason + "). You can start a mission.";
                }
                else
                {
                    msg = "StorySync: no mission in progress. You can start a mission.";
                }
            }
            Reply(ctx, msg);
        }

        private void AbortCommand(CommandContext ctx)
        {
            lock (_lock)
            {
                if (_state != MissionState.Active)
                {
                    Reply(ctx, "StorySync: no mission in progress — nothing to abort.");
                    return;
                }

                // Server console (ctx.Client == null) may always abort.
                // A player may only abort their own mission.
                if (ctx != null && ctx.Client != null && !SamePlayer(ctx.Client, _hostId))
                {
                    Reply(ctx, "StorySync: only the mission host (" + _hostName
                        + ") can abort. Ask them to run /mission abort.");
                    return;
                }

                EndMissionLocked(StorySyncEvents.ReasonAborted);
                Reply(ctx, "StorySync: mission aborted. You can start a mission now.");
            }
        }

        // ------------------------------------------------------------------
        // Helpers — all assume the caller holds _lock unless noted
        // ------------------------------------------------------------------

        // Ends the active mission: records it, broadcasts, returns to Idle.
        // Callers must hold _lock and have checked _state == Active.
        private void EndMissionLocked(string reason)
        {
            string endedHost = _hostName;
            int endedId = _hostId;

            _state = MissionState.Ended; // transient, recorded state
            _lastHostName = endedHost;
            _lastEndedAtUtc = DateTime.UtcNow;
            _lastEndedReason = reason;

            BroadcastAll(StorySyncEvents.MissionEndedBroadcast, endedHost, endedId, reason);
            API.SendChatMessage("Mission ended (" + reason + ") — host was "
                + endedHost + ". You can start a mission now.", null, "StorySync");
            Logger?.Info("StorySync: mission ended (" + reason + "), host was "
                + endedHost + ".");

            _host = null;
            _hostName = "";
            _hostId = 0;
            _alsoInMission.Clear();
            _state = MissionState.Idle;
        }

        private void Reply(CommandContext ctx, string message)
        {
            try
            {
                if (ctx != null && ctx.Client != null)
                {
                    ctx.Client.SendChatMessage(message, "StorySync");
                }
                else
                {
                    // Server console: log it and tell everyone.
                    Logger?.Info(message);
                    API.SendChatMessage(message, null, "StorySync");
                }
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: reply failed: " + ex);
            }
        }

        private static int PlayerIdOf(Client c)
        {
            try
            {
                if (c != null && c.Player != null)
                {
                    return c.Player.ID;
                }
            }
            catch (Exception)
            {
                // Fall through to 0 — a client with no ped yet.
            }
            return 0;
        }

        private static bool SamePlayer(Client c, int id)
        {
            if (c == null || id == 0)
            {
                return false;
            }
            return PlayerIdOf(c) == id;
        }

        // Send to one client. Uses the queued variant so the handler runs
        // on the client's script thread.
        private void SendTo(Client target, int hash, params object[] args)
        {
            if (target == null)
            {
                return;
            }
            try
            {
                API.SendCustomEventQueued(new List<Client> { target }, hash, args);
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: send to " + (target.Username ?? "?") + " failed: " + ex);
            }
        }

        // Broadcast to everyone. targets=null means "all" in the API.
        private void BroadcastAll(int hash, params object[] args)
        {
            try
            {
                API.SendCustomEventQueued(null, hash, args);
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: broadcast failed: " + ex);
            }
        }

        // Broadcast to everyone except one client. Never pass null here —
        // null means "all", so build the list explicitly and skip the send
        // entirely when it would be empty.
        private void BroadcastExcept(Client except, int hash, params object[] args)
        {
            try
            {
                List<Client> targets = new List<Client>();
                Dictionary<int, Client> all = API.GetAllClients();
                if (all != null)
                {
                    foreach (Client c in all.Values)
                    {
                        if (c != null && c != except)
                        {
                            targets.Add(c);
                        }
                    }
                }
                if (targets.Count == 0)
                {
                    return;
                }
                API.SendCustomEventQueued(targets, hash, args);
            }
            catch (Exception ex)
            {
                Logger?.Error("StorySync: broadcast-except failed: " + ex);
            }
        }
    }
}
