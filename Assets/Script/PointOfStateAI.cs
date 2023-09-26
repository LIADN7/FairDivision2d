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
    private int spriteStatus;
    private int myKey;
    private static int key = 0;

    void OnMouseEnter()
    {
        //Debug.LogError(myKey);
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            PlayerAI.inst.statusChange();
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            PlayerAI.inst.statusChange();
        }


    }

    void OnMouseDown()
    {
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            PlayerAI.inst.statusChange();
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            PlayerAI.inst.statusChange();

        }


    }


    // Start is called before the first frame update
    void Start()
    {
        // spriteStatus = 0;
        this.squareSprite = this.GetComponent<SpriteRenderer>();
        this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 0.59f);


        spriteStatus = 1;
        this.myKey = key++;
    }


    public float getMyVal(int numPlayer)
    {
        return numPlayer == 1 ? this.myVal1 : this.myVal2;
    }

    public int getSpriteStatus()
    {
        return this.spriteStatus;
    }


    // i is the color number, iPower =1 is my power and 2 for power of the other player
    public void setSpriteStatus(int iColor, int iPower)
    {
        float colorPower = iPower == 1 ? myPowerColor : otherPowerColor;
        if (iColor == 1)
        {
            // Red
            this.squareSprite.color = new Color(colorPower * 0.555f, 0.012f, 0.012f, 0.59f);

            spriteStatus = 1;

        }
        if (iColor == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, colorPower * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;

        }
        if (iColor == 3)
        {
            // Yellow
            this.squareSprite.color = new Color(colorPower * 0.9f, colorPower * 0.9f, 0f, 0.79f);
            spriteStatus = 3;

        }
        if (iColor == 4)
        {
            // Blue
            this.squareSprite.color = new Color(0f, 0f, colorPower * 0.4f, 0.59f);
            spriteStatus = 4;

        }


    }

    public int getMyKey()
    {
        return this.myKey;
    }


    public SpriteRenderer getSquareSprite()
    {
        return this.squareSprite;
    }

    public void setmyPowerColor(int numPlayer, float sum)
    {
        this.myPowerColor = (getMyVal(numPlayer) / sum) * 500;
    }

    public void setOtherPowerColor(int numPlayer, float sum)
    {
        this.otherPowerColor = (getMyVal(numPlayer) / sum) * 500;
    }

}
