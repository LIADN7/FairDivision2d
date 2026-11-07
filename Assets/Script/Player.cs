using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using TMPro;
//using static UnityEditor.Experimental.GraphView.GraphView;


public class Player : MonoBehaviourPunCallbacks
{
    //[SerializeField] public GameObject cutBt;
    private PhotonView view;
    public static Player inst;
    // private Dictionary<int, int> player1 = new Dictionary<int, int>();
    [SerializeField] protected Text[] buttonsValueText; // {red, green, yellow, blue}
    [SerializeField] protected Text palyerName;
    [SerializeField] protected Text chatText;
    [SerializeField] protected InputField chatInput;
    // Display only (UX-013/UX-031): readable Hebrew chat lines with a sender label. The legacy chatText stays hidden
    // but is still written. chatIsLocal mirrors chatHistory one-to-one and is never sent or saved.
    [SerializeField] protected TextMeshProUGUI chatMessagesRTL;
    private readonly List<bool> chatIsLocal = new List<bool>();
    private bool chatMessagesMaterialReady = false;
    private const string chatOwnLabelRTL = "אני";
    private const string chatOtherLabelRTL = "השחקן השני";
    private const int chatShownMessages = 10; // same as the legacy display (last 10)
    [SerializeField] protected GameObject[] colorsValueObj; // {red, green, yellow, blue}
    // Display only: Hebrew labels shown on the region buttons (the legacy buttonsValueText stay hidden but are still written)
    [SerializeField] protected TextMeshProUGUI[] buttonsValueTextRTL; // {red, green, yellow, blue}
    // Display only: turn status under the region buttons, derived from the role and chooseNum
    [SerializeField] protected Image turnLabelPlate;
    [SerializeField] protected TextMeshProUGUI turnLabelRTL;
    // Display only: cut-phase status derived from the line state and the cuts; hidden once both cuts exist
    [SerializeField] protected Image cutStatusPlate;
    [SerializeField] protected TextMeshProUGUI cutStatusRTL;
    // Display only: which revealed line belongs to whom, from the actual line colours
    [SerializeField] protected Image lineOwnerPlate;
    [SerializeField] protected TextMeshProUGUI lineOwnerRTL;
    // Display only: dim tint plus a centred message while the other player has to act (no raycast targets)
    [SerializeField] protected GameObject waitingOverlay;
    [SerializeField] protected TextMeshProUGUI waitingOverlayRTL;
    private const string cutReadoutPlaceholderRTL = "שרטטו קו כדי לראות את השווי";
    private static readonly string[] regionNamesRTL = { "חלק 1 - אדום", "חלק 2 - ירוק", "חלק 3 - צהוב", "חלק 4 - כחול" };
    private static readonly string[] cutSideNamesRTL = { "הצד האדום", "הצד הירוק" };


    private float[] sumPlayer = { 0f, 0f }; // sum of all the squares for player 1 and 2 -> {1, 2}
    private float[] choosePlayer = { 0f, 0f }; // sum of all the squares that the player choose (1 and 2) -> {1, 2}
    private float lastOwnGreenVal = 0f; // Display only: last own green share from statusChange (0..1)
    private LineDrawingManager cachedLineManager;
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private HashValues HelpHashValue;
    [SerializeField] protected SumSquares[] stateObjects; // All the state obj with square childs
    private int chooseNum = 0;
    private string[] endExplanationPlayers = { "You get:\n", "You get:\n" };
    private string[] endExplanationPlayersRTL = { "קיבלתם:\n", "קיבלתם:\n" };
    private bool[] playerCut = { false, false };
    private bool[] palyersGameOver = { false, false };

    private CutAndChoosePlayerDatabase gameDatabase = new CutAndChoosePlayerDatabase();
    //private Task databaseTask;
    [DllImport("__Internal")]
    private static extern System.IntPtr GetCurrentUrl();
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

