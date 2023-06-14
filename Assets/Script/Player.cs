using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
//using static UnityEditor.Experimental.GraphView.GraphView;

public class Player : MonoBehaviourPunCallbacks
{
    [SerializeField] public Text myNameText;
    [SerializeField] public GameObject cutBt;
    private PhotonView view;
    //private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private int isSet = 0;

    void Start()
    {
        view = GetComponent<PhotonView>();

        myNameText.text = PhotonNetwork.NickName + ", ID: ";

/*        myNameText.text = "Player: "+ PhotonNetwork.CountOfPlayersInRooms;
        print(Launcher.LauncherInst.rooms[0]);

        print(PhotonNetwork.CurrentRoom);
        

        for (int i = 0;i< PhotonNetwork.PlayerList.Length;i++)
            print(PhotonNetwork.PlayerList[i]);*/
    }

    // Update is called once per frame
    void Update()
    {
        
        //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
    }

    public void CutView()
    {
        if(isSet < 2)
        {
        view.RPC("Cut", RpcTarget.All);

        }
    }

    [PunRPC]
    public void Cut()
    {
        Dictionary<int, int> player = new Dictionary<int, int>();
        foreach (var it in GameManager.inst.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            player.Add(p.getMyKey(), p.getSpriteStatus());
            p.setSpriteStatus(1);
        }
        GameManager.inst.Cut(player);
        isSet++;
        /*        double sum=0;
                for (int j = 0; j < NUM_OF_STATE; j++)
                {
                    this.playersValues[0, j] = valuesText[j].text == "" ? 0 : Convert.ToDouble(valuesText[j].text);
                    valuesText[j].text = "";
                    sum += this.playersValues[0, j];
                    this.states[j].GetComponent<StateObj>().setChanges();

                }
                for (int j = 0; j < NUM_OF_STATE; j++)
                {
                    this.playersValues[0, j] = this.playersValues[0, j] / sum;
                }
                this.cutButton.SetActive(false);
                this.chooseButtons.SetActive(true);
                this.status++;
                return true;*/

    }

}
