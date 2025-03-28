using System.Collections;
using TMPro;
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
    public TMP_Text errorText;

    private void Start()
    {
        LoadPlayerData();
        errorText.text = "";
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
        // PlayerData.inst?.LoadData();

        string name = nameInput.text.Trim();
        string email = emailInput.text.Trim();
        string phone = phoneInput.text.Trim();
        string id = idInput.text.Trim();

        // Basic validation checks
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter your name.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Invalid email format.");
            return;
        }

        if (!IsValidPhone(phone))
        {
            ShowError("Phone must start with 05 and contain exactly 10 digits.");
            return;
        }

        if (!IsValidID(id))
        {
            ShowError("ID must be exactly 9 digits.");
            return;
        }

        // All good – save and hide inputs
        nameInput.gameObject.SetActive(false);
        emailInput.gameObject.SetActive(false);
        phoneInput.gameObject.SetActive(false);
        idInput.gameObject.SetActive(false);
        submit.gameObject.SetActive(false);

        if (PlayerData.inst != null)
        {
            PlayerData.inst.Name = name;
            PlayerData.inst.Email = email;
            PlayerData.inst.PhoneNumber = phone;
            PlayerData.inst.ID = id;
            PlayerData.inst.SaveDatabaseAsync();
            winningsText.text = "Data Saved!";
        }

        errorText.text = ""; // Clear error
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


    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        StartCoroutine(ClearErrorAfterDelay(3f)); // Hide after 3 seconds
    }

    private IEnumerator ClearErrorAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        errorText.text = "";
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".") && email.IndexOf("@") < email.LastIndexOf(".");
    }

    private bool IsValidPhone(string phone)
    {
        return phone.Length == 10 && phone.StartsWith("05") && long.TryParse(phone, out _);
    }

    private bool IsValidID(string id)
    {
        return id.Length == 9 && long.TryParse(id, out _);
    }

}
