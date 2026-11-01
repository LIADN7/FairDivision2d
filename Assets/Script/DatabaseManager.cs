using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using System;
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
    private async Task ConnectToCloud()
    {
        if (isInitializing || isInitialized) return;

        isInitializing = true;

        try
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

            Debug.Log("Starting authentication...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Authentication successful");

            isInitialized = true;
            Debug.Log("Cloud services fully initialized and ready to use");
        }
        catch (System.Exception e)
        {
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
        if (isInitialized) return true;

        if (!isInitializing)
        {
            await ConnectToCloud();
        }
        else
        {
            // Wait for initialization to complete
            while (isInitializing)
            {
                await Task.Delay(100);
            }
        }

        return isInitialized;
    }

    public async void SaveData(string name, string jsonData)
    {
        if (!await EnsureInitialized())
        {
            Debug.LogError("Cannot save data - Unity Services not initialized");
            return;
        }

        try
        {
            string keyName = name;
            string value = jsonData;
            var playerData = new Dictionary<string, object>{
              {keyName, jsonData }
            };

            Debug.Log($"Saving data to cloud with key: {keyName}");
            await CloudSaveService.Instance.Data.ForceSaveAsync(playerData);
            Debug.Log($"Data saved successfully to cloud");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
            Debug.LogError($"Full exception: {e}");
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
