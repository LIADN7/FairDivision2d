using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowAndHideOnClick : MonoBehaviour
{
    [SerializeField] protected GameObject[] layersToShow;
    [SerializeField] protected GameObject[] layersToHide;
    private bool isShown;


    void Start()
    {
        isShown = false;
        for(int i = 0;i < layersToShow.Length; i++)
        {
            layersToShow[i].SetActive(isShown);
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
    }

}
