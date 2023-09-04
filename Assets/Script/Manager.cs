using Photon.Pun;
using System;
using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{

    [SerializeField] protected int numOfStatus = 2;
    [SerializeField] protected GameObject viewAfterCut;
    [SerializeField] protected GameObject viewEndGame;
    [SerializeField] protected GameObject SeeOtherPlayerBT;
    [SerializeField] public GameObject cutBt;
    [SerializeField] public GameObject cutHelperBt;
    [SerializeField] protected Text textEndGame;
    [SerializeField] protected Text textNote;
    [SerializeField] protected Text textChatAlert;
    [SerializeField] protected SpriteRenderer backgraundStatus;
    [SerializeField] protected KeyCode normalKey;
    [SerializeField] protected KeyCode redKey;
    [SerializeField] protected KeyCode greenKey;
    float[] sumRGYB = { 0, 0, 0, 0 }; //GREEN, RED, YELLOW, BLUE (after cut!)

    public static Manager inst;
    private int status;
    private bool isCut=false;
    private bool isShowView = false;
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
        this.viewEndGame.SetActive(false);
        this.SeeOtherPlayerBT.SetActive(true);
        this.textChatAlert.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {

            if (Input.GetKeyDown(this.normalKey))
            {
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


    public void setViewAfterCut()
    {

        this.SeeOtherPlayerBT.SetActive(false);
        this.cutBt.SetActive(false);
        this.cutHelperBt.SetActive(false);
        this.status = 0;
        backgraundStatus.color = Color.white;
        this.isCut = true;

    }

    public void setViewToChoose()
    {
        viewAfterCut.SetActive(true);

    }

    public void setSumRGYB(float[] sums) { this.sumRGYB = sums; }
    public float getSumRGYB(int i) {
        if(i >= 0 && i < this.sumRGYB.Length)
            return this.sumRGYB[i];
        return 0;
    }

    public void setEndGameLayer(float sum)
    {
        this.textEndGame.text = "You get total: "+sum+"%";
        this.viewEndGame.SetActive(true);
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


    public void setNewMessage()
    {
        StartCoroutine(messageAlert(5f));

    }


    private IEnumerator messageAlert(float sec)
    {
        this.textChatAlert.enabled = true;

        yield return new WaitForSeconds(sec);
        this.textChatAlert.enabled = false;

    }

    public void changeSeeOtherPlayerBT()
    {
        this.status = 0;
        backgraundStatus.color = Color.white;
        Image tempSprit = this.SeeOtherPlayerBT.GetComponent<Image>();
        if (this.isShowView)
        {
            tempSprit.color = Color.white;
            this.cutBt.SetActive(true&&!this.isCut); // not after cut!
        }
        else
        {
            tempSprit.color = Color.red;
            this.cutBt.SetActive(false && !this.isCut); // not after cut!
        }
        this.isShowView = !this.isShowView;

    }


    public void initialSeeOtherPlayerBT()
    {
        this.SeeOtherPlayerBT.SetActive(true);
        backgraundStatus.color = Color.white;
        this.isShowView = false;
        Image tempSprit = this.SeeOtherPlayerBT.GetComponent<Image>();
        tempSprit.color = Color.white;
    }


    public void setStatusClick(int color)
    {
        if (!isCut && !isShowView)
        {
            if (color == 0)
            {
                this.status = 0;
                backgraundStatus.color = Color.white;

            }
            if (color == 1)
            {
                this.status = 1;
                backgraundStatus.color = Color.red;

            }
            if (color == 2)
            {
                this.status = 2;
                backgraundStatus.color = Color.green;

            }
        }
    }


    public bool getIsShowView()
    {
        return this.isShowView;
    }
}
