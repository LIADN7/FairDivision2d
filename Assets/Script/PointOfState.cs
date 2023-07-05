using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfState : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 1;
    [SerializeField] protected float myVal2 = 2;
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
            this.squareSprite.color = new Color(0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus()==2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            Player.inst.statusChange();
        }


    }

    void OnMouseDown()
    {
        if (Manager.inst.getStatus() == 1)
        {
            // Red
            this.squareSprite.color = new Color(0.555f, 0.012f, 0.012f, 0.59f);
            spriteStatus = 1;
            Player.inst.statusChange();
        }
        if (Manager.inst.getStatus() == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;
            Player.inst.statusChange();
            
        }


    }


    // Start is called before the first frame update
    void Start()
    {
        // spriteStatus = 0;
        this.squareSprite = this.GetComponent<SpriteRenderer>();
        this.squareSprite.color = new Color(0.555f, 0.012f, 0.012f, 0.59f);


        spriteStatus = 1;
        this.myKey = key++;
    }

    // Update is called once per frame
    void Update()
    {

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
            this.squareSprite.color = new Color(0.555f, 0.012f, 0.012f, 0.59f);
            
            spriteStatus = 1;
            
        }
        if (i == 2)
        {
            // Green
            this.squareSprite.color = new Color(0.014f, 0.525f, 0.053f, 0.59f);
            spriteStatus = 2;

        }
        if (i == 3)
        {
            // Yellow
            this.squareSprite.color = new Color(0.9f, 0.9f, 0f, 0.79f);
            spriteStatus = 3;

        }
        if (i == 4)
        {
            // Blue
            this.squareSprite.color = new Color(0f, 0f, 0.4f, 0.59f);
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
}
