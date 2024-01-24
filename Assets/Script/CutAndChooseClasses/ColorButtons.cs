using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorButtons : MonoBehaviour
{
    private Text[] buttonsValueText; // {red, green, yellow, blue}
    private GameObject[] colorsValueObj; // {red, green, yellow, blue}
    // Start is called before the first frame update
    public ColorButtons(Text[] buttonsValueText, GameObject[] colorsValueObj)
    {
        this.buttonsValueText = buttonsValueText;
        this.colorsValueObj = colorsValueObj;
    }



}
