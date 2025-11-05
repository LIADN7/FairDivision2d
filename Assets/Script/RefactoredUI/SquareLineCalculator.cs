using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Calculates which side of a line each square is on and automatically colors them.
/// Right/Up side = Red (status 1), Left/Down side = Green (status 2)
/// </summary>
public class SquareLineCalculator : MonoBehaviour
{
    /// <summary>
    /// Determines which side of the line a point is on using cross product.
    /// Normalizes line direction to ensure consistent coloring regardless of drawing direction.
    /// Returns 1 for Red (right/up side), 2 for Green (left/down side)
    /// </summary>
    public static int GetSquareColorByLine(Vector3 squarePosition, Vector3 lineStart, Vector3 lineEnd)
    {
        // Normalize the line direction to ensure consistent coloring
        // regardless of drawing direction (top-to-bottom vs bottom-to-top)
        Vector3 normalizedStart, normalizedEnd;

        // Always make sure the line goes from left-to-right first
        // If X coordinates are same, use Y coordinate (bottom-to-top)
        if (lineStart.x < lineEnd.x || (Mathf.Approximately(lineStart.x, lineEnd.x) && lineStart.y < lineEnd.y))
        {
            normalizedStart = lineStart;
            normalizedEnd = lineEnd;
        }
        else
        {
            normalizedStart = lineEnd;
            normalizedEnd = lineStart;
        }

        // Calculate the cross product to determine which side of the line the point is on
        Vector3 lineVector = normalizedEnd - normalizedStart;
        Vector3 pointVector = squarePosition - normalizedStart;

        // Cross product in 2D (using only x and y components)
        float crossProduct = lineVector.x * pointVector.y - lineVector.y * pointVector.x;

        // If cross product is positive, point is on the right side (Red)
        // If cross product is negative or zero, point is on the left side (Green)
        return crossProduct > 0 ? 1 : 2; // 1 = Red, 2 = Green
    }    /// <summary>
         /// Colors all squares on the map based on the drawn line.
         /// Uses the existing Player.cs system to update square colors.
         /// </summary>
    public static void ColorAllSquaresByLine(Vector3 lineStart, Vector3 lineEnd)
    {
        // Find all squares with the SquarePoint tag
        GameObject[] allSquares = GameObject.FindGameObjectsWithTag("SquarePoint");

        Debug.Log($"Found {allSquares.Length} squares to color based on line from {lineStart} to {lineEnd}");

        // Get the Player instance to access the existing coloring system
        Player playerInstance = Player.inst;
        if (playerInstance == null)
        {
            Debug.LogError("Player instance not found! Cannot color squares.");
            return;
        }

        // Counter for debugging
        int redCount = 0, greenCount = 0;

        // Process each square
        foreach (GameObject squareObj in allSquares)
        {
            PointOfState pointOfState = squareObj.GetComponent<PointOfState>();
            if (pointOfState != null)
            {
                // Calculate which side of the line this square is on
                int colorStatus = GetSquareColorByLine(squareObj.transform.position, lineStart, lineEnd);

                // Update the square using the existing system (status, power)
                pointOfState.setSpriteStatus(colorStatus, 1);

                // Count for debugging
                if (colorStatus == 1) redCount++;
                else greenCount++;
            }
        }

        Debug.Log($"Colored {redCount} squares Red and {greenCount} squares Green");

        // Update the UI using the existing Player system
        playerInstance.statusChange();
    }

    /// <summary>
    /// Alternative method that works with Player's squares dictionary for better performance.
    /// </summary>
    public static void ColorSquaresByLineUsingDictionary(Vector3 lineStart, Vector3 lineEnd, Dictionary<int, GameObject> squares)
    {
        if (squares == null)
        {
            Debug.LogError("Squares dictionary is null!");
            return;
        }

        Debug.Log($"Coloring {squares.Count} squares based on line from {lineStart} to {lineEnd}");

        // Counter for debugging
        int redCount = 0, greenCount = 0;

        // Process each square in the dictionary
        foreach (var kvp in squares)
        {
            GameObject squareObj = kvp.Value;
            PointOfState pointOfState = squareObj.GetComponent<PointOfState>();

            if (pointOfState != null)
            {
                // Calculate which side of the line this square is on
                int colorStatus = GetSquareColorByLine(squareObj.transform.position, lineStart, lineEnd);

                // Update the square using the existing system (status, power)
                pointOfState.setSpriteStatus(colorStatus, 1);

                // Count for debugging
                if (colorStatus == 1) redCount++;
                else greenCount++;
            }
        }

        Debug.Log($"Colored {redCount} squares Red and {greenCount} squares Green using dictionary method");

        // Update the UI using the existing Player system
        Player playerInstance = Player.inst;
        if (playerInstance != null)
        {
            playerInstance.statusChange();
        }
    }
}