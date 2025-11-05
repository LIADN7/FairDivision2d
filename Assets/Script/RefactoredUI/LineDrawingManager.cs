using UnityEngine;
using Photon.Pun;

/// <summary>
/// Manages the drawing of straight lines on the map for dividing territories.
/// Each player draws one line to divide the map into two colored regions.
/// </summary>
public class LineDrawingManager : MonoBehaviourPunCallbacks
{
    [Header("Line Rendering")]
    public Camera gameCamera;

    [Header("Map Boundaries (Auto-Created)")]
    public Vector2 mapSize = new Vector2(15f, 10f); // Size of the drawable area - covers the whole map
    public Vector3 mapCenter = Vector3.zero; // Center position of the map

    [Header("Line Colors")]
    public Color player1LineColor = Color.black;
    public Color player2LineColor = Color.white;

    [Header("Line Properties")]
    public float lineWidth = 0.1f;

    // Auto-created components
    private LineRenderer playerLineRenderer;
    private RectTransform mapBounds;
    private Canvas uiCanvas;
    private Material lineMaterial;

    // Private variables
    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool isDrawing = false;
    private bool hasDrawnLine = false;
    private bool isSubmitted = false; // New: tracks if player has submitted their line
    private int playerNumber; // 1 or 2

    // Events
    public System.Action<Vector3, Vector3, int> OnLineCompleted;
    public System.Action OnLineSubmitted;

    void Start()
    {
        CreateLineMaterial();
        CreateMapBounds();
        InitializeLineRenderer();
        DeterminePlayerNumber();
    }

    void Update()
    {
        if (CanDrawLine())
        {
            HandleLineDrawing();
        }
    }

    /// <summary>
    /// Creates a default line material at runtime.
    /// </summary>
    private void CreateLineMaterial()
    {
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = Color.white;
    }

