using System.Collections.Generic;
using UnityEngine;

public class StateRR : MonoBehaviour
{
    // Name of the state within Northern Ireland
    private string stateName;

    // List of SquareRR cubes within this state
    private List<SquareRR> squares;

    void Awake()
    {
        // Assign the current object's name to stateName
        stateName = gameObject.name;

        // Calculate total value by iterating through child objects
        squares = new List<SquareRR>();
        foreach (Transform child in transform)
        {
            SquareRR square = child.GetComponent<SquareRR>();
            if (square != null)
            {
                squares.Add(square);
            }
        }
    }

    // Calculates total value of all squares
    public float CalculateTotalValue(int playerNumber)
    {
        float calculatedValue = 0;
        foreach (SquareRR square in squares)
        {
            calculatedValue += square.GetValueForPlayer(playerNumber);
        }
        return calculatedValue;
    }

    public string GetStateName()
    {
        return stateName;
    }
}
