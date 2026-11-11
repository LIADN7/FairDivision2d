using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LoadScene : MonoBehaviour
{

    public void LoadTheScene(string sceneName)
    {
        //Config.inst.RedirectToSurvey(true);
        SceneManager.LoadScene(sceneName);

    }



    // Home button (CutScene). UX-025: asks first (GameSessionDialogs); the exit below runs only after "יציאה".
    // The round record is saved only by Next, so the "not saved" line is shown until this player has pressed Next.
    public void LeaveRoomBeforeLoadTheScene(string sceneName)
    {
        bool roundUnsaved = Player.inst != null && !Player.inst.HasPressedNext();
        if (GameSessionDialogs.RequestExit(() => LeaveRoomNow(sceneName), roundUnsaved))
        {
            return;
        }
        LeaveRoomNow(sceneName);
    }

    private void LeaveRoomNow(string sceneName)
    {

        try
        {

        // PlayerAITutorial.inst?.LeaveGame(sceneName);

        //AuthenticationService.Instance.SignOut(true);
        Player.inst.LeaveGame(sceneName);

        }
        catch(Exception ex)
        {
            Debug.Log($"Error exit Cloud services: {ex.Message}");

        }

        // Only for test 
        /*        PhotonNetwork.LeaveRoom();
                StartCoroutine(DelayAndLoadScene(0.2f, sceneName));*/
        //LoadTheScene(sceneName);
    }




    // Home button (CutSceneAITutorial). UX-025: asks first; practice is never saved, so no "not saved" line.
    public void LeaveRoomBeforeLoadTheSceneAITutorial(string sceneName)
    {
        if (GameSessionDialogs.RequestExit(() => LeaveRoomNowAITutorial(sceneName), false))
        {
            return;
        }
        LeaveRoomNowAITutorial(sceneName);
    }

    private void LeaveRoomNowAITutorial(string sceneName)
    {

        try
        {

        PlayerAITutorial.inst?.LeaveGame(sceneName);

        }
        catch(Exception ex)
        {
            Debug.Log($"Error exit Cloud services: {ex.Message}");

        }

        // Only for test 
        /*        PhotonNetwork.LeaveRoom();
                StartCoroutine(DelayAndLoadScene(0.2f, sceneName));*/
        //LoadTheScene(sceneName);
    }







    private IEnumerator DelayAndLoadScene(float sec, string sceneName)
    {
        yield return new WaitForSeconds(sec);
        LoadTheScene(sceneName);
    }



}
