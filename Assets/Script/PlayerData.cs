using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData inst { get; private set; }

    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ID { get; set; }
    private int winRandIndex { get; set; } // The random round that pays
    private float Winnings { get; set; } // Store the winnings amount

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
            DontDestroyOnLoad(gameObject); // Ensure the object persists between scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    // Load data from Unity Cloud
    public async void LoadData()
    {
        string keyName = "PlayerData"; // Unique key for cloud storage
        DatabaseManager.inst.LoadData(keyName);
    }

    // Set player data
    public void SetPlayerData(string name, string email = "", string phoneNumber = "", string id = "")
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        ID = id;
    }


    // Save data asynchronously to Unity Cloud
    public async Task<bool> SaveDatabaseAsync()
    {
        string keyName = "PlayerData";
        DatabaseManager.inst.SaveData(keyName, ToJson());
        return true; // Consider handling the actual save success response
    }

    // Convert object to JSON format
    public string ToJson()
    {
        try
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting to JSON: {ex.Message}");
            return null;
        }
    }

    public void ExportDatabase(string filename)
    {
        try
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, this);
            }
            Console.WriteLine("Database exported successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting database: {ex.Message}");
        }
    }

    // Method to deserialize the database from a file
    public static CutAndChoosePlayerDatabase ImportDatabase(string filename)
    {
        CutAndChoosePlayerDatabase database = null;
        try
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                database = (CutAndChoosePlayerDatabase)formatter.Deserialize(fs);
            }
            Console.WriteLine("Database imported successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing database: {ex.Message}");
        }
        return database;
    }

    public void SetNormalizeWinning(float x)
    {
        this.Winnings = 30 + Math.Max(0, (x - 45) * 1.5f);
    }

    public void SetRandIndex(int iRand)
    {
        this.winRandIndex = iRand;
        this.SetNormalizeWinning(RandArrays.inst ? RandArrays.inst.GetNumber(iRand) : 0);
    }
    public int GetRandIndex()
    {
        return this.winRandIndex;
    }
    public float GetNormalizeWinning()
    {
        return this.Winnings;
    }

}
