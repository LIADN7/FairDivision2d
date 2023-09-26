using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config : MonoBehaviour
{
    public static Config inst;
    [SerializeField] protected bool chatEnable = false;
    [SerializeField] protected bool seeOtherEnable = false;
    [SerializeField] protected int timeForHelpPage = 10;
    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Config");
        if (objs.Length > 1)
        {
            Destroy(this.gameObject); // Destroy duplicate GameManager instances
        }
        DontDestroyOnLoad(this.gameObject); // Don't destroy this instance when loading new scenes
    }

    void Start()
    {
        inst = this;

    }


    public bool getChetEnable() { return chatEnable; }
    public bool getSeeOtherEnable() { return seeOtherEnable; }
    public int getTimeForHelpPage() { return timeForHelpPage; }
    public void setChetEnable(bool chatEnable) { this.chatEnable = chatEnable; }
    public void setSeeOtherEnable(bool seeOtherEnable) { this.seeOtherEnable = seeOtherEnable; }
    public void setTimeForHelpPage(int timeForHelpPage) { this.timeForHelpPage = timeForHelpPage; }
}
