
using UnityEngine;
using UnityEngine.UI;


public class PointOfStateRR : MonoBehaviour
{

    [SerializeField] protected float myVal1 = 2;
    [SerializeField] protected float myVal2 = 4;
    private float myPowerColor = 1;
    private float otherPowerColor = 1;
    private SpriteRenderer squareSprite;
    // private GameObject childObject;
    // private GameObject textChildObject;
    private int spriteStatus;
    private int myKey;
    private string stateName;
    private static int key = 0;

    void Start()
    {
        this.squareSprite = this.GetComponent<SpriteRenderer>();
        this.stateName = this.transform.parent.name;

        this.squareSprite.color = new Color(myPowerColor * 0.555f, 0.012f, 0.012f, 1f);
        spriteStatus = 1;
        this.myKey = key++;
    }


    public float getMyVal(int i)
    {
        return i == 1 ? this.myVal1 : this.myVal2;
    }

    public int getSpriteStatus()
    {
        return this.spriteStatus;
    }

    public string getMyStateName()
    {
        return this.stateName;
    }

    public void setmyPowerColor(int i, float sum)
    {
        this.myPowerColor = (getMyVal(i) / sum) * 700;
    }

    public void setOtherPowerColor(int i, float sum)
    {
        this.otherPowerColor = (getMyVal(i) / sum) * 700;
    }

    public int getMyKey()
    {
        return this.myKey;
    }

    // =================== Required to apply ===================
    public void setSpriteStatus(int i, int iPower)
    {
        float colorPower = iPower == 1 ? myPowerColor : otherPowerColor;
        if (i == 1)
        {
            //  0 Gray --> open to choose 
            //  1 Green --> choose by me
            //  2 Red --> choose by other player
            //this.squareSprite.color = new Color(colorPower * 0.555f, 0.012f, 0.012f, 1f);
            // this.squareSprite.color = Color.red;
            // spriteStatus = 1;

        }
        // if (i == 2)
        // {
        //     // Green
        //     //this.squareSprite.color = new Color(0.014f, colorPower * 0.525f, 0.053f, 1f);
        //     this.squareSprite.color = Color.green;
        //     spriteStatus = 2;

        // }
        // if (i == 3)
        // {
        //     // Yellow
        //     //this.squareSprite.color = new Color(colorPower * 0.9f, colorPower * 0.9f, 0f,  0.79f);
        //     this.squareSprite.color = Color.red;
        //     spriteStatus = 3;

        // }
        // if (i == 4)
        // {
        //     // Blue
        //     //this.squareSprite.color = new Color(0f, 0f, colorPower * 0.4f,  1f);
        //     this.squareSprite.color = Color.green;
        //     spriteStatus = 4;
        // 
        // }
        // Need to be the main object
        // this.childObject.GetComponent<SpriteRenderer>().color = new Color(colorPower * 0.5f, 0.5f, 0.5f, 0.8f);
    }

    // =================== Required to apply ===================
    void OnMouseDown()
    {
        // Need to create

    }


}
