#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.UI;

// Shows a persistent "TEST MODE" label in the Editor and in Development builds, so a test session
// (separate Photon pool, "development" Unity Services environment, no survey redirect) is never
// mistaken for the real experiment. Not compiled into Release builds; touches no game state.
public class TestModeBadge : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject root = new GameObject("TestModeBadge");
        DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(8f, -8f);
        panelRect.sizeDelta = new Vector2(250f, 30f);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.75f, 0f, 0f, 0.85f);
        background.raycastTarget = false;

        GameObject label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(panel.transform, false);
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        Text text = label.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "TEST MODE - not research data";
        text.raycastTarget = false;
    }
}
#endif
