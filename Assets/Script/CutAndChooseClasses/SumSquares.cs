using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SumSquares : MonoBehaviour
{
    [SerializeField] protected string stateName="";
    [SerializeField] protected Text titleText;
    private float sumA =0, sumB = 0;

    void Start()
    {
        foreach (Transform childTransform in transform)
        {
            PointOfState p = childTransform.gameObject.GetComponent<PointOfState>(); ;
            if(p)
            {
                sumA +=p.getMyVal(1);
                sumB += p.getMyVal(2);
            }

        }
        titleText.text = stateName;
    }


    public void InitForAIGame()
    {
        foreach (Transform childTransform in transform)
        {
            PointOfStateAI p = childTransform.gameObject.GetComponent<PointOfStateAI>(); ;
            if (p)
            {
                sumA += p.getMyVal(1);
                sumB += p.getMyVal(2);
            }

        }
        titleText.text = stateName;
    }

    public void SetMyTitle(int playerNum, float sumOfAll)
    {

        float temp = playerNum == 1 ? sumA:sumB;
        string amountPercentages = ((100.0*temp / sumOfAll).ToString("F2"));
        titleText.text = stateName + " = " + amountPercentages + "%";
    }


}
