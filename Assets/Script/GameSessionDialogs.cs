using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// UX-016 / UX-025 (Batch 6, owner-approved scope): display-only session dialogs for the two game scenes
// (CutScene, CutSceneAITutorial). Lives on the SessionDialogs prefab: its own Screen Space - Overlay canvas, drawn above
// the game canvas, the cut lines, the waiting overlay, the Help Page and the tutorial panels.
//
// Exit confirmation (modal): the Home button still calls LoadScene, which now asks here first. "יציאה" runs the unchanged
// exit path (Player / PlayerAITutorial.LeaveGame -> LeaveGameForAll) exactly once; "ביטול" only closes the dialog
// (no RPC, no change to the round, the input state or the turn).
//
// Connection notice (not modal): shown only for events Photon confirms - the other player left the room without the
// Home/exit flow (OnPlayerLeftRoom), or this client lost its connection (OnDisconnected). It explains that the round
// cannot continue and offers the existing way back to the main screen. Nothing else changes: no reconnection, no role or
// master-client change, no saving, no result or payment change. The game UI underneath keeps its existing behaviour
// (Help, chat, Next and its save); only the round instruction and the waiting overlay are hidden while it is shown.
public class GameSessionDialogs : MonoBehaviourPunCallbacks
{
    public static GameSessionDialogs Instance { get; private set; }

    [SerializeField] private bool practiceScene = false; // CutSceneAITutorial: exit via PlayerAITutorial, practice wording
    [SerializeField] private string lobbySceneName = "MainScene";

    [Header("Exit confirmation (modal)")]
    [SerializeField] private GameObject exitDialog;
    [SerializeField] private RectTransform exitTitle;
    [SerializeField] private TextMeshProUGUI exitBodyRTL;
    [SerializeField] private Button exitCancelButton;
    [SerializeField] private Button exitConfirmButton;

    [Header("Connection notice (not modal)")]
    [SerializeField] private GameObject connectionNotice;
    [SerializeField] private TextMeshProUGUI noticeTitleRTL;
    [SerializeField] private TextMeshProUGUI noticeBodyRTL;
    [SerializeField] private Button noticeReturnButton;

    private const string UnsavedRoundRTL = "ההתקדמות בסבב הנוכחי לא תישמר.";
    private const string OtherPlayerLeftRTL = "החיבור לשחקן השני נותק.";
    private const string LocalConnectionLostRTL = "החיבור לשרת המשחק נותק.";
    private const string RoundCannotContinueRTL = "לא ניתן להמשיך בסבב הנוכחי.";
    // Practice: the practice round itself is played against the computer; it is the game that cannot continue.
    private const string GameCannotContinueRTL = "לא ניתן להמשיך במשחק.";
    private static readonly Vector2 ExitTitleWithBody = new Vector2(0f, 80f);
    private static readonly Vector2 ExitTitleAlone = new Vector2(0f, 40f);

    private Action pendingExit;
    private bool exitStarted;   // "יציאה" was pressed: the exit path runs once
    private bool returnStarted; // the notice's return button was pressed
    private bool leavingRoom;   // a normal room exit (Home/exit on either client) is in progress: never a disconnect

    // Display only: read by Player / PlayerAITutorial.UpdatePhaseInstruction to hide the round instruction and waiting overlay.
    public static bool IsConnectionLost => Instance != null && Instance.connectionLost;
    // Display only: read by RtlChatInputWebGL so the HTML chat input (drawn above the WebGL canvas) does not cover a dialog.
    public static bool IsAnyDialogOpen => Instance != null && Instance.AnyDialogOpen;

    private bool connectionLost;
    private bool AnyDialogOpen =>
        (exitDialog != null && exitDialog.activeSelf) || (connectionNotice != null && connectionNotice.activeSelf);

