using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using System;

public class GameManagerRR : MonoBehaviourPunCallbacks
{
    public static GameManagerRR Instance { get; private set; }

    public enum Turn { Player1, Player2, AI }
    public Turn currentTurn = Turn.Player1;
    public bool isGameActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetGame(int firstPlayerActorNumber)
    {
        isGameActive = true;
        currentTurn = Turn.Player1;

        // Reset all squares
        foreach (var square in PlayerDataRR.Instance.allGameSquares)
        {
            square.gameObject.SetActive(true);
        }
        // Reset player data
        PlayerDataRR.Instance.ResetData();
    }

    public void OnGameEnded()
    {
        isGameActive = false;
        // Disable all interactions
        foreach (var square in PlayerDataRR.Instance.allGameSquares)
        {
            square.enabled = false;
        }

        // Show final scores
        ShowFinalScores();
    }

    private void ShowFinalScores()
    {
        var players = FindObjectsOfType<PlayerRR>();
        foreach (var player in players)
        {
            if (player != null)
            {
                Debug.Log($"Player {player.playerNumber} final score: {GameManagerRR.Instance.GetPlayerIndexByTurn()}");
                // TODO: Update UI with final scores if needed
            }
        }
        // Show the AI score
        // Debug.Log($"AI final score: {PlayerRR.GetAIScore()}");
    }

    // public void RegisterSquare(SquareRR square)
    // {
    //     PlayerDataRR.Instance.RegisterSquare(square);
    // }

    public void OnSquareClicked(SquareRR square)
    {

        PhotonView squareView = square.GetComponent<PhotonView>();
        if (squareView != null)
        {
            int playerNumber = this.GetPlayerIndexByTurn(); // Get the player number from PlayerRR instance
            PhotonView.Get(this).RPC("ProcessSquareChoiceAll", RpcTarget.All, squareView.ViewID, playerNumber);
            PhotonView.Get(this).RPC("ProcessSquareChoiceOters", RpcTarget.Others, squareView.ViewID, playerNumber);
            AdvanceTurn();
            PlayerUIViewRR.Instance.SetScoreText(PlayerDataRR.Instance.GetScoreNormalized(playerNumber)); // Update player score text
        }
    }

    [PunRPC]
    private void ProcessSquareChoiceAll(int squareViewID, int playerNumber)
    {
        PhotonView squareView = PhotonView.Find(squareViewID);
        if (squareView != null)
        {
            SquareRR square = squareView.GetComponent<SquareRR>();
            if (square != null)
            {
                Debug.Log($"Processing choice for square {square.gameObject.name} by player {playerNumber}");
                // Add value fo player on the manager
                PlayerDataRR.Instance.playerScores[playerNumber] += square.GetValueForPlayer(playerNumber);
                PlayerDataRR.Instance.AddTakenSquare(square, playerNumber); // Add the square to the player's taken squares
            }
        }
    }
    [PunRPC]
    private void ProcessSquareChoiceOters(int squareViewID, int playerNumber)
    {
        PhotonView squareView = PhotonView.Find(squareViewID);
        if (squareView != null)
        {
            SquareRR square = squareView.GetComponent<SquareRR>();
            if (square != null)
            {

                square.HideSquare(); // Hide the square and move it to the player's score text
            }
        }
    }

    [PunRPC]
    private void ProcessSquareChoiceAI(int squareViewID, int playerNumber)
    {
        PhotonView squareView = PhotonView.Find(squareViewID);
        if (squareView != null)
        {
            SquareRR square = squareView.GetComponent<SquareRR>();
            if (square != null)
            {
                PlayerDataRR.Instance.playerScores[playerNumber] += square.GetValueForPlayer(playerNumber);
                square.HideSquare(); // Hide the square and move it to the player's score text
                Debug.Log("playerScores= " + PlayerDataRR.Instance.playerScores[0] + ", " + PlayerDataRR.Instance.playerScores[1] + ", " + PlayerDataRR.Instance.playerScores[2]);
            }
        }
    }

    private void AdvanceTurn()
    {

        switch (currentTurn)
        {
            case Turn.Player1:
                currentTurn = Turn.Player2;
                break;
            case Turn.Player2:
                currentTurn = Turn.AI;
                // Trigger AI turn 
                TriggerAITurn();
                break;
            case Turn.AI:
                currentTurn = Turn.Player1;
                break;
        }
        PhotonView.Get(this).RPC("UpdateTurn", RpcTarget.All, currentTurn); // Update all clients with the new turn

    }

    [PunRPC]
    private void UpdateTurn(Turn newTurn)
    {
        currentTurn = newTurn;
        Debug.Log($"Current turn updated to: {currentTurn}");
    }


    private void TriggerAITurn()
    {
        SquareRR[] squares = FindObjectsOfType<SquareRR>();
        SquareRR bestSquare = null;
        float bestValue = float.MinValue;
        int playerIndex = 2; // AI uses index 2
        foreach (SquareRR square in squares)
        {
            if (!square.IsTaken())
            {
                float value = square.GetValueForPlayer(playerIndex);
                // Debug.Log($"AI evaluating square {square.gameObject.name} with value: {value}");
                if (value > bestValue)
                {
                    bestValue = value;
                    bestSquare = square;
                }
            }
        }

        if (bestSquare != null)
        {
            Debug.Log($"AI chose square {bestSquare.gameObject.name} with value: {bestValue}");
            PhotonView squareView = bestSquare.GetComponent<PhotonView>();
            if (squareView != null)
            {
                AdvanceTurn();
                PhotonView.Get(this).RPC("ProcessSquareChoiceAI", RpcTarget.All, squareView.ViewID, playerIndex); // 2 for AI
            }
        }
        else
        {
            Debug.LogWarning("AI couldn't find any valid squares to choose!");
        }
    }

    public bool IsMyTurn()
    {
        return PlayerRR.Instance.playerNumber == currentTurn;
    }

    private void CheckGameEnd()
    {
        // Check if all squares are taken
        if (PlayerDataRR.Instance.allGameSquares.All(s => !s.IsTaken()))
        {
            OnGameEnded();
        }
    }

    /// <summary>
    /// Returns the player index (0 for Player1, 1 for Player2, 2 for AI) based on the Turn enum.
    /// </summary>
    public int GetPlayerIndexByTurn()
    {
        Turn turn = PlayerRR.Instance.playerNumber;
        switch (turn)
        {
            case Turn.Player1:
                return 0;
            case Turn.Player2:
                return 1;
            case Turn.AI:
                return 2;
            default:
                return -1; // Invalid
        }
    }

    /// <summary>
    /// Leaves the RR game and loads the given scene for all players.
    /// </summary>
    public void LeaveGame(string sceneName)
    {
        PhotonView.Get(this).RPC("LeaveGameForAll", RpcTarget.All, sceneName);
    }

    [PunRPC]
    private void LeaveGameForAll(string sceneName)
    {
        Photon.Pun.PhotonNetwork.LeaveRoom();
        StartCoroutine(DelayAndLoadScene(0.2f, sceneName));
    }

    private System.Collections.IEnumerator DelayAndLoadScene(float sec, string sceneName)
    {
        yield return new WaitForSeconds(sec);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

}
