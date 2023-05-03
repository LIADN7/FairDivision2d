using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseButton : MonoBehaviour
{
    // Start is called before the first frame update
    void OnMouseDown()
    {
        GameManager.inst.Choose(this.gameObject.GetComponent<SpriteRenderer>().tag == "Green" ? Color.green : Color.white);

    }
}
