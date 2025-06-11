using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointOfState : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 2;
    [SerializeField] protected float myVal2 = 4;
    private float myPowerColor = 1;
    private float otherPowerColor = 1;
    private SpriteRenderer squareSprite;
    private GameObject childObject;
    private GameObject textChildObject;
    private int spriteStatus;
    private int myKey ;
    private string stateName;
    private static int key=0;



    // Start is called before the first frame update
    void Start()
    {
        // spriteStatus = 0;
        this.squareSprite = this.GetComponent<SpriteRenderer>();

        //this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 1f);
        this.setChaildSprite();
        this.setChaildText();
        this.stateName= this.transform.parent.name;

        this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 1f);
        spriteStatus = 1;
        this.myKey = key++;
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

        this.textChildObject.transform.localPosition = new Vector3(0, 0, -5);
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
        childObject.transform.localPosition = new Vector3(0, 0, -3);
    }


    void OnMouseEnter()
    {
        //Debug.LogError(myKey);
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            //this.squareSprite.color = new Color(myPowerColor*0.555f, 0.012f, 0.012f, 1f);
            this.squareSprite.color = Color.red;
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus()==2)
        {
            // Green
            //this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 1f);
            this.squareSprite.color = Color.green;
            spriteStatus = 2;
            Player.inst.statusChange();
        }
        //this.childObject.GetComponent<SpriteRenderer>().color = new Color(myPowerColor * 0.5f, 0.5f, 0.5f, 0.85f);

    }

    void OnMouseDown()
    {
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            //this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 1f);
            this.squareSprite.color = Color.red;
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            //this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 1f);
            this.squareSprite.color = Color.green;
            spriteStatus = 2;
            Player.inst.statusChange();
            
        }


    }




    public float getMyVal(int i)
    {
        return i==1? this.myVal1: this.myVal2;
    }

    public int getSpriteStatus()
    {
        return this.spriteStatus;
    }

    public string getMyStateName()
    {
        return this.stateName;
    }

    // i is the color number, iPower =1 is my power and 2 for power of the other player
    public void setSpriteStatus(int i, int iPower)
    {
        float colorPower = iPower == 1 ? myPowerColor : otherPowerColor;
        if (i == 1)
        {
            // Red
            //this.squareSprite.color = new Color(colorPower * 0.555f, 0.012f, 0.012f, 1f);
            this.squareSprite.color = Color.red;
            spriteStatus = 1;
            
        }
        if (i == 2)
        {
            // Green
            //this.squareSprite.color = new Color(0.014f, colorPower * 0.525f, 0.053f, 1f);
            this.squareSprite.color = Color.green;
            spriteStatus = 2;

        }
        if (i == 3)
        {
            // Yellow
            //this.squareSprite.color = new Color(colorPower * 0.9f, colorPower * 0.9f, 0f,  0.79f);
            this.squareSprite.color = Color.red;
            spriteStatus = 3;

        }
        if (i == 4)
        {
            // Blue
            //this.squareSprite.color = new Color(0f, 0f, colorPower * 0.4f,  1f);
            this.squareSprite.color = Color.green;
            spriteStatus = 4;

        }
        this.childObject.GetComponent<SpriteRenderer>().color = new Color(colorPower * 0.5f, 0.5f, 0.5f, 0.8f);


    }

    public int getMyKey()
    {
        return this.myKey;
    }

    public void setChildText(string x)
    {
        this.textChildObject.GetComponent<Text>().text = x;
    }
    /*    public SpriteRenderer getSquareSprite()
        {
            //return this.squareSprite;
            return this.childObject.GetComponent<SpriteRenderer>();
        }*/

    public void setmyPowerColor(int i, float sum)
    {
        float factor = getMyVal(i) < 4 ? 0 : getMyVal(i);
        this.myPowerColor = (factor / sum) * 500;
    }

    public void setOtherPowerColor(int i, float sum)
    {
        float factor = getMyVal(i) < 4 ? 0 : getMyVal(i);
        this.otherPowerColor = (factor / sum) * 500;
    }

}
