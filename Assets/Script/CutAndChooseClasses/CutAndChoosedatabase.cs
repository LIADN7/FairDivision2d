using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Newtonsoft.Json;

[Serializable]
public class CutAndChoosePlayerDatabase
{
    private string ScenraioName = "CutAChoose";
    public int PlayerID { get; set; } = -1; // A number
    public int ScenarioNum { get; set; } = -1; // V
    public string ScenarioType { get; set; } = "Normal";
    //1 Normal, 2 Info, 3 Chat, 4 InfoAChat
    public string PlayerName { get; set; } = ""; //{player1, player2} V
    public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
    public bool isAgreed { get; set; } = false;
    public Dictionary<string, float> PlayerCutValues { get; set; } = new Dictionary<string, float>()
    {
        {"Red", 100.0f},
        {"Green", 0f}
    }; //{Red:100, Green:0}
    public Dictionary<string, float> PlayerColorValues { get; set; } = new Dictionary<string, float>()
    {
        {"RedX",0f },
        {"GreenO",0f },
        {"RedO",0f},
        {"GreenX",0f}
    }; // {RedX:100,GreenO:0,RedO:0,GreenX:0}


    public Dictionary<string, float> OtherPlayerColorValues { get; set; } = new Dictionary<string, float>()
    {
        {"RedX",0f },
        {"GreenO",0f },
        {"RedO",0f},
        {"GreenX",0f}
    }; // {RedX:100,GreenO:0,RedO:0,GreenX:0}

    public Dictionary<string, int> AmountOfColor { get; set; } = new Dictionary<string, int>()
    {
        {"RedX",0 },
        {"GreenO",0 },
        {"RedO",0},
        {"GreenX",0}
    }; //{RedX:0,GreenO:0,RedO:0,GreenX:0}


    public DateTime? PlayerStartTime { get; set; } = null; // V
    public DateTime? PlayerTimeCut { get; set; } = null; // V
    public int CountPlayerSeeOther { get; set; } = 0; // 0 V
    public int[] PlayerChoosenColors { get; set; } = new int[2]; //{-1,-1}
    public float SumOfMyPart { get; set; } = 0; // 50%... (The sum of all the part that I take)

    public Dictionary<int, Dictionary<int, string>> playerCutSquares { get; set; }  // {{key, status},...}


/*    public async Task InitializeCloud()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"init database");
    }*/


    // Method to serialize the database and write it to a file
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


    public bool SetPlayerCutValues(float[] val)
    {
        if (val.Length != PlayerCutValues.Count)
        {
            return false;
        }
        this.PlayerCutValues["Red"] = val[0];
        this.PlayerCutValues["Green"] = val[1];
        return true;
    }

    public bool SetPlayerColorValues(float[] val)
    {
        if (val.Length != PlayerColorValues.Count)
        {
            return false;
        }
        this.PlayerColorValues["RedX"] = val[0];
        this.PlayerColorValues["GreenO"] = val[1];
        this.PlayerColorValues["RedO"] = val[2];
        this.PlayerColorValues["GreenX"] = val[3];
        return true;
    }




    public bool SetOtherPlayerColorValues(float[] val)
    {
        if (val.Length != OtherPlayerColorValues.Count)
        {
            return false;
        }
        this.OtherPlayerColorValues["RedX"] = val[0];
        this.OtherPlayerColorValues["GreenO"] = val[1];
        this.OtherPlayerColorValues["RedO"] = val[2];
        this.OtherPlayerColorValues["GreenX"] = val[3];
        return true;
    }

    public bool SetAmountOfColor(int[] val)
    {
        if (val.Length != AmountOfColor.Count)
        {
            return false;
        }
        this.AmountOfColor["RedX"] = val[0];
        this.AmountOfColor["GreenO"] = val[1];
        this.AmountOfColor["RedO"] = val[2];
        this.AmountOfColor["GreenX"] = val[3];
        return true;
    }

    public bool SetPlayerTimeCut()
    {
        this.PlayerTimeCut = DateTime.Now;

        return true;
    }

    public bool AddCountPlayerSeeOther()
    {
        this.CountPlayerSeeOther++;
        return true;

    }

    public bool SetPlayerChoosenColors(int chooseNum, int buttonNum)
    {

        this.PlayerChoosenColors[chooseNum] = buttonNum;
        /*        for (int i = 0; i < 2; i++)
                {
                    Debug.LogError(this.PlayerName+": "+this.PlayerChoosenColors[i]);
                }*/
        return true;
    }

    public async void LoadData()
    {
        string keyName = "CutAChoose_0_1";
        DatabaseManager.inst.LoadData(keyName); 
            }
    public async Task<bool> SaveDatabaseAsync(string fileName)
        {
        string keyName = this.ScenraioName + "_" + this.ScenarioNum + "_" + this.ScenarioType;

            DatabaseManager.inst.SaveData(keyName, ToJson());
            return true; // This might be premature, consider returning based on task completion
        }



    public string ToJson()
    {
        try
        {
            // Use Newtonsoft.Json to serialize the object to a JSON string
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting to JSON: {ex.Message}");
            return null;
        }
    }


}


