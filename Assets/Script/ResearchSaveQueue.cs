using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Research data persistence (Cloud Save & data integrity batch, owner-approved). Every research record (the six round
// records and the final reward record) is saved through this queue: one entry per Cloud Save key, holding a frozen
// payload and its state Pending -> Saving -> Saved | Failed.
// - Saved means the Cloud Save call completed without an exception, i.e. the service acknowledged the write. Starting
//   or scheduling a save never counts as success.
// - A failed attempt is retried at most MaxAttemptsPerRequest times per request, with short delays. After that the
//   entry stays Failed, payload included, in memory until a later request retries it (EndGameFlow Save button).
// - Every attempt writes the same key with the same payload (an overwrite of one logical record), so a retry cannot
//   create a duplicate record. The first payload registered for a key is kept.
// - The state is static: it survives scene loads, so a round record keeps saving while the next round loads.
// Not covered (documented limitation): a browser tab closed or reloaded before the write reaches the service; nothing
// is stored locally.
public static class ResearchSaveQueue
{
    public enum SaveState { Pending, Saving, Saved, Failed }

    private class Entry
    {
        public string Key;
        public JToken Payload;
        public SaveState State = SaveState.Pending;
        public int Attempts;
        public string LastError;
        public Task<bool> Running;
    }

    private const int MaxAttemptsPerRequest = 3;
    private static readonly float[] RetryDelaySeconds = { 1f, 3f };
    private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    // The Cloud Save write. A delegate only so that a test session can substitute a failing or slow write at runtime
    // (Editor script eval, never committed); nothing in the game changes it.
    private static Func<string, JToken, Task> write = (key, payload) =>
    {
        DatabaseManager.EnsureInstance();
        return DatabaseManager.inst.WriteRecordAsync(key, payload);
    };

    // Registers the record (first payload wins) and starts saving it in the background. Returns at once; the outcome is
    // kept in the entry and awaited later by EnsureSavedAsync.
    public static void Submit(string key, JToken payload)
    {
        Register(key, payload);
        _ = SaveAsync(key);
    }

    // Registers a record without starting the save. Returns false when the key already holds a different payload
    // (the first payload is kept).
    public static bool Register(string key, JToken payload)
    {
        if (string.IsNullOrEmpty(key) || payload == null)
        {
            Debug.LogError("SAVE: invalid record (key or payload missing)");
            return false;
        }
        if (entries.TryGetValue(key, out Entry existing))
        {
            if (!JToken.DeepEquals(existing.Payload, payload))
            {
                Debug.LogError("SAVE: " + key + " is already registered with a different payload; the first is kept");
                return false;
            }
            return true;
        }
        entries[key] = new Entry { Key = key, Payload = payload.DeepClone() };
        return true;
    }

    public static bool IsRegistered(string key)
    {
        return key != null && entries.ContainsKey(key);
    }

    public static SaveState? GetState(string key)
    {
        return key != null && entries.TryGetValue(key, out Entry e) ? e.State : (SaveState?)null;
    }

    // Saves one registered record: already saved -> true; a running save -> the same task (single flight per key);
    // pending or failed -> a new bounded series of attempts. Unknown key -> false.
    public static Task<bool> SaveAsync(string key)
    {
        if (key == null || !entries.TryGetValue(key, out Entry e))
        {
            Debug.LogError("SAVE: " + key + " is not registered");
            return Task.FromResult(false);
        }
        if (e.State == SaveState.Saved)
        {
            return Task.FromResult(true);
        }
        if (e.Running != null && !e.Running.IsCompleted)
        {
            return e.Running;
        }
        e.Running = RunAttemptsAsync(e);
        return e.Running;
    }

    // Completion gate: retries every failed record of the list, waits for running ones, and returns true only when every
    // listed record is confirmed by Cloud Save. An empty list or an unknown key is not a success.
    public static async Task<bool> EnsureSavedAsync(IEnumerable<string> keys)
    {
        var tasks = new List<Task<bool>>();
        foreach (string key in keys)
        {
            tasks.Add(SaveAsync(key));
        }
        if (tasks.Count == 0)
        {
            return false;
        }
        bool[] results = await Task.WhenAll(tasks);
        foreach (bool ok in results)
        {
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }

    // One line per record, for logs and test reports (no payload, no credentials).
    public static string Describe(IEnumerable<string> keys)
    {
        var parts = new List<string>();
        foreach (string key in keys)
        {
            if (entries.TryGetValue(key, out Entry e))
            {
                parts.Add(key + "=" + e.State + "/" + e.Attempts + (e.LastError != null ? " (" + e.LastError + ")" : ""));
            }
            else
            {
                parts.Add(key + "=unregistered");
            }
        }
        return string.Join("; ", parts);
    }

    private static async Task<bool> RunAttemptsAsync(Entry e)
    {
        for (int attempt = 1; attempt <= MaxAttemptsPerRequest; attempt++)
        {
            e.State = SaveState.Saving;
            e.Attempts++;
            try
            {
                await write(e.Key, e.Payload);
                e.State = SaveState.Saved;
                e.LastError = null;
                Debug.Log("SAVE: " + e.Key + " confirmed by Cloud Save (attempt " + e.Attempts + ")");
                return true;
            }
            catch (Exception ex)
            {
                e.LastError = ex.GetType().Name + ": " + ex.Message;
                Debug.LogWarning("SAVE: " + e.Key + " attempt " + e.Attempts + " failed: " + e.LastError);
            }
            if (attempt < MaxAttemptsPerRequest)
            {
                await Awaitable.WaitForSecondsAsync(RetryDelaySeconds[attempt - 1]);
            }
        }
        e.State = SaveState.Failed;
        Debug.LogError("SAVE: " + e.Key + " not saved after " + e.Attempts + " attempts; kept in memory for a retry");
        return false;
    }
}
