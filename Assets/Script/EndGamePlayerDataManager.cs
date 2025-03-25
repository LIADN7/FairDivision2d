using UnityEngine;
using UnityEngine.UI;

public class EndGamePlayerDataManager : MonoBehaviour
{
    public InputField nameInput;
    public InputField emailInput;
    public InputField phoneInput;
    public InputField idInput;
    public Text winningsText;
    public Button submit;

    private void Start()
    {
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        if (PlayerData.inst != null)
        {
            nameInput.text = PlayerData.inst.Name;
            emailInput.text = PlayerData.inst.Email;
            phoneInput.text = PlayerData.inst.PhoneNumber;
            idInput.text = PlayerData.inst.ID;


            this.SetWinnings(PlayerData.inst.GetNormalizeWinning(), PlayerData.inst.GetRandIndex());
        }
        else
        {
            Debug.LogError("PlayerData instance not found!");
        }
    }

    public void SetWinnings(float amount, int roundIndex = 0)
    {
        winningsText.text = $"You won: {amount} ₪ from round " + (roundIndex + 1);
    }
    public void HideInputFields()
    {
        if (nameInput.text != "" && emailInput.text != "" && phoneInput.text != "" && idInput.text != "")
        {

            nameInput.gameObject.SetActive(false);
            emailInput.gameObject.SetActive(false);
            phoneInput.gameObject.SetActive(false);
            idInput.gameObject.SetActive(false);
            submit.gameObject.SetActive(false);
            if (PlayerData.inst != null)
            {
                PlayerData.inst.Name = nameInput.text;
                PlayerData.inst.Email = emailInput.text;
                PlayerData.inst.PhoneNumber = phoneInput.text;
                PlayerData.inst.ID = idInput.text;
                PlayerData.inst.SaveDatabaseAsync();
                winningsText.text = "Save Data!";
            }
        }
    }

}
