using System;
using System.Collections.Generic;
using System.IO;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;

// Research data identity (Cloud Save & data integrity batch, owner-approved). One experiment ("run") is one Photon room
// session of two paired players: practice, the six real rounds and EndGameFlow.
//
// The run ID is drawn once by the client that creates the room and stored as a custom room property at creation time
// (RoomOptions.CustomRoomProperties is applied only when JoinOrCreateRoom really creates the room). The joining client
// receives the same property with the room, so both clients read one value from Photon instead of trusting their own.
// It is cached here (static: survives every scene load, including the room leave before EndGameFlow) and cleared only
// when the lobby starts a new experiment. The ID uses System.Guid and the UTC clock, never UnityEngine.Random, so the
// gameplay random draws (condition, payment round, practice opponent) are unchanged.
//
// Cloud Save keys (pattern [A-Za-z0-9_-]{1,50}): CC_<runId>_P<role>_R<round> for each round record and
// CC_<runId>_P<role>_final for the reward record. The role is part of the key so that two players who share one
// anonymous identity (e.g. two tabs of one browser profile) still never overwrite each other's records.
public static class ExperimentRun
{
    public const int SchemaVersion = 2;
    public const string RoomPropertyKey = "ccRunId";
    private const string KeyPrefix = "CC";
    private const int MaxRunIdLength = 32; // keeps every key below the 50-character Cloud Save limit

    public static string RunId { get; private set; }
    public static bool RunIdShared { get; private set; }  // true when RunId came from the Photon room (both clients)
    public static int PlayerRole { get; private set; }    // 1 = master client (Player 1), 2 = Player 2, 0 = unknown
    public static string RoomName { get; private set; } = "";

    // Real rounds of one experiment: indices 0-5 (the practice round is never saved).
    public const int RequiredRounds = 6;

    private static readonly List<string> roundKeys = new List<string>();
    private static readonly Dictionary<int, string> roundKeyByIndex = new Dictionary<int, string>();
    private static readonly List<string> roundConflicts = new List<string>();

    // Keys of the round records this client submitted in the current run, in submission order.
    public static IReadOnlyList<string> RoundKeys => roundKeys.AsReadOnly();

    // Problems found while recording rounds (refused or second record for one round, index outside 0-5, invalid key).
    public static IReadOnlyList<string> RoundConflicts => roundConflicts.AsReadOnly();

    // Round indices 0-5 for which this client has no accepted round record in the current run.
    public static List<int> MissingRounds()
    {
        var missing = new List<int>();
        for (int i = 0; i < RequiredRounds; i++)
        {
            if (!roundKeyByIndex.ContainsKey(i))
            {
                missing.Add(i);
            }
        }
        return missing;
    }

    // True when every round 0-5 has exactly one accepted record and nothing conflicted. A missing round cannot be
    // repaired by a later save retry: it was never collected.
    public static bool RoundsComplete => MissingRounds().Count == 0 && roundConflicts.Count == 0;

    // Lobby room buttons: a new experiment starts on this client. The next room provides a new run ID.
    public static void BeginNewExperiment()
    {
        string previous = RunId;
        RunId = null;
        RunIdShared = false;
        PlayerRole = 0;
        RoomName = "";
        roundKeys.Clear();
        roundKeyByIndex.Clear();
        roundConflicts.Clear();
        Debug.Log("RUN: new experiment (previous run " + (previous ?? "none") + ")");
    }

    // Room options for JoinOrCreateRoom: used by Photon only if this client creates the room.
    public static Hashtable NewRoomProperties()
    {
        return new Hashtable { { RoomPropertyKey, NewRunId() } };
    }

    // Reads the run ID and this client's role from the current room. Called when the room is joined and at the start of
    // every real round; outside a room the cached values are kept.
    public static string CaptureFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return RunId;
        }
        PlayerRole = PhotonNetwork.IsMasterClient ? 1 : 2;
        RoomName = PhotonNetwork.CurrentRoom.Name;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropertyKey, out object value)
            && value is string roomRunId && IsValidRunId(roomRunId))
        {
            if (RunId != roomRunId)
            {
                if (RunId != null)
                {
                    Debug.LogWarning("RUN: cached run " + RunId + " replaced by the room's run " + roomRunId);
                }
                RunId = roomRunId;
                RunIdShared = true;
                Debug.Log("RUN: run " + RunId + " (shared by the room " + RoomName + "), role P" + PlayerRole);
            }
        }
        else
        {
            EnsureRunId(); // a room without the property (e.g. created by an older build): local, flagged as not shared
        }
        return RunId;
    }

    // Returns the run ID; when none was received from a room, creates a local one that is flagged RunIdShared = false.
    public static string EnsureRunId()
    {
        if (string.IsNullOrEmpty(RunId))
        {
            RunId = "L" + NewRunId();
            RunIdShared = false;
            Debug.LogWarning("RUN: no shared run ID available; using local run " + RunId + " (RunIdShared=false)");
        }
        return RunId;
    }

    public static string RoundKey(int role, int roundIndex)
    {
        return KeyPrefix + "_" + EnsureRunId() + "_P" + role + "_R" + roundIndex;
    }

    public static string FinalKey()
    {
        return KeyPrefix + "_" + EnsureRunId() + "_P" + PlayerRole + "_final";
    }

    // Records the round record this client submitted for roundIndex. accepted = false when the save queue refused it
    // (the key already holds a different payload, whose first version is kept). A refused record, a second record for
    // the same round under another key, or an index outside 0-5 is a conflict: the completion gate then fails instead
    // of reporting a false success. Re-submitting the identical record is accepted and changes nothing.
    public static void NoteRoundRecord(int roundIndex, string key, bool accepted)
    {
        if (!accepted)
        {
            NoteRoundConflict("R" + roundIndex + " refused: " + key + " already holds a different record");
            return;
        }
        if (!roundKeys.Contains(key))
        {
            roundKeys.Add(key); // accepted records are saved and confirmed by the gate, even when they conflict
        }
        if (roundIndex < 0 || roundIndex >= RequiredRounds)
        {
            NoteRoundConflict("R" + roundIndex + " is outside rounds 0-" + (RequiredRounds - 1) + ": " + key);
        }
        else if (roundKeyByIndex.TryGetValue(roundIndex, out string existing) && existing != key)
        {
            NoteRoundConflict("R" + roundIndex + " recorded twice: " + existing + " and " + key);
        }
        else
        {
            roundKeyByIndex[roundIndex] = key;
        }
    }

    public static void NoteRoundConflict(string problem)
    {
        roundConflicts.Add(problem);
        Debug.LogError("SAVE: round conflict - " + problem);
    }

    // A record as a JSON object for Cloud Save. Serialized exactly as before (Newtonsoft defaults), then parsed without
    // date or number conversion, so field names, numbers and the original date strings are stored unchanged. Parsing
    // also freezes the content: later changes to the live object (e.g. a late chat message) cannot alter the payload.
    public static JToken ToPayload(object record)
    {
        string json = JsonConvert.SerializeObject(record);
        using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
        {
            return JToken.ReadFrom(reader);
        }
    }

    // UTC time of creation plus 24 random bits, e.g. 20261112-201530-a3f9c1 (sortable, 22 characters).
    private static string NewRunId()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static bool IsValidRunId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > MaxRunIdLength)
        {
            return false;
        }
        foreach (char c in id)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-';
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }
}
