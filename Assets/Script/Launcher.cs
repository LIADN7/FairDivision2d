using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using TMPro;


public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher LauncherInst;



    public GameObject loadingScreen;
    public GameObject menu;
    public TMP_Text loadingText;
    public Text playerName;


    private string[] roomNames = { "CutScene", "Room2", "Room3" };
    //public Room[] rooms = {null, null, null };
    // Start is called before the first frame update

    private void Awake()
    {
/*            GameObject[] objs = GameObject.FindGameObjectsWithTag("Launcher");
            if (objs.Length > 1)
            {
                Destroy(this.gameObject); // Destroy duplicate GameManager instances
            }
            DontDestroyOnLoad(this.gameObject); // Don't destroy this instance when loading new scenes*/

            LauncherInst = this;
            if (!PhotonNetwork.IsConnected)
                PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = "0.0.1";
        


    }

    void Start()
    {
        //LauncherInst = this;
        if (!PhotonNetwork.IsConnected)
        {

            this.menu.SetActive(false);
        this.loadingScreen.SetActive(true);
        
        this.loadingText.text = "Connecting to network...";
        //PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        }
    }



    public override void OnConnectedToMaster()
    {

        PhotonNetwork.JoinLobby();
        this.loadingText.text = "Joining lobby...";

    }

    public override void OnJoinedLobby()
    {
        this.loadingScreen.SetActive(false);
        this.menu.SetActive(true);
    }


    public override void OnCreatedRoom()
    {
        this.loadingScreen.SetActive(false);
        //rooms[0] = PhotonNetwork.CurrentRoom;
        PhotonNetwork.LoadLevel(roomNames[0]);
    }

/*    public override void OnJoinedRoom()
    {
        this.loadingScreen.SetActive(false);
        PhotonNetwork.LoadLevel(roomNames[0]);
    }*/


    public void CreateGame1(int scenarioNumber)
    {
        if (playerName.text != "")
        {


             PhotonNetwork.NickName= playerName.text;

             Config.inst.createConfig(scenarioNumber);
 //           if (rooms[0] == null)
   //         {
                RoomOptions opt = new RoomOptions();
                opt.MaxPlayers= 2;

                this.menu.SetActive(false);
                this.loadingScreen.SetActive(true); 
                this.loadingText.text = "Create room...";
                Debug.Log(PhotonNetwork.JoinOrCreateRoom(roomNames[0], opt, PhotonNetwork.CurrentLobby));
                
/*            }
            else
            {
                
                this.loadingText.text = "Joining room...";
                this.loadingScreen.SetActive(true);
                
                PhotonNetwork.JoinRoom(roomNames[0]);
            }*/
          /*       
           PhotonNetwork.CreateRoom(roomNames[0], opt);
                        this.menu.SetActive(false);
                        this.loadingText.text = "Create room...";*/
            print(PhotonNetwork.CurrentLobby);
            print(PhotonNetwork.CurrentRoom);

            //loadingScreen.SetActive(true);
            //PhotonNetwork.LoadLevel(roomNames[0]);
            //SceneManager.LoadScene(roomNames[0]);
        }

        
    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {

            // Display a message to the user indicating that the room is full
            Debug.Log("Room is full. Cannot join.");
            this.menu.SetActive(true);
            this.loadingScreen.SetActive(false);
            // You can also display this message on your UI if needed.
        

    }





    // Update is called once per frame
    void Update()
    {
        //print(PhotonNetwork.CurrentRoom);
    }
}
