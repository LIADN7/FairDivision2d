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
    public Room[] rooms = {null, null, null };
    // Start is called before the first frame update

    private void Awake()
    {
        if (LauncherInst == null)
        {
            LauncherInst = this;
            if (!PhotonNetwork.IsConnected)
                PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = "0.0.1";
        }


    }

    void Start()
    {
        //LauncherInst = this;
        this.menu.SetActive(false);
        this.loadingScreen.SetActive(true);
        
        this.loadingText.text = "Connecting to network...";
        //PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
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
        rooms[0] = PhotonNetwork.CurrentRoom;
        PhotonNetwork.LoadLevel(roomNames[0]);
    }

/*    public override void OnJoinedRoom()
    {
        this.loadingScreen.SetActive(false);
        PhotonNetwork.LoadLevel(roomNames[0]);
    }*/


    public void CreateGame1()
    {
        if (playerName.text != "")
        {

                PhotonNetwork.NickName= playerName.text;
            if (rooms[0] == null)
            {
                RoomOptions opt = new RoomOptions();
                opt.MaxPlayers= 2;

                this.menu.SetActive(false);
                this.loadingScreen.SetActive(true); 
                this.loadingText.text = "Create room...";
                
                PhotonNetwork.JoinOrCreateRoom(roomNames[0], opt, PhotonNetwork.CurrentLobby);
            }
            else
            {
                
                this.loadingText.text = "Joining room...";
                this.loadingScreen.SetActive(true);
                PhotonNetwork.JoinRoom(roomNames[0]);
            }
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
    // Update is called once per frame
    void Update()
    {
        //print(PhotonNetwork.CurrentRoom);
    }
}