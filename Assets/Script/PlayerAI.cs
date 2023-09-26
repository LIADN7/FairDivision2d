using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

//using static UnityEditor.Experimental.GraphView.GraphView;


public class PlayerAI : MonoBehaviour
{

    public static PlayerAI inst;
    //private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private bool[] playerCut = { false, true };
    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;
    [SerializeField] protected Text yellowValueText;
    [SerializeField] protected Text blueValueText;
    [SerializeField] protected Text palyerName;
    //[SerializeField] protected Text chatText;
    //[SerializeField] protected InputField chatInput;
    [SerializeField] protected GameObject[] colorsValueObj; //GREEN, RED, YELLOW, BLUE


    private float[] sumPlayer = { 0f, 0f }; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f };
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    //private HashValues HelpHashValue;
    // Saves the player's state
    private Dictionary<int, int> savePlayer1 = new Dictionary<int, int>();
    //private Dictionary<int, int> savePlayer2 = new Dictionary<int, int>();
    private int chooseNum = 0;
    private float[] sumRGYBPlayer2 = { 0f, 0f, 0f, 0f };
    private string[] endExplanationPlayers = { "You get:\n", "You get:\n" };


    void Start()
    {

        inst = this;
        string palyerId = "Player 1";
        palyerName.text = palyerId;




    }


    public void InitSquares()
    {

        GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
        StartCoroutine(withSec(0.5f, tempSquares));

    }

    private IEnumerator withSec(float sec, GameObject[] tempSquares)
    {
        yield return new WaitForSeconds(sec);

        int j1 = 1, j2 = 2;
        for (int i = 0; i < tempSquares.Length; i++)
        {

            PointOfStateAI temp = tempSquares[i].GetComponent<PointOfStateAI>();
            if (!this.squares.ContainsKey(temp.getMyKey()))
            {
                this.squares.Add(temp.getMyKey(), tempSquares[i]);

            }
            this.sumPlayer[j1 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfStateAI>().getMyVal(j1);
            this.sumPlayer[j2 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfStateAI>().getMyVal(j2);

        }

        int playerNum = 1;
        int otherPlayerNum = 2;
        foreach (var it in squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            p.setmyPowerColor(playerNum, this.sumPlayer[playerNum - 1]);
            p.setOtherPowerColor(otherPlayerNum, this.sumPlayer[otherPlayerNum - 1]);
            p.setSpriteStatus(1, 1);
        }
        greenValueText.text = "All green value: " + (0) + "%";
        redValueText.text = "All red value: " + (100) + "%";
        

        // Check for values counter
        //------------------------
/*        this.HelpHashValue = new HashValues();
        this.HelpHashValue.buildHelp(this.squares);*/
        //h.printHelp(1);
        //h.printHelp(2);

        //--------------------
    }



    public void CutView()
    {
        float cutSum = this.statusChange();
        //if (cutSum == 50) { 
        int playerNum = 1;
        Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
        int randStatus = 1;
        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            int status = p.getSpriteStatus();
            int key = p.getMyKey();
            tempPlayer.Add(key, status);
            status = randStatus == 1? 1:2;
            this.player2.Add(key, status);
            randStatus *= -1;
        }
        //view.RPC("Cut", RpcTarget.All, );
        Cut(tempPlayer, playerNum);
        Manager.inst.setViewAfterCut();


        setAllState();
        

    }


    public void Cut(Dictionary<int, int> tempPlayer, int i)
    {

        this.player1 = tempPlayer;
        this.playerCut[i - 1] = true;
        Manager.inst.setViewToChoose();
    }

    public void setAllState()
    {
        //GREEN, RED, YELLOW, BLUE
        int playerNum = 1;
        float[] sumRGYB = createSumRGYB(playerNum);
        this.sumRGYBPlayer2 = createSumRGYB(2);
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            sumRGYB[k] = (sumRGYB[k] / this.sumPlayer[playerNum - 1]) * 100;

        }

        Manager.inst.setSumRGYB(sumRGYB);
        redValueText.text = "All red value: " + (sumRGYB[0]) + "%";
        greenValueText.text = "All green value: " + (sumRGYB[1]) + "%";
        yellowValueText.text = "All yellow value: " + (sumRGYB[2]) + "%";
        blueValueText.text = "All blue value: " + (sumRGYB[3]) + "%";
        Manager.inst.initialSeeOtherPlayerBT();
        Manager.inst.setNote(-1, "The meaning of the colors:\nRed = 2 players choose red\nGreen = 2 players choose green\nYellow = p1 red, p2 green\nBlue = p1 green, p2 red\n\nPlease choose one color", false);
        

    }




    public void ChooseView(int i)
    {
        if (isAllTrue() && (i >= 0 && i < 4))
        {
            int playerNum = 1;
            int player2 = 2;
            float tempSum = Manager.inst.getSumRGYB(i);
            if (playerNum == 1 && chooseNum < 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                choosePlayer[playerNum - 1] = tempSum;
                Choose(i);
                //view.RPC("Choose", RpcTarget.All, i);
                i = getMaxI();
                tempSum = Manager.inst.getSumRGYB(i);
                endExplanationPlayers[player2] += getExplanationPlayer(i, tempSum);
                choosePlayer[player2] += tempSum;
                Choose(i);

                i = getMaxI();
                tempSum = Manager.inst.getSumRGYB(i);
                endExplanationPlayers[player2] += getExplanationPlayer(i, tempSum);
                choosePlayer[player2] += tempSum;
                Choose(i);

                GetValues();
                //view.RPC("GetValues", RpcTarget.All);

            }


        }
    }

    private void Choose(int i)
    {
        colorsValueObj[i].SetActive(false);
        chooseNum++;

    }

    // Need to fix and do it good
    private int getMaxI()
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

    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = { "Red", "Green", "Yellow", "Blue" };
        return "" + strColors[color] + " with value: " + value + "\n";
    }


    private void GetValues()
    {
        int playerNum = 1;
        int last = GetLast();

            float tempSum = Manager.inst.getSumRGYB(last);
            endExplanationPlayers[playerNum - 1] += getExplanationPlayer(last, tempSum);
            choosePlayer[playerNum - 1] += tempSum;
            chooseNum++;
        
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
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
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
            p.setSpriteStatus(spriteStatus, 1);

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
        lock (this)
        {
            float sumG = 0;
            int playerNum = 1;

            foreach (var it in squares)
            {
                PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
                if (p.getSpriteStatus() == 2)
                {
                    sumG += p.getMyVal(playerNum);
                }

            }


            float greenVal = (sumG / this.sumPlayer[playerNum - 1]);
            greenValueText.text = "Value: " + (greenVal * 100) + "%";
            redValueText.text = "Value: " + ((1 - greenVal) * 100) + "%";
            return (greenVal * 100);
        }
    }

    // This method is used to retrieve the other player's Heatmap.
    public void getOtherView()
    {
        int playerNum = 1;
        int otherPlayerNum = 2;
        if (Manager.inst.getIsShowView())
        {
            SetMap(this.savePlayer1, playerNum);
        }
        else if (isAllTrue())
        {
            Dictionary<int, int> tempPlayer = getTempPlayer();
            if (playerNum == 1) savePlayer1 = tempPlayer;
            setOtherCutMap(); // Set other player map after cut
        }
        else
        {

            Dictionary<int, int> tempPlayer = getTempPlayer();
            if (playerNum == 1) savePlayer1 = tempPlayer;
            setOtherHeatmap(); // Set other player heatmap in green color
        }
        Manager.inst.changeSeeOtherPlayerBT();


    }



    // Update the map based on the received data.
    private void SetMap(Dictionary<int, int> tempPlayer, int playerNum)
    {
        if (tempPlayer == null) return;

        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            float val = p.getMyVal(playerNum);
            int tempKey = p.getMyKey();
            int spriteStatus = tempPlayer[tempKey]; // Red, Green, Yellow or Blue
            p.setSpriteStatus(spriteStatus, 1);

        }

    }

    // Get temporary player data for sending.
    private Dictionary<int, int> getTempPlayer()
    {
        Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
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
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            p.setSpriteStatus(2, 2); //set green and pther val power
        }
    }


    private void setOtherCutMap()
    {
        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            p.setSpriteStatus(p.getSpriteStatus(), 2); //set green and pther val power
        }
    }



/*    public void getHelpToCut()
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

        }
        statusChange();
    }*/


}



