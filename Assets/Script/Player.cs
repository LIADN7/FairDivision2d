using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using System;
using System.Linq;
//using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviourPunCallbacks
{
    //[SerializeField] public GameObject cutBt;
    private PhotonView view;
    public static Player inst;
    //private Dictionary<int, int> player1 = new Dictionary<int, int>();
    [SerializeField] protected Text[] buttonsValueText; // {red, green, yellow, blue}
    [SerializeField] protected Text palyerName;
    [SerializeField] protected Text chatText;
    [SerializeField] protected InputField chatInput;
    [SerializeField] protected GameObject[] colorsValueObj; // {red, green, yellow, blue}


    private float[] sumPlayer = {0f,0f}; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f }; // sum of all the squares thatthe player choose (1 and 2) -> {1, 2}
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private HashValues HelpHashValue;
    // Saves the player's state
    private Dictionary<int, int> savePlayer1 = new Dictionary<int, int>();
    private Dictionary<int, int> savePlayer2 = new Dictionary<int, int>();
    private int chooseNum = 0;
    private string[] endExplanationPlayers = {"You get:\n", "You get:\n" };
    private bool[] playerCut = { false, false };
    private bool[] palyersGameOver = {false,false};


    void Start()
    {

        view = GetComponent<PhotonView>();
        inst = this;
        string palyerId = PhotonNetwork.IsMasterClient ? "Player 1" : "Player 2";
        palyerName.text = PhotonNetwork.NickName + ", "+ palyerId+ "\nThis is round number "+ (int)(Config.inst.getRoundNumber())+1;
        if (PhotonNetwork.IsMasterClient)
            view.RPC("InitSquares", RpcTarget.AllBufferedViaServer);



    }

    [PunRPC]
    public void InitSquares()
    {

            GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
        Debug.Log(Config.inst.getRoundNumber()) ;
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
                this.sumPlayer[j1-1] += this.squares[temp.getMyKey()].GetComponent<PointOfState>().getMyVal(j1);
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
        // Check for values counter
        //------------------------
        // this.HelpHashValue = new HashValues();
        // this.HelpHashValue.buildHelp(this.squares);
        //h.printHelp(1);
        //h.printHelp(2);

        //--------------------
    }

    // Update is called once per frame
