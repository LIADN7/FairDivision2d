
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;
using TMPro;


public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher LauncherInst;



    public GameObject loadingScreen;
    public GameObject menu;
    public TMP_Text loadingText;
    public Text playerName;
    public Text playerEmail;

    [SerializeField] public Text[] roomTextButtons;
    [SerializeField] public Text roomStatusText; // Lobby message shown when joining a room fails


    private string[] roomNames = { "Room_1", "Room_2", "Room_3", "Room_4" };
    private string[] sceneName = { "CutScene" };
    // private string sceneToLoad = "CutScene";
    private string sceneToLoad = "CutSceneAITutorial";
    //public Room[] rooms = {null, null, null };
    private int roomCounter = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string TestGameVersion = "uxtest"; // Photon AppVersion becomes "uxtest_<PUN version>"
#endif


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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test isolation: MainScene also loads while still connected (Home/Leave). Setting GameVersion
        // rewrites the AppVersion used for every later authentication, so keep the test pool here too.
        PhotonNetwork.GameVersion = TestGameVersion;
        Debug.Log("TEST MODE: Photon AppVersion = " + PhotonNetwork.AppVersion + " (lobby load, connected=" + PhotonNetwork.IsConnected + ")");
#else
        PhotonNetwork.GameVersion = "0.0.1";
#endif




    }

    void Start()
    {
        //LauncherInst = this;
        if (!PhotonNetwork.IsConnected)
        {


            this.loadingText.text = "Connecting to the game server...";
            //PhotonNetwork.AutomaticallySyncScene = true;
            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Test isolation: Editor and Development builds join a separate Photon matchmaking pool
            // (same App ID and region, different AppVersion) so tests never meet research participants.
            AppSettings testSettings = PhotonNetwork.PhotonServerSettings.AppSettings.CopyTo(new AppSettings());
            testSettings.AppVersion = TestGameVersion;
            PhotonNetwork.ConnectUsingSettings(testSettings);
            Debug.Log("TEST MODE: Photon AppVersion = " + PhotonNetwork.AppVersion);
#else
            PhotonNetwork.ConnectUsingSettings();
#endif
        }
        else
        {
            this.menu.SetActive(true);
            this.loadingScreen.SetActive(false);
            Debug.Log("Connected");
        }
        for (int i = 0; i < roomTextButtons.Length; i++)
        {
            roomTextButtons[i].text = $"Room {i + 1}  (0/2 players)";
        }
    }



    public override void OnConnectedToMaster()
    {

        PhotonNetwork.JoinLobby();
        this.loadingText.text = "Loading the lobby...";

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
        PhotonNetwork.LoadLevel(this.sceneToLoad);
    }

    /*    public override void OnJoinedRoom()
        {
            this.loadingScreen.SetActive(false);
            PhotonNetwork.LoadLevel(roomNames[0]);
        }*/



    //=================================================
    //------------------- GAME AI TUTORIAL 1 ----------------
    //=================================================

    public void CreateAITutorialGame1(int roomIndex)
    {
        if (playerName.text != "")
        {

            PlayerData.inst.SetPlayerData(this.playerName.text, this.playerEmail.text, "", "");
            PhotonNetwork.NickName = playerName.text;
            int scenarioNumber = 1;
            Config.inst.createConfig(scenarioNumber,0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;

            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
            this.loadingText.text = "Joining the room...";
            Debug.Log(PhotonNetwork.JoinOrCreateRoom(roomNames[roomIndex < roomNames.Length ? roomIndex : 0], opt, PhotonNetwork.CurrentLobby));

            print(PhotonNetwork.CurrentLobby);
            print(PhotonNetwork.CurrentRoom);
        }
    }

    //=================================================
    //------------------- GAME 1 ----------------
    //=================================================

    public void CreateGame1(int roomIndex)
    {
        if (playerName.text != "")
        {

            PlayerData.inst.SetPlayerData(this.playerName.text, this.playerEmail.text, "", "");
            PhotonNetwork.NickName = playerName.text;
            int scenarioNumber = 1;
            Config.inst.createConfig(scenarioNumber,0);
            //           if (rooms[0] == null)
            //         {
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;

            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
            this.loadingText.text = "Joining the room...";
            Debug.Log(PhotonNetwork.JoinOrCreateRoom(roomNames[roomIndex < roomNames.Length ? roomIndex : 0], opt, PhotonNetwork.CurrentLobby));

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

    // =================================================
    //------------------- TEST GAME 1 ----------------  
    //=================================================
    public void CreateTetsGame1(int scenarioNumber)
    {
        if (playerName.text != "")
        {

            PlayerData.inst.SetPlayerData(this.playerName.text, this.playerEmail.text, "", "");
            PhotonNetwork.NickName = playerName.text;
            Config.inst.createConfig(scenarioNumber, 0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;

            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
            this.loadingText.text = "Joining the room...";
            Debug.Log(PhotonNetwork.JoinOrCreateRoom(roomNames[0], opt, PhotonNetwork.CurrentLobby));

            print(PhotonNetwork.CurrentLobby);
            print(PhotonNetwork.CurrentRoom);

        }
    }

    //=================================================
    //------------------- GAME 2 - RR ----------------
    //=================================================
    public void CreateGame2(int scenarioNumber)
    {
        if (playerName.text != "")
        {


            PhotonNetwork.NickName = playerName.text;

            Config.inst.createConfig(scenarioNumber, 0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;

            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
            this.loadingText.text = "Joining the room...";
            this.sceneToLoad = "DemoScene";
            Debug.Log(PhotonNetwork.JoinOrCreateRoom("DemoScene", opt, PhotonNetwork.CurrentLobby));
            print(PhotonNetwork.CurrentLobby);
            print(PhotonNetwork.CurrentRoom);
        }


    }

    //=================================================
    //------------------- DEMO GAME ----------------
    //=================================================
    public void CreateDemoGame(int scenarioNumber)
    {
        if (playerName.text != "")
        {


            PhotonNetwork.NickName = playerName.text;

            Config.inst.createConfig(scenarioNumber, 0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;

            this.menu.SetActive(false);
            this.loadingScreen.SetActive(true);
            this.loadingText.text = "Joining the room...";
            this.sceneToLoad = "RRScene";
            // this.sceneToLoad = "RRTestScene";
            Debug.Log(PhotonNetwork.JoinOrCreateRoom(this.sceneToLoad, opt, PhotonNetwork.CurrentLobby));
            print(PhotonNetwork.CurrentLobby);
            print(PhotonNetwork.CurrentRoom);
        }


    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {

        // Display a message to the user indicating that the room is full
        Debug.Log("Room is full. Cannot join.");
        this.menu.SetActive(true);
        this.loadingScreen.SetActive(false);
        if (this.roomStatusText != null)
        {
            this.roomStatusText.text = "This room is full or unavailable.\nPlease choose another room.";
        }


    }



    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            for (int i = 0; i < roomNames.Length; i++)
            {
                if (room.Name == roomNames[i])
                {
                    roomTextButtons[i].text = $"Room {i + 1}  ({room.PlayerCount}/2 players)";
                }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        //print(PhotonNetwork.CurrentRoom);
    }



}