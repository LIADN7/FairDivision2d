using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// If you use TextMeshPro, uncomment the next line
// using TMPro;

/// <summary>
/// Handles the player's UI view, such as score text and other player-specific UI elements.
/// </summary>
public class PlayerUIViewRR : MonoBehaviour
{
    // Singleton instance
    public static PlayerUIViewRR Instance { get; private set; }

    // Assign this in the inspector or via script
    public Text playerScoreText;
    // If you use TextMeshProUGUI, use this instead:
    // public TextMeshProUGUI playerScoreText;

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

    /// <summary>
    /// Updates the player's score text.
    /// </summary>
    /// <param name="score">The new score to display.</param>
    public void SetScoreText(float score)
    {
        if (playerScoreText != null)
        {
            string percentage = (100f * score).ToString("F2");
            playerScoreText.text = "Score: " + percentage + "%";
        }
    }

    // In the future, add more methods and references for additional UI elements per player.
}
