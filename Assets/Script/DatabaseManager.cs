using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Newtonsoft.Json;
using System;
using ExitGames.Client.Photon.StructWrapping;

public class DatabaseManager : MonoBehaviour
{

    public static DatabaseManager inst=null;

    private void Awake()
    {
        inst = this;
        GameObject[] objs = GameObject.FindGameObjectsWithTag("DatabaseManager");
        if (objs.Length > 1)
        {
            Destroy(this.gameObject); // Destroy duplicate GameManager instances
        }
        DontDestroyOnLoad(this.gameObject); // Don't destroy this instance when loading new scenes
    }


    // Start is called before the first frame update
    void Start()
    {
        this.ConnectToCloud();
    }

    private async Task ConnectToCloud()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"Init cloud");
    }


    public async void SaveData(string name, string jsonData)
    {
        string keyName = name;
        string value = jsonData;
        var playerData = new Dictionary<string, object>{
          {keyName, jsonData }
        };

        await CloudSaveService.Instance.Data.ForceSaveAsync(playerData);// Player.SaveAsync(playerData);
        Debug.Log($"Saved data");
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

    public async void LoadData(string name)
    {
        string keyName = name;
        var playerData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> {
          keyName
        });

        if (playerData.TryGetValue(keyName, out var firstKey))
        {
            string jsonData = firstKey.Get<string>();
            CutAndChoosePlayerDatabase oldDataBase = FromJson(jsonData);
            Debug.Log(oldDataBase.PlayerName);
            Debug.Log(oldDataBase.PlayerName);
            // Value.Get<string>()}");
        }
    }
}
