using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfState : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 2;
    [SerializeField] protected float myVal2 = 4;
    private float myPowerColor = 1;
    private SpriteRenderer squareSprite;
    private int spriteStatus;
    private int myKey ;
    private static int key=0;

    void OnMouseEnter()
    {
        //Debug.LogError(myKey);
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = new Color(myPowerColor*0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus()==2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            Player.inst.statusChange();
        }


    }

    void OnMouseDown()
    {
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            Player.inst.statusChange();
            
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


    public float getMyVal(int i)
    {
        return i==1? this.myVal1: this.myVal2;
    }

    public int getSpriteStatus()
    {
        return this.spriteStatus;
    }

    public void setSpriteStatus(int i)
    {
        if (i == 1)
        {
            // Red
            this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 0.59f);
            
            spriteStatus = 1;
            
        }
        if (i == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, myPowerColor * 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;

        }
        if (i == 3)
        {
            // Yellow
            this.squareSprite.color = new Color(myPowerColor * 0.9f, myPowerColor * 0.9f, 0f,  0.79f);
            spriteStatus = 3;

        }
        if (i == 4)
        {
            // Blue
            this.squareSprite.color = new Color(0f, 0f, myPowerColor * 0.4f,  0.59f);
            spriteStatus = 4;

        }
        //Player.inst.statusChange();

    }

    public int getMyKey()
    {
        return this.myKey;
    }


    public SpriteRenderer getSquareSprite()
    {
        return this.squareSprite;
    }

    public void setmyPowerColor(int i, float sum)
    {
        this.myPowerColor= (getMyVal(i)/sum)*500;
    }
}
