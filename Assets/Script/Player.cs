using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviourPunCallbacks
{
    //[SerializeField] public GameObject cutBt;
    private PhotonView view;
    public static Player inst;
    // private Dictionary<int, int> player1 = new Dictionary<int, int>();
    [SerializeField] protected Text[] buttonsValueText; // {red, green, yellow, blue}
    [SerializeField] protected Text palyerName;
    [SerializeField] protected Text chatText;
    [SerializeField] protected InputField chatInput;
    [SerializeField] protected GameObject[] colorsValueObj; // {red, green, yellow, blue}


    private float[] sumPlayer = { 0f, 0f }; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f }; // sum of all the squares that the player choose (1 and 2) -> {1, 2}
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    private string[] playerSaveBtTexts = { "", "", "", "" };
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private HashValues HelpHashValue;
    // Saves the player's state
    private Dictionary<int, int> savePlayer1 = new Dictionary<int, int>();
    private Dictionary<int, int> savePlayer2 = new Dictionary<int, int>();
    [SerializeField] protected SumSquares[] stateObjects; // All the state obj with square childs
    private int chooseNum = 0;
    private string[] endExplanationPlayers = { "You get:\n", "You get:\n" };
    private bool[] playerCut = { false, false };
    private bool[] palyersGameOver = { false, false };

    private CutAndChoosePlayerDatabase gameDatabase = new CutAndChoosePlayerDatabase();
    //private Task databaseTask;
    [DllImport("__Internal")]
    private static extern System.IntPtr GetCurrentUrl();
    void Start()
    {


        view = GetComponent<PhotonView>();
        inst = this;
        string palyerId = PhotonNetwork.IsMasterClient ? "Player 1" : "Player 2";
        palyerName.text = PhotonNetwork.NickName + ", " + palyerId + "\nThis is round number " + ((int)(Config.inst.getRoundNumber()) + 1);
        if (PhotonNetwork.IsMasterClient)
        {
            view.RPC("InitSquares", RpcTarget.AllBufferedViaServer);
        }

        this.InitDatabaseAsync();
        Manager.inst.setNote(-1, "Please divide the map into 2 parts (if you divide into 2 equal parts you are guaranteed to get at least 50%)", false);


    }


    public async Task InitDatabaseAsync()
    {
        this.gameDatabase.ScenarioNum = Config.inst.getRoundNumber();
        this.gameDatabase.ScenarioType = Config.inst.getScenarioTypeName();
        this.gameDatabase.PlayerStartTime = DateTime.Now;
        this.gameDatabase.PlayerName = PhotonNetwork.NickName;
        //this.gameDatabase.PlayerID = 20548; // Need to add the real id from the user
        int[] tempChoosen = { -1, -1 };
        this.gameDatabase.PlayerChoosenColors = tempChoosen;
        // this.SetDataFromURL();

        /*        await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Init cloud");*/
        //int palyerId = PhotonNetwork.IsMasterClient ? 0 : 1;

        //this.databaseTask =this.gameDatabase.InitializeCloud();
    }

    private void SetDataFromURL()
    {
        string surveyID = "defaultSurveyID";
        string userID = "defaultUserID";
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            System.IntPtr urlPtr = GetCurrentUrl();
            string jsonInput = Marshal.PtrToStringUTF8(urlPtr);
            // Debug.Log("aaaaa= " + urlPtr);
            // Debug.Log("jsonInput= " + jsonInput);
            if (string.IsNullOrEmpty(jsonInput) || jsonInput == "{}")
            {
                Debug.Log("Received empty JSON, using default values.");
            }
            else
            {
                try
                {
                    var parsedData = JsonUtility.FromJson<SurveyData>(jsonInput);
                    if (!string.IsNullOrEmpty(parsedData.surveyID))
                    {
                        surveyID = parsedData.surveyID;
                    }
                    if (!string.IsNullOrEmpty(parsedData.userID))
                    {
                        userID = parsedData.userID;
                    }
                    Debug.Log("surveyID= " + surveyID + " , userID= " + userID);
                }

                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing JSON: {ex.Message}");
                }
            }
            //Marshal.PtrToStringUTF8(urlPtr);
        }
        this.gameDatabase.userID = userID;
        this.gameDatabase.surveyID = surveyID;
        Config.inst.userID = userID;
        Config.inst.surveyID = surveyID;
        // Open new tab for the player's survey
        // Application.OpenURL("http://www.panel4all.co.il");
        // http://www.panel4all.co.il/survey_runtime/external_survey_status.php?surveyID=XXXXX&userID=YYYYY&status=finish
        // http://www.panel4all.co.il/survey_runtime/external_survey_status.php?surveyID=XXXXX&userID=YYYYY&status=filterout

    }

    [Serializable]
    private class SurveyData
    {
        public string surveyID;
        public string userID;
    }

    [PunRPC]
    public void InitSquares()
    {

        GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
        int palyerId = PhotonNetwork.IsMasterClient ? 0 : 1;


        Debug.Log("Round Number - " + Config.inst.getRoundNumber());
        StartCoroutine(withSec(0.2f, tempSquares));


    }

    private IEnumerator withSec(float sec, GameObject[] tempSquares)
    {
        yield return new WaitForSeconds(sec);

        int j1 = 1, j2 = 2;
        for (int i = 0; i < tempSquares.Length; i++)
        {

            PointOfState temp = tempSquares[i].GetComponent<PointOfState>();
            if (!this.squares.ContainsKey(temp.getMyKey()))
            {
                this.squares.Add(temp.getMyKey(), tempSquares[i]);

            }
            this.sumPlayer[j1 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfState>().getMyVal(j1);
            this.sumPlayer[j2 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfState>().getMyVal(j2);

        }

        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        foreach (var it in squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            p.setmyPowerColor(playerNum, this.sumPlayer[playerNum - 1]);
            p.setOtherPowerColor(otherPlayerNum, this.sumPlayer[otherPlayerNum - 1]);
            p.setSpriteStatus(1, 1);
        }
        /*        greenValueText.text = "All green value: " + (0) + "%";
                redValueText.text = "All red value: " + (100) + "%";*/
        this.statusChange();
        this.setAllStateTitle(playerNum);

        // Check for values counter
        //------------------------
        // this.HelpHashValue = new HashValues();
        // this.HelpHashValue.buildHelp(this.squares);
        //h.printHelp(1);
        //h.printHelp(2);

        //--------------------
    }


    public void setAllStateTitle(int playerNum)
    {
        for (int i = 0; i < this.stateObjects.Length; i++)
        {
            this.stateObjects[i].SetMyTitle(playerNum, this.sumPlayer[playerNum - 1]);
        }
    }
    // Update is called once per frame
    /*    void Update()
        {

            //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
        }*/


    public void CutView()
    {
        float cutSum = this.statusChange();
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            this.gameDatabase.SetPlayerTimeCut();
            float[] cutVal = { 100 - cutSum, cutSum };
            this.gameDatabase.SetPlayerCutValues(cutVal);
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, string>> tempPlayerWithParentName = new Dictionary<int, Dictionary<int, string>>();
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                int status = p.getSpriteStatus();
                int key = p.getMyKey();
                string stateName = p.getMyStateName();
                tempPlayer.Add(key, status);

                tempPlayerWithParentName.Add(key, new Dictionary<int, string>() { { status, stateName } });

                //p.setSpriteStatus(1);
            }
            this.gameDatabase.playerCutSquares = tempPlayerWithParentName;
            view.RPC("Cut", RpcTarget.All, tempPlayer, playerNum);
            Manager.inst.setViewAfterCut();
            Manager.inst.setNote(-1, "Please wait for the other player to cut", false);
        }
        else
        {
            Manager.inst.setNote(7f, "Waiting for the other player to connect!", true);
        }
        if (isAllTrue())
        {
            view.RPC("setAllState", RpcTarget.All);
            float[] tempMySum = Manager.inst.getAllSumRGYB();
            this.gameDatabase.SetPlayerColorValues(tempMySum);
            view.RPC("SetForOtherPlayerColorValuesDataBase", RpcTarget.Others, tempMySum);
            //setAllState();
        }

    }

    [PunRPC]
    private void SetForOtherPlayerColorValuesDataBase(float[] tempOtherSum)
    {

        this.gameDatabase.SetOtherPlayerColorValues(tempOtherSum);
    }

    [PunRPC]
    public void Cut(Dictionary<int, int> tempPlayer, int i)
    {

        if (i == 1)
        {
            this.player1 = tempPlayer;
        }
        else if (i == 2)
        {
            this.player2 = tempPlayer;
        }
        this.playerCut[i - 1] = true;
        if (isAllTrue())
        {
            Manager.inst.setViewToChoose();

            // Set other player name on the database
            photonView.RPC("UpdateOtherPlayerName", RpcTarget.Others, this.gameDatabase.PlayerName);

            //setAllState(player1, player2);
        }


    }

    [PunRPC]
    public void setAllState()
    {
        //GREEN, RED, YELLOW, BLUE
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        float[] sumRGYB = createSumRGYB(playerNum);
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            //Debug.LogError(sumRGYB[k] + "   " + this.sumPlayer[playerNum - 1]);
            sumRGYB[k] = (sumRGYB[k] / this.sumPlayer[playerNum - 1]) * 100;

        }

        Manager.inst.setSumRGYB(sumRGYB);
        buttonsValueText[0].text = "Part 1 (X) value: " + (sumRGYB[0]) + "%";
        buttonsValueText[1].text = "Part 2 (O) value: " + (sumRGYB[1]) + "%";
        buttonsValueText[2].text = "Part 3 (O) value: " + (sumRGYB[2]) + "%";
        buttonsValueText[3].text = "Part 4 (X) value: " + (sumRGYB[3]) + "%";
        Manager.inst.initialSeeOtherPlayerBT();
        if (isIntuitive(sumRGYB))
        {
            //if(Manager.inst.getIndexOfMaxSumRGYB()) // need to do a intuitive choose (Maybe we will create in the future)
        }
        else
        {

            if (playerNum == 1)
            {
                Manager.inst.setNote(-1, "The meaning:\nO =  player 2 choose green\nX =  player 2 choose red\n\nPlease choose one color", false);
            }
            else if (playerNum == 2)
            {
                Manager.inst.setNote(-1, "The meaning:\nO =  player 2 choose green\nX =  player 2 choose red\n\nPlease wait player 1 choose one color,\nafter that you choose 2 colors", false);
            }
        }
    }



    private bool isIntuitive(float[] sumRGYB)
    {
        bool flag = sumRGYB[2] + sumRGYB[3] == 0 || sumRGYB[0] + sumRGYB[1] == 0;
        return false;
    }

    public void ChooseView(int i)
    {
        if (isAllTrue() && (i >= 0 && i < 4))
        {
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            float tempSum = Manager.inst.getSumRGYB(i);
            if (playerNum == 1 && chooseNum < 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] = tempSum;
                this.gameDatabase.SetPlayerChoosenColors(0, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "Player 1 choose, please pick 2 parts");
                Manager.inst.setNote(-1, "Please wait for player 2 to pick 2 parts)", false);


            }
            else if (playerNum == 2 && chooseNum == 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                this.gameDatabase.SetPlayerChoosenColors(0, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "Player 2 choose 1 part");
                Manager.inst.setNote(-1, "Please pick one more part)", false);
            }
            else if (playerNum == 2 && chooseNum == 2)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                this.gameDatabase.SetPlayerChoosenColors(1, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("GetValues", RpcTarget.All);
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "You got the last remaining part");
                Manager.inst.setNote(-1, "Player 1 got the last remaining part)", false);
            }

        }
    }

    [PunRPC]
    private void Choose(int i, int playerNum)
    {
        buttonsValueText[i].text = "Taken By Player " + playerNum;
        colorsValueObj[i].GetComponent<Button>().interactable = false;
        //colorsValueObj[i].SetActive(false);
        chooseNum++;

    }
    [PunRPC]
    private void SetNoteForOtherPlayer(string message)
    {
        Manager.inst.setNote(-1, message, false);

    }
    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = { "RED-X", "GREEN-O", "RED-O", "GREEN-X" };
        return "" + strColors[color] + " with value: " + value + "\n";
    }

    [PunRPC]
    private void GetValues()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int last = GetLast();
        buttonsValueText[last].text = "Taken By Player 1";
        if (PhotonNetwork.IsMasterClient)
        {
            this.gameDatabase.SetPlayerChoosenColors(1, last);
            float tempSum = Manager.inst.getSumRGYB(last);
            endExplanationPlayers[playerNum - 1] += getExplanationPlayer(last, tempSum);
            choosePlayer[playerNum - 1] += tempSum;
            chooseNum++;
            buttonsValueText[last].text += " (You)";
        }
        colorsValueObj[last].GetComponent<Button>().interactable = false;
        //colorsValueObj[last].SetActive(false);
        Manager.inst.setNote(-1, endExplanationPlayers[playerNum - 1], false);
        Manager.inst.setEndGameLayer(choosePlayer[playerNum - 1]);
        this.gameDatabase.SumOfMyPart = choosePlayer[playerNum - 1];
    }

    // Get the last number of the remaining color
    private int GetLast()
    {

        //chooseObj

        for (int j = 0; j < colorsValueObj.Length; j++)
        {

            if (colorsValueObj[j].GetComponent<Button>().IsInteractable())
                return j;
        }
        return -1;

    }


    private float[] createSumRGYB(int j)
    {
        float[] sumRGYB = { 0, 0, 0, 0 }; //GREEN, RED, YELLOW, BLUE
        int[] statusCounter = new int[4];

        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float val = p.getMyVal(j);
            int tempKey = p.getMyKey();
            // X mean that player 2 chooce Red, O mean that player 2 chooce Green
            int spriteStatus = 1; // Red+X = Player 2 choose Red
            string colorText = "X";
            if (this.player1[tempKey] == 2 && this.player2[tempKey] == 2) // Green+O = Player 2 choose Green
            {
                colorText = "O";
                spriteStatus = 2;
            }
            else if (this.player1[tempKey] == 1 && this.player2[tempKey] == 2) // Red+O = Player 2 choose Green
            {
                colorText = "O";
                spriteStatus = 3;
            }
            else if (this.player1[tempKey] == 2 && this.player2[tempKey] == 1) // Green+X = Player 2 choose Red
            {
                colorText = "X";
                spriteStatus = 4;
            }
            p.setChildText(colorText);
            sumRGYB[spriteStatus - 1] += val;
            statusCounter[spriteStatus - 1]++;
            p.setSpriteStatus(spriteStatus, 1);

        }
        this.gameDatabase.SetAmountOfColor(statusCounter);
        return sumRGYB;
    }

    public bool isAllTrue()
    {
        for (int i = 0; i < this.playerCut.Length; i++)
        {
            if (!this.playerCut[i]) return false;
        }
        return true;
    }



    public float statusChange()
    {
        lock (this)
        {
            float sumG = 0;
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;

            foreach (var it in squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                if (p.getSpriteStatus() == 2)
                {
                    sumG += p.getMyVal(playerNum);
                }

            }
            float greenVal = (sumG / this.sumPlayer[playerNum - 1]);
            buttonsValueText[0].text = "Part 1 value: " + ((1 - greenVal) * 100) + "%";
            buttonsValueText[1].text = "Part 2 value: " + (greenVal * 100) + "%";
            if ((greenVal * 100) == 50)
            {
                Manager.inst.setStatusClick(0);
                Manager.inst.setNote(-1, "The map is divided into 50%\n(if you wish, you can change the division or choose not to divide equally)", false);

            }
            return (greenVal * 100);
        }
    }

    // This method is used to retrieve the other player's Heatmap.
    public void getOtherView()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        this.gameDatabase.AddCountPlayerSeeOther();
        if (Manager.inst.getIsShowView())
        {
            Debug.Log("second");

            SetMap(playerNum == 1 ? this.savePlayer1 : this.savePlayer2, playerNum);
            //this.statusChange();
            this.setAllStateTitle(playerNum);
            float[] sumRGYB = setOtherCutMap(playerNum);

            for (int i = 0; i < buttonsValueText.Length; i++)
            {
                buttonsValueText[i].text = playerSaveBtTexts[i];
            }


        }
        else
        {
            Debug.Log("first");
            Dictionary<int, int> tempPlayer = getTempPlayer();
            if (playerNum == 1) savePlayer1 = tempPlayer;
            else savePlayer2 = tempPlayer;
            for (int i = 0; i < buttonsValueText.Length; i++)
            {
                playerSaveBtTexts[i] = buttonsValueText[i].text;
            }
            //setOtherHeatmap(); // Set other player heatmap in green color
            float[] sumRGYB = setOtherCutMap(otherPlayerNum);
            buttonsValueText[0].text = "Other player val: " + (sumRGYB[0]) + "%";
            buttonsValueText[1].text = "Other player val: " + (sumRGYB[1]) + "%";
            buttonsValueText[2].text = "Other player val: " + (sumRGYB[2]) + "%";
            buttonsValueText[3].text = "Other player val: " + (sumRGYB[3]) + "%";
        }
        Manager.inst.changeSeeOtherPlayerBT();


    }



    // Update the map based on the received data.
    private void SetMap(Dictionary<int, int> tempPlayer, int playerNum)
    {
        if (tempPlayer == null) return;

        //float[] sumRGYB = { 0, 0 }; //GREEN, RED, YELLOW, BLUE
        //int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float val = p.getMyVal(playerNum);
            int tempKey = p.getMyKey();
            int spriteStatus = tempPlayer[tempKey]; // Red, Green, Yellow or Blue

            //sumRGYB[spriteStatus - 1] += val;
            p.setSpriteStatus(spriteStatus, 1);

        }

    }

    // Get temporary player data for sending.
    private Dictionary<int, int> getTempPlayer()
    {
        //int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            int status = p.getSpriteStatus();
            int key = p.getMyKey();
            tempPlayer.Add(key, status);
        }
        return tempPlayer;
    }


    // Set the heatmap of the other player
    private void setOtherHeatmap()
    {
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            p.setSpriteStatus(2, 2); //set green and pther val power
        }
    }


    private float[] setOtherCutMap(int otherPlayer)
    {
        float[] sumRGYB = { 0, 0, 0, 0 };
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float otherVal = p.getMyVal(otherPlayer);
            sumRGYB[p.getSpriteStatus() - 1] += otherVal;
            p.setSpriteStatus(p.getSpriteStatus(), otherPlayer); //set this color and other player val power
        }
        // Set the other player values on the buttons
        float sumAll = 0;
        for (int i = 0; i < sumRGYB.Length; i++)
        {
            sumAll += sumRGYB[i];
        }
        for (int i = 0; i < sumRGYB.Length; i++)
        {
            sumRGYB[i] = (sumRGYB[i] / sumAll) * 100;
        }

        // Set all the title states
        this.setAllStateTitle(otherPlayer);
        return sumRGYB;
    }

    /*    private  void setAllButtonValues(float[] sumRGYB)
        {
            float sumAll = 0;
            for (int i = 0; i < sumRGYB.Length; i++)
            {
                sumAll += sumRGYB[i];
            }
            for (int i = 0; i < sumRGYB.Length; i++)
            {
                sumRGYB[i] = (sumRGYB[i] / sumAll) * 100;
            }
            buttonsValueText[0].text = "Other player val: " + (sumRGYB[0]) + "%";
            buttonsValueText[1].text = "Other player val: " + (sumRGYB[1]) + "%";
            buttonsValueText[2].text = "Other player val: " + (sumRGYB[2]) + "%";
            buttonsValueText[3].text = "Other player val: " + (sumRGYB[3]) + "%";
        }*/


    // Send message from the Game Manger
    private void sendMangerMessage(string message)
    {
        if (this.chatInput.text.Length >= 1)
        {
            addMessageToList("Manager", message);
            view.RPC("sendMessageToAll", RpcTarget.Others, "Manager", message);
            this.chatInput.text = "";
            getAllMessages();

        }

    }

    // Send this message
    public void sendMessage2()
    {
        if (this.chatInput.text.Length >= 1)
        {
            addMessageToList(PhotonNetwork.NickName, this.chatInput.text);
            view.RPC("sendMessageToAll", RpcTarget.Others, PhotonNetwork.NickName, this.chatInput.text);
            this.chatInput.text = "";
            getAllMessages();

        }

    }

    // Adds a new chat message to the chat history list.
    // If the chat history has more than 11 messages, it removes the oldest message.
    private void addMessageToList(string name, string newMessage)
    {
        // Save only the last 200 messeges 
        if (this.chatHistory.Count() >= 200)
        {
            this.chatHistory.RemoveAt(0);
        }
        ChatMessage tempMessage = new ChatMessage(name, newMessage);
        this.chatHistory.Add(tempMessage);
        this.gameDatabase.ChatHistory = this.chatHistory; // Save chat (this func is called twice)

    }

    // Updates the chat UI by displaying all chat messages in the chat history list.
    // Clears the existing text in the chat UI before updating.
    private void getAllMessages()
    {
        this.chatText.text = ""; // Clear the text before updating
        int i = 0;
        foreach (ChatMessage m in chatHistory)
        {
            // Show only the first 10 messeges
            if (chatHistory.Count - i < 11)
            {
                this.chatText.text += m.ToString() + "\n"; // Add each message followed by a newline
            }
            i++;
        }

    }

    // Sends the message to the other players
    [PunRPC]
    private void sendMessageToAll(string name, string newMessage)
    {
        //Manager.inst.setNewMessage();
        addMessageToList(name, newMessage);
        // save chat for the sender player
        getAllMessages();
    }


    public void getHelpToCut()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        Dictionary<float, int> helpCutPlayer = new Dictionary<float, int>(this.HelpHashValue.getPlayerHelp(playerNum));
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            int tempKey = p.getMyKey();
            float val = p.getMyVal(playerNum);
            if (helpCutPlayer.ContainsKey(val) && helpCutPlayer[val] > 0)
            {
                helpCutPlayer[val]--;
                p.setSpriteStatus(2, playerNum);
            }
            else
            {
                p.setSpriteStatus(1, playerNum);
            }
            //int spriteStatus = tempPlayer[tempKey]; // Red, Green, Yellow or Blue

            //sumRGYB[spriteStatus - 1] += val;


        }
        statusChange();
    }

    [PunRPC]
    public void NextGame(string sceneName)
    {
        //Manager.inst.setNote(-1,"Other player left the game...", false); // need to send in RPC!
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        this.palyersGameOver[playerNum - 1] = true;
        Manager.inst.NextSceneClick();
        this.SaveData();
        if (this.palyersGameOver[0] && this.palyersGameOver[1])
        {

            photonView.RPC("MasterNextGame", RpcTarget.All, sceneName);
        }
        else
        {
            photonView.RPC("GameOverClicked", RpcTarget.Others, playerNum);
        }
    }

    [PunRPC]
    private void GameOverClicked(int playerNum)
    {
        // Master client loads the scene on behalf of non-master clients
        this.palyersGameOver[playerNum - 1] = true;
        Manager.inst.setNote(-1, "Other player continue the game...", false); // need to send in RPC!
    }

    [PunRPC]
    private void MasterNextGame(string sceneName)
    {
        // Master client loads the scene on behalf of non-master clients
        if (PhotonNetwork.IsMasterClient)
        {
            int roundNumberConfig = Config.inst.addRoundNumber();
            //roundNumberConfig = 4;
            //Debug.LogError("scenarioNum= " + roundNumberConfig);
            if (roundNumberConfig == 2)
            {

                int scenarioNum = UnityEngine.Random.Range(2, 4); // Create next 2 scenarios for round 3 and 4

                //Config.inst.createConfig(scenarioNum);
                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, scenarioNum, roundNumberConfig);
            }
            else if (roundNumberConfig == 4)
            {

                int scenarioNum = 4; // Create next 2 scenarios for round 5 and 6
                //Config.inst.createConfig(scenarioNum);
                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, scenarioNum, roundNumberConfig);
            }
            else if (roundNumberConfig == 6)
            {
                int randomNumber = UnityEngine.Random.Range(0, roundNumberConfig);
                photonView.RPC("SetRandomPaymentRound", RpcTarget.All, randomNumber);
                LeaveGame("EndGameFlow", true); //Save data after click on the ending button!!!!
            }
            else
            {

                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, Config.inst.getScenarioType(), roundNumberConfig);
            }
        }
    }

    [PunRPC]
    private void SetRandomPaymentRound(int randRound)
    {
        PlayerData.inst.SetRandIndex(randRound);
    }

    [PunRPC]
    private void RequestSceneLoad(string sceneName, int scenarioNum, int roundNumberConfig)
    {
        Config.inst.createConfig(scenarioNum, roundNumberConfig);
        // Master client loads the scene on behalf of non-master clients
        PhotonNetwork.LoadLevel(sceneName);
    }


    public void LeaveGame(string sceneName, bool isRedirectToSurvey = false)
    {

        photonView.RPC("LeaveGameForAll", RpcTarget.All, sceneName, isRedirectToSurvey);
    }
    [PunRPC]
    private void UpdateOtherPlayerName(string otherName)
    {
        this.gameDatabase.otherPlayerName= otherName;
    }

    [PunRPC]
    private void LeaveGameForAll(string sceneName, bool isRedirectToSurvey = false)
    {
        // Only for test 
        // Config.inst.RedirectToSurvey(isRedirectToSurvey);
        PhotonNetwork.LeaveRoom();
        StartCoroutine(DelayAndLoadScene(0.2f, sceneName));
    }
    private IEnumerator DelayAndLoadScene(float sec, string sceneName)
    {
        yield return new WaitForSeconds(sec);
        SceneManager.LoadScene(sceneName);
    }

    // This function save the data of the game
    private void SaveData()
    {
        this.gameDatabase.isAgreed = Manager.inst.getIsAgreedMark();
        this.gameDatabase.SaveDatabaseAsync("CutAndChhosedatabase");
    }

    // This function load the data that saved
    public void LoadDatabaseAsync(string path)
    {
        // string loadTestFile = "CutAndChhosedatabase.dat";
        /*string loadTestFile = "./Assets/DataFiles/CutAndChhosedatabase.dat";
        CutAndChoosePlayerDatabase newData = CutAndChoosePlayerDatabase.ImportDatabase(loadTestFile);*/
        this.gameDatabase.LoadData();
        /*            CutAndChoosePlayerDatabase newData
                Debug.Log("Saved player name: "+newData.PlayerName);
                Debug.Log("Num of clicked 'see': " + newData.CountPlayerSeeOther);
        */
    }

    public bool RedirectToSurvey(string url)
    {
        Application.OpenURL(url);
        return true; // Open new tab successfully
    }


}



