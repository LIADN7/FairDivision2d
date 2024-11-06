using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointOfStateAI : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 2;
    [SerializeField] protected float myVal2 = 4;
    private float myPowerColor = 1;
    private float otherPowerColor = 1;
    private SpriteRenderer squareSprite;
    private GameObject childObject;
    private GameObject textChildObject;
    private int spriteStatus;
    private int myKey;
    private static int key = 0;
    private string[] TEXT_COLOR = {"O","O","X","X" }; // {(RED RED), (GREEN GREEN), (RED GREEN), (GREEN RED)}

    void Start()
    {
        this.squareSprite = this.GetComponent<SpriteRenderer>();
        this.setChaildSprite();
        this.setChaildText();
        this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 1f);
        spriteStatus = 1;
        this.myKey = key++;
    }

    private void setChaildSprite()
    {
        this.childObject = new GameObject("ChildSquare");
        Transform parentTransform = this.gameObject.transform;
        childObject.transform.parent = parentTransform;
        // Add a SpriteRenderer component to the child GameObject
        SpriteRenderer spriteRenderer = childObject.AddComponent<SpriteRenderer>();

        // Load a gray square sprite (replace "GraySquareSprite" with your gray square sprite's name)
        Sprite graySquareSprite = Resources.Load<Sprite>("GraySquareSprite");
        spriteRenderer.sprite = graySquareSprite;
        spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.85f);
        // Set the scale to 0.8 x 0.8
        childObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // You can also set the position and other properties of the child GameObject if needed
        childObject.transform.localPosition = new Vector3(0, 0, -1);
    }


    private void setChaildText()
    {

        this.textChildObject = new GameObject("ChildText");
        Transform parentTransform = this.gameObject.transform;
        this.textChildObject.transform.parent = parentTransform;
        Text textRenderer = textChildObject.AddComponent<Text>();
        Font arielFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textRenderer.font = arielFont;
        textRenderer.fontSize = 60;
        textChildObject.transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        textRenderer.color = Color.black;
        textRenderer.alignment = TextAnchor.MiddleCenter;
        //int randNum = UnityEngine.Random.Range(1, 3);
        //string s = randNum == 1 ? "X":"O";
        textRenderer.text = "";

        this.textChildObject.transform.localPosition = new Vector3(0, 0, 0);
    }
    void OnMouseEnter()
    {
        //Debug.LogError(myKey);
        changeColorOnEvent();

    }

    void OnMouseDown()
    {
        changeColorOnEvent();
    }


    private void changeColorOnEvent()
    {
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = Color.red;
            spriteStatus = 1;
            PlayerVsAI.inst.statusChange(); // Need to update values for the player
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            this.squareSprite.color = Color.green;
            spriteStatus = 2;
            PlayerVsAI.inst.statusChange(); // Need to update values for the player
        }
    }


    public float getMyVal(int i)
    {
        return i == 1 ? this.myVal1 : this.myVal2;
    }

    public int getSpriteStatus()
    {
        return this.spriteStatus;
    }


    // i is the color number, iPower =1 is my power and 2 for power of the other player
    public void setSpriteStatus(int i, int iPower)
    {
        float colorPower = iPower == 1 ? myPowerColor : otherPowerColor;
        if (i == 1)
        {
            // Red
            //this.squareSprite.color = new Color(colorPower * 0.555f, 0.012f, 0.012f, 1f);
            //this.textChildObject.GetComponent<Text>().text = "";
            this.squareSprite.color = Color.red;
            spriteStatus = 1;
            //this.textChildObject.GetComponent<Text>().text = "XX";

        }
        else if (i == 2)
        {
            // Green
            //this.squareSprite.color = new Color(0.014f, colorPower * 0.525f, 0.053f, 1f);
            //this.textChildObject.GetComponent<Text>().text = "";
            this.squareSprite.color = Color.green;
            spriteStatus = 2;
            //this.textChildObject.GetComponent<Text>().text = "OO";

        }
        else if (i == 3)
        {
            // Yellow
            //this.squareSprite.color = new Color(colorPower * 0.9f, colorPower * 0.9f, 0f,  0.79f);
            //this.squareSprite.color = Color.yellow;
            spriteStatus = 3;
            this.squareSprite.color = Color.red;
            //this.textChildObject.GetComponent<Text>().text = "X";
            //this.textChildObject.GetComponent<Text>().text = "XO";
        }
        else if (i == 4)
        {
            // Blue
            //this.squareSprite.color = new Color(0f, 0f, colorPower * 0.4f,  1f);
            //this.squareSprite.color = Color.blue;
            spriteStatus = 4;
            this.squareSprite.color = Color.green;
            //this.textChildObject.GetComponent<Text>().text = "X";
            //this.textChildObject.GetComponent<Text>().text = "OX";

        }
        //this.textChildObject.GetComponent<Text>().text = this.TEXT_COLOR[i-1];
        this.childObject.GetComponent<SpriteRenderer>().color = new Color(colorPower * 0.5f, 0.5f, 0.5f, 0.8f);


    }


    public void setChildText(string x)
    {
        this.textChildObject.GetComponent<Text>().text = x;
    }

    public int getMyKey()
    {
        return this.myKey;
    }

    public void setmyPowerColor(int i, float sum)
    {
        this.myPowerColor = (getMyVal(i) / sum) * 700;
    }

    public void setOtherPowerColor(int i, float sum)
    {
        this.otherPowerColor = (getMyVal(i) / sum) * 700;
    }

    public void setColorTextByValue(int i)
    {
        
    }

}
