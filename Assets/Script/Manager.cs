using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] protected Text textEndGame;
    [SerializeField] protected Text textNote;

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

    }

    // Update is called once per frame
    void Update()
    {
        if(!isCut && !isShowView) { 
            if (Input.GetKeyDown(this.normalKey))
            {
                this.status = 0;
                backgraundStatus.color = Color.white;

            }
            if (Input.GetKeyDown(this.redKey))
            {
                this.status = 1;
                backgraundStatus.color = Color.red;

            }
            if (Input.GetKeyDown(this.greenKey))
            {
                this.status = 2;
                backgraundStatus.color = Color.green;

            }
        }

    }



    public int getStatus() { return status; }


    public void setViewAfterCut()
    {

        this.SeeOtherPlayerBT.SetActive(false);
        this.cutBt.SetActive(false);

    }

    public void setViewToChoose()
    {
        viewAfterCut.SetActive(true);
        this.status = 0;
        backgraundStatus.color = Color.white;
        this.isCut = true;
        //this.SeeOtherPlayerBT.SetActive(false);
        //setAllState(player1, player2);
    }

    public void setSumRGYB(float[] sums) { this.sumRGYB = sums; }
    public float getSumRGYB(int i) {
        if(i >= 0 && i < this.sumRGYB.Length)
            return this.sumRGYB[i];
        return 0;
    }

    public void setEndGameLayer(float sum)
    {
        this.textEndGame.text = "You get: "+sum+"%";
        this.viewEndGame.SetActive(true);
        this.cutBt.SetActive(false);
        
    }

    public void setNote(float sec, string massage, bool isError)
    {
        StartCoroutine(withSec(sec, massage, isError));

    }


    private IEnumerator withSec(float sec, string massage, bool isError)
    {
        this.textNote.color = isError ? Color.red: Color.black;
        this.textNote.text = massage;
        yield return new WaitForSeconds(sec);
        this.textNote.text = "";
    }
    public void changeSeeOtherPlayerBT()
    {
        this.status = 0;
        backgraundStatus.color = Color.white;
        Image tempSprit = this.SeeOtherPlayerBT.GetComponent<Image>();
        if (this.isShowView)
        {
            tempSprit.color = Color.white;
            this.cutBt.SetActive(true);
        }
        else
        {
            this.cutBt.SetActive(false);
            tempSprit.color = Color.red;
        }
        this.isShowView = !this.isShowView;

    }

    public bool getIsShowView()
    {
        return this.isShowView;
    }
}
