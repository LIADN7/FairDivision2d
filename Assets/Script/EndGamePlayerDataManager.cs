using System.Collections;
using System.Linq;
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
    public  TextMeshProUGUI     winningsTextRTL   ;
    public Button submit;
    public TMP_Text errorText;

    private void Start()
    {
        if (errorText != null) errorText.text = "";
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        if (PlayerData.inst != null)
        {
            // Old personal info loading (kept in comment for future reference):
            if (nameInput != null) nameInput.text = PlayerData.inst.Name;
            // if (emailInput != null) emailInput.text = PlayerData.inst.Email;
            // if (phoneInput != null) phoneInput.text = PlayerData.inst.PhoneNumber;
            // if (idInput != null) idInput.text = PlayerData.inst.ID;

            this.SetWinnings(PlayerData.inst.GetNormalizeWinning(), PlayerData.inst.GetRandIndex());
        }
        else
        {
            Debug.LogError("PlayerData instance not found!");
        }
    }

    public void SetWinnings(float amount, int roundIndex = 0)
    {
        if (winningsText != null)
        {
            winningsText.text = $"You won: {amount} ₪ from round " + (roundIndex + 1);
            string valueText = new string(amount.ToString().Reverse().ToArray());
            winningsTextRTL.text = $"הזכייה שלכם: {valueText} ₪ מהסיבוב " + (roundIndex + 1);

        }
    }

    public void OnFinishButtonClicked()
    {
        float winnings = 0f;
        if (PlayerData.inst != null)
        {
            winnings = PlayerData.inst.GetNormalizeWinning();
            PlayerData.inst.SaveDatabaseAsync();
        }

        if (Config.inst != null)
        {
            Config.inst.RedirectToSurvey(winnings);
        }
        else
        {
            Debug.LogError("Config instance not found for redirect!");
        }
    }

    // Maintained for backward compatibility if wired up to UI Button in scene Inspector
    public void HideInputFields()
    {
        OnFinishButtonClicked();
    }

    /*
    // Legacy validation & saving logic kept in comments for potential future use:
    
    public void OldHideInputFieldsValidation()
    {
        string name = nameInput != null ? nameInput.text.Trim() : "";
        string email = emailInput != null ? emailInput.text.Trim() : "";
        string phone = phoneInput != null ? phoneInput.text.Trim() : "";
        string id = idInput != null ? idInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter your full name.");
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

        if (PlayerData.inst != null)
        {
            PlayerData.inst.Name = name;
            PlayerData.inst.Email = email;
            PlayerData.inst.PhoneNumber = phone;
            PlayerData.inst.ID = id;
            PlayerData.inst.SaveDatabaseAsync();
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            StartCoroutine(ClearErrorAfterDelay(3f));
        }
    }

    private IEnumerator ClearErrorAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (errorText != null) errorText.text = "";
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
    */
}
