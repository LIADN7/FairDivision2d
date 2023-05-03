using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateObj : MonoBehaviour
{



    private bool isOn;
    private SpriteRenderer stateSprite;
    private GameObject state;
    private bool canChange;

    void OnMouseDown()
    {
        if (canChange)
        {
            if (!isOn)
            {
                this.stateSprite.color = Color.green;
          
            }
            else
            {
                this.stateSprite.color = Color.white;
            }
            isOn = !isOn;
        }



    }


    void Start()
    {
            this.state = gameObject;
            this.stateSprite = this.state.GetComponent<SpriteRenderer>();
            this.isOn = false;
            this.canChange = true;


    }



    public bool getIsOn() { return isOn; }
    public void setChanges() { this.canChange = false; }

}
