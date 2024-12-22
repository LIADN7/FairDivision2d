using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractPlaceHolder : MonoBehaviour
{
    private PhotonView view;
    public static InteractPlaceHolder inst;
    [SerializeField] protected Text chatViewText;
    [SerializeField] protected InputField chatInput;
    [SerializeField] protected Button sendButton;
    void Start()
    {
        view = GetComponent<PhotonView>();
        inst = this;
        string palyerId = PhotonNetwork.IsMasterClient ? "Player 1" : "Player 2";
        Debug.Log(palyerId);
        if (!PhotonNetwork.IsMasterClient)
        {
            view.RPC("InitGame", RpcTarget.AllBufferedViaServer);
        }
    }

    [PunRPC]
    private void InitGame()
    {
       // Debug.Log("Round Number - " + Config.inst.getRoundNumber());
    }

    public void SendMessage()
    {
        
        if (this.chatInput.text.Length > 0 )
        {
            Debug.Log("Send message - "+ this.chatInput.text);
            view.RPC("OtersGetMessage", RpcTarget.Others, this.chatInput.text);
        }
    }

    [PunRPC]
    public void OtersGetMessage(string message)
    {
        this.chatViewText.text = message;

    }
/**/
}
