using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutButton : MonoBehaviour
{

    private int isCut=0;



    void OnMouseDown()
    {
        if (isCut<2)
        {

            GameManager.inst.Cut(isCut);
            isCut++;
            
        }

        


    }

}
