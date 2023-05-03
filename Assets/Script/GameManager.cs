using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    /*    private int NUM_OF_STATE = 9;
        [SerializeField] protected int numPlayers;
        [SerializeField] protected GameObject[] states;
        [SerializeField] protected GameObject cutButton;
        [SerializeField] protected InputField[] valuesText;
        private double[,] playersValues;
        private bool[,] cutAndChooce;
        private Color playerChooce;
        */
    [SerializeField] protected int numOfStatus=2;
    
    [SerializeField] protected SpriteRenderer backgraundStatus;
    [SerializeField] protected KeyCode normalKey;
    [SerializeField] protected KeyCode redKey;
    [SerializeField] protected KeyCode greenKey;

    [SerializeField] protected Text redValueText;
    [SerializeField] protected Text greenValueText;

    public static GameManager inst;
    private int status;
    //private GameObject[] squares;
    private Dictionary<int, GameObject> squares = new Dictionary<int, GameObject>();
    private float sum;

    private Dictionary<int, int> player1 = new Dictionary<int, int>();
    private Dictionary<int, int> player2 = new Dictionary<int, int>();
    void Start()
    {
        inst = this;
        this.status = 0;
        GameObject[] tempSquares = GameObject.FindGameObjectsWithTag("SquarePoint");

        StartCoroutine(withSec(2f, tempSquares));

        redValueText.text = "Value: " +100+"%";

    }

    private IEnumerator withSec(float sec, GameObject[] tempSquares)
    {
        yield return new WaitForSeconds(sec);

        for (int i = 0; i < tempSquares.Length; i++)
        {

            PointOfState temp = tempSquares[i].GetComponent<PointOfState>();
           // if (!this.squares.ContainsKey(temp.getMyKey()))
            {
                this.squares.Add(temp.getMyKey(), tempSquares[i]);
                this.sum += temp.getMyVal();
            }

        }

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(this.normalKey))
        {
            this.status = 0;
            backgraundStatus.color = Color.white;

        }
        if (Input.GetKeyDown(this.redKey) )
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


    public void Cut(int i)
    {

/*        for (int i = 0; i < squares.Length; i++)
        {
            PointOfState temp = this.squares[i].GetComponent<PointOfState>();
            player1.Add(temp.getMyKey(), temp.getSpriteStatus());
            
        }*/


        if(i == 0)
        {
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                player1.Add(p.getMyKey(), p.getSpriteStatus());
                p.setSpriteStatus(1);
            }
        }
        else
        {
            foreach (var it in this.squares)
            {
                PointOfState p = it.Value.GetComponent<PointOfState>();
                player2.Add(p.getMyKey(), p.getSpriteStatus());
                p.setSpriteStatus(1);
            }
        }




        this.statusChange();




        /*        double sum=0;
                for (int j = 0; j < NUM_OF_STATE; j++)
                {
                    this.playersValues[0, j] = valuesText[j].text == "" ? 0 : Convert.ToDouble(valuesText[j].text);
                    valuesText[j].text = "";
                    sum += this.playersValues[0, j];
                    this.states[j].GetComponent<StateObj>().setChanges();

                }
                for (int j = 0; j < NUM_OF_STATE; j++)
                {
                    this.playersValues[0, j] = this.playersValues[0, j] / sum;
                }
                this.cutButton.SetActive(false);
                this.chooseButtons.SetActive(true);
                this.status++;
                return true;*/

    }

    public void Choose(Color choose)
    {
/*        double sum = 0;
        for (int j = 0; j < NUM_OF_STATE; j++)
        {
            this.playersValues[1, j] = valuesText[j].text == "" ? 0 : Convert.ToDouble(valuesText[j].text);
            sum += this.playersValues[1, j];

        }
        for (int j = 0; j < NUM_OF_STATE; j++)
        {
            this.playersValues[1, j] = this.playersValues[1, j] / sum;
        }
        status++;
        playerChooce = choose;
        double cutVal = 0;
        double chooseVal = 0;

        for (int i = 0; i < NUM_OF_STATE; i++)
        {
            bool temp = this.states[i].GetComponent<StateObj>().getIsOn();
            if ((choose==Color.green && temp) || (!temp && choose == Color.white))
            {
                this.cutAndChooce[1, i] = true;
                chooseVal += playersValues[1, i];
            }
            else 
            {
                this.cutAndChooce[0, i] = true;
                cutVal += playersValues[0, i];
            }
            this.inputObj.SetActive(false);
            

        }
        this.chooseButtons.SetActive(false);
        print("sum of Cut player values: "+ cutVal);
        print("sum of Choose player values: "+ chooseVal);

*/


    }


    public int getStatus() { return status; }


    public void statusChange() {
        float sumG = 0;




        foreach (var it in this.squares)
        {
            PointOfState p = it.Value.GetComponent<PointOfState>();
            if (p.getSpriteStatus() == 2)
                sumG += p.getMyVal();

        }



        float greenVal = (sumG / this.sum);
        greenValueText.text = "Value: " + (greenVal*100) + "%";
        redValueText.text = "Value: " + ((1-greenVal) * 100) + "%";
       
    }

}
