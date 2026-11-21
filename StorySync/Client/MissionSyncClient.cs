// StorySync — client side (RAGECOOP resource, net48).
//
// Watches GTA.Game.IsMissionActive and tells the server when it flips.
// The server owns the lock; this client only reports edges and shows
// what the server decides. It cannot stop a mission that has already
// started locally — if you walk into a trigger while someone else holds
// the lock, the best this script can do is warn you loudly.
//
// Threading (read this before touching the code):
//  - OnStart()/OnStop() run on a background thread. Only subscribe and
//    register handlers there; never call GTA natives directly in OnStart.
//  - API.Events.OnTick runs on the script thread: GTA natives are safe
//    there (Game.IsMissionActive, Game.Player.Character.Position).
//  - Custom-event handlers run on a background thread (see
//    API.RegisterCustomEventHandler docs). They must use
//    API.QueueAction() for anything that touches the game — including
//    GTA.UI.Notification.Show. Plain field updates are fine directly.
//  - This script starts NO background threads/tasks, so OnStop() only
//    unsubscribes from OnTick.
//
// Edge handling: only send on a real change, and only after the new
// value has held for 500 ms (Game.GameTime). A flicker that flips back
// inside 500 ms is dropped and nothing is sent.

using GTA;
using GTA.Math;
using RageCoop.Client.Scripting;
using System;
using System.Collections.Generic;

namespace StorySync.Client
{
    public class MissionSyncClient : ClientScript
    {
        private const int EdgeGuardMs = 500;

        private bool _stopped;
        private bool _wasMissionActive;   // last mission state we reported
        private bool? _pendingEdge;       // edge waiting out the 500 ms guard
        private int _pendingSinceGameTime;
        private int _lastSentGameTime;

        // What the server last told us.
        private bool _amMissionHost;
        private bool _missionLocked;      // someone else holds the lock
        private string _lockedHostName = "";
        private int _lockedHostId;

        private readonly HashSet<string> _loggedOnce = new HashSet<string>();

