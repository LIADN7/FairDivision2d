using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SumSquaresAITutorial : MonoBehaviour
{
    [SerializeField] protected string stateName = "";
    [SerializeField] protected Text titleText;
    private float sumA = 0, sumB = 0;

    void Start()
    {
        foreach (Transform childTransform in transform)
        {
            PointOfStateAITutorial p = childTransform.gameObject.GetComponent<PointOfStateAITutorial>();
            if (p)
            {
                sumA += p.getMyVal(1);
                sumB += p.getMyVal(2);
            }

        }
        titleText.text = stateName;
    }


    public void InitForAIGame()
    {
        foreach (Transform childTransform in transform)
        {
            PointOfStateAITutorial p = childTransform.gameObject.GetComponent<PointOfStateAITutorial>();
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

        float temp = playerNum == 1 ? sumA : sumB;
        string amountPercentages = ((100.0 * temp / sumOfAll).ToString("F2"));
        titleText.text = stateName + " = " + amountPercentages + "%";
    }


}