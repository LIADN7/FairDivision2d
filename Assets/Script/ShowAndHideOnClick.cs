using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowAndHideOnClick : MonoBehaviour
{
    [SerializeField] protected GameObject[] layersToShow;
    [SerializeField] protected GameObject[] layersToHide;
    [SerializeField] protected bool isShown=false;
    private bool[] hiddenLayersWereActive;


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
        // On close, restore each hidden layer to its state before opening, so closing the panel
        // does not re-activate objects that were hidden meanwhile (e.g. SeeOtherPlayerBT after the cut).
        if (!isShown)
        {
            hiddenLayersWereActive = new bool[layersToHide.Length];
        }
        for (int i = 0; i < layersToHide.Length; i++)
        {
            if (!isShown)
            {
                hiddenLayersWereActive[i] = layersToHide[i].activeSelf;
                layersToHide[i].SetActive(false);
            }
            else
            {
                layersToHide[i].SetActive(hiddenLayersWereActive == null || hiddenLayersWereActive[i]);
            }
        }
        isShown = !isShown;
        for (int i = 0; i < layersToShow.Length; i++)
        {
            layersToShow[i].SetActive(isShown);
        }
    }

}
