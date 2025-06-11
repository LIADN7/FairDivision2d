using UnityEngine;

public class SquareRR : MonoBehaviour
{
    // Array to store values for each player
    public float[] playerValues = new float[] { 2f, 2f, 2f, 2f }; // [Player1, Player2, AI, spare]

    // Name of the parent country
    private string parentCountryName;
    // Reference to the SpriteRenderer component
    private bool isTaken = false;
    private SpriteRenderer squareSpriteRenderer;
    private int myKey;
    private static int key = 0;

    void Start()
    {
        // Assign parent country name
        parentCountryName = transform.parent.name;

        // Get SpriteRenderer component
        squareSpriteRenderer = GetComponent<SpriteRenderer>();

        // Set unique key for this object
        myKey = key++;

        SetColorByValue(GameManagerRR.Instance.GetPlayerIndexByTurn()); // Default to player 1's value
        // Set color based on playerValues 
        ///
        /// Need to change base on the player number, get from the server....
        /// 

        // if (playerValues != null && playerValues.Length > 0)
        // {
        //     SetColorByValue(playerValues[0]);
        // }
    }

    // Sets the color based on the provided player value
    private void SetColorByValue(int playerNumber)
    {
        // Debug.LogWarning("'SetColorByValue' need to be on other Script!!!");
        if (playerNumber >= playerValues.Length) return;
        switch (playerValues[playerNumber])
        {
            case 2:
                SetColor(new Color(0, 0.5f, 0)); // Dark Green
                break;
            case 4:
                SetColor(Color.green); // Regular Green
                break;
            case 6:
                SetColor(new Color(0.5f, 1f, 0)); // Bright Green
                break;
            case 10:
                SetColor(Color.blue); // Blue
                break;
            default:
                SetColor(Color.white); // Default Color
                break;
        }
    }

    // Returns the value of this square for the given player number
    public float GetValueForPlayer(int playerNumber)
    {
        if (playerValues == null || playerNumber < 0 || playerNumber >= playerValues.Length)
        {
            Debug.LogError($"Invalid player number: {playerNumber} for square {gameObject.name}. Array length: {playerValues?.Length ?? 0}");
            return 0;
        }
        float value = playerValues[playerNumber];
        // Debug.Log($"Square {gameObject.name} value for player {playerNumber}: {value}");
        return value;
    }

    // // Initializes values or performs setup
    // public void InitializeSquare(string countryName, int[] initialValues)
    // {
    //     // Implementation goes here
    // }

    // Helper method to set color
    private void SetColor(Color color)
    {
        if (squareSpriteRenderer != null)
        {
            squareSpriteRenderer.color = color;
        }
    }

    // Returns true if the square is visible (active in the scene)
    public bool IsTaken()
    {
        return isTaken;
    }

    // Hides the square by moving it to a target and fading out, then disables it
    public bool HideSquare(GameObject target = null)
    {
        isTaken = true;
        if (target == null)
        {
            // Fade out in place and then disable
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                LeanTween.value(gameObject, 1f, 0f, 0.5f)
                    .setOnUpdate((float val) =>
                    {
                        Color c = sr.color;
                        c.a = val;
                        sr.color = c;
                    })
                    .setOnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                gameObject.SetActive(false);
            }
            return false;
        }
        // Stop any previous tweens on this object
        LeanTween.cancel(gameObject);

        // // Move to target position over 0.5 seconds
        // LeanTween.move(gameObject, target.transform.position, 0.5f).setOnComplete(() =>
        // {
        //     // Fade out after moving (assuming you have a SpriteRenderer)
        //     SpriteRenderer sr = GetComponent<SpriteRenderer>();
        //     if (sr != null)
        //     {
        //         LeanTween.value(gameObject, 1f, 0f, 0.5f)
        //             .setOnUpdate((float val) =>
        //             {
        //                 Color c = sr.color;
        //                 c.a = val;
        //                 sr.color = c;
        //             })
        //             .setOnComplete(() =>
        //             {
        //                 gameObject.SetActive(false);
        //             });
        //     }
        //     else
        //     {
        //         // If no SpriteRenderer, just disable
        //         gameObject.SetActive(false);
        //     }
        // });
        return true;
    }

    // Handles mouse click on the square
    private void OnMouseDown()
    {
        // Only allow click if this is the player's turn
        if (GameManagerRR.Instance != null && GameManagerRR.Instance.IsMyTurn())
        {
            this.HideSquare(); // Hide the square and move it to the player's score text
            GameManagerRR.Instance.OnSquareClicked(this);

        }
    }
}