        this.InitDatabaseAsync();
        Manager.inst.setNote(-1, "Please divide the map into 2 parts (if you divide into 2 equal parts you are guaranteed to get at least 50%)\n\nYou can redraw it until you click the confirm button.", false, "אנא חלקו את המפה לשני חלקים (אם תחלקו לשני חלקים שווים, מובטח שתקבלו לפחות 50%)\n\nאפשר לשרטט מחדש עד שתלחצו על אישור החלוקה.");


    }


    public async Task InitDatabaseAsync()
    {
        this.gameDatabase.ScenarioNum = Config.inst.getRoundNumber();
        this.gameDatabase.ScenarioType = Config.inst.getScenarioTypeName();
        this.gameDatabase.PlayerStartTime = DateTime.Now;
        this.gameDatabase.PlayerName = PhotonNetwork.NickName;
        //this.gameDatabase.PlayerID = 20548; // Need to add the real id from the user
        int[] tempChoosen = { -1, -1 };
        this.gameDatabase.PlayerChoosenColors = tempChoosen;
        this.SetDataFromURL();

        /*        await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Init cloud");*/
        //int palyerId = PhotonNetwork.IsMasterClient ? 0 : 1;

        //this.databaseTask =this.gameDatabase.InitializeCloud();
    }

    private void SetDataFromURL()
    {
        string surveyID = "defaultSurveyID";
        string userID = "defaultUserID";
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            System.IntPtr urlPtr = GetCurrentUrl();
            string jsonInput = Marshal.PtrToStringUTF8(urlPtr);
            // Debug.Log("aaaaa= " + urlPtr);
            // Debug.Log("jsonInput= " + jsonInput);
            if (string.IsNullOrEmpty(jsonInput) || jsonInput == "{}")
            {
                Debug.Log("Received empty JSON, using default values.");
            }
            else
            {
                try
                {
                    var parsedData = JsonUtility.FromJson<SurveyData>(jsonInput);
                    if (!string.IsNullOrEmpty(parsedData.surveyID))
                    {
                        surveyID = parsedData.surveyID;
                    }
                    if (!string.IsNullOrEmpty(parsedData.userID))
                    {
                        userID = parsedData.userID;
                    }
                    Debug.Log("surveyID= " + surveyID + " , userID= " + userID);
                }

                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing JSON: {ex.Message}");
                }
            }
            //Marshal.PtrToStringUTF8(urlPtr);
        }
        this.gameDatabase.userID = userID;
        this.gameDatabase.surveyID = surveyID;
        Config.inst.userID = userID;
        Config.inst.surveyID = surveyID;
        if (PlayerData.inst != null)
        {
            PlayerData.inst.SurveyID = surveyID;
            PlayerData.inst.UserID = userID;
        }
    }

    [Serializable]
    private class SurveyData
    {
        public string surveyID;
        public string userID;
    }

    [PunRPC]
    public void InitSquares()
    {

        GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");
        int palyerId = PhotonNetwork.IsMasterClient ? 0 : 1;


        Debug.Log("Round Number - " + Config.inst.getRoundNumber());
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
            this.sumPlayer[j1 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfState>().getMyVal(j1);
            this.sumPlayer[j2 - 1] += this.squares[temp.getMyKey()].GetComponent<PointOfState>().getMyVal(j2);

        }

        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        foreach (var it in squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            p.setmyPowerColor(playerNum, this.sumPlayer[playerNum - 1]);
            p.setOtherPowerColor(otherPlayerNum, this.sumPlayer[otherPlayerNum - 1]);
            p.setSpriteStatus(1, playerNum);
        }
        /*        greenValueText.text = "All green value: " + (0) + "%";
                redValueText.text = "All red value: " + (100) + "%";*/
        this.statusChange();
        this.setAllStateTitle(playerNum);
        StartCoroutine(SetCutBt(1f));

        // Check for values counter
        //------------------------
        // this.HelpHashValue = new HashValues();
        // this.HelpHashValue.buildHelp(this.squares);
        //h.printHelp(1);
        //h.printHelp(2);

        //--------------------
    }

    private IEnumerator SetCutBt(float sec)
    {
        yield return new WaitForSeconds(sec);
        Manager.inst?.cutBt.SetActive(true);
        UpdatePhaseInstruction();
    }

    public void setAllStateTitle(int playerNum)
    {
        for (int i = 0; i < this.stateObjects.Length; i++)
        {
            this.stateObjects[i].SetMyTitle(playerNum, this.sumPlayer[playerNum - 1]);
        }
    }
    // Update is called once per frame
    /*    void Update()
        {

            //myNameText.text = "Player: " + Launcher.LauncherInst.rooms[0];
        }*/


    public void CutView()
    {
        // Submit the drawn line to prevent further editing
        GameObject lineManagerObj = GameObject.Find("LineDrawingManager");
        if (lineManagerObj != null)
        {
            var lineManager = lineManagerObj.GetComponent<LineDrawingManager>();
            if (lineManager != null)
            {
                lineManager.SubmitLine();
            }
        }

        float cutSum = this.statusChange();
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            this.gameDatabase.SetPlayerTimeCut();
            float[] cutVal = { 100 - cutSum, cutSum };
            this.gameDatabase.SetPlayerCutValues(cutVal);
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            Dictionary<int, int> tempPlayer = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, string>> tempPlayerWithParentName = new Dictionary<int, Dictionary<int, string>>();
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                int status = p.getSpriteStatus();
                int key = p.getMyKey();
                string stateName = p.getMyStateName();
                tempPlayer.Add(key, status);

                tempPlayerWithParentName.Add(key, new Dictionary<int, string>() { { status, stateName } });

                //p.setSpriteStatus(1);
            }
            this.gameDatabase.playerCutSquares = tempPlayerWithParentName;
            view.RPC("Cut", RpcTarget.All, tempPlayer, playerNum);
            // UX-004 (owner-approved): no local drawing after the cut was sent, with or without a line.
            FindLineManager()?.LockDrawingAfterCut();
            Manager.inst.setViewAfterCut();
            // Display only: after the own cut the readout shows the submitted split (also for a cut without a line).
            if (!isAllTrue())
            {
                SetCutReadoutRTL(lastOwnGreenVal, false);
            }
            UpdatePhaseInstruction();
            // RpcTarget.All runs Cut locally at once: for the second cutter setAllState has already shown the turn note.
            if (!isAllTrue())
            {
                // Display only: the waiting overlay shows this; the note paper is cleared (hidden while empty).
                Manager.inst.setNote(-1, "", false, "");
            }
        }
        else
        {
            Manager.inst.setNote(7f, "Waiting for the other player to connect!", true, "ממתינים להתחברות השחקן השני!");
        }
        /*
        if (isAllTrue())
        {
            view.RPC("setAllState", RpcTarget.All);
            float[] tempMySum = Manager.inst.getAllSumRGYB();
            this.gameDatabase.SetPlayerColorValues(tempMySum);
            view.RPC("SetForOtherPlayerColorValuesDataBase", RpcTarget.Others, tempMySum);
            //setAllState();
        }
        */

    }

    [PunRPC]
    private void SetForOtherPlayerColorValuesDataBase(float[] tempOtherSum)
    {

        this.gameDatabase.SetOtherPlayerColorValues(tempOtherSum);
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

            // Set other player name on the database
            photonView.RPC("UpdateOtherPlayerName", RpcTarget.Others, this.gameDatabase.PlayerName);

            // Calculate and save state for both players
            setAllState();
            float[] tempMySum = Manager.inst.getAllSumRGYB();
            this.gameDatabase.SetPlayerColorValues(tempMySum);
            view.RPC("SetForOtherPlayerColorValuesDataBase", RpcTarget.Others, tempMySum);

            //setAllState(player1, player2);
        }


    }

    [PunRPC]
    public void setAllState()
    {
        // Display both players' lines when both have submitted
        GameObject lineManagerObj = GameObject.Find("LineDrawingManager");
        if (lineManagerObj != null)
        {
            var lineManager = lineManagerObj.GetComponent<LineDrawingManager>();
            if (lineManager != null)
            {
                lineManager.ShowBothLines();
            }
        }

        //GREEN, RED, YELLOW, BLUE
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        float[] sumRGYB = createSumRGYB(playerNum);
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            //Debug.LogError(sumRGYB[k] + "   " + this.sumPlayer[playerNum - 1]);
            sumRGYB[k] = (sumRGYB[k] / this.sumPlayer[playerNum - 1]) * 100;

        }

        Manager.inst.setSumRGYB(sumRGYB);
        buttonsValueText[0].text = "Part 1 (X) value: " + (sumRGYB[0]) + "%";
        buttonsValueText[1].text = "Part 2 (O) value: " + (sumRGYB[1]) + "%";
        buttonsValueText[2].text = "Part 3 (O) value: " + (sumRGYB[2]) + "%";
        buttonsValueText[3].text = "Part 4 (X) value: " + (sumRGYB[3]) + "%";
        for (int k = 0; k < sumRGYB.Length; k++)
        {
            SetRegionLabelRTL(k, "שווה לכם: " + sumRGYB[k].ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "%");
        }
        Manager.inst.initialSeeOtherPlayerBT();
        if (isIntuitive(sumRGYB))
        {
            //if(Manager.inst.getIndexOfMaxSumRGYB()) // need to do a intuitive choose (Maybe we will create in the future)
        }
        else
        {

            if (playerNum == 1)
            {
                Manager.inst.setNote(-1, "", false, "");
            }
            else if (playerNum == 2)
            {
                Manager.inst.setNote(-1, "", false, "");
            }
        }
        UpdatePhaseInstruction();
        UpdateLineOwnerLabel(FindLineManager());
    }

    // Display only: Hebrew region label "חלק N - צבע" plus a second line (value or who took it).
    private void SetRegionLabelRTL(int i, string secondLine)
    {
        SetButtonLabelRTL(i, regionNamesRTL[i] + "\n" + secondLine);
    }

    // Display only: cut-phase readout on the red/green buttons (0, 1) with the unchanged value precision.
    private void SetCutSideLabelRTL(int i, string secondLine)
    {
        SetButtonLabelRTL(i, (i < cutSideNamesRTL.Length ? cutSideNamesRTL[i] : regionNamesRTL[i]) + "\n" + secondLine);
    }

    // Display only: the live cut readout on the red/green buttons, same values and precision as the legacy text.
    private void SetCutReadoutRTL(float greenVal, bool showingOtherPlayer)
    {
        string prefix = showingOtherPlayer ? "שווה לשחקן השני: " : "שווה לכם: ";
        SetCutSideLabelRTL(0, prefix + ((1 - greenVal) * 100) + "%");
        SetCutSideLabelRTL(1, prefix + (greenVal * 100) + "%");
    }

    private LineDrawingManager FindLineManager()
    {
        if (cachedLineManager == null)
        {
            GameObject lineManagerObj = GameObject.Find("LineDrawingManager");
            cachedLineManager = lineManagerObj != null ? lineManagerObj.GetComponent<LineDrawingManager>() : null;
        }
        return cachedLineManager;
    }

    // Display only: the single phase instruction ("what do I do now?") and the waiting overlay, derived from the
    // line state, Manager.isCut, isAllTrue, the local role and chooseNum. It gates nothing; all rules keep their own checks.
    // Cut phase uses the plate above the cut button, choose phase the plate under the region buttons; at most one shows.
    public void UpdatePhaseInstruction()
    {
        if (Manager.inst == null)
        {
            return;
        }
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        bool bothCut = isAllTrue();
        string action = null;
        string waiting = null;
        LineDrawingManager lineManager = FindLineManager();
        if (!bothCut)
        {
            if (Manager.inst.getIsCut())
            {
                waiting = "ממתינים לחיתוך של השחקן השני";
            }
            else if (lineManager != null && !Manager.inst.getIsShowView())
            {
                action = lineManager.HasDrawnLine() ? "לחצו על אישור החלוקה" : "שרטטו קו על המפה";
            }
        }
        else if (chooseNum < 3)
        {
            if (playerNum == 1 && chooseNum == 0)
            {
                action = "בחרו חלק אחד";
            }
            else if (playerNum == 2 && chooseNum == 1)
            {
                action = "בחרו שני חלקים";
            }
            else if (playerNum == 2 && chooseNum == 2)
            {
                action = "בחרו חלק נוסף";
            }
            else
            {
                waiting = "ממתינים לבחירת השחקן השני";
            }
        }
        if (GameSessionDialogs.IsConnectionLost)
        {
            // UX-016: the connection notice explains the state; no round instruction or waiting message under it.
            action = null;
            waiting = null;
        }
        SetInstructionPlate(cutStatusPlate, cutStatusRTL, bothCut ? null : action);
        SetInstructionPlate(turnLabelPlate, turnLabelRTL, bothCut ? action : null);
        if (waitingOverlay != null && waitingOverlayRTL != null)
        {
            waitingOverlay.SetActive(waiting != null);
            if (waiting != null)
            {
                waitingOverlayRTL.text = waiting;
            }
        }
    }

    private static void SetInstructionPlate(Image plate, TextMeshProUGUI label, string text)
    {
        if (plate == null || label == null)
        {
            return;
        }
        plate.gameObject.SetActive(text != null);
        if (text != null)
        {
            label.text = text;
        }
    }

    // Display only: names the revealed lines by their actual colours (own line renderer, other player's colour).
    private void UpdateLineOwnerLabel(LineDrawingManager lineManager)
    {
        if (lineOwnerPlate == null || lineOwnerRTL == null)
        {
            return;
        }
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        string mine = null, other = null;
        if (lineManager != null && lineManager.playerLineRenderer != null)
        {
            // Name a line only if it is actually shown (a cut submitted without a line has none).
            if (lineManager.playerLineRenderer.enabled)
            {
                mine = LineColorNameRTL(lineManager.playerLineRenderer.startColor);
            }
            foreach (LineRenderer line in lineManager.GetComponentsInChildren<LineRenderer>())
            {
                if (line != lineManager.playerLineRenderer && line.enabled)
                {
                    other = LineColorNameRTL(otherPlayerNum == 1 ? lineManager.player1LineColor : lineManager.player2LineColor);
                }
            }
        }
        if (mine != null && mine == other)
        {
            mine = other = null;
        }
        string text = "";
        if (mine != null)
        {
            text = "הקו " + mine + ": החיתוך שלכם";
        }
        if (other != null)
        {
            text += (text.Length > 0 ? "\n" : "") + "הקו " + other + ": החיתוך של השחקן השני";
        }
        bool show = isAllTrue() && text.Length > 0;
        lineOwnerPlate.gameObject.SetActive(show);
        if (show)
        {
            lineOwnerRTL.text = text;
        }
    }

    private static string LineColorNameRTL(Color c)
    {
        if (c.r < 0.2f && c.g < 0.2f && c.b < 0.2f) return "השחור";
        if (c.r > 0.8f && c.g > 0.8f && c.b > 0.8f) return "הלבן";
        return null;
    }

    private void SetButtonLabelRTL(int i, string text)
    {
        if (buttonsValueTextRTL == null || i < 0 || i >= buttonsValueTextRTL.Length || buttonsValueTextRTL[i] == null)
        {
            return;
        }
        buttonsValueTextRTL[i].text = Manager.PrepareRtlNote(text);
    }

    private bool isIntuitive(float[] sumRGYB)
    {
        bool flag = sumRGYB[2] + sumRGYB[3] == 0 || sumRGYB[0] + sumRGYB[1] == 0;
        return false;
    }

    public void ChooseView(int i)
    {
        if (isAllTrue() && (i >= 0 && i < 4))
        {
            int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
            float tempSum = Manager.inst.getSumRGYB(i);
            if (playerNum == 1 && chooseNum < 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                endExplanationPlayersRTL[playerNum - 1] += getExplanationPlayerRTL(i, tempSum);
                choosePlayer[playerNum - 1] = tempSum;
                this.gameDatabase.SetPlayerChoosenColors(0, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "Player 1 chose one part.\nIt is your turn to choose two parts.", "שחקן 1 בחר חלק אחד.\nתורכם לבחור שני חלקים.");
                Manager.inst.setNote(-1, "", false, "");


            }
            else if (playerNum == 2 && chooseNum == 1)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                endExplanationPlayersRTL[playerNum - 1] += getExplanationPlayerRTL(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                this.gameDatabase.SetPlayerChoosenColors(0, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "Player 2 chose one part.\nPlease wait for the other player to choose.", "שחקן 2 בחר חלק אחד.\nהמתינו לבחירת השחקן השני.");
                Manager.inst.setNote(-1, "", false, "");
            }
            else if (playerNum == 2 && chooseNum == 2)
            {
                endExplanationPlayers[playerNum - 1] += getExplanationPlayer(i, tempSum);
                endExplanationPlayersRTL[playerNum - 1] += getExplanationPlayerRTL(i, tempSum);
                choosePlayer[playerNum - 1] += tempSum;
                this.gameDatabase.SetPlayerChoosenColors(1, i);
                view.RPC("Choose", RpcTarget.All, i, playerNum);
                buttonsValueText[i].text += " (You)";
                view.RPC("GetValues", RpcTarget.All);
                view.RPC("SetNoteForOtherPlayer", RpcTarget.Others, "You got the last remaining part", "קיבלתם את החלק האחרון שנשאר");
                // No local note here: GetValues (already run locally via RpcTarget.All) shows the result breakdown,
                // which must stay visible. The remainder is shown on its region button.
            }

        }
    }

    [PunRPC]
    private void Choose(int i, int playerNum)
    {
        buttonsValueText[i].text = "Taken By Player " + playerNum;
        int localPlayerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        SetRegionLabelRTL(i, playerNum == localPlayerNum ? "נבחר על ידכם" : "נבחר על ידי שחקן " + playerNum);
        colorsValueObj[i].GetComponent<Button>().interactable = false;
        //colorsValueObj[i].SetActive(false);
        chooseNum++;
        UpdatePhaseInstruction();

    }
    [PunRPC]
    private void SetNoteForOtherPlayer(string message, string messageRTL)
    {
        // Display only: the RPC and its texts are unchanged, but turn notes are no longer shown. The phase
        // instruction and the waiting overlay show whose turn it is, and the result breakdown must stay visible.

    }
    private string getExplanationPlayer(int color, float value)
    {
        string[] strColors = { "Red", "Green", "Yellow", "Blue" };
        return "" + strColors[color] + " with value: " + value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%\n";
    }

    private string getExplanationPlayerRTL(int color, float value)
    {
        string[] strColors = { "אדום", "ירוק", "צהוב", "כחול" };
        // Digit order in the RTL note is handled by Manager.PrepareRtlNote; no manual reversal here.
        string valueText = value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        return "" + strColors[color] + ": " + valueText + "%\n";
    }

    [PunRPC]
    private void GetValues()
    {
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int last = GetLast();
        buttonsValueText[last].text = "Taken By Player 1";
        SetRegionLabelRTL(last, PhotonNetwork.IsMasterClient ? "החלק שנשאר - שלכם" : "החלק שנשאר - לשחקן 1");
        if (PhotonNetwork.IsMasterClient)
        {
            this.gameDatabase.SetPlayerChoosenColors(1, last);
            float tempSum = Manager.inst.getSumRGYB(last);
            endExplanationPlayers[playerNum - 1] += getExplanationPlayer(last, tempSum);
            endExplanationPlayersRTL[playerNum - 1] += getExplanationPlayerRTL(last, tempSum);
            choosePlayer[playerNum - 1] += tempSum;
            chooseNum++;
            buttonsValueText[last].text += " (You)";
        }
        colorsValueObj[last].GetComponent<Button>().interactable = false;
        //colorsValueObj[last].SetActive(false);
        Manager.inst.setNote(-1, endExplanationPlayers[playerNum - 1], false, endExplanationPlayersRTL[playerNum - 1]);
        Manager.inst.setEndGameLayer(choosePlayer[playerNum - 1]);
        this.gameDatabase.SumOfMyPart = choosePlayer[playerNum - 1];
        UpdatePhaseInstruction();
    }

    // Get the last number of the remaining color
    private int GetLast()
    {

        //chooseObj

        for (int j = 0; j < colorsValueObj.Length; j++)
        {

            if (colorsValueObj[j].GetComponent<Button>().IsInteractable())
                return j;
        }
        return -1;

    }


    private float[] createSumRGYB(int j)
    {
        float[] sumRGYB = { 0, 0, 0, 0 }; //GREEN, RED, YELLOW, BLUE
        int[] statusCounter = new int[4];

        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float val = p.getMyVal(j);
            int tempKey = p.getMyKey();
            // Calculate sprite status based on both players' choices
            // Same logic as before but using colors instead of X/O text
            int spriteStatus = 1; // Default: Both chose Red
            if (this.player1[tempKey] == 2 && this.player2[tempKey] == 2) // Both Green
            {
                spriteStatus = 2;
            }
            else if (this.player1[tempKey] == 1 && this.player2[tempKey] == 2) // P1 Red, P2 Green
            {
                spriteStatus = 3; // Yellow
            }
            else if (this.player1[tempKey] == 2 && this.player2[tempKey] == 1) // P1 Green, P2 Red
            {
                spriteStatus = 4; // Blue
            }
            // No longer setting text - using colors only
            // p.setChildText(colorText);
            sumRGYB[spriteStatus - 1] += val;
            statusCounter[spriteStatus - 1]++;
            p.setSpriteStatus(spriteStatus, j);

        }
        this.gameDatabase.SetAmountOfColor(statusCounter);
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



    public float statusChange(int? targetPlayerNum = null)
    {
        lock (this)
        {
            float sumG = 0;
            int playerNum = targetPlayerNum ?? (PhotonNetwork.IsMasterClient ? 1 : 2);

            foreach (var it in squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                if (p.getSpriteStatus() == 2)
                {
                    sumG += p.getMyVal(playerNum);
                }

            }
            float greenVal = (sumG / this.sumPlayer[playerNum - 1]);

            // Update button texts based on whether we're showing other player's values or not
            bool showingOtherPlayer = (targetPlayerNum != null && targetPlayerNum != (PhotonNetwork.IsMasterClient ? 1 : 2));
            if (showingOtherPlayer)
            {
                buttonsValueText[0].text = "Other player Part 1: " + ((1 - greenVal) * 100) + "%";
                buttonsValueText[1].text = "Other player Part 2: " + (greenVal * 100) + "%";
            }
            else
            {
                lastOwnGreenVal = greenVal;
                buttonsValueText[0].text = "Part 1 value: " + ((1 - greenVal) * 100) + "%";
                buttonsValueText[1].text = "Part 2 value: " + (greenVal * 100) + "%";
            }
            // Display only: before the first line the default all-red split is not shown as a result.
            LineDrawingManager lineManager = FindLineManager();
            if (lineManager != null && !lineManager.HasLineStarted() && !Manager.inst.getIsCut())
            {
                SetButtonLabelRTL(0, cutReadoutPlaceholderRTL);
                SetButtonLabelRTL(1, cutReadoutPlaceholderRTL);
            }
            else
            {
                SetCutReadoutRTL(greenVal, showingOtherPlayer);
            }
            if ((greenVal * 100) == 50)
            {
                Manager.inst.setNote(-1, "The map is divided into 50%\n(if you wish, you can change the division or choose not to divide equally)", false, "המפה מחולקת ל־50%\n(אם תרצו, תוכלו לשנות את החלוקה או לבחור שלא לחלק באופן שווה)");

            }
            return (greenVal * 100);
        }
    }

    // This method is used to retrieve the other player's Heatmap.
    public void getOtherView()
    {
        // Info rule: the other player's valuation may be viewed only before this player's own cut.
        if (!Config.inst.getSeeOtherEnable() || Manager.inst.getIsCut())
        {
            return;
        }
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        int otherPlayerNum = PhotonNetwork.IsMasterClient ? 2 : 1;
        this.gameDatabase.AddCountPlayerSeeOther();

        if (Manager.inst.getIsShowView())
        {
            Debug.Log("Returning to normal view - showing current real state");

            // Calculate current red and green values for logging
            float redValuePlayer1 = 0f, greenValuePlayer1 = 0f;
            float redValuePlayer2 = 0f, greenValuePlayer2 = 0f;

            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                if (p.getSpriteStatus() == 1) // Red
                {
                    redValuePlayer1 += p.getMyVal(1);
                    redValuePlayer2 += p.getMyVal(2);
                }
                else if (p.getSpriteStatus() == 2) // Green
                {
                    greenValuePlayer1 += p.getMyVal(1);
                    greenValuePlayer2 += p.getMyVal(2);
                }
            }

            // Convert to percentages
            float redPercent1 = (redValuePlayer1 / this.sumPlayer[0]) * 100;
            float greenPercent1 = (greenValuePlayer1 / this.sumPlayer[0]) * 100;
            float redPercent2 = (redValuePlayer2 / this.sumPlayer[1]) * 100;
            float greenPercent2 = (greenValuePlayer2 / this.sumPlayer[1]) * 100;

            Debug.Log($"Current values - Player 1: Red={redPercent1:F5}%, Green={greenPercent1:F5}% | Player 2: Red={redPercent2:F5}%, Green={greenPercent2:F5}%");

            // Show current real state based on current square colors
            // No need to restore old saved state - just calculate current values
            this.statusChange(); // This calculates and displays current player's values
            this.setAllStateTitle(playerNum); // Set titles for current player

            // Ensure squares show current player's values (not other player's)
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                // Keep current color, but show current player's values
                p.setSpriteStatus(p.getSpriteStatus(), playerNum);
            }
        }
        else
        {
            Debug.Log("Switching to other player's view");

            // Calculate current red and green values for logging before switching
            float redValuePlayer1 = 0f, greenValuePlayer1 = 0f;
            float redValuePlayer2 = 0f, greenValuePlayer2 = 0f;

            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                if (p.getSpriteStatus() == 1) // Red
                {
                    redValuePlayer1 += p.getMyVal(1);
                    redValuePlayer2 += p.getMyVal(2);
                }
                else if (p.getSpriteStatus() == 2) // Green
                {
                    greenValuePlayer1 += p.getMyVal(1);
                    greenValuePlayer2 += p.getMyVal(2);
                }
            }

            // Convert to percentages
            float redPercent1 = (redValuePlayer1 / this.sumPlayer[0]) * 100;
            float greenPercent1 = (greenValuePlayer1 / this.sumPlayer[0]) * 100;
            float redPercent2 = (redValuePlayer2 / this.sumPlayer[1]) * 100;
            float greenPercent2 = (greenValuePlayer2 / this.sumPlayer[1]) * 100;

            Debug.Log($"Switching to other view - Player 1: Red={redPercent1:F5}%, Green={greenPercent1:F5}% | Player 2: Red={redPercent2:F5}%, Green={greenPercent2:F5}%");

            // Show other player's view with their values
            float[] sumRGYB = setOtherCutMap(otherPlayerNum);

            // Update button texts to show other player values (using F5 format to match interface)
            buttonsValueText[0].text = "Other player val: " + sumRGYB[0].ToString("F5") + "%";
            buttonsValueText[1].text = "Other player val: " + sumRGYB[1].ToString("F5") + "%";
            buttonsValueText[2].text = "Other player val: " + sumRGYB[2].ToString("F5") + "%";
            buttonsValueText[3].text = "Other player val: " + sumRGYB[3].ToString("F5") + "%";
            // Display only: before the first line the default all-red split is not shown as a result.
            LineDrawingManager lineManager = FindLineManager();
            bool noLineYet = lineManager != null && !lineManager.HasLineStarted();
            for (int k = 0; k < sumRGYB.Length; k++)
            {
                if (noLineYet && k < 2)
                {
                    SetButtonLabelRTL(k, cutReadoutPlaceholderRTL);
                }
                else
                {
                    SetCutSideLabelRTL(k, "שווה לשחקן השני: " + sumRGYB[k].ToString("F5") + "%");
                }
            }
        }

        Manager.inst.changeSeeOtherPlayerBT();
        UpdatePhaseInstruction();
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


    private float[] setOtherCutMap(int otherPlayer)
    {
        float[] sumRGYB = { 0, 0, 0, 0 };
        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            float otherVal = p.getMyVal(otherPlayer);
            sumRGYB[p.getSpriteStatus() - 1] += otherVal;
            p.setSpriteStatus(p.getSpriteStatus(), otherPlayer); //set this color and other player val power
        }
        // Set the other player values on the buttons
        float sumAll = 0;
        for (int i = 0; i < sumRGYB.Length; i++)
        {
            sumAll += sumRGYB[i];
        }
        for (int i = 0; i < sumRGYB.Length; i++)
        {
            sumRGYB[i] = (sumRGYB[i] / sumAll) * 100;
        }

        // Set all the title states
        this.setAllStateTitle(otherPlayer);
        return sumRGYB;
    }

    /*    private  void setAllButtonValues(float[] sumRGYB)
        {
            float sumAll = 0;
            for (int i = 0; i < sumRGYB.Length; i++)
            {
                sumAll += sumRGYB[i];
            }
            for (int i = 0; i < sumRGYB.Length; i++)
            {
                sumRGYB[i] = (sumRGYB[i] / sumAll) * 100;
            }
            buttonsValueText[0].text = "Other player val: " + (sumRGYB[0]) + "%";
            buttonsValueText[1].text = "Other player val: " + (sumRGYB[1]) + "%";
            buttonsValueText[2].text = "Other player val: " + (sumRGYB[2]) + "%";
            buttonsValueText[3].text = "Other player val: " + (sumRGYB[3]) + "%";
        }*/


    // Send message from the Game Manger
    private void sendMangerMessage(string message)
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
            addMessageToList(PhotonNetwork.NickName, this.chatInput.text, true);
            view.RPC("sendMessageToAll", RpcTarget.Others, PhotonNetwork.NickName, this.chatInput.text);
            this.chatInput.text = "";
            getAllMessages();

        }

    }

    // Adds a new chat message to the chat history list.
    // If the chat history has more than 11 messages, it removes the oldest message.
    private void addMessageToList(string name, string newMessage, bool isLocal = false)
    {
        // Save only the last 200 messeges
        if (this.chatHistory.Count() >= 200)
        {
            this.chatHistory.RemoveAt(0);
            if (this.chatIsLocal.Count > 0)
            {
                this.chatIsLocal.RemoveAt(0);
            }
        }
        ChatMessage tempMessage = new ChatMessage(name, newMessage);
        this.chatHistory.Add(tempMessage);
        this.chatIsLocal.Add(isLocal); // Display only: who sent it, from the call site (not from the nickname)
        this.gameDatabase.ChatHistory = this.chatHistory; // Save chat (this func is called twice)

    }

    // Updates the chat UI by displaying all chat messages in the chat history list.
    // Clears the existing text in the chat UI before updating.
    private void getAllMessages()
    {
        this.chatText.text = ""; // Clear the text before updating
        int i = 0;
        foreach (ChatMessage m in chatHistory)
        {
            // Show only the first 10 messeges
            if (chatHistory.Count - i < 11)
            {
                this.chatText.text += m.ToString() + "\n"; // Add each message followed by a newline
            }
            i++;
        }
        RefreshChatMessagesRTL();

    }

    // Display only (UX-013/UX-031): the same last messages in chronological order, newest at the bottom.
    // Own lines: "אני" label, right-aligned; other player's lines: "השחקן השני" label, left-aligned.
    // Oldest lines are left out (not clipped) when they do not fit the panel. The message text is not altered.
    private void RefreshChatMessagesRTL()
    {
        if (this.chatMessagesRTL == null)
        {
            return;
        }
        if (!this.chatMessagesMaterialReady)
        {
            // The shared Hebrew material has a green face colour that tints every vertex colour; this text gets its own
            // runtime instance with a white face so the sender colours show as set (no asset is changed).
            this.chatMessagesRTL.fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            this.chatMessagesMaterialReady = true;
        }
        int first = Math.Max(0, this.chatHistory.Count - chatShownMessages);
        float boxHeight = this.chatMessagesRTL.rectTransform.rect.height;
        this.chatMessagesRTL.enableAutoSizing = false; // measure at the normal size
        // Measured on the laid-out text (GetPreferredValues under-measured the paragraph spacing at runtime).
        float height = SetChatMessagesAndMeasure(first);
        while (first < this.chatHistory.Count - 1 && height > boxHeight)
        {
            first++;
            height = SetChatMessagesAndMeasure(first);
        }
        // Only a single message longer than the whole panel is shrunk (down to fontSizeMin) instead of overflowing.
        this.chatMessagesRTL.enableAutoSizing = height > boxHeight;
    }

    private float SetChatMessagesAndMeasure(int first)
    {
        this.chatMessagesRTL.text = BuildChatMessagesRTL(first);
        this.chatMessagesRTL.ForceMeshUpdate();
        return this.chatMessagesRTL.preferredHeight;
    }

    private string BuildChatMessagesRTL(int first)
    {
        System.Text.StringBuilder lines = new System.Text.StringBuilder();
        for (int k = first; k < this.chatHistory.Count; k++)
        {
            bool isLocal = k < this.chatIsLocal.Count && this.chatIsLocal[k];
            string line = (isLocal ? "<align=right><color=#8FE3FF><b>" + chatOwnLabelRTL : "<align=left><color=#FFD36B><b>" + chatOtherLabelRTL)
                + ":</b></color> <noparse>" + this.chatHistory[k].Message + "</noparse></align>";
            if (lines.Length > 0)
            {
                lines.Append('\n');
            }
            lines.Append(FixRtlForTmp(line));
        }
        return lines.ToString();
    }

    // RTLTMPro's own conversion (as in RTLTextMeshPro.GetFixedText), for a TMP text with isRightToLeftText enabled:
    // Hebrew, Latin words and numbers keep their reading order and rich-text tags stay valid.
    private static string FixRtlForTmp(string input)
    {
        RTLTMPro.FastStringBuilder output = new RTLTMPro.FastStringBuilder(RTLTMPro.RTLSupport.DefaultBufferSize);
        RTLTMPro.RTLSupport.FixRTL(input, output, false, true, true);
        output.Reverse();
        return output.ToString();
    }

    // Sends the message to the other players
    [PunRPC]
    private void sendMessageToAll(string name, string newMessage)
    {
        //Manager.inst.setNewMessage();
        addMessageToList(name, newMessage);
        // save chat for the sender player
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
            if (helpCutPlayer.ContainsKey(val) && helpCutPlayer[val] > 0)
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

    [PunRPC]
    public void NextGame(string sceneName)
    {
        //Manager.inst.setNote(-1,"Other player left the game...", false); // need to send in RPC!
        int playerNum = PhotonNetwork.IsMasterClient ? 1 : 2;
        this.palyersGameOver[playerNum - 1] = true;
        Manager.inst.NextSceneClick();
        this.SaveData();
        if (this.palyersGameOver[0] && this.palyersGameOver[1])
        {

            photonView.RPC("MasterNextGame", RpcTarget.All, sceneName);
        }
        else
        {
            photonView.RPC("GameOverClicked", RpcTarget.Others, playerNum);
        }
    }

    [PunRPC]
    private void GameOverClicked(int playerNum)
    {
        // Master client loads the scene on behalf of non-master clients
        this.palyersGameOver[playerNum - 1] = true;
        Manager.inst.setNote(-1, "Other player continue the game...", false, "השחקן השני ממשיך במשחק..."); // need to send in RPC!
    }

    [PunRPC]
    private void MasterNextGame(string sceneName)
    {
        // Master client loads the scene on behalf of non-master clients
        if (PhotonNetwork.IsMasterClient)
        {
            int roundNumberConfig = Config.inst.addRoundNumber();
            //roundNumberConfig = 4;
            //Debug.LogError("scenarioNum= " + roundNumberConfig);
            if (roundNumberConfig == 2)
            {

                int scenarioNum = UnityEngine.Random.Range(2, 4); // Create next 2 scenarios for round 3 and 4

                //Config.inst.createConfig(scenarioNum);
                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, scenarioNum, roundNumberConfig);
            }
            else if (roundNumberConfig == 4)
            {

                int scenarioNum = 4; // Create next 2 scenarios for round 5 and 6
                //Config.inst.createConfig(scenarioNum);
                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, scenarioNum, roundNumberConfig);
            }
            else if (roundNumberConfig == 6)
            {
                int randomNumber = UnityEngine.Random.Range(0, roundNumberConfig);
                photonView.RPC("SetRandomPaymentRound", RpcTarget.All, randomNumber);
                LeaveGame("EndGameFlow", true); //Save data after click on the ending button!!!!
            }
            else
            {

                photonView.RPC("RequestSceneLoad", RpcTarget.All, sceneName, Config.inst.getScenarioType(), roundNumberConfig);
            }
        }
    }

    [PunRPC]
    private void SetRandomPaymentRound(int randRound)
    {
        PlayerData.inst.SetRandIndex(randRound);
    }

    [PunRPC]
    private void RequestSceneLoad(string sceneName, int scenarioNum, int roundNumberConfig)
    {
        Config.inst.createConfig(scenarioNum, roundNumberConfig);
        // Master client loads the scene on behalf of non-master clients
        PhotonNetwork.LoadLevel(sceneName);
    }


    public void LeaveGame(string sceneName, bool isRedirectToSurvey = false)
    {

        photonView.RPC("LeaveGameForAll", RpcTarget.All, sceneName, isRedirectToSurvey);
    }
    [PunRPC]
    private void UpdateOtherPlayerName(string otherName)
    {
        this.gameDatabase.otherPlayerName = otherName;
    }

    [PunRPC]
    private void LeaveGameForAll(string sceneName, bool isRedirectToSurvey = false)
    {
        // Only for test 
        // Config.inst.RedirectToSurvey(isRedirectToSurvey);
        PhotonNetwork.LeaveRoom();
        StartCoroutine(DelayAndLoadScene(0.2f, sceneName));
    }
    private IEnumerator DelayAndLoadScene(float sec, string sceneName)
    {
        yield return new WaitForSeconds(sec);
        SceneManager.LoadScene(sceneName);
    }

    // This function save the data of the game
    private void SaveData()
    {
        this.gameDatabase.isAgreed = Manager.inst.getIsAgreedMark();
        this.gameDatabase.SaveDatabaseAsync("CutAndChhosedatabase");
    }

    // This function load the data that saved
    public void LoadDatabaseAsync(string path)
    {
        // string loadTestFile = "CutAndChhosedatabase.dat";
        /*string loadTestFile = "./Assets/DataFiles/CutAndChhosedatabase.dat";
        CutAndChoosePlayerDatabase newData = CutAndChoosePlayerDatabase.ImportDatabase(loadTestFile);*/
        this.gameDatabase.LoadData();
        /*            CutAndChoosePlayerDatabase newData
                Debug.Log("Saved player name: "+newData.PlayerName);
                Debug.Log("Num of clicked 'see': " + newData.CountPlayerSeeOther);
        */
    }

    public bool RedirectToSurvey(string url)
    {
        Application.OpenURL(url);
        return true; // Open new tab successfully
    }


}



