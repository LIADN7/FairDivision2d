using UnityEngine;

/// <summary>
/// Toggles between two sets of GameObjects based on a boolean state.
/// </summary>
public class ObjectVisibilityToggle : MonoBehaviour
{
    [Header("Objects to Hide When State is True")]
    [SerializeField] private GameObject[] objectsToHide;

    [Header("Objects to Show When State is True")]
    [SerializeField] private GameObject[] objectsToShow;

    [Header("Settings")]
    [SerializeField] private bool startState = false;

    private bool currentState;

    void Start()
    {
        // Initialize the game state
        currentState = startState;
        SetVisibilityState(currentState);
    }

    /// <summary>
    /// Sets the visibility state of both object arrays.
    /// If state is true: hide first array, show second array
    /// If state is false: show first array, hide second array
    /// </summary>
    public void SetVisibilityState(bool state)
    {
        currentState = state;

        // Handle objects to hide
        if (objectsToHide != null)
        {
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null)
                {
                    obj.SetActive(!state); // Hide when state is true
                }
            }
        }

        // Handle objects to show
        if (objectsToShow != null)
        {
            foreach (GameObject obj in objectsToShow)
            {
                if (obj != null)
                {
                    obj.SetActive(state); // Show when state is true
                }
            }
        }
    }

    /// <summary>
    /// Toggles the current state and updates visibility accordingly.
    /// </summary>
    public void ToggleState()
    {
        SetVisibilityState(!currentState);
    }

    /// <summary>
    /// Returns the current state.
    /// </summary>
    public bool GetCurrentState()
    {
        return currentState;
    }
}