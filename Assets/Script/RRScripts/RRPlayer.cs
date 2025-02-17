using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public class RRPlayer : MonoBehaviour
{
    private PhotonView view;
    public static RRPlayer inst;
    [SerializeField] protected Text[] buttonsValueText; // {red, green, yellow, blue}
    [SerializeField] protected Text palyerName;
    [SerializeField] protected Text chatText;
    [SerializeField] protected InputField chatInput;


    private float[] sumPlayer = { 0f, 0f }; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f }; // sum of all the squares that the player choose (1 and 2) -> {1, 2}
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    [SerializeField] protected RRStates[] stateObjects; // All the state obj with square childs
    // private Dictionary<int, int> player1 = new Dictionary<int, int>();
    // private Dictionary<int, int> player2 = new Dictionary<int, int>();
    // private string[] endExplanationPlayers = { "You get:\n", "You get:\n" };
    // private bool[] palyersGameOver = { false, false };
    // [SerializeField] protected GameObject[] colorsValueObj; // {red, green, yellow, blue}
    // private string[] playerSaveBtTexts = { "", "", "", "" };
    // private HashValues HelpHashValue;
    // Saves the player's state
    // private Dictionary<int, int> savePlayer1 = new Dictionary<int, int>();
    // private Dictionary<int, int> savePlayer2 = new Dictionary<int, int>();
    // private bool[] playerCut = { false, false };



    // Start is called before the first frame update
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
        Debug.Log("Round Number - " + Config.inst.getRoundNumber());

        // this.InitDatabaseAsync();
        //ManagerRR.inst.setNote(-1, "Start to choose square", false);
    }

    public async Task InitDatabaseAsync()
    {
        // this.gameDatabase.ScenarioNum = Config.inst.getRoundNumber();
        // this.gameDatabase.ScenarioType = Config.inst.getScenarioTypeName();
        // this.gameDatabase.PlayerStartTime = DateTime.Now;
        // this.gameDatabase.PlayerName = PhotonNetwork.NickName;
        // //this.gameDatabase.PlayerID = 20548; // Need to add the real id from the user
        // int[] tempChoosen = { -1, -1 };
        // this.gameDatabase.PlayerChoosenColors = tempChoosen;
        // this.SetDataFromURL();
    }

    [PunRPC]
    public void InitSquares()
    {

        GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
        Debug.Log("Round Number - " + Config.inst.getRoundNumber());
        StartCoroutine(InitGameAfterSec(0.1f, tempSquares));


    }

    private IEnumerator InitGameAfterSec(float sec, GameObject[] tempSquares)
    {
        yield return new WaitForSeconds(sec);

        int j1 = 1, j2 = 2;
        for (int i = 0; i < tempSquares.Length; i++)
        {

            PointOfStateRR temp = tempSquares[i].GetComponent<PointOfStateRR>();
            if (!this.squares.ContainsKey(temp.getMyKey()))
            {
                this.squares.Add(temp.getMyKey(), tempSquares[i]);

            }
            this.sumPlayer[j1 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfStateRR>().getMyVal(j1);
            this.sumPlayer[j2 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfStateRR>().getMyVal(j2);

        }

        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        foreach (var it in squares)
        {
            PointOfStateRR p = it.Value.GetComponent<PointOfStateRR>();
            p.setmyPowerColor(playerNum, this.sumPlayer[playerNum - 1]);
            p.setOtherPowerColor(otherPlayerNum, this.sumPlayer[otherPlayerNum - 1]);
            p.setSpriteStatus(1, 1);
        }
        this.statusChange();
        this.setAllStateTitle(playerNum);


    }

    public void setAllStateTitle(int playerNum)
    {
        for (int i = 0; i < this.stateObjects.Length; i++)
        {
            this.stateObjects[i].SetMyTitle(playerNum, this.sumPlayer[playerNum - 1]);
        }
    }

    public float statusChange()
    {
        lock (this)
        {
            float sumG = 0;
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;

            foreach (var it in squares)
            {
                PointOfStateRR p = it.Value.GetComponent<PointOfStateRR>();
                if (p.getSpriteStatus() == 2)
                {
                    sumG += p.getMyVal(playerNum);
                }

            }
            float greenVal = (sumG / this.sumPlayer[playerNum - 1]);
            // buttonsValueText[0].text = "Part 1 value: " + ((1 - greenVal) * 100) + "%";
            // buttonsValueText[1].text = "Part 2 value: " + (greenVal * 100) + "%";
            if ((greenVal * 100) == 50)
            {
                // Manager.inst.setStatusClick(0);
                // Manager.inst.setNote(-1, "The map is divided into 50%\n(if you wish, you can change the division or choose not to divide equally)", false);

            }
            return (greenVal * 100);
        }
    }

    // Send this message
    public void sendMessage2()
    {
        // if (this.chatInput.text.Length >= 1)
        // {
        //     addMessageToList(PhotonNetwork.NickName, this.chatInput.text);
        //     view.RPC("sendMessageToAll", RpcTarget.Others, PhotonNetwork.NickName, this.chatInput.text);
        //     this.chatInput.text = "";
        //     getAllMessages();

        // }

    }

    // Adds a new chat message to the chat history list.
    // If the chat history has more than 11 messages, it removes the oldest message.
    private void addMessageToList(string name, string newMessage)
    {
        // Save only the last 200 messeges 
        // if (this.chatHistory.Count() >= 200)
        // {
        //     this.chatHistory.RemoveAt(0);
        // }
        // ChatMessage tempMessage = new ChatMessage(name, newMessage);
        // this.chatHistory.Add(tempMessage);
        // this.gameDatabase.ChatHistory = this.chatHistory; // Save chat (this func is called twice)

    }

    // Updates the chat UI by displaying all chat messages in the chat history list.
    // Clears the existing text in the chat UI before updating.
    private void getAllMessages()
    {
        // this.chatText.text = ""; // Clear the text before updating
        // int i = 0;
        // foreach (ChatMessage m in chatHistory)
        // {
        //     // Show only the first 10 messeges
        //     if (chatHistory.Count - i < 11)
        //     {
        //         this.chatText.text += m.ToString() + "\n"; // Add each message followed by a newline
        //     }
        //     i++;
        // }

    }

    // Sends the message to the other players
    [PunRPC]
    private void sendMessageToAll(string name, string newMessage)
    {
        // addMessageToList(name, newMessage);
        // // save chat for the sender player
        // getAllMessages();
    }
}
