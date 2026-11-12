
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
        // C1: only before a connection. MainScene also loads while still connected (Home/exit, the connection notice's
        // return, EndGameFlow -> Home); setting GameVersion then would move the next master-server authentication to
        // "0.0.1_2.42", a separate matchmaking pool. IsConnected stays true while switching from the game server back to
        // the master server. A fresh start is unchanged: ConnectUsingSettings() in Start overwrites this with the settings'
        // AppVersion, so the client connects as before (empty AppVersion -> "_2.42").
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.GameVersion = "0.0.1";
        }
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

    // Cloud Save batch: early cache of the experiment's shared run ID (room property) and this client's role. Reliable
    // only for the room creator: on the joining client the room's scene sync can unload the lobby before this callback
    // runs (observed in test). The authoritative capture is Player.InitDatabaseAsync at the start of every real round,
    // which both clients run inside the room before their first research save.
    public override void OnJoinedRoom()
    {
        ExperimentRun.CaptureFromRoom();
    }

    /*    public override void OnJoinedRoom()
        {
            this.loadingScreen.SetActive(false);
            PhotonNetwork.LoadLevel(roomNames[0]);
        }*/



    // Batch 6 (owner-approved): the new-experiment boundary on this client. The Cut-and-Choose room buttons
    // (CreateAITutorialGame1, CreateGame1, CreateTetsGame1) call this before createConfig(..., 0) re-initialises the
    // condition and round counter. Here the client is in the lobby: not in a room, no map cells loaded, no round
    // message pending, and the previous session's EndGameFlow (Finish/save) is gone. Session-local state then starts
    // exactly as after a page load: map-cell keys from 0 (the Cut RPC carries them; both clients must count from the
    // same start) and an empty result list for the round-6 payment draw. Rounds within the experiment are untouched.
    private static void ResetExperimentSessionState()
    {
        int previousKeys = PointOfState.ResetKeyCounter();
        int previousPracticeKeys = PointOfStateAITutorial.ResetKeyCounter();
        int previousResults = RandArrays.inst != null ? RandArrays.inst.Clear() : 0;
        ExperimentRun.BeginNewExperiment(); // Cloud Save batch: the next room provides a new shared run ID
        Debug.Log("SESSION-RESET: new experiment; cell key counter " + previousKeys + " -> 0, practice key counter "
            + previousPracticeKeys + " -> 0, results cleared " + previousResults);
    }

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
            ResetExperimentSessionState();
            Config.inst.createConfig(scenarioNumber,0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;
            opt.CustomRoomProperties = ExperimentRun.NewRoomProperties(); // used only if this client creates the room

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
            ResetExperimentSessionState();
            Config.inst.createConfig(scenarioNumber,0);
            //           if (rooms[0] == null)
            //         {
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;
            opt.CustomRoomProperties = ExperimentRun.NewRoomProperties(); // used only if this client creates the room

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
            ResetExperimentSessionState();
            Config.inst.createConfig(scenarioNumber, 0);
            RoomOptions opt = new RoomOptions();
            opt.MaxPlayers = 2;
            opt.CustomRoomProperties = ExperimentRun.NewRoomProperties(); // used only if this client creates the room

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