    /// <summary>
    /// Creates the map boundaries UI element at runtime.
    /// </summary>
    private void CreateMapBounds()
    {
        // Find existing canvas or create new one
        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("Auto-Created Canvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create map bounds rectangle
        GameObject boundsObj = new GameObject("MapBounds");
        boundsObj.transform.SetParent(uiCanvas.transform, false);

        mapBounds = boundsObj.AddComponent<RectTransform>();
        UnityEngine.UI.Image boundsImage = boundsObj.AddComponent<UnityEngine.UI.Image>();

        // Make it transparent (invisible but functional)
        Color transparentColor = Color.white;
        transparentColor.a = 0f;
        boundsImage.color = transparentColor;
        boundsImage.raycastTarget = false; // Don't block mouse clicks

        // Set size and position based on map parameters
        SetupMapBoundsSize();
    }

    /// <summary>
    /// Sets up the size and position of the map bounds based on world coordinates.
    /// </summary>
    private void SetupMapBoundsSize()
    {
        if (gameCamera == null || mapBounds == null) return;

        // Calculate screen coordinates for the map bounds
        Vector3 bottomLeft = mapCenter - new Vector3(mapSize.x / 2, mapSize.y / 2, 0);
        Vector3 topRight = mapCenter + new Vector3(mapSize.x / 2, mapSize.y / 2, 0);

        Vector2 screenBottomLeft = gameCamera.WorldToScreenPoint(bottomLeft);
        Vector2 screenTopRight = gameCamera.WorldToScreenPoint(topRight);

        // Set the RectTransform to match these screen coordinates
        mapBounds.position = (screenBottomLeft + screenTopRight) / 2;
        mapBounds.sizeDelta = screenTopRight - screenBottomLeft;
    }

    /// <summary>
    /// Initializes the line renderer with default settings.
    /// </summary>
    private void InitializeLineRenderer()
    {
        if (playerLineRenderer == null)
        {
            GameObject lineObj = new GameObject("PlayerLine");
            lineObj.transform.SetParent(transform);
            playerLineRenderer = lineObj.AddComponent<LineRenderer>();
        }

        playerLineRenderer.material = lineMaterial;
        playerLineRenderer.startWidth = lineWidth;
        playerLineRenderer.endWidth = lineWidth;
        playerLineRenderer.positionCount = 2;
        playerLineRenderer.useWorldSpace = true;
        playerLineRenderer.enabled = false;
    }

    /// <summary>
    /// Determines the player number based on Photon network role.
    /// </summary>
    private void DeterminePlayerNumber()
    {
        playerNumber = PhotonNetwork.IsMasterClient ? 1 : 2;
        Color lineColor = playerNumber == 1 ? player1LineColor : player2LineColor;
        playerLineRenderer.startColor = lineColor;
        playerLineRenderer.endColor = lineColor;
    }

    /// <summary>
    /// Checks if the current player can draw a line.
    /// Now allows redrawing to replace existing lines, but not after submission.
    /// </summary>
    private bool CanDrawLine()
    {
        return IsMyTurnToDraw() && !isSubmitted; // Cannot draw after submission
    }

    /// <summary>
    /// Determines if it's the current player's turn to draw.
    /// This should be connected to your existing turn system.
    /// </summary>
    private bool IsMyTurnToDraw()
    {
        // TODO: Connect this to your existing turn management system
        // For now, allow both players to draw
        return true;
    }

    /// <summary>
    /// Handles the mouse input for drawing lines.
    /// </summary>
    private void HandleLineDrawing()
    {
        // Allow canceling current drawing with ESC key
        if (Input.GetKeyDown(KeyCode.Escape) && isDrawing)
        {
            CancelCurrentDrawing();
            return;
        }

        // Debug information
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse clicked!");
            Debug.Log($"CanDrawLine: {CanDrawLine()}");
            Debug.Log($"hasDrawnLine: {hasDrawnLine}");
            Debug.Log($"IsMyTurnToDraw: {IsMyTurnToDraw()}");
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            UpdateDrawing();
        }
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            FinishDrawing();
        }
    }

    /// <summary>
    /// Cancels the current line drawing process.
    /// </summary>
    private void CancelCurrentDrawing()
    {
        isDrawing = false;
        if (playerLineRenderer != null)
        {
            playerLineRenderer.enabled = false;
        }
        Debug.Log("Line drawing canceled");
    }

    /// <summary>
    /// Starts the line drawing process. 
    /// If a line already exists, it will be replaced with a new one.
    /// </summary>
    private void StartDrawing()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Debug.Log($"Mouse World Position: {mouseWorldPos}");
        Debug.Log($"Is Point Within Bounds: {IsPointWithinBounds(mouseWorldPos)}");

        if (IsPointWithinBounds(mouseWorldPos))
        {
            // Reset previous line state to allow redrawing
            if (hasDrawnLine)
            {
                Debug.Log("Replacing existing line with new one");
                hasDrawnLine = false;

                // Clean up other player lines created by previous attempts
                LineRenderer[] existingLines = GetComponentsInChildren<LineRenderer>();
                foreach (LineRenderer line in existingLines)
                {
                    if (line != playerLineRenderer)
                    {
                        DestroyImmediate(line.gameObject);
                    }
                }
            }

            startPoint = mouseWorldPos;
            isDrawing = true;
            playerLineRenderer.enabled = true;
            playerLineRenderer.SetPosition(0, startPoint);
            playerLineRenderer.SetPosition(1, startPoint);
            Debug.Log("Started drawing line!");
        }
        else
        {
            Debug.Log("Point is outside bounds!");
        }
    }

    /// <summary>
    /// Updates the line while drawing.
    /// </summary>
    private void UpdateDrawing()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        endPoint = ClampPointToBounds(mouseWorldPos);
        playerLineRenderer.SetPosition(1, endPoint);
    }

    /// <summary>
    /// Finishes the line drawing and stores data locally (no network sync until submit).
    /// </summary>
    private void FinishDrawing()
    {
        isDrawing = false;
        hasDrawnLine = true;

        // Only notify local systems that line is complete
        // DO NOT send to other players yet - they will see it after submit
        OnLineCompleted?.Invoke(startPoint, endPoint, playerNumber);

        Debug.Log($"Player {playerNumber} completed line from {startPoint} to {endPoint} (local only)");
    }

    /// <summary>
    /// Receives line data from other players via RPC.
    /// </summary>
    [PunRPC]
    private void ReceiveLineData(float startX, float startY, float startZ,
                                float endX, float endY, float endZ,
                                int senderPlayerNumber)
    {
        Vector3 receivedStart = new Vector3(startX, startY, startZ);
        Vector3 receivedEnd = new Vector3(endX, endY, endZ);

        // Create a line renderer for the other player's line
        CreateOtherPlayerLine(receivedStart, receivedEnd, senderPlayerNumber);

        // Notify local systems
        OnLineCompleted?.Invoke(receivedStart, receivedEnd, senderPlayerNumber);

        Debug.Log($"Received line from Player {senderPlayerNumber}");
    }

    /// <summary>
    /// Creates a visual representation of another player's line.
    /// </summary>
    private void CreateOtherPlayerLine(Vector3 start, Vector3 end, int otherPlayerNumber)
    {
        GameObject otherLineObj = new GameObject($"Player{otherPlayerNumber}Line");
        otherLineObj.transform.SetParent(transform);

        LineRenderer otherLineRenderer = otherLineObj.AddComponent<LineRenderer>();
        otherLineRenderer.material = lineMaterial;
        otherLineRenderer.startWidth = lineWidth;
        otherLineRenderer.endWidth = lineWidth;
        otherLineRenderer.positionCount = 2;
        otherLineRenderer.useWorldSpace = true;

        Color otherPlayerColor = otherPlayerNumber == 1 ? player1LineColor : player2LineColor;
        otherLineRenderer.startColor = otherPlayerColor;
        otherLineRenderer.endColor = otherPlayerColor; otherLineRenderer.SetPosition(0, start);
        otherLineRenderer.SetPosition(1, end);
    }

    /// <summary>
    /// Converts mouse screen position to world position.
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Fixed distance from camera
        Vector3 worldPos = gameCamera.ScreenToWorldPoint(mouseScreenPos);
        worldPos.z = 0; // Force Z to 0 for 2D
        return worldPos;
    }

    /// <summary>
    /// Checks if a point is within the defined map boundaries using simple world coordinates.
    /// </summary>
    private bool IsPointWithinBounds(Vector3 point)
    {
        // Simple world coordinate bounds check
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        bool withinX = point.x >= (mapCenter.x - halfWidth) && point.x <= (mapCenter.x + halfWidth);
        bool withinY = point.y >= (mapCenter.y - halfHeight) && point.y <= (mapCenter.y + halfHeight);

        Debug.Log($"Point: {point}, Center: {mapCenter}, Size: {mapSize}");
        Debug.Log($"Within bounds: {withinX && withinY}");

        return withinX && withinY;
    }

    /// <summary>
    /// Clamps a point to stay within map boundaries using world coordinates.
    /// </summary>
    private Vector3 ClampPointToBounds(Vector3 point)
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        point.x = Mathf.Clamp(point.x, mapCenter.x - halfWidth, mapCenter.x + halfWidth);
        point.y = Mathf.Clamp(point.y, mapCenter.y - halfHeight, mapCenter.y + halfHeight);
        point.z = 0f;

        return point;
    }

    /// <summary>
    /// Public method to reset the drawing state (useful for restarting the game).
    /// </summary>
    public void ResetDrawing()
    {
        hasDrawnLine = false;
        isDrawing = false;
        isSubmitted = false; // Reset submission state

        if (playerLineRenderer != null)
        {
            playerLineRenderer.enabled = false;
        }

        // Clean up other player lines
        LineRenderer[] allLines = GetComponentsInChildren<LineRenderer>();
        foreach (LineRenderer line in allLines)
        {
            if (line != playerLineRenderer)
            {
                DestroyImmediate(line.gameObject);
            }
        }

        Debug.Log("Drawing state reset - can draw new lines again");
    }

    /// <summary>
    /// Public method to get the current drawn line data.
    /// </summary>
    public bool GetLineData(out Vector3 start, out Vector3 end)
    {
        start = startPoint;
        end = endPoint;
        return hasDrawnLine;
    }

    /// <summary>
    /// Submits the current line and shares it with other players.
    /// After submission, no more drawing is allowed.
    /// </summary>
    public void SubmitLine()
    {
        if (!hasDrawnLine)
        {
            Debug.LogWarning("Cannot submit: No line has been drawn yet");
            return;
        }

        if (isSubmitted)
        {
            Debug.LogWarning("Line already submitted");
            return;
        }

        isSubmitted = true;

        // Send line data to other players via RPC
        photonView.RPC("ReceiveSubmittedLine", RpcTarget.Others,
            startPoint.x, startPoint.y, startPoint.z,
            endPoint.x, endPoint.y, endPoint.z,
            playerNumber);

        // Notify local systems that line is submitted
        OnLineSubmitted?.Invoke();

        Debug.Log($"Player {playerNumber} submitted line from {startPoint} to {endPoint}");
    }

    /// <summary>
    /// Receives submitted line data from other players via RPC.
    /// This will be visible to all players after submission.
    /// </summary>
    [PunRPC]
    private void ReceiveSubmittedLine(float startX, float startY, float startZ,
                                     float endX, float endY, float endZ,
                                     int senderPlayerNumber)
    {
        Vector3 receivedStart = new Vector3(startX, startY, startZ);
        Vector3 receivedEnd = new Vector3(endX, endY, endZ);

        // Create a line renderer for the other player's submitted line
        CreateOtherPlayerLine(receivedStart, receivedEnd, senderPlayerNumber);

        Debug.Log($"Received submitted line from Player {senderPlayerNumber}");
    }

    /// <summary>
    /// Checks if the current player has submitted their line.
    /// </summary>
    public bool IsLineSubmitted()
    {
        return isSubmitted;
    }

    /// <summary>
    /// Checks if both players have submitted their lines (for game progression).
    /// This should be called by the game manager.
    /// </summary>
    public bool AreBothLinesSubmitted()
    {
        // This would need to be tracked by a game manager that knows about both players
        // For now, return false - implement in game manager
        return false;
    }
}