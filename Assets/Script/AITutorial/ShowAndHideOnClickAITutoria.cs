using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShowAndHideOnClickAITutoria : MonoBehaviour
{
    [SerializeField] protected GameObject[] layersToShow;
    [SerializeField] protected GameObject[] layersToHide;
    [SerializeField] protected bool isShown = false;

    [Header("Optional Panel Events")]
    [SerializeField] public UnityEvent onShowEvent;
    [SerializeField] public UnityEvent onHideEvent;

    private bool hasTriggeredTutorialFirstTime = false;


    void Start()
    {
        isShown = false;
        for(int i = 0;i < layersToShow.Length; i++)
        {
            layersToShow[i].SetActive(isShown);
        }
        for (int i = 0; i < layersToHide.Length; i++)
        {
            layersToHide[i].SetActive(!isShown);
        }
    }


    public void ShowAndHide()
    {
        for (int i = 0; i < layersToHide.Length; i++)
        {
            layersToHide[i].SetActive(isShown);
        }
        isShown = !isShown;
        for (int i = 0; i < layersToShow.Length; i++)
        {
            layersToShow[i].SetActive(isShown);
        }

        if (isShown)
        {
            onShowEvent?.Invoke();
        }
        else
        {
            onHideEvent?.Invoke();
            if (!hasTriggeredTutorialFirstTime && TutorialFplanManager.Instance != null)
            {
                hasTriggeredTutorialFirstTime = true;
                TutorialFplanManager.Instance.StartTutorial();
            }
        }
    }
}
