using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerVsAI : MonoBehaviour
{
    public static PlayerVsAI inst;
    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;
    [SerializeField] protected Text yellowValueText;
    [SerializeField] protected Text blueValueText;
    [SerializeField] protected Text palyerName;
    [SerializeField] protected GameObject[] colorsValueObj; //GREEN, RED, YELLOW, BLUE
    
    private float[] sumRGYBPlayer2 = { 0, 0, 0, 0 };

    private float[] sumPlayer = { 0f, 0f }; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f }; // sum of all the squares thatthe player choose (1 and 2) -> {1, 2}
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>(); //???????

    // Saves the player's state
    private Dictionary<int, int> savePlayer1 = new Dictionary<int, int>();
    private Dictionary<int, int> savePlayer2 = new Dictionary<int, int>(); //???????
    private int chooseNum = 0; //???????
    private string endExplanationPlayers = "You get:\n" ;
    private bool playerCut = false; 
    private bool palyersGameOver = false;



    // Start is called before the first frame update
    void Start()
    {
        inst = this;
        palyerName.text = "Demo Game, You Are Player 1";
        InitSquares();



    }

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

    }
    public void CutView()
    {
        float cutSum = this.statusChange();

        Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            int status = p.getSpriteStatus();
            int key = p.getMyKey();
            tempPlayer.Add(key, status);

        }
        this.player1 = tempPlayer;
        this.playerCut = true;
        this.getHelpToCut();
        Manager.inst.setViewToChoose();
        Manager.inst.setViewAfterCut();
        setAllState();
        // Need to do!
        // view.RPC("setAllState", RpcTarget.All);


    }

    //Need to check!
    private void getHelpToCut()
    {
        int playerNum = 2;
        // (value, amount of state with that val)
        Dictionary<float, int> helpCutPlayer = new Dictionary<float, int>{
            { 2f, 44 },
            { 4f, 92 },
            { 6f, 60 },
            { 8f, 32 },
            { 10f, 10 }
        };

        Dictionary<int, int> tempPlayer = new Dictionary<int, int>(); // (State key, status)
        foreach (var it in this.squares)
        {
            PointOfStateAI p = it.Value.GetComponent<PointOfStateAI>();
            int tempKey = p.getMyKey();
            float val = p.getMyVal(playerNum);
            if (helpCutPlayer.ContainsKey(val) && helpCutPlayer[val] > 0) 
            {
                helpCutPlayer[val]--;
                tempPlayer.Add(tempKey, 2);
            }
            else
            {
                tempPlayer.Add(tempKey, 1);
            }


        }
        this.player2 = tempPlayer;

    }

    private void setAllState()
    {
        //GREEN, RED, YELLOW, BLUE
        int playerNum = 1, otherPlayerNum = 2;

        this.sumRGYBPlayer2 = createSumRGYB(otherPlayerNum); // Set other player values
        float[] sumRGYB = createSumRGYB(playerNum);
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            //Debug.LogError(sumRGYB[k] + "   " + this.sumPlayer[playerNum - 1]);
            sumRGYB[k] = (sumRGYB[k] / this.sumPlayer[playerNum - 1]) * 100;
            this.sumRGYBPlayer2[k] = (this.sumRGYBPlayer2[k] / this.sumPlayer[otherPlayerNum - 1]) * 100;
        }
/*        for (int k = 0; k < sumRGYB.Length; k++)
        {

            Debug.Log("Player1[ " + k + "]: " + sumRGYB[k]);
            Debug.Log("Player2[ " + k + "]: " + sumRGYBPlayer2[k]);
        }*/
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
            greenValueText.text = "Part 1 value: " + (greenVal * 100) + "%";
            redValueText.text = "Part 2 value: " + ((1 - greenVal) * 100) + "%";
            return (greenVal * 100);
        }
    }


    public void ChooseView(int i)
    {
        if (this.playerCut && (i >= 0 && i < 4))
        {
            int playerNum = 1;
            float tempSum = Manager.inst.getSumRGYB(i);
            this.endExplanationPlayers += getExplanationPlayer(i, tempSum);
            choosePlayer[playerNum - 1] = tempSum;
            //colorsValueObj[i].SetActive(false);
            chooseNum++;
            int last = getValuesForPlayer2(i);
            tempSum = Manager.inst.getSumRGYB(last);
            this.endExplanationPlayers += getExplanationPlayer(last, tempSum);
            choosePlayer[playerNum - 1] += tempSum;
            //colorsValueObj[i].SetActive(false);
            chooseNum++;
            for (int k=0; k < this.colorsValueObj.Length; k++)
            {
                colorsValueObj[k].SetActive(false);
            }

            Manager.inst.setNote(-1, endExplanationPlayers, false);
            Manager.inst.setEndGameLayer(choosePlayer[playerNum - 1]);

            /*
                        if (playerNum == 1 && chooseNum < 1)
                        {
                            endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                            choosePlayer[playerNum - 1] = tempSum;
                            view.RPC("Choose", RpcTarget.All, i);
                        }
                        else if (playerNum == 2 && chooseNum == 1)
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
            */
        }
    }
    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = { "Red", "Green", "Yellow", "Blue" };
        return "" + strColors[color] + " with value: " + value + "\n";
    }


    /** 
     takenNum -> the color that player 1 choose
     Return the last color left
    */
    private int getValuesForPlayer2(int takenNum)
    {
        int i = takenNum==0 ? 1 : 0;
        int last=i;
        for(; i < this.sumRGYBPlayer2.Length; i++)
        {
            if (takenNum != i)
            {
                if(this.sumRGYBPlayer2[i]< this.sumRGYBPlayer2[last])
                    last= i;
            }
        }
        return last;
    }

}
