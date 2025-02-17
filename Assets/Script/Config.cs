
using System;
using System.Linq.Expressions;
using UnityEngine;

public class Config : MonoBehaviour
{
    public static Config inst;
    [SerializeField] protected bool chatEnable = false;
    [SerializeField] protected bool seeOtherEnable = false;
    [SerializeField] protected int timeForHelpPage = 10;

    public string userID { get; set; } = "defaultUserID"; // A User ID of the player
    public string surveyID { get; set; } = "defaultSurveyID"; // A survey ID of the survey
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
    public int addRoundNumber()
    {
        return ++this.roundNumber;
    }


    /**
     * Create config by scenario number:
     * 1 - without chat and without see other heatmap
     * 2 - without chat and with see other heatmap
     * 3 - with chat and without see other heatmap
     * 4 - with chat and with see other heatmap
     */
    public void createConfig(int scenarioNumber, int roundNumberConfig)
    {
        bool isSeeOtherEnable = scenarioNumber == 2 || scenarioNumber == 4;
        bool isChetEnable = scenarioNumber == 3 || scenarioNumber == 4;
        int timer = -1;
        this.chatEnable = isChetEnable;
        this.seeOtherEnable = isSeeOtherEnable;
        this.timeForHelpPage = timer;
        this.roundNumber = roundNumberConfig;
    }

    public int getScenarioType()
    {
        if (!this.chatEnable && !this.seeOtherEnable)
        {
            return 1;
        }
        else if (!this.chatEnable && this.seeOtherEnable)
        {
            return 2;
        }
        else if (this.chatEnable && !this.seeOtherEnable)
        {
            return 3;
        }
        else if (this.chatEnable && this.seeOtherEnable)
        {
            return 4;
        }
        return -1;
    }

    public string getScenarioTypeName()
    {
        int typeNum = this.getScenarioType();
        //1 Normal, 2 Info, 3 Chat, 4 InfoAChat
        switch (typeNum)
        {
            case 2:
                // code block
                return "Info";
            case 3:
                // code block
                return "Chat";
            case 4:
                // code block
                return "InfoAChat";
            default:
                // code block
                return "Normal";
        }
        return "";
    }

    public bool RedirectToSurvey(bool isFinish = true)
    {
        // Open new tab for the player's survey
        // Application.OpenURL("http://www.panel4all.co.il");
        // http://www.panel4all.co.il/survey_runtime/external_survey_status.php?surveyID=XXXXX&userID=YYYYY&status=finish
        // http://www.panel4all.co.il/survey_runtime/external_survey_status.php?surveyID=XXXXX&userID=YYYYY&status=filterout
        // string url = $"http://www.panel4all.co.il/survey_runtime/external_survey_status.php?surveyID={surveyID}&userID={userID}&status=";

        // url = isFinish ? url + "finish" : url + "filterout";

        // Application.OpenURL(url);
        return true; // Open new tab successfully
    }

}