/*    void Update()
    {
        
        //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
    }*/

    public void CutView()
    {
        float cutSum = this.statusChange();
        //if (cutSum == 50) { 
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                int status = p.getSpriteStatus();
                int key = p.getMyKey();
                tempPlayer.Add(key, status);
                //p.setSpriteStatus(1);
            }
            view.RPC("Cut", RpcTarget.All, tempPlayer, playerNum);
        Manager.inst.setViewAfterCut();
        Manager.inst.setNote(-1, "Please wait for the other player to cut", false);
        // }
        /*        else
                {
                    Manager.inst.setNote(7f,"Youneed to set 50% for for both values!!!", true);
                }*/
        if (isAllTrue())
            {
                view.RPC("setAllState", RpcTarget.All);
                //setAllState();
            }

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
        buttonsValueText[0].text = "P2 choose X(Red) = (1,1) value: " + (sumRGYB[0]) + "%";
        buttonsValueText[1].text = "P2 choose O(Green) = (2,2) value: " + (sumRGYB[1]) + "%";
        buttonsValueText[2].text = "P2 choose O(Green) = (1,2) value: " + (sumRGYB[2]) + "%";
        buttonsValueText[3].text = "P2 choose X(Red) = (2,1) value: " + (sumRGYB[3]) + "%";
        Manager.inst.initialSeeOtherPlayerBT();
        if (isIntuitive(sumRGYB))
        {
            //if(Manager.inst.getIndexOfMaxSumRGYB()) // need to do a intuitive choose
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
        bool flag = sumRGYB[2] + sumRGYB[3] == 0 || sumRGYB[0] + sumRGYB[1]==0;
        return false;
    }

    public void ChooseView(int i)
    {
        if (isAllTrue()&&(i >= 0 && i < 4))
        {
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            float tempSum = Manager.inst.getSumRGYB(i);
            if (playerNum == 1 && chooseNum < 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] = tempSum;
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";

            }
            else if (playerNum == 2 && chooseNum ==1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
            }
            else if (playerNum == 2 && chooseNum == 2)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("GetValues", RpcTarget.All);
            }

        }
    }

    [PunRPC]
    private void Choose(int i, int playerNum)
    {
        buttonsValueText[i].text = "Taken By Player " + playerNum;
        colorsValueObj[i].GetComponent<Button>().interactable= false;
        //colorsValueObj[i].SetActive(false);
        chooseNum++;

    }
    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = { "RED-X", "GREEN-O", "RED-O", "GREEN-X" };
        return "" + strColors[color]+" with value: "+ value+"\n";
    }

    [PunRPC]
    private void GetValues()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int last = GetLast();
        buttonsValueText[last].text = "Taken By Player 1";
        if (PhotonNetwork.IsMasterClient)
        {
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
            p.setSpriteStatus(spriteStatus,1);

        }
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
        lock (this) { 
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
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        if (Manager.inst.getIsShowView())
        {
            SetMap(playerNum == 1 ? this.savePlayer1 : this.savePlayer2, playerNum);
        }
        else if (isAllTrue())
        {
            Dictionary<int, int> tempPlayer = getTempPlayer();
            if (playerNum == 1) savePlayer1 = tempPlayer;
            else savePlayer2 = tempPlayer;
            setOtherCutMap(); // Set other player map after cut
        }
        else
        {

            Dictionary<int, int> tempPlayer = getTempPlayer();
            if (playerNum == 1) savePlayer1 = tempPlayer;
            else savePlayer2 = tempPlayer;
            //setOtherHeatmap(); // Set other player heatmap in green color
            setOtherCutMap();
        }
        Manager.inst.changeSeeOtherPlayerBT();


    }



    // Update the map based on the received data.
    private void SetMap(Dictionary<int, int> tempPlayer, int playerNum)
    {
        if(tempPlayer==null) return;

        //float[] sumRGYB = { 0, 0 }; //GREEN, RED, YELLOW, BLUE
        //int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float val = p.getMyVal(playerNum);
            int tempKey = p.getMyKey();
            int spriteStatus = tempPlayer[tempKey] ; // Red, Green, Yellow or Blue

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


    private void setOtherCutMap()
    {
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            p.setSpriteStatus(p.getSpriteStatus(), 2); //set green and pther val power
        }
    }


    // Send message from the Game Manger
    public void sendMangerMessage(string message)
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
        
        if (this.chatHistory.Count() >= 11)
        {
            this.chatHistory.RemoveAt(0);
        }
        ChatMessage tempMessage = new ChatMessage(name, newMessage);
        this.chatHistory.Add(tempMessage);
    }

    // Updates the chat UI by displaying all chat messages in the chat history list.
    // Clears the existing text in the chat UI before updating.
    private void getAllMessages()
    {
        this.chatText.text = ""; // Clear the text before updating

        foreach (ChatMessage m in chatHistory)
        {
            this.chatText.text += m.ToString() + "\n"; // Add each message followed by a newline
        }
    }

    // Sends the message to the other players
    [PunRPC]
    private void sendMessageToAll(string name, string newMessage)
    {
        //Manager.inst.setNewMessage();
        addMessageToList(name, newMessage);
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
            if (helpCutPlayer.ContainsKey(val)&& helpCutPlayer[val]>0)
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

        if (this.palyersGameOver[0]&& this.palyersGameOver[1])
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
            //Debug.LogError("scenarioNum= " + roundNumberConfig);
            if (roundNumberConfig == 2)
            {

                int scenarioNum = UnityEngine.Random.Range(2, 5); // Create next 2 scenarios for round 3 and 4
                
                //Config.inst.createConfig(scenarioNum);
                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, scenarioNum);
            }
            else
            {

                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName,-1);
            }
        }
    }
    [PunRPC]
    private void RequestSceneLoad(string sceneName,int scenarioNum)
    {
        if (scenarioNum > 0)
        {

            Config.inst.createConfig(scenarioNum);
        }
        // Master client loads the scene on behalf of non-master clients
        PhotonNetwork.LoadLevel(sceneName);
    }
}



