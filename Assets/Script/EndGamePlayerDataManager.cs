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
    // Display only (UX-010): how the reward was chosen, and which round when that round's result is really in RandArrays.
    public TextMeshProUGUI rewardInfoRTL;
    // Display only (game-feel Stage 4): optional highlighted line for the selected round. When assigned, the round
    // sentence moves here (same text) and rewardInfoRTL keeps the explanation; when not assigned, both stay in rewardInfoRTL.
    public TextMeshProUGUI selectedRoundRTL;
    public GameObject selectedRoundPlate;
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
        }
        // Display only: the stored and redirected amount stays the unrounded PlayerData value.
        if (winningsTextRTL != null)
        {
            winningsTextRTL.text = Manager.PrepareRtlNote(FormatRewardAmount(amount) + " ₪");
        }
        if (rewardInfoRTL != null)
        {
            string info = "התגמול חושב לפי סבב אחד שנבחר באקראי.";
            // roundIndex is the 0-based RandArrays index drawn by the master (0-5 = real rounds 1-6; practice is never added).
            bool roundIsKnown = RandArrays.inst != null && roundIndex >= 0 && roundIndex < RandArrays.inst.GetSize();
            string roundLine = "סבב " + (roundIndex + 1) + " נבחר לחישוב התגמול.";
            if (selectedRoundRTL != null)
            {
                selectedRoundRTL.text = roundIsKnown ? Manager.PrepareRtlNote(roundLine) : "";
                if (selectedRoundPlate != null)
                {
                    selectedRoundPlate.SetActive(roundIsKnown);
                }
            }
            else if (roundIsKnown)
            {
                info += "\n" + roundLine;
            }
            rewardInfoRTL.text = Manager.PrepareRtlNote(info);
        }
    }

    // Display only: at most two decimals ("30", "37.50", "41.12"); the value itself is not changed.
    private static string FormatRewardAmount(float amount)
    {
        string text = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return text.EndsWith(".00") ? text.Substring(0, text.Length - 3) : text;
    }

    // A1: the Finish submission (save + survey redirect) starts at most once per EndGameFlow visit. Guarded here, the one
    // entry point of every path (button OnClick -> HideInputFields, keyboard Submit on the selected button, direct calls).
    // The save itself is unchanged: it is still not awaited, its result is not reported, and the redirect is not delayed.
    private bool finishStarted = false;

    public void OnFinishButtonClicked()
    {
        if (finishStarted)
        {
            return;
        }
        finishStarted = true;
        if (submit != null)
        {
            submit.interactable = false; // existing disabled look: the submission has already started
        }

        float winnings = 0f;
        if (PlayerData.inst != null)
        {
            winnings = PlayerData.inst.GetNormalizeWinning();
            _ = PlayerData.inst.SaveFinalRecordAsync();
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
