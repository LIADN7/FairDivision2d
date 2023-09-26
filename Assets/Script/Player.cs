using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
//using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviourPunCallbacks
{
    //[SerializeField] public GameObject cutBt;
    private PhotonView view;
    public static Player inst;
    //private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private bool[] playerCut = { false, false };
    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;
    [SerializeField] protected Text yellowValueText;
    [SerializeField] protected Text blueValueText;
    [SerializeField] protected Text palyerName;
    [SerializeField] protected Text chatText;
    [SerializeField] protected InputField chatInput;
    [SerializeField] protected GameObject[] colorsValueObj; //GREEN, RED, YELLOW, BLUE


    private float[] sumPlayer = {0f,0f}; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f };
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


    void Start()
    {

        view = GetComponent<PhotonView>();
        inst = this;
        string palyerId = PhotonNetwork.IsMasterClient ? "Player 1" : "Player 2";
        palyerName.text = PhotonNetwork.NickName + ", "+ palyerId;
        if (PhotonNetwork.IsMasterClient)
            view.RPC("InitSquares", RpcTarget.AllBufferedViaServer);



    }

    [PunRPC]
    public void InitSquares()
    {

            GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
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
        greenValueText.text = "All green value: " + (0) + "%";
        redValueText.text = "All red value: " + (100) + "%";

        // Check for values counter
        //------------------------
        this.HelpHashValue = new HashValues();
        this.HelpHashValue.buildHelp(this.squares);
        //h.printHelp(1);
        //h.printHelp(2);

        //--------------------
    }

    // Update is called once per frame
    void Update()
    {
        
        //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
    }

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
        redValueText.text = "All part 1 value: " + (sumRGYB[0]) + "%";
        greenValueText.text = "All part 2 value: " + (sumRGYB[1]) + "%";
        yellowValueText.text = "All part 3 value: " + (sumRGYB[2]) + "%";
        blueValueText.text = "All part 4 value: " + (sumRGYB[3]) + "%";
        Manager.inst.initialSeeOtherPlayerBT();
        if (playerNum == 1)
        {
            Manager.inst.setNote(-1, "The meaning of the colors:\nRed = 2 players choose red\nGreen = 2 players choose green\nYellow = p1 red, p2 green\nBlue = p1 green, p2 red\n\nPlease choose one color", false);
        }
        else if (playerNum == 2)
        {
            Manager.inst.setNote(-1, "The meaning of the colors:\nRed = 2 players choose red\nGreen = 2 players choose green\nYellow = p1 red, p2 green\nBlue = p1 green, p2 red\n\nPlease wait player 1 choose one color,\nafter that you choose 2 colors", false);
        }
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
                view.RPC("Choose", RpcTarget.All, i);
            }
            else if (playerNum == 2 && chooseNum ==1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                view.RPC("Choose", RpcTarget.All, i);
            }
            else if (playerNum == 2 && chooseNum == 2)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                view.RPC("Choose", RpcTarget.All, i);
                view.RPC("GetValues", RpcTarget.All);
            }

        }
    }

    [PunRPC]
    private void Choose(int i)
    {
        colorsValueObj[i].SetActive(false);
        chooseNum++;

    }
    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = {"Red","Green","Yellow","Blue" };
        return "" + strColors[color]+" with value: "+ value+"\n";
    }

    [PunRPC]
    private void GetValues()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int last = GetLast();
        if (PhotonNetwork.IsMasterClient)
        {
            float tempSum = Manager.inst.getSumRGYB(last);
            endExplanationPlayers[playerNum - 1] += getExplanationPlayer(last, tempSum);
            choosePlayer[playerNum - 1] += tempSum;
            chooseNum++;
        }
        colorsValueObj[last].SetActive(false);
        Manager.inst.setNote(-1, endExplanationPlayers[playerNum - 1], false);
        Manager.inst.setEndGameLayer(choosePlayer[playerNum - 1]);
    }

    // Get the last number of the remaining color
    private int GetLast()
    {
        //chooseObj
        int i = 0;
        for (int j = 0; j < colorsValueObj.Length; j++)
        {

            if (colorsValueObj[j].activeSelf)
                i = j;
        }
        return i;

    }


    private float[] createSumRGYB(int j)
    {
        float[] sumRGYB = { 0, 0, 0, 0 }; //GREEN, RED, YELLOW, BLUE

        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float val = p.getMyVal(j);
            int tempKey = p.getMyKey();
            int spriteStatus = 1; // RED

            if (this.player1[tempKey] == 2 && this.player2[tempKey] == 2) // GREEN
            {
                spriteStatus = 2;
            }
            else if (this.player1[tempKey] == 1 && this.player2[tempKey] == 2) // YELLOW
            {
                spriteStatus = 3;
            }
            else if (this.player1[tempKey] == 2 && this.player2[tempKey] == 1) // BLUE
            {
                spriteStatus = 4;
            }

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
            greenValueText.text = "Part 1 value: " + (greenVal * 100) + "%";
            redValueText.text = "Part 2 value: " + ((1 - greenVal) * 100) + "%";
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

    public void ExitGame(string sceneName)
    {
        Manager.inst.setNote(-1,"Other player left the game...", false); // need to send in RPC!
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // seems its need to wait?
            SceneManager.LoadScene(sceneName);

        }
    }
}



