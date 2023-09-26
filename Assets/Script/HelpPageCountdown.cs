
using Photon.Pun.UtilityScripts;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HelpPageCountdown : MonoBehaviour
{

    [SerializeField] protected int countdownTime;
    [SerializeField] protected Text countdownTimeText;
    [SerializeField] protected GameObject objectToShowAfter;


    // Start is called before the first frame update
    void Start()
    {
        this.objectToShowAfter.SetActive(false);
        this.countdownTime = Config.inst.getTimeForHelpPage();
        StartCoroutine(withBeforeStart(1f));
        
    }

    // Update is called once per frame
    void Update()
    {
        if (countdownTime > 0)
            objectToShowAfter.SetActive(false);
    }



    private IEnumerator withBeforeStart(float sec)
    {
        yield return new WaitForSeconds(sec);
        objectToShowAfter.GetComponent<ShowAndHideOnClick>().ShowAndHide();
        objectToShowAfter.SetActive(false);
        StartCoroutine(startCountdown());
    }


        private IEnumerator startCountdown()
    {
        if(countdownTime > 0)
        {
            countdownTimeText.text = "Wait " + this.countdownTime+ " sec to continue";
            yield return new WaitForSeconds(1.0f);
            this.countdownTime--;
            StartCoroutine(startCountdown());
        }
        else
        {
            yield return new WaitForSeconds(0f);
            countdownTimeText.text = "Help Page";
            objectToShowAfter.SetActive(true);
        }

    }

}
