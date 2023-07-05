using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
//using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviourPunCallbacks
{
    [SerializeField] public GameObject cutBt;
    private PhotonView view;
    public static Player inst;
    //private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private bool[] playerCut = { false, false };
    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;
    [SerializeField] protected Text yellowValueText;
    [SerializeField] protected Text blueValueText;
    [SerializeField] protected Text palyerName;
    [SerializeField] protected GameObject[] colorsValueObj; //GREEN, RED, YELLOW, BLUE


    private float[] sumPlayer = {0f,0f};
    private float[] choosePlayer = { 0f, 0f };
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    private int chooseNum = 0;


    void Start()
    {

        view = GetComponent<PhotonView>();
        inst = this;
        palyerName.text = PhotonNetwork.NickName + ", ID: ";
        if(PhotonNetwork.IsMasterClient)
            view.RPC("InitSquares", RpcTarget.AllBufferedViaServer);

    }

    [PunRPC]
    public void InitSquares()
    {

            GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
            StartCoroutine(withSec(2f, tempSquares));

        


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

    }

    // Update is called once per frame
    void Update()
    {
        
        //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
    }

    public void CutView()
    {
        int i = PhotonNetwork.IsMasterClient ? 1 : 2;
        Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            int status = p.getSpriteStatus();
            int key = p.getMyKey();
            tempPlayer.Add(key, status);
            //p.setSpriteStatus(1);
        }
        view.RPC("Cut", RpcTarget.All, tempPlayer, i);
        this.cutBt.SetActive(false);
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
            Debug.LogError("1");
            this.player1 = tempPlayer;
        }
        else if (i == 2)
        {
            Debug.LogError("2");
            this.player2 = tempPlayer;
        }
        this.playerCut[i - 1] = true;
        Debug.LogError("1: " + this.playerCut[0] + ", 2: " + this.playerCut[1]);
        if (isAllTrue())
        {
            Manager.inst.setViewAfterCut();
            //setAllState(player1, player2);
        }
       

    }

    [PunRPC]
    public void setAllState()
    {
        //GREEN, RED, YELLOW, BLUE
        int j = PhotonNetwork.IsMasterClient ? 1 : 2;
        float[] sumRGYB = createSumRGYB(j); 
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            Debug.LogError(sumRGYB[k] + "   " + this.sumPlayer[j - 1]);
            sumRGYB[k] = (sumRGYB[k] / this.sumPlayer[j - 1]) * 100;

        }

        Manager.inst.setSumRGYB(sumRGYB);
        redValueText.text = "Value: " + (sumRGYB[0]) + "%";
        greenValueText.text = "Value: " + (sumRGYB[1]) + "%";
        yellowValueText.text = "Value: " + (sumRGYB[2]) + "%";
        blueValueText.text = "Value: " + (sumRGYB[3]) + "%";

    }




    public void ChooseView(int i)
    {
        if (isAllTrue()&&(i >= 0 && i < 4))
        {
            int j = PhotonNetwork.IsMasterClient ? 1 : 2;
            if (j == 1 && chooseNum < 1)
            {
                choosePlayer[j - 1] = +Manager.inst.getSumRGYB(i);
                view.RPC("Choose", RpcTarget.All, i);
            }
            else if (j == 2 && chooseNum ==1)
            {
                choosePlayer[j - 1] += Manager.inst.getSumRGYB(i);
                view.RPC("Choose", RpcTarget.All, i);
            }
            else if (j == 2 && chooseNum == 2)
            {
                choosePlayer[j - 1] += Manager.inst.getSumRGYB(i);
                view.RPC("Choose", RpcTarget.All, i);
                view.RPC("GetValues", RpcTarget.All);
            }

        }
    }

    [PunRPC]
    private void Choose(int i)
    {
        //chooseObj
        colorsValueObj[i].SetActive(false);
        chooseNum++;

    }

    [PunRPC]
    private void GetValues()
    {
        int j = PhotonNetwork.IsMasterClient ? 1 : 2;
        int last = GetLast();
        if (PhotonNetwork.IsMasterClient)
        {
            choosePlayer[j - 1] += Manager.inst.getSumRGYB(last);
            chooseNum++;
        }
        colorsValueObj[last].SetActive(false);
        
        Debug.LogError("I get: " + choosePlayer[j - 1]);
    }

    //[PunRPC]
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
            p.setSpriteStatus(spriteStatus);

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



    public void statusChange()
    {
        float sumG = 0;
        int j = PhotonNetwork.IsMasterClient ? 1 : 2;

        foreach (var it in squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            if (p.getSpriteStatus() == 2)
            {
                sumG += p.getMyVal(j);
            }

        }



        float greenVal = (sumG / this.sumPlayer[j-1]);
        greenValueText.text = "Value: " + (greenVal * 100) + "%";
        redValueText.text = "Value: " + ((1 - greenVal) * 100) + "%";

    }





}
