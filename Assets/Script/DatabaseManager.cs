using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using ExitGames.Client.Photon.StructWrapping;

using System.Runtime.InteropServices;

public class DatabaseManager : MonoBehaviour
{
    // [DllImport("__Internal")]
    // private static extern System.IntPtr GetCurrentUrl();


    // private string surveyID = "defaultSurveyID";
    // private string userID = "defaultUserID";
    public static DatabaseManager inst = null;
    private bool isInitialized = false;
    private bool isInitializing = false;

    private void Awake()
    {
        // Ensure singleton pattern
        if (inst != null && inst != this)
        {
            Destroy(this.gameObject);
            return;
        }

        inst = this;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("DatabaseManager");
        if (objs.Length > 1)
        {
            Destroy(this.gameObject); // Destroy duplicate GameManager instances
        }
        DontDestroyOnLoad(this.gameObject); // Don't destroy this instance when loading new scenes
    }

    // Static method to ensure DatabaseManager exists
    public static void EnsureInstance()
    {
        if (inst == null)
        {
            GameObject dbManagerObj = new GameObject("DatabaseManager");
            dbManagerObj.tag = "DatabaseManager";
            dbManagerObj.AddComponent<DatabaseManager>();
            DontDestroyOnLoad(dbManagerObj);
            Debug.Log("DatabaseManager created automatically");
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        _ = ConnectToCloud(); // Fire and forget, but track initialization status

    }


    // [Serializable]
    // private class SurveyData
    // {
    //     public string surveyID;
    //     public string userID;
    // }
    // Initializes Unity Services and signs in anonymously. Safe to call again: it only does what is still missing, so a
    // failed start (e.g. offline) or an expired/signed-out session is repaired by the next save attempt. A new sign-in
    // reuses this browser's cached session token, i.e. the same anonymous player.
    private async Task ConnectToCloud()
    {
        if (isInitializing) return;

        isInitializing = true;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("Starting Unity Services initialization...");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Test isolation: Editor and Development builds use the "development" environment and their
                // own sign-in profile, so they never read or write production Cloud Save data. If this fails,
                // the error is logged below and nothing is saved (no fallback to production).
                var testOptions = new InitializationOptions();
                Unity.Services.Core.Environments.EnvironmentsOptionsExtensions.SetEnvironmentName(testOptions, "development");
                testOptions.SetProfile("uxtest");
                Debug.Log("TEST MODE: Unity Services environment = development, profile = uxtest");
                await UnityServices.InitializeAsync(testOptions);
#else
                await UnityServices.InitializeAsync();
#endif
                Debug.Log("Unity Services initialized successfully");
            }

            if (!AuthenticationService.Instance.IsAuthorized)
            {
                Debug.Log("Starting authentication...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Authentication successful");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("TEST MODE: anonymous player " + AuthenticationService.Instance.PlayerId);
#endif
            }

            isInitialized = true;
            Debug.Log("Cloud services fully initialized and ready to use");
        }
        catch (System.Exception e)
        {
            isInitialized = false;
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            Debug.LogError($"Full exception: {e}");
        }
        finally
        {
            isInitializing = false;
        }
    }

    public async Task<bool> EnsureInitialized()
    {
        if (isInitialized && UnityServices.State == ServicesInitializationState.Initialized
            && AuthenticationService.Instance.IsAuthorized)
        {
            return true;
        }

        if (!isInitializing)
        {
            await ConnectToCloud();
        }
        else
        {
            // Wait for the running initialization (Unity frame-based wait: also works in WebGL)
            while (isInitializing)
            {
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
        }

        return isInitialized;
    }

    // One Cloud Save write (default access class, no write lock: an overwrite of this key). Completes only when the
    // service acknowledged the write; throws on any failure. Called by ResearchSaveQueue, which tracks and retries it.
    public async Task WriteRecordAsync(string key, JToken record)
    {
        if (!await EnsureInitialized())
        {
            throw new InvalidOperationException("Unity Services are not initialized or the player is not signed in");
        }

        JToken toSend = record;
        if (record is JObject obj && obj.ContainsKey("UnityPlayerId") && string.IsNullOrEmpty((string)obj["UnityPlayerId"]))
        {
            toSend = obj.DeepClone();
            toSend["UnityPlayerId"] = AuthenticationService.Instance.PlayerId; // owner of the record, for the export
        }

        Debug.Log($"Saving data to cloud with key: {key}");
        var result = await CloudSaveService.Instance.Data.Player.SaveAsync(
            new Dictionary<string, object> { { key, toSend } },
            new Unity.Services.CloudSave.Models.Data.Player.SaveOptions());
        if (result == null || !result.ContainsKey(key))
        {
            throw new InvalidOperationException("Cloud Save did not acknowledge the key " + key);
        }
    }




    public static CutAndChoosePlayerDatabase FromJson(string jsonString)
    {
        try
        {
            // Use Newtonsoft.Json to deserialize the JSON string back to the object
            return JsonConvert.DeserializeObject<CutAndChoosePlayerDatabase>(jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting from JSON: {ex.Message}");
            return null;
        }
    }

    public async Task<string> LoadData(string name)
    {
        if (!await EnsureInitialized())
        {
            Debug.LogError("Cannot load data - Unity Services not initialized");
            return null;
        }

        try
        {
            string keyName = name;
            Debug.Log($"Loading data from cloud with key: {keyName}");

            var playerData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> {
              keyName
            });

            if (playerData.TryGetValue(keyName, out var firstKey))
            {
                string jsonData = firstKey.Get<string>();
                Debug.Log($"Data loaded successfully from cloud");

                // Optional: parse it into an object if needed
                // CutAndChoosePlayerDatabase oldDataBase = FromJson(jsonData);

                // Return the raw JSON
                return jsonData;
            }

            Debug.Log($"No data found for key: {keyName}");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load data: {e.Message}");
            Debug.LogError($"Full exception: {e}");
            return null;
        }
    }



}
