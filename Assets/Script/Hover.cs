using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Hover : MonoBehaviour
{
    /*    // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }*/
/*    [SerializeField] protected string sceneName;
    [SerializeField] protected Text playerName;

    void OnMouseDown()
    {
        if (playerName.text != "")
        {
            SceneManager.LoadScene(sceneName);
        }

    }*/


    void OnMouseOver()
    {
        GetComponent<SpriteRenderer>().color = Color.grey;
    }


    void OnMouseExit()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }

}
