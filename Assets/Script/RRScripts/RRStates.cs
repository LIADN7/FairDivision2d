
using UnityEngine;
using UnityEngine.UI;

public class RRStates : MonoBehaviour
{

    protected string stateName = "";
    [SerializeField] protected Text titleText;
    private float sumA = 0, sumB = 0;

    // Start is called before the first frame update
    void Start()
    {
        this.stateName = this.gameObject.name;
        foreach (Transform childTransform in transform)
        {
            PointOfStateRR p = childTransform.gameObject.GetComponent<PointOfStateRR>(); ;
            if (p)
            {
                sumA += p.getMyVal(1);
                sumB += p.getMyVal(2);
            }

        }
        titleText.text = stateName;
    }

    // public void InitForAIGame()
    // {
    //     foreach (Transform childTransform in transform)
    //     {
    //         PointOfStateAI p = childTransform.gameObject.GetComponent<PointOfStateAI>(); ;
    //         if (p)
    //         {
    //             sumA += p.getMyVal(1);
    //             sumB += p.getMyVal(2);
    //         }

    //     }
    //     titleText.text = stateName;
    // }

    public void SetMyTitle(int playerNum, float sumOfAll)
    {

        float temp = playerNum == 1 ? sumA : sumB;
        string amountPercentages = ((100.0 * temp / sumOfAll).ToString("F2"));
        titleText.text = stateName + " = " + amountPercentages + "%";
    }
}
