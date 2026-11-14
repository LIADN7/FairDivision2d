using UnityEngine;

// Display only (game-feel batch). Scene default for the UI-motion switch, plus a tester hotkey (F8) that exists only in
// the Editor and Development builds (never in participant Release builds).
public class GameFeelSettings : MonoBehaviour
{
    [SerializeField] private bool effectsEnabled = true;

    private void Awake()
    {
        if (!GameFeel.OverriddenByTester)
        {
            GameFeel.SetEffects(effectsEnabled);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            GameFeel.SetEffects(!GameFeel.EffectsEnabled, true);
        }
    }

    // Test hook (Development/Editor only): unityInstance.SendMessage("GameFeel", "SetEffectsFromTest", "0" or "1").
    public void SetEffectsFromTest(string value)
    {
        GameFeel.SetEffects(value == "1" || value == "true", true);
    }
#endif
}
