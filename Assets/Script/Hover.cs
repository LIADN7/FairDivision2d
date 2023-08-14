using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Hover : MonoBehaviour
{

    void OnMouseOver()
    {
        GetComponent<SpriteRenderer>().color = Color.grey;
    }


    void OnMouseExit()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }

}
