using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfStateAI : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 2;
    [SerializeField] protected float myVal2 = 4;
    private float myPowerColor = 1;
    private float otherPowerColor = 1;
    private SpriteRenderer squareSprite;
    private GameObject childObject;
    private int spriteStatus;
    private int myKey;
    private static int key = 0;


    void Start()
    {
        this.squareSprite = this.GetComponent<SpriteRenderer>();
        this.setChaildSprite();

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
        childObject.transform.localPosition = new Vector3(0, 0, -47);
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
            this.squareSprite.color = Color.yellow;
            spriteStatus = 3;

        }
        if (i == 4)
        {
            // Blue
            //this.squareSprite.color = new Color(0f, 0f, colorPower * 0.4f,  1f);
            this.squareSprite.color = Color.blue;
            spriteStatus = 4;

        }
        this.childObject.GetComponent<SpriteRenderer>().color = new Color(colorPower * 0.5f, 0.5f, 0.5f, 0.8f);


    }

    public int getMyKey()
    {
        return this.myKey;
    }

    public void setmyPowerColor(int i, float sum)
    {
        this.myPowerColor = (getMyVal(i) / sum) * 500;
    }

    public void setOtherPowerColor(int i, float sum)
    {
        this.otherPowerColor = (getMyVal(i) / sum) * 500;
    }


}
