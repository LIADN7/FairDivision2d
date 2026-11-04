using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialStep
{
    [Tooltip("Identifier name for Inspector clarity (e.g. CutPhaseIntro, Step1_CutIntroP1, Step1_CutIntroP2)")]
    public string stepName;

    [Tooltip("The UI Panel / GameObject containing text, arrows, highlights for this step")]
    public GameObject panelObject;

    [Tooltip("If true, gameplay interaction (line drawing, board clicks) is locked when this step is active")]
    public bool locksGameplay = true;

    [Tooltip("If true, clicking 'Next' on this panel automatically unlocks gameplay for play intermission")]
    public bool autoUnlockOnNext = true;
}

public class TutorialFplanManager : MonoBehaviour
{
    public static TutorialFplanManager Instance { get; private set; }

    [Header("Tutorial Sequence Configuration")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    [Header("UI Blocking Layer")]
    [Tooltip("Fullscreen CanvasGroup overlay behind step panels to block raycasts to underlying game UI")]
    [SerializeField] private CanvasGroup raycastBlocker;

    [Header("State Readouts")]
    [SerializeField] private int currentStepIndex = -1;
    [SerializeField] private bool isGameplayLocked = false;

    public bool IsGameplayLocked => isGameplayLocked;
    public int CurrentStepIndex => currentStepIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        HideAllStepPanels();
    }

    /// <summary>
    /// Returns 1 for Player 1 (MasterClient) or 2 for Player 2.
    /// </summary>
    public int GetLocalPlayerNumber()
    {
        if (PlayerAITutorial.inst != null && PlayerAITutorial.inst.myPlayerNum > 0)
        {
            return PlayerAITutorial.inst.myPlayerNum;
        }

        if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            return 1;
        }

        return 1; // Default fallback to Player 1
    }

    /// <summary>
    /// Begins the tutorial sequence starting at step 0.
    /// </summary>
    public void StartTutorial()
    {
        if (currentStepIndex >= 0)
        {
            Debug.Log("[TutorialFplanManager] StartTutorial called but tutorial is already in progress.");
            return;
        }

        if (tutorialSteps != null && tutorialSteps.Count > 0)
        {
            ShowStep(0);
        }
        else
        {
            Debug.LogWarning("[TutorialFplanManager] No tutorial steps assigned in Inspector.");
        }
    }

    /// <summary>
    /// Finds the valid step index for the local player, skipping steps ending with the opposite player suffix (P1/P2).
    /// </summary>
    private int GetValidStepIndexForPlayer(int startIndex, int direction = 1)
    {
        int playerNum = GetLocalPlayerNumber();
        string otherSuffix = "P" + (playerNum == 1 ? 2 : 1);

        int index = startIndex;
        while (index >= 0 && index < tutorialSteps.Count)
        {
            TutorialStep step = tutorialSteps[index];
            string nameToCheck = "";
            if (step != null)
            {
                if (step.panelObject != null) nameToCheck = step.panelObject.name;
                else if (!string.IsNullOrEmpty(step.stepName)) nameToCheck = step.stepName;
            }

            // If step ends with the OTHER player's suffix (e.g. P2 for Player 1), skip it
            if (!string.IsNullOrEmpty(nameToCheck) && nameToCheck.Trim().EndsWith(otherSuffix, StringComparison.OrdinalIgnoreCase))
            {
                index += direction;
            }
            else
            {
                return index;
            }
        }

        return index;
    }

    /// <summary>
    /// Displays a specific tutorial step by index, automatically matching P1/P2 suffixes.
    /// </summary>
    public void ShowStep(int stepIndex)
    {
        int validIndex = GetValidStepIndexForPlayer(stepIndex, 1);
        if (validIndex < 0 || validIndex >= tutorialSteps.Count)
        {
            if (stepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
            }
            else
            {
                Debug.LogWarning($"[TutorialFplanManager] Step index {stepIndex} out of bounds.");
            }
            return;
        }

        HideAllStepPanels();

        currentStepIndex = validIndex;
        TutorialStep step = tutorialSteps[currentStepIndex];

        if (step.panelObject != null)
        {
            step.panelObject.SetActive(true);
            FilterPanelChildrenForPlayer(step.panelObject);
        }

        SetGameplayLock(step.locksGameplay);
        Debug.Log($"[TutorialFplanManager] Switched to Tutorial Step [{currentStepIndex}]: {step.stepName} for Player {GetLocalPlayerNumber()}");
    }

    /// <summary>
    /// Toggles child objects inside panelObject ending with P1 / P2 based on local player number.
    /// </summary>
    private void FilterPanelChildrenForPlayer(GameObject panel)
    {
        if (panel == null) return;

        int playerNum = GetLocalPlayerNumber();
        string mySuffix = "P" + playerNum;
        string otherSuffix = "P" + (playerNum == 1 ? 2 : 1);

        foreach (Transform child in panel.transform)
        {
            string childName = child.name.Trim();
            if (childName.EndsWith(mySuffix, StringComparison.OrdinalIgnoreCase))
            {
                child.gameObject.SetActive(true);
            }
            else if (childName.EndsWith(otherSuffix, StringComparison.OrdinalIgnoreCase))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when player clicks 'Next' button on active instruction panel.
    /// </summary>
    public void OnNextButtonClicked()
    {
        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count) return;

        TutorialStep currentStep = tutorialSteps[currentStepIndex];

        if (currentStep.autoUnlockOnNext)
        {
            UnlockPlay();
        }
        else
        {
            NextStep();
        }
    }

    /// <summary>
    /// Advances to the next valid step index for the current player.
    /// </summary>
    public void NextStep()
    {
        int nextIndex = GetValidStepIndexForPlayer(currentStepIndex + 1, 1);
        if (nextIndex < tutorialSteps.Count)
        {
            ShowStep(nextIndex);
        }
        else
        {
            CompleteTutorial();
        }
    }

    /// <summary>
    /// Unlocks play intermission without advancing panel (allows player to perform gameplay action).
    /// </summary>
    public void UnlockPlay()
    {
        HideAllStepPanels();
        SetGameplayLock(false);
        Debug.Log($"[TutorialFplanManager] Gameplay UNLOCKED for intermission phase after Step [{currentStepIndex}].");
    }

    /// <summary>
    /// Controls raycast blocker and gameplay lock flag.
    /// </summary>
    private void SetGameplayLock(bool locked)
    {
        isGameplayLocked = locked;

        if (raycastBlocker != null)
        {
            raycastBlocker.alpha = locked ? 1f : 0f;
            raycastBlocker.blocksRaycasts = locked;
            raycastBlocker.interactable = locked;
        }

        // Display only: the phase instruction and waiting overlay are hidden while a step is shown; refresh them.
        PlayerAITutorial.inst?.UpdatePhaseInstruction();
    }

    /// <summary>
    /// Hides all assigned step panels.
    /// </summary>
    private void HideAllStepPanels()
    {
        foreach (var step in tutorialSteps)
        {
            if (step != null && step.panelObject != null)
            {
                step.panelObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Wraps up tutorial sequence and handoffs to PlayerAITutorial end match logic.
    /// </summary>
    public void CompleteTutorial()
    {
        HideAllStepPanels();
        SetGameplayLock(false);
        Debug.Log("[TutorialFplanManager] All tutorial steps finished! Triggering match completion.");

        if (PlayerAITutorial.inst != null)
        {
            PlayerAITutorial.inst.CompleteTutorialAndShowResults();
        }
    }
}
