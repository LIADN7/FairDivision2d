using Photon.Pun;
using UnityEngine;

public class ServerRR : MonoBehaviourPun
{
    public static ServerRR Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public enum TargetType
    {
        All,
        Others
    }

    // Triggers a function by name on PlayerRR objects with arbitrary parameters
    public void CallPlayerFunction(TargetType target, string functionName, params object[] parameters)
    {
        switch (target)
        {
            case TargetType.All:
                photonView.RPC(functionName, RpcTarget.All, parameters);
                break;
            case TargetType.Others:
                photonView.RPC(functionName, RpcTarget.Others, parameters);
                break;
        }
    }
}
