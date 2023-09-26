using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadScene : MonoBehaviour
{

    public void loadTheScene(string sceneName)
    {
        PhotonNetwork.JoinRoom(sceneName);
    }
}
