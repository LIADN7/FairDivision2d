using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{

    public void LoadTheScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);

    }

    public void LeaveRoomBeforeLoadTheScene(string sceneName)
    {

        // Only for test 
        PhotonNetwork.LeaveRoom();
        LoadTheScene(sceneName);
    }

}
