using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapCut : MonoBehaviour
{
    public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;
    [SerializeField] protected Text yellowValueText;
    [SerializeField] protected Text blueValueText;
    private float sumPlayer = 0f;
    public MapCut(int sum)
    {
        this.sumPlayer=sum;
        this.squares = Player.inst.squares; 
    }



    public void statusChange()
    {
        float sumG = 0;
        int j = PhotonNetwork.IsMasterClient ? 1 : 2;




        foreach (var it in squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            if (p.getSpriteStatus() == 2)
            {
                sumG += p.getMyVal(j);
            }

        }



        float greenVal = (sumG / this.sumPlayer);
        greenValueText.text = "Value: " + (greenVal * 100) + "%";
        redValueText.text = "Value: " + ((1 - greenVal) * 100) + "%";

    }


}
