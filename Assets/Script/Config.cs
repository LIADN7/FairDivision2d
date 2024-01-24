
using UnityEngine;

public class Config : MonoBehaviour
{
    public static Config inst;
    [SerializeField] protected bool chatEnable = false;
    [SerializeField] protected bool seeOtherEnable = false;
    [SerializeField] protected int timeForHelpPage = 10;
    
    private int roundNumber = 0;
    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Config");
        if (objs.Length > 1)
        {
            Destroy(this.gameObject); // Destroy duplicate GameManager instances
        }
        DontDestroyOnLoad(this.gameObject); // Don't destroy this instance when loading new scenes
    }

    void Start()
    {
        inst = this;

    }


    public bool getChetEnable() { return chatEnable; }
    public bool getSeeOtherEnable() { return seeOtherEnable; }
    public int getTimeForHelpPage() { return timeForHelpPage; }
    public int getRoundNumber() { return this.roundNumber; }
    public void setChetEnable(bool chatEnable) { this.chatEnable = chatEnable; }
    public void setSeeOtherEnable(bool seeOtherEnable) { this.seeOtherEnable = seeOtherEnable; }
    public void setTimeForHelpPage(int timeForHelpPage) { this.timeForHelpPage = timeForHelpPage; }
    public int addRoundNumber() {
        return ++this.roundNumber;
    }


    /**
     * Create config by scenario number:
     * 1 - without chat and without see other heatmap
     * 2 - without chat and with see other heatmap
     * 3 - with chat and without see other heatmap
     * 4 - with chat and with see other heatmap
     */
    public void createConfig(int scenarioNumber)
    {
        bool isSeeOtherEnable = scenarioNumber == 2 || scenarioNumber == 4;
        bool isChetEnable = scenarioNumber == 3 || scenarioNumber == 4;
        int timer = -1;
        this.chatEnable = isChetEnable;
        this.seeOtherEnable = isSeeOtherEnable;
        this.timeForHelpPage = timer;
        this.roundNumber = 0;
    }

}