        public override void OnStart()
        {
            try
            {
                API.RegisterCustomEventHandler(StorySyncEvents.MissionGranted, OnMissionGranted);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionDenied, OnMissionDenied);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionStarted, OnMissionStarted);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionEndedBroadcast, OnMissionEnded);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionHostTransferred, OnMissionHostTransferred);
                API.RegisterCustomEventHandler(StorySyncEvents.MissionState, OnMissionState);

                API.Events.OnTick += OnTick;

                Logger?.Info("StorySync client loaded: watching for mission starts.");
            }
            catch (Exception ex)
            {
                // OnStart runs on a background thread; never let it throw.
                LogOnce("onstart", "StorySync: OnStart failed: " + ex);
            }
        }

        public override void OnStop()
        {
            // No background threads were started, so there is nothing to
            // terminate. Just stop polling.
            _stopped = true;
            try
            {
                API.Events.OnTick -= OnTick;
            }
            catch (Exception ex)
            {
                LogOnce("onstop", "StorySync: OnStop failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // Tick: poll the mission flag (script thread — natives are safe)
        // ------------------------------------------------------------------

        private void OnTick()
        {
            if (_stopped)
            {
                return;
            }

            try
            {
                if (!API.IsOnServer)
                {
                    // Not connected: forget any pending edge and the last
                    // reported state, so reconnecting starts clean.
                    _pendingEdge = null;
                    _wasMissionActive = false;
                    _amMissionHost = false;
                    _missionLocked = false;
                    return;
                }

                bool current;
                try
                {
                    current = Game.IsMissionActive;
                }
                catch (Exception ex)
                {
                    LogOnce("ismissionactive", "StorySync: Game.IsMissionActive failed: " + ex);
                    return;
                }

                int now = Game.GameTime;

                if (current != _wasMissionActive && !_pendingEdge.HasValue)
                {
                    // New edge: start the 500 ms guard, do not send yet.
                    _pendingEdge = current;
                    _pendingSinceGameTime = now;
                    return;
                }

                if (current == _wasMissionActive)
                {
                    // Flicker flipped back (or nothing changed): drop the
                    // pending edge, nothing to report.
                    _pendingEdge = null;
                    return;
                }

                if (!_pendingEdge.HasValue)
                {
                    return;
                }

                int elapsed = now - _pendingSinceGameTime;
                if (elapsed < 0)
                {
                    // Game.GameTime wrapped around; treat the guard as met
                    // rather than waiting ~24 days.
                    elapsed = EdgeGuardMs;
                }
                if (elapsed < EdgeGuardMs)
                {
                    return; // still inside the guard window
                }

                bool toSend = _pendingEdge.Value;
                _pendingEdge = null;

                // Re-read: if it flipped again during the guard, drop it.
                bool fresh;
                try
                {
                    fresh = Game.IsMissionActive;
                }
                catch (Exception ex)
                {
                    LogOnce("ismissionactive", "StorySync: Game.IsMissionActive failed: " + ex);
                    return;
                }
                if (fresh != toSend)
                {
                    return; // flicker — mission started and ended in <500 ms
                }

                if (toSend)
                {
                    SendMissionStart();
                }
                else
                {
                    SendMissionEnded();
                }
            }
            catch (Exception ex)
            {
                // A tick handler must never throw: it would break the
                // whole RAGECOOP tick for every resource.
                LogOnce("ontick", "StorySync: OnTick failed: " + ex);
            }
        }

        private void SendMissionStart()
        {
            try
            {
                Vector3 pos = Vector3.Zero;
                try
                {
                    Ped character = Game.Player != null ? Game.Player.Character : null;
                    if (character != null && character.Exists())
                    {
                        pos = character.Position;
                    }
                }
                catch (Exception ex)
                {
                    LogOnce("position", "StorySync: could not read player position: " + ex);
                }

                string username = "";
                try
                {
                    username = API.Config.Username ?? "";
                }
                catch (Exception ex)
                {
                    LogOnce("username", "StorySync: could not read username: " + ex);
                }

                API.SendCustomEvent(StorySyncEvents.MissionStartRequest, username, pos);
                _wasMissionActive = true;
                _lastSentGameTime = Game.GameTime;
            }
            catch (Exception ex)
            {
                // Don't mark it sent: re-arm the guard so we retry in 500 ms.
                LogOnce("sendstart", "StorySync: sending MissionStartRequest failed: " + ex);
                _pendingEdge = true;
                _pendingSinceGameTime = Game.GameTime;
            }
        }

        private void SendMissionEnded()
        {
            try
            {
                string username = "";
                try
                {
                    username = API.Config.Username ?? "";
                }
                catch (Exception ex)
                {
                    LogOnce("username", "StorySync: could not read username: " + ex);
                }

                API.SendCustomEvent(StorySyncEvents.MissionEnded, username);
                _wasMissionActive = false;
                _amMissionHost = false;
                _lastSentGameTime = Game.GameTime;
            }
            catch (Exception ex)
            {
                // Don't mark it sent: re-arm the guard so we retry in 500 ms.
                LogOnce("sendend", "StorySync: sending MissionEnded failed: " + ex);
                _pendingEdge = false;
                _pendingSinceGameTime = Game.GameTime;
            }
        }

        // ------------------------------------------------------------------
        // Server -> client events (background thread: QueueAction for UI)
        // ------------------------------------------------------------------

        private void OnMissionGranted(CustomEventReceivedArgs e)
        {
            try
            {
                string hostName;
                int hostId;
                if (!TryHostArgs(e, out hostName, out hostId))
                {
                    return;
                }
                _amMissionHost = true;
                _missionLocked = false;
                _lockedHostName = hostName;
                _lockedHostId = hostId;
                API.QueueAction(() =>
                {
                    try
                    {
                        GTA.UI.Notification.Show(
                            "~g~StorySync:~s~ You are the mission host. "
                            + "Your friends have been told not to start a mission.");
                    }
                    catch (Exception ex)
                    {
                        LogOnce("notify", "StorySync: notification failed: " + ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogOnce("granted", "StorySync: OnMissionGranted failed: " + ex);
            }
        }

        private void OnMissionDenied(CustomEventReceivedArgs e)
        {
            try
            {
                string hostName;
                int hostId;
                if (!TryHostArgs(e, out hostName, out hostId))
                {
                    return;
                }
                _amMissionHost = false;
                _missionLocked = true;
                _lockedHostName = hostName;
                _lockedHostId = hostId;

                // If my own flag is already true, my mission is already
                // running locally — the deny could not stop it. Say so.
                bool alreadyInMission = _wasMissionActive;
                API.QueueAction(() =>
                {
                    try
                    {
                        string msg = "~y~StorySync:~s~ " + hostName
                            + " is already in a mission. Don't trigger a mission "
                            + "or entities will duplicate.";
                        if (alreadyInMission)
                        {
                            msg += " You already triggered one — it may duplicate. "
                                + "Fail it and rejoin the host if you can.";
                        }
                        GTA.UI.Notification.Show(msg);
                    }
                    catch (Exception ex)
                    {
                        LogOnce("notify", "StorySync: notification failed: " + ex);
                    }
                });
                Logger?.Info("StorySync: mission denied, host is " + hostName + ".");
            }
            catch (Exception ex)
            {
                LogOnce("denied", "StorySync: OnMissionDenied failed: " + ex);
            }
        }

        private void OnMissionStarted(CustomEventReceivedArgs e)
        {
            try
            {
                string hostName;
                int hostId;
                if (!TryHostArgs(e, out hostName, out hostId))
                {
                    return;
                }
                _missionLocked = true;
                _lockedHostName = hostName;
                _lockedHostId = hostId;
                API.QueueAction(() =>
                {
                    try
                    {
                        GTA.UI.Notification.Show("~y~StorySync:~s~ " + hostName
                            + " started a mission. Don't trigger one.");
                    }
                    catch (Exception ex)
                    {
                        LogOnce("notify", "StorySync: notification failed: " + ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogOnce("started", "StorySync: OnMissionStarted failed: " + ex);
            }
        }

        private void OnMissionEnded(CustomEventReceivedArgs e)
        {
            try
            {
                string hostName;
                int hostId;
                string reason;
                if (e == null || e.Args == null || e.Args.Length < 3
                    || !StorySyncEvents.TryGetString(e.Args, 0, out hostName)
                    || !StorySyncEvents.TryGetInt(e.Args, 1, out hostId)
                    || !StorySyncEvents.TryGetString(e.Args, 2, out reason))
                {
                    LogOnce("ended-args", "StorySync: MissionEnded had bad args, ignoring.");
                    return;
                }

                _amMissionHost = false;
                _missionLocked = false;
                _lockedHostName = "";
                _lockedHostId = 0;
                API.QueueAction(() =>
                {
                    try
                    {
                        GTA.UI.Notification.Show("~g~StorySync:~s~ Mission ended ("
                            + reason + "). You can start a mission now.");
                    }
                    catch (Exception ex)
                    {
                        LogOnce("notify", "StorySync: notification failed: " + ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogOnce("ended", "StorySync: OnMissionEnded failed: " + ex);
            }
        }

        private void OnMissionHostTransferred(CustomEventReceivedArgs e)
        {
            try
            {
                string oldHost;
                string newHost;
                int newHostId;
                if (e == null || e.Args == null || e.Args.Length < 3
                    || !StorySyncEvents.TryGetString(e.Args, 0, out oldHost)
                    || !StorySyncEvents.TryGetString(e.Args, 1, out newHost)
                    || !StorySyncEvents.TryGetInt(e.Args, 2, out newHostId))
                {
                    LogOnce("transfer-args", "StorySync: MissionHostTransferred had bad args, ignoring.");
                    return;
                }

                _lockedHostName = newHost;
                _lockedHostId = newHostId;
                _missionLocked = true;

                bool iAmNewHost = false;
                try
                {
                    iAmNewHost = (newHostId == API.LocalPlayerID);
                }
                catch (Exception ex)
                {
                    LogOnce("localid", "StorySync: could not read LocalPlayerID: " + ex);
                }
                _amMissionHost = iAmNewHost;

                API.QueueAction(() =>
                {
                    try
                    {
                        if (iAmNewHost)
                        {
                            GTA.UI.Notification.Show("~g~StorySync:~s~ " + oldHost
                                + " left. You are now the mission host.");
                        }
                        else
                        {
                            GTA.UI.Notification.Show("~y~StorySync:~s~ " + oldHost
                                + " left. " + newHost + " is now the mission host.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogOnce("notify", "StorySync: notification failed: " + ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogOnce("transfer", "StorySync: OnMissionHostTransferred failed: " + ex);
            }
        }

        private void OnMissionState(CustomEventReceivedArgs e)
        {
            // Sent once on connect (OnPlayerReady) so a late joiner learns
            // the lock state. Args: (string state, string hostUsername, int hostPlayerId)
            try
            {
                string state;
                string hostName;
                int hostId;
                if (e == null || e.Args == null || e.Args.Length < 3
                    || !StorySyncEvents.TryGetString(e.Args, 0, out state)
                    || !StorySyncEvents.TryGetString(e.Args, 1, out hostName)
                    || !StorySyncEvents.TryGetInt(e.Args, 2, out hostId))
                {
                    LogOnce("state-args", "StorySync: MissionState had bad args, ignoring.");
                    return;
                }

                if (state == StorySyncEvents.StateActive)
                {
                    _missionLocked = true;
                    _lockedHostName = hostName;
                    _lockedHostId = hostId;
                    API.QueueAction(() =>
                    {
                        try
                        {
                            GTA.UI.Notification.Show("~y~StorySync:~s~ " + hostName
                                + " is already in a mission. Don't trigger one.");
                        }
                        catch (Exception ex)
                        {
                            LogOnce("notify", "StorySync: notification failed: " + ex);
                        }
                    });
                }
                else
                {
                    _missionLocked = false;
                    _lockedHostName = "";
                    _lockedHostId = 0;
                }
            }
            catch (Exception ex)
            {
                LogOnce("state", "StorySync: OnMissionState failed: " + ex);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static bool TryHostArgs(CustomEventReceivedArgs e, out string hostName, out int hostId)
        {
            hostName = "";
            hostId = 0;
            if (e == null || e.Args == null || e.Args.Length < 2)
            {
                return false;
            }
            return StorySyncEvents.TryGetString(e.Args, 0, out hostName)
                && StorySyncEvents.TryGetInt(e.Args, 1, out hostId);
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
                if (Logger != null)
                {
                    Logger.Error(message);
                }
            }
            catch (Exception)
            {
                // Logging must never throw.
            }
        }
    }
}
