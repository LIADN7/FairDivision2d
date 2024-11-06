
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{

    [SerializeField] protected int numOfStatus = 2;
    [SerializeField] protected GameObject viewAfterCut;
    [SerializeField] protected GameObject viewTotalValueEndGame;
    [SerializeField] protected GameObject SeeOtherPlayerBT;
    [SerializeField] public GameObject cutBt;
    [SerializeField] public GameObject chatView;
    [SerializeField] private Toggle isAgreedToggle;
    [SerializeField] protected GameObject HomeButton;
    [SerializeField] protected GameObject NextButton;
    [SerializeField] protected Text textEndGameStatus;
    [SerializeField] protected Text textNote;
    //[SerializeField] protected Text textChatAlert;
    //[SerializeField] protected SpriteRenderer backgraundStatus;
    [SerializeField] protected Image buttonNormalKey;
    [SerializeField] protected Image buttonRedKey;
    [SerializeField] protected Image buttonGreenKey;
    [SerializeField] protected KeyCode normalKey;
    [SerializeField] protected KeyCode redKey;
    [SerializeField] protected KeyCode greenKey;
    float[] sumRGYB = { 0, 0, 0, 0 }; //GREEN, RED, YELLOW, BLUE (after cut!)

    public static Manager inst;
    private int status;
    private bool isCut = false;
    private bool isShowView = false;
    private bool isGameOver = false;

    //private GameObject[] squares;
    //public Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();




    private void Awake()
    {

        inst = this;


    }
    void Start()
    {
        this.status = 0;
        this.viewAfterCut.SetActive(false);
        this.viewTotalValueEndGame.SetActive(false);
        if (!Config.inst.getSeeOtherEnable())
        {
            setAllChildFalse(this.SeeOtherPlayerBT);
        }
        this.SeeOtherPlayerBT.SetActive(Config.inst.getSeeOtherEnable());
        this.chatView.SetActive(Config.inst.getChetEnable());
        NextButton.SetActive(false);
        buttonNormalKey.color = Color.white;
        buttonRedKey.color = Color.red;
        buttonGreenKey.color = Color.green;


    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(this.normalKey))
        {
            buttonNormalKey.color = Color.grey;
            //buttonNormalKey.transition = Selectable.Transition.Animation;
            setStatusClick(0);
        }
        if (Input.GetKeyDown(this.redKey))
        {
            setStatusClick(1);
        }
        if (Input.GetKeyDown(this.greenKey))
        {
            setStatusClick(2);
        }


    }



    public int getStatus() { return status; }

    public bool getIsAgreedMark() { return this.isAgreedToggle.isOn; }
    public void setViewAfterCut()
    {

        this.SeeOtherPlayerBT.SetActive(false);
        this.cutBt.SetActive(false);
        this.status = 0;
        resetAllKeyButtons();
        //backgraundStatus.color = Color.white;
        this.isCut = true;

    }

    public void setViewToChoose()
    {
        viewAfterCut.SetActive(true);

    }

    public void setSumRGYB(float[] sums) { this.sumRGYB = sums; }
    public float getSumRGYB(int i)
    {
        if (i >= 0 && i < this.sumRGYB.Length)
            return this.sumRGYB[i];
        return 0;
    }

    public float[] getAllSumRGYB()
    {
        return this.sumRGYB;
    }

    public int getIndexOfMaxSumRGYB()
    {

        int maxIndex = 0;
        float locMax = sumRGYB[maxIndex];
        for (int i = 0; i < sumRGYB.Length; i++)
        {

            if (sumRGYB[i] == 50) return -1 * i; // Dont have somting that is more then 50 and it's dosent metter what he choose
            else if (sumRGYB[maxIndex] > sumRGYB[i])
            {
                maxIndex = i;
            }
        }

        return maxIndex;
    }


    public void setEndGameLayer(float sum)
    {
        this.textEndGameStatus.text = "You get total: " + sum + "%";
        this.viewTotalValueEndGame.SetActive(true);
        NextButton.SetActive(true);
        this.cutBt.SetActive(false);

    }

    public void setNote(float sec, string massage, bool isError)
    {
        StartCoroutine(withSec(sec, massage, isError));

    }


    private IEnumerator withSec(float sec, string massage, bool isError)
    {
        this.textNote.color = isError ? Color.red : Color.black;
        this.textNote.text = massage;
        if (sec < 0)
        {
            yield return new WaitForSeconds(1);
        }
        else
        {
            yield return new WaitForSeconds(sec);
            this.textNote.text = "";

        }
    }


    public void changeSeeOtherPlayerBT()
    {
        if (Config.inst.getSeeOtherEnable())
        {
            this.status = 0;
            resetAllKeyButtons();
            //backgraundStatus.color = Color.white;
            Image tempSprit = this.SeeOtherPlayerBT.GetComponent<Image>();
            if (this.isShowView)
            {
                tempSprit.color = Color.white;
                this.cutBt.SetActive(true && !this.isCut); // not after cut!
            }
            else
            {
                tempSprit.color = Color.red;
                this.cutBt.SetActive(false && !this.isCut); // not after cut!
            }
            this.isShowView = !this.isShowView;
        }
    }


    public void initialSeeOtherPlayerBT()
    {
        this.SeeOtherPlayerBT.SetActive(true && Config.inst.getSeeOtherEnable());
        //backgraundStatus.color = Color.white;
        resetAllKeyButtons();
        this.isShowView = false;
        Image tempSprit = this.SeeOtherPlayerBT.GetComponent<Image>();
        tempSprit.color = Color.white;
    }


    public void setStatusClick(int color)
    {
        if (!isCut && !isShowView)
        {
            resetAllKeyButtons();
            if (color == 0)
            {
                this.status = 0;
                buttonNormalKey.color = new Color(0.8f, 0.8f, 0.8f);
                //backgraundStatus.color = Color.white;

            }
            if (color == 1)
            {
                this.status = 1;
                buttonRedKey.color = new Color(0.5f, 0, 0);
                //backgraundStatus.color = Color.red;

            }
            if (color == 2)
            {
                this.status = 2;
                buttonGreenKey.color = new Color(0, 0.5f, 0);
                //backgraundStatus.color = Color.green;

            }
        }
    }


    public bool getIsShowView()
    {
        return this.isShowView;
    }

    public void resetAllKeyButtons()
    {
        buttonNormalKey.color = Color.white;
        buttonRedKey.color = Color.red;
        buttonGreenKey.color = Color.green;
    }


    public void setAllChildFalse(GameObject obj)
    {

        Image im = obj.GetComponent<Image>();
        if (im != null)
        {
            im.enabled = false;
        }
        Transform parentTransform = obj.transform; // Assuming this script is attached to the parent GameObject

        // Loop through all child objects
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            // Get the child GameObject at index 'i' and set active to false
            parentTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void NextSceneClick()
    {
        NextButton.SetActive(false);
    }
}
