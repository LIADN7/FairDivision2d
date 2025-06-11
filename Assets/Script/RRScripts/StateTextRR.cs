using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StateTextRR : MonoBehaviour
{
    // Text component to display state details
    private Text detailsText;

    // Reference to the associated StateRR object
    [SerializeField] private StateRR stateRR;

    void Awake()
    {
        // Get the Text component on this GameObject
        detailsText = GetComponent<Text>();
    }

    void Start()
    {
        // Call UpdateText with the player number from PlayerRR
        if (PlayerRR.Instance != null)
        {
            UpdateText((int)PlayerRR.Instance.playerNumber);
        }
    }

    // Updates the text with the state's name and value for the given player number
    public void UpdateText(int playerNumber)
    {
        if (stateRR != null && detailsText != null)
        {
            //Need to Normalized the value
            // Debug.LogWarning("Need to Normalized the value on 'sumOfAll'!!!");
            float sumOfAll = PlayerDataRR.Instance.GetSumAllSquaresPerPlayer(playerNumber);

            string stateName = stateRR.GetStateName();
            float playerValue = stateRR.CalculateTotalValue(playerNumber);
            string percentage = (100f * playerValue / sumOfAll).ToString("F2");
            detailsText.text = stateName + " = " + percentage + "%";
        }
    }
}