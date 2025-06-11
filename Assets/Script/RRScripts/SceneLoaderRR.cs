using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles leaving the RR game and loading a new scene for all players.
/// </summary>
public class SceneLoaderRR : MonoBehaviour
{

    /// <summary>
    /// Call this to leave the RR game and load a scene for all players.
    /// </summary>
    public void LeaveGame(string sceneName)
    {
        // Delegate the leave logic to GameManagerRR
        if (GameManagerRR.Instance != null)
        {
            GameManagerRR.Instance.LeaveGame(sceneName);
        }
        else
        {
            Debug.LogError("GameManagerRR.Instance is null. Cannot leave game.");
        }
    }

}
