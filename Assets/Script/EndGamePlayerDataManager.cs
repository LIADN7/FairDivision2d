using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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
    // Two-step completion (Cloud Save & data integrity batch, owner-approved). Step A: the existing Submit button saves
    // the final record and waits for every research record of the run. Step B: after a confirmed save, a separate,
    // user-initiated button returns to the survey. Nothing navigates automatically.
    public TextMeshProUGUI saveStatusRTL;  // saving / saved / failed line under the buttons (no raycast target)
    public Button returnToSurveyButton;    // hidden until the save is confirmed; never saves anything
    [SerializeField] private Color saveStatusColor = Color.white;
    [SerializeField] private Color saveSucceededColor = Color.white;
    [SerializeField] private Color saveFailedColor = new Color(1f, 0.72f, 0.66f, 1f);

    private const string savingRTL = "שומרים את הנתונים...";
    private const string savedRTL = "הנתונים נשמרו בהצלחה.";
    private const string saveFailedRTL = "השמירה נכשלה. בדקו את החיבור לאינטרנט ולחצו שוב על \"שלח\".";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string returnUnavailableTestRTL = "מצב בדיקה: המעבר לסקר אינו זמין.";
#endif

    private bool saveInProgress = false;
    private bool saveSucceeded = false;
    private bool returnStarted = false;

    private void Start()
    {
        if (errorText != null) errorText.text = "";
        if (saveStatusRTL != null)
        {
            saveStatusRTL.raycastTarget = false;
            saveStatusRTL.text = "";
        }
        if (returnToSurveyButton != null)
        {
            returnToSurveyButton.gameObject.SetActive(false);
        }
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

    // Step A - Save (button OnClick -> HideInputFields, keyboard Submit, direct calls). One submission at a time (A1
    // guard extended): while saving, and after a confirmed save, further presses are ignored. On failure the button is
    // enabled again for a manual retry, which reuses the same final record (same reward, same payment round) and retries
    // only the records that are not yet confirmed. No redirect here.
    public void OnFinishButtonClicked()
    {
        if (saveInProgress || saveSucceeded)
        {
            return;
        }
        _ = SaveAndShowResultAsync();
    }

    private async Task SaveAndShowResultAsync()
    {
        saveInProgress = true;
        if (submit != null)
        {
            submit.interactable = false; // existing disabled look while saving
        }
        SetSaveStatus(savingRTL, saveStatusColor);

        bool saved = false;
        try
        {
            if (PlayerData.inst != null)
            {
                saved = await PlayerData.inst.SaveFinalRecordAsync();
            }
            else
            {
                Debug.LogError("PlayerData instance not found - nothing to save");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SAVE: final submission failed: " + ex.Message);
            saved = false;
        }
        saveInProgress = false;
        if (this == null)
        {
            return; // the scene was left while saving; the queue keeps the records' state
        }

        if (saved)
        {
            saveSucceeded = true;
            ShowSaved();
        }
        else
        {
            SetSaveStatus(saveFailedRTL, saveFailedColor);
            if (submit != null)
            {
                submit.interactable = true; // manual retry
            }
        }
    }

    private void ShowSaved()
    {
        if (submit != null)
        {
            submit.gameObject.SetActive(false);
        }
        string status = savedRTL;
        if (returnToSurveyButton != null)
        {
            returnToSurveyButton.gameObject.SetActive(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Test mode never reports to the survey panel (Config.RedirectToSurvey is suppressed): shown as unavailable.
            returnToSurveyButton.interactable = false;
            status += "\n" + returnUnavailableTestRTL;
#endif
        }
        SetSaveStatus(status, saveSucceededColor);
    }

    // Step B - return to the survey: a separate click, only after a confirmed save, at most once per EndGameFlow visit.
    // Uses the existing redirect unchanged (Config.RedirectToSurvey with the unrounded PlayerData reward; suppressed in
    // test mode). It never triggers a Cloud Save write.
    public void OnReturnToSurveyClicked()
    {
        if (!saveSucceeded || returnStarted)
        {
            return;
        }
        returnStarted = true;
        if (returnToSurveyButton != null)
        {
            returnToSurveyButton.interactable = false;
        }
        float winnings = PlayerData.inst != null ? PlayerData.inst.GetNormalizeWinning() : 0f;
        if (Config.inst != null)
        {
            Config.inst.RedirectToSurvey(winnings);
        }
        else
        {
            Debug.LogError("Config instance not found for redirect!");
        }
    }

    private void SetSaveStatus(string text, Color color)
    {
        if (saveStatusRTL == null)
        {
            return;
        }
        saveStatusRTL.text = Manager.PrepareRtlNote(text);
        saveStatusRTL.color = color;
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
