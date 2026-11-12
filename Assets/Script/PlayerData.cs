using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData inst { get; private set; }

    // Survey metadata
    public string SurveyID { get; set; }
    public string UserID { get; set; }

    // Player Info
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ID { get; set; }

    // Private data not directly serialized
    private int winRandIndex { get; set; }
    private float Winnings { get; set; }

    // Called when the script instance is loaded
    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Save the final reward record of this run (CC_<runId>_P<role>_final; replaces the legacy fixed "PlayerData" key,
    // which every later experiment of the same anonymous player overwrote). Same eight fields and meanings as the legacy
    // record, plus the run identity and the keys of this player's round records.
    public void SaveDatabaseAsync()
    {
        DatabaseManager.EnsureInstance();
        DatabaseManager.inst.SaveData(ExperimentRun.FinalKey(), ExperimentRun.ToPayload(ToFinalRecord()));
    }

    private FinalRecordModel ToFinalRecord()
    {
        PlayerDataModel m = ToModel();
        return new FinalRecordModel
        {
            SchemaVersion = ExperimentRun.SchemaVersion,
            RunId = ExperimentRun.EnsureRunId(),
            RunIdShared = ExperimentRun.RunIdShared,
            PlayerRole = ExperimentRun.PlayerRole,
            RoomName = ExperimentRun.RoomName,
            SurveyID = m.SurveyID,
            UserID = m.UserID,
            Name = m.Name,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            ID = m.ID,
            WinRandIndex = m.WinRandIndex,
            Winnings = m.Winnings,
            RoundKeys = new System.Collections.Generic.List<string>(ExperimentRun.RoundKeys)
        };
    }

    // Load data from cloud (you'd need to parse JSON back into model)
    public async void LoadData()
    {
        string keyName = "PlayerData";
        DatabaseManager.inst.LoadData(keyName);

        // You can call ApplyJson(...) here after retrieving the JSON string

        string key = "PlayerData";

        // This assumes LoadData returns the JSON string. If not, use a callback.
        string json = await DatabaseManager.inst.LoadData(key); // <-- make sure LoadData returns Task<string>

        if (!string.IsNullOrEmpty(json))
        {
            PlayerData.PlayerDataModel model = JsonConvert.DeserializeObject<PlayerData.PlayerDataModel>(json);

            if (model != null)
            {
                Debug.Log("Loaded Player Data:");
                Debug.Log($"SurveyID: {model.SurveyID}");
                Debug.Log($"UserID: {model.UserID}");
                Debug.Log($"Name: {model.Name}");
                Debug.Log($"Email: {model.Email}");
                Debug.Log($"Phone: {model.PhoneNumber}");
                Debug.Log($"ID: {model.ID}");
                Debug.Log($"Winnings: {model.Winnings}");
                Debug.Log($"Win Round Index: {model.WinRandIndex}");

                // You can also use the model to populate the live PlayerData instance if needed:
                //this.inst?.ApplyJson(json);
            }
            else
            {
                Debug.LogError("Failed to deserialize PlayerDataModel");
            }
        }
        else
        {
            Debug.LogWarning("No data found in the cloud.");
        }
    }

    // Export to file (Binary format)
    public void ExportDatabase(string filename)
    {
        try
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, ToModel());
            }
            Debug.Log("Database exported successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error exporting database: {ex.Message}");
        }
    }

    // Import from file
    public static PlayerDataModel ImportDatabase(string filename)
    {
        PlayerDataModel database = null;
        try
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                database = (PlayerDataModel)formatter.Deserialize(fs);
            }
            Debug.Log("Database imported successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error importing database: {ex.Message}");
        }
        return database;
    }

    // Set data
    public void SetPlayerData(string name, string email = "", string phoneNumber = "", string id = "")
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        ID = id;
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

    // Convert to JSON using internal model
    public string ToJson()
    {
        try
        {
            return JsonConvert.SerializeObject(ToModel(), Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting to JSON: {ex.Message}");
            return null;
        }
    }

    // Apply JSON (if needed)
    public void ApplyJson(string json)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<PlayerDataModel>(json);
            LoadFromModel(model);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading from JSON: {ex.Message}");
        }
    }

    // Convert to model
    private PlayerDataModel ToModel()
    {
        return new PlayerDataModel
        {
            SurveyID = string.IsNullOrEmpty(this.SurveyID) ? Config.inst?.surveyID : this.SurveyID,
            UserID = string.IsNullOrEmpty(this.UserID) ? Config.inst?.userID : this.UserID,
            Name = this.Name,
            Email = this.Email,
            PhoneNumber = this.PhoneNumber,
            ID = this.ID,
            WinRandIndex = this.winRandIndex,
            Winnings = this.Winnings
        };
    }

    // Load from model
    private void LoadFromModel(PlayerDataModel model)
    {
        SurveyID = model.SurveyID;
        UserID = model.UserID;
        Name = model.Name;
        Email = model.Email;
        PhoneNumber = model.PhoneNumber;
        ID = model.ID;
        winRandIndex = model.WinRandIndex;
        Winnings = model.Winnings;
    }

    // 🧪 Internal data class (used for saving/loading)
    [Serializable]
    public class PlayerDataModel
    {
        public string SurveyID;
        public string UserID;
        public string Name;
        public string Email;
        public string PhoneNumber;
        public string ID;
        public int WinRandIndex;
        public float Winnings;
    }

    // Final reward record (schema 2): run identity + the PlayerDataModel fields unchanged + this player's round keys.
    [Serializable]
    public class FinalRecordModel
    {
        public int SchemaVersion;
        public string RecordType = "final";
        public string RunId;
        public bool RunIdShared;
        public int PlayerRole;
        public string UnityPlayerId = ""; // filled when the record is sent (owner of the record)
        public string RoomName;
        public string SurveyID;
        public string UserID;
        public string Name;
        public string Email;
        public string PhoneNumber;
        public string ID;
        public int WinRandIndex;
        public float Winnings;
        public System.Collections.Generic.List<string> RoundKeys;
    }
}
