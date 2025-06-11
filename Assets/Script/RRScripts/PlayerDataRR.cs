using System.Collections.Generic;
using UnityEngine;

public class PlayerDataRR : MonoBehaviour
{
    public static PlayerDataRR Instance { get; private set; }

    public List<SquareRR> allGameSquares = new List<SquareRR>();
    public List<SquareRR>[] playersTakenSquares = new List<SquareRR>[3];

    public float[] playerScores = new float[3]; // [Player1, Player2, AI]

    // Stores the sum of all squares' values for each player
    private float[] sumAllSquaresPerPlayer = new float[3];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Initialize each list in the array
            for (int i = 0; i < playersTakenSquares.Length; i++)
            {
                playersTakenSquares[i] = new List<SquareRR>();
            }
            for (int i = 0; i < playerScores.Length; i++)
                playerScores[i] = 0;
            // Populate allGameSquares with all SquareRR objects tagged as "SquarePoint"
            GameObject[] squareObjects = GameObject.FindGameObjectsWithTag("SquarePoint");
            foreach (var obj in squareObjects)
            {
                SquareRR square = obj.GetComponent<SquareRR>();
                if (square != null)
                {
                    allGameSquares.Add(square);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CalculateSumAllSquaresPerPlayer();
    }

    public void RegisterSquare(SquareRR square)
    {
        allGameSquares.Add(square);
    }

    public void ResetData()
    {
        allGameSquares.Clear();
        for (int i = 0; i < playerScores.Length; i++)
            playerScores[i] = 0;
    }

    public float GetScoreNormalized(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < playerScores.Length)
        {

            return playerScores[playerNumber] / GetSumAllSquaresPerPlayer(playerNumber);
        }
        return 0;
    }

    /// <summary>
    /// Adds a square to the list of squares taken by the specified player.
    /// </summary>
    public void AddTakenSquare(SquareRR square, int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < playersTakenSquares.Length && square != null)
        {
            playersTakenSquares[playerNumber].Add(square);
        }
        else
        {
            Debug.LogWarning($"Invalid playerNumber {playerNumber} or null square in AddTakenSquare.");
        }
    }

    /// <summary>
    /// Calculates and stores the sum of all squares' values for each player at the start of the game.
    /// </summary>
    public void CalculateSumAllSquaresPerPlayer()
    {
        // Reset sums
        for (int i = 0; i < sumAllSquaresPerPlayer.Length; i++)
            sumAllSquaresPerPlayer[i] = 0;

        foreach (var square in allGameSquares)
        {
            for (int i = 0; i < sumAllSquaresPerPlayer.Length && i < square.playerValues.Length; i++)
            {
                sumAllSquaresPerPlayer[i] += square.playerValues[i];
            }
        }
        Debug.Log($"Player 00000 sum: {sumAllSquaresPerPlayer[0]}");
        Debug.Log($"Player 11111 sum: {sumAllSquaresPerPlayer[1]}");
        Debug.Log($"Player 22222 sum: {sumAllSquaresPerPlayer[2]}");
    }

    public float GetSumAllSquaresPerPlayer(int playerNumber)
    {
        if (playerNumber >= 0 && playerNumber < sumAllSquaresPerPlayer.Length)
        {
            return sumAllSquaresPerPlayer[playerNumber];
        }
        Debug.LogWarning($"Invalid playerNumber {playerNumber} in SumAllSquaresPerPlayer.");
        return 1;
    }

}