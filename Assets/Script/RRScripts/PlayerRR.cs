using Photon.Pun;
using UnityEngine;

public class PlayerRR : MonoBehaviourPunCallbacks
{
    public static PlayerRR Instance { get; private set; }

    public GameManagerRR.Turn playerNumber;
    public string playerName;
    private PhotonView photonView;
    // public bool isMyTurn = false;


    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        photonView = GetComponent<PhotonView>();

        // The first player (MasterClient) starts the game
        if (PhotonNetwork.IsMasterClient)
        {
            playerNumber = GameManagerRR.Turn.Player1; // First player
        }
        else
        {
            playerNumber = GameManagerRR.Turn.Player2; // Second player
        }
        // Debug.LogError("playerNumber: " + playerNumber);
    }

    void Start()
    {

    }


    [PunRPC]
    public void StartTurn()
    {
        // isMyTurn = true;
    }

    [PunRPC]
    public void EndTurn()
    {
        // isMyTurn = false;
        // If this is player 1, give turn to player 2
        // if (PhotonNetwork.IsMasterClient)
        // {
        //     photonView.RPC("StartTurn", RpcTarget.Others);
        // }
        // // If this is player 2, trigger the AI turn
        // else
        // {
        //     if (PhotonNetwork.IsMasterClient)
        //     {
        //         TriggerAITurn();
        //     }
        // }
    }

    private void TriggerAITurn()
    {
        // // Find the highest value square for the AI
        // SquareRR[] squares = FindObjectsOfType<SquareRR>();
        // SquareRR bestSquare = null;
        // float bestValue = float.MinValue;

        // foreach (SquareRR square in squares)
        // {
        //     if (square.IsVisible())
        //     {
        //         float value = square.GetValueForPlayer(2); // AI uses index 2
        //         if (value > bestValue)
        //         {
        //             bestValue = value;
        //             bestSquare = square;
        //         }
        //     }
        // }

        // if (bestSquare != null)
        // {
        //     // AI chooses the best square
        //     photonView.RPC("ChooseSquare", RpcTarget.All, bestSquare.GetComponent<PhotonView>().ViewID, 2);
        //     // Give turn back to player 1
        //     photonView.RPC("StartTurn", RpcTarget.All);
        // }
    }

}