    private void Awake()
    {
        Instance = this;
        if (exitDialog != null) exitDialog.SetActive(false);
        if (connectionNotice != null) connectionNotice.SetActive(false);
        if (exitCancelButton != null) exitCancelButton.onClick.AddListener(CancelExit);
        if (exitConfirmButton != null) exitConfirmButton.onClick.AddListener(ConfirmExit);
        if (noticeReturnButton != null) noticeReturnButton.onClick.AddListener(ReturnToLobby);
        UseWhiteFace();
    }

    private void Start()
    {
        // Real rounds: both players stay in the room for the whole session. If the other player left while this scene was
        // loading (its Leave event was handled by the previous scene), the room already has one player here.
        if (practiceScene)
        {
            return;
        }
        if (!PhotonNetwork.IsConnected)
        {
            ShowConnectionNotice(LocalConnectionLostRTL, "scene start: not connected");
        }
        else if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            ShowConnectionNotice(OtherPlayerLeftRTL, "scene start: one player in the room");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // The shared Hebrew material has a green face colour that tints every vertex colour (see PROJECT_CONTEXT §17);
    // the dialog texts share one runtime copy with a white face so their colours show as set. No asset is changed.
    private void UseWhiteFace()
    {
        Material white = null;
        foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.fontSharedMaterial == null)
            {
                continue;
            }
            if (white == null)
            {
                white = new Material(text.fontSharedMaterial);
                white.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            }
            text.fontSharedMaterial = white;
        }
    }

    // ---------------------------------------------------------------- UX-025: exit confirmation

    // Called by LoadScene from the Home button. Returns false when this scene has no dialog (the caller then exits as before).
    public static bool RequestExit(Action exitAction, bool roundUnsaved)
    {
        if (Instance == null)
        {
            return false;
        }
        Instance.OpenExitDialog(exitAction, roundUnsaved);
        return true;
    }

    private void OpenExitDialog(Action exitAction, bool roundUnsaved)
    {
        if (exitStarted || returnStarted)
        {
            return; // already leaving
        }
        if (connectionLost)
        {
            ReturnToLobby(); // the round has already ended: same as the notice's button
            return;
        }
        pendingExit = exitAction;
        if (exitBodyRTL != null)
        {
            exitBodyRTL.text = UnsavedRoundRTL;
            exitBodyRTL.gameObject.SetActive(roundUnsaved);
        }
        if (exitTitle != null)
        {
            exitTitle.anchoredPosition = roundUnsaved ? ExitTitleWithBody : ExitTitleAlone;
        }
        SetInteractable(exitCancelButton, true);
        SetInteractable(exitConfirmButton, true);
        exitDialog.SetActive(true);
    }

    private void CancelExit()
    {
        if (exitStarted)
        {
            return;
        }
        pendingExit = null;
        exitDialog.SetActive(false);
    }

    private void ConfirmExit()
    {
        if (exitStarted)
        {
            return;
        }
        exitStarted = true;
        leavingRoom = true;
        SetInteractable(exitCancelButton, false);
        SetInteractable(exitConfirmButton, false);
        Action exit = pendingExit;
        pendingExit = null;
        if (exit != null && PhotonNetwork.InRoom)
        {
            exit(); // the unchanged exit path; the scene changes shortly after
        }
        else
        {
            LoadLobbyScene(); // not in a room any more: the RPC-based exit could not run here
        }
    }

    // ---------------------------------------------------------------- UX-016: connection notice

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log("UX-016: OnPlayerLeftRoom actor=" + (otherPlayer != null ? otherPlayer.ActorNumber.ToString() : "?")
            + " inactive=" + (otherPlayer != null && otherPlayer.IsInactive) + " state=" + PhotonNetwork.NetworkClientState
            + " " + Stamp());
        // Home/exit by the other player: its LeaveGameForAll RPC arrives before its Leave event and makes this client leave
        // too, so this client is no longer in the room (state Leaving) and no notice is shown.
        if (leavingRoom || exitStarted || returnStarted || !PhotonNetwork.InRoom)
        {
            return;
        }
        ShowConnectionNotice(OtherPlayerLeftRTL, "other player left the room");
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // Logged only: roles are not reassigned here (Player 1 = master is derived elsewhere and stays unchanged).
        Debug.Log("UX-016: OnMasterClientSwitched newMaster=" + (newMasterClient != null ? newMasterClient.ActorNumber.ToString() : "?")
            + " " + Stamp());
    }

    public override void OnLeftRoom()
    {
        // A normal leave switches back to the master server (state DisconnectingFromGameServer). A lost connection also
        // reports OnLeftRoom, but in state Disconnecting, and then OnDisconnected.
        bool normalLeave = PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.DisconnectingFromGameServer;
        if (normalLeave)
        {
            leavingRoom = true;
        }
        Debug.Log("UX-016: OnLeftRoom state=" + PhotonNetwork.NetworkClientState + " normalLeave=" + normalLeave + " " + Stamp());
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log("UX-016: OnDisconnected cause=" + cause + " " + Stamp());
        if (leavingRoom || exitStarted || returnStarted)
        {
            return;
        }
        ShowConnectionNotice(LocalConnectionLostRTL, "local disconnect (" + cause + ")");
    }

    private void ShowConnectionNotice(string titleRTL, string reason)
    {
        if (connectionNotice == null)
        {
            return;
        }
        // The first confirmed event decides the wording; later events (e.g. a disconnect after the other player left) keep it.
        if (!connectionLost)
        {
            connectionLost = true;
            noticeTitleRTL.text = titleRTL;
            noticeBodyRTL.text = practiceScene ? GameCannotContinueRTL : RoundCannotContinueRTL;
            SetInteractable(noticeReturnButton, true);
        }
        if (exitDialog != null && exitDialog.activeSelf && !exitStarted)
        {
            pendingExit = null;
            exitDialog.SetActive(false);
        }
        connectionNotice.SetActive(true);
        Debug.Log("UX-016: connection notice shown (" + reason + ") " + Stamp());
        RefreshRoundInstruction();
    }

    // Hide the round instruction and the waiting overlay (display only).
    private void RefreshRoundInstruction()
    {
        try
        {
            if (practiceScene)
            {
                if (PlayerAITutorial.inst != null) PlayerAITutorial.inst.UpdatePhaseInstruction();
            }
            else if (Player.inst != null)
            {
                Player.inst.UpdatePhaseInstruction();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("UX-016: instruction refresh failed: " + ex.Message);
        }
    }

    // The existing way back to the main screen. Still in the room (the other player left): the same exit path as Home.
    // No longer connected: the RPC-based exit cannot run, so the main screen is loaded directly; the lobby then connects
    // as on a fresh start (Launcher), exactly as it does today.
    private void ReturnToLobby()
    {
        if (returnStarted || exitStarted)
        {
            return;
        }
        returnStarted = true;
        leavingRoom = true;
        SetInteractable(noticeReturnButton, false);
        Debug.Log("UX-016: return to main screen (inRoom=" + PhotonNetwork.InRoom + ") " + Stamp());
        if (PhotonNetwork.InRoom)
        {
            try
            {
                if (practiceScene && PlayerAITutorial.inst != null)
                {
                    PlayerAITutorial.inst.LeaveGame(lobbySceneName);
                    return;
                }
                if (!practiceScene && Player.inst != null)
                {
                    Player.inst.LeaveGame(lobbySceneName);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("UX-016: exit path failed: " + ex.Message);
            }
            PhotonNetwork.LeaveRoom();
        }
        LoadLobbyScene();
    }

    private void LoadLobbyScene()
    {
        SceneManager.LoadScene(lobbySceneName);
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    // Wall-clock time (same machine for both test clients) and time since start, for latency measurements.
    private static string Stamp()
    {
        return "utc=" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + " t=" + Time.realtimeSinceStartup.ToString("F2");
    }
}
