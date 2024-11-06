using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using System;

public class LoadScene : MonoBehaviour
{

    public void LoadTheScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);

    }


    public void LeaveRoomBeforeLoadTheScene(string sceneName)
    {
        try
        {
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




    private IEnumerator DelayAndLoadScene(float sec, string sceneName)
    {
        yield return new WaitForSeconds(sec);
        LoadTheScene(sceneName);
    }



}
