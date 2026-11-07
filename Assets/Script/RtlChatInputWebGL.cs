using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

// UX-013 follow-up (Batch 5, owner-approved): correct Hebrew reading order WHILE typing in the chat.
// The legacy InputField lays typed Hebrew out left-to-right (reversed). In WebGL builds this component places a native
// HTML text input (Assets/Plugins/RtlChatInput.jslib) exactly over the InputField; the browser handles bidi, caret,
// selection, paste and IME. Every edit copies the raw string (logical order, unchanged) into InputField.text, and Enter
// calls the InputField's own onSubmit (Player.sendMessage2), so the send pipeline, ChatMessage/ChatHistory and the RPC
// payload are unchanged. Clearing InputField.text (after sending) clears the HTML input.
// In the Editor and other platforms, and while the browser is in fullscreen, it does nothing and the InputField works as before.
[RequireComponent(typeof(InputField))]
public class RtlChatInputWebGL : MonoBehaviour
{
    private const string PlaceholderRTL = "כתבו הודעה כאן...";

    private InputField field;
    private bool applyingHtmlValue;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void RtlInput_Create(string objectName, string placeholder);
    [DllImport("__Internal")] private static extern void RtlInput_SetRect(float x, float y, float w, float h, float fontPx);
    [DllImport("__Internal")] private static extern void RtlInput_SetVisible(int visible);
    [DllImport("__Internal")] private static extern void RtlInput_SetValue(string value);
    [DllImport("__Internal")] private static extern void RtlInput_Destroy();

    private bool created;
    private Rect lastRect;
    private bool lastVisible;
#endif

    private void Awake()
    {
        field = GetComponent<InputField>();
    }

    private void OnEnable()
    {
        field.onValueChanged.AddListener(OnFieldValueChanged);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!created)
        {
            RtlInput_Create(gameObject.name, PlaceholderRTL);
            created = true;
        }
        RtlInput_SetValue(field.text);
        lastRect = Rect.zero;
        lastVisible = false;
#endif
    }

    private void OnDisable()
    {
        field.onValueChanged.RemoveListener(OnFieldValueChanged);
#if UNITY_WEBGL && !UNITY_EDITOR
        // Help Page, scene change: hide the HTML input and give the keyboard back to Unity.
        RtlInput_SetVisible(0);
        lastVisible = false;
        WebGLInput.captureAllKeyboardInput = true;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (created)
        {
            RtlInput_Destroy();
        }
        WebGLInput.captureAllKeyboardInput = true;
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void LateUpdate()
    {
        bool visible = field.IsInteractable();
        if (visible)
        {
            Rect rect = GetScreenRectTopLeft();
            if (rect != lastRect)
            {
                lastRect = rect;
                RtlInput_SetRect(rect.x, rect.y, rect.width, rect.height, rect.height * 0.42f);
            }
        }
        if (visible != lastVisible)
        {
            lastVisible = visible;
            RtlInput_SetVisible(visible ? 1 : 0);
        }
    }

    // Field rectangle in canvas pixels with a top-left origin (the HTML side scales it to CSS pixels).
    private Rect GetScreenRectTopLeft()
    {
        RectTransform rt = (RectTransform)transform;
        Canvas canvas = field.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        return new Rect(Mathf.Round(min.x), Mathf.Round(Screen.height - max.y), Mathf.Round(max.x - min.x), Mathf.Round(max.y - min.y));
    }
#endif

    // Unity -> HTML: e.g. Player.sendMessage2 clears the field after sending.
    private void OnFieldValueChanged(string value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!applyingHtmlValue)
        {
            RtlInput_SetValue(value);
        }
#endif
    }

    // HTML -> Unity (called by SendMessage from the jslib): the raw string, unchanged.
    public void OnHtmlInput(string value)
    {
        if (field.text == value)
        {
            return;
        }
        applyingHtmlValue = true;
        field.text = value;
        applyingHtmlValue = false;
    }

    // Enter in the HTML input: same path as Enter in the InputField (its onSubmit -> Player.sendMessage2).
    public void OnHtmlSubmit(string value)
    {
        OnHtmlInput(value);
        field.onSubmit.Invoke(field.text);
    }

    // While the HTML input has focus, Unity must not swallow the key events meant for it.
    public void OnHtmlFocus(string focused)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = focused != "1";
#endif
    }
}
