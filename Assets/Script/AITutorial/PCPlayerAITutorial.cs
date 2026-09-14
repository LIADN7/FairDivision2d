using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;



public class PCPlayerAITutorial : MonoBehaviour
{
    [System.Serializable]
    private class CutLineOption
    {
        public Vector3 startPoint;
        public Vector3 endPoint;

        public CutLineOption(Vector3 start, Vector3 end)
        {
            startPoint = start;
            endPoint = end;
        }
    }

    public static PCPlayerAITutorial inst;
    public int pcPlayerNum = 0;
    [SerializeField] private List<CutLineOption> defaultCutLines = new List<CutLineOption>();

    private LineRenderer activeCutLineRenderer;

    void Awake()
    {
        inst = this;
        InitializeDefaultCutLines();
    }

    /// <summary>
    /// Populates the four predefined cut lines used by the PC.
    /// </summary>
    private void InitializeDefaultCutLines()
    {
        if (defaultCutLines.Count > 0)
        {
            return;
        }

        defaultCutLines.Add(new CutLineOption(new Vector3(-1.33f, 4.78f, 0f), new Vector3(-7.67f, -1.90f, 0f)));
        defaultCutLines.Add(new CutLineOption(new Vector3(-4.48f, 4.76f, 0f), new Vector3(-4.48f, -1.90f, 0f)));
        defaultCutLines.Add(new CutLineOption(new Vector3(-8.05f, 4.34f, 0f), new Vector3(-1.40f, -1.35f, 0f)));
        defaultCutLines.Add(new CutLineOption(new Vector3(-1.22f, 2.96f, 0f), new Vector3(-8.32f, 0.31f, 0f)));
    }

    /// <summary>
    /// Shows one random predefined cut line for the PC player.
    /// </summary>
    public void ShowRandomDefaultCutLine()
    {
        if (!TryGetRandomCutLine(out CutLineOption selectedCutLine, out int randomIndex))
        {
            return;
        }

        LineDrawingManagerAITutoria lineManager = Object.FindAnyObjectByType<LineDrawingManagerAITutoria>();
        if (lineManager == null)
        {
            Debug.LogWarning("PCPlayerAITutorial could not find LineDrawingManagerAITutoria");
            return;
        }

        if (activeCutLineRenderer != null)
        {
            Destroy(activeCutLineRenderer.gameObject);
            activeCutLineRenderer = null;
        }

        activeCutLineRenderer = lineManager.ShowLineForPlayer(selectedCutLine.startPoint, selectedCutLine.endPoint, pcPlayerNum);
        Debug.Log($"PCPlayerAITutorial showed random cut line {randomIndex} for player {pcPlayerNum}: Start {selectedCutLine.startPoint}, End {selectedCutLine.endPoint}");
    }

    public void GenerateAndInjectLocalCut(PlayerAITutorial playerTutorial, Dictionary<int, int> localPlayerCut)
    {
        if (playerTutorial == null)
        {
            Debug.LogWarning("PCPlayerAITutorial cannot inject a local cut because PlayerAITutorial is missing");
            return;
        }

        playerTutorial.RegisterLocalPlayerCut(localPlayerCut);

        if (!TryGetRandomCutLine(out CutLineOption selectedCutLine, out int randomIndex))
        {
            return;
        }

        ShowCutLineForPlayer(selectedCutLine);

        Dictionary<int, int> pcPlayerCut = new Dictionary<int, int>();

        foreach (var squareEntry in playerTutorial.squares)
        {
            GameObject squareObject = squareEntry.Value;
            PointOfStateAITutorial pointOfState = squareObject.GetComponent<PointOfStateAITutorial>();
            if (pointOfState == null)
            {
                continue;
            }

            int status = SquareLineCalculatorAITutoria.GetSquareColorByLine(squareObject.transform.position, selectedCutLine.startPoint, selectedCutLine.endPoint);
            int key = pointOfState.getMyKey();

            pcPlayerCut[key] = status;

            pointOfState.setSpriteStatus(status, pcPlayerNum);
        }

        playerTutorial.ApplyLocalPcCut(pcPlayerCut);

        Debug.Log($"PCPlayerAITutorial generated and injected local cut {randomIndex} for player {pcPlayerNum}");
    }

    private bool TryGetRandomCutLine(out CutLineOption selectedCutLine, out int randomIndex)
    {
        selectedCutLine = null;
        randomIndex = -1;

        if (defaultCutLines.Count == 0)
        {
            InitializeDefaultCutLines();
        }

        if (defaultCutLines.Count == 0)
        {
            Debug.LogWarning("PCPlayerAITutorial has no default cut lines to show");
            return false;
        }

        if (pcPlayerNum == 0)
        {
            Debug.LogWarning("PCPlayerAITutorial cannot show a cut line because pcPlayerNum is not set yet");
            return false;
        }

        randomIndex = Random.Range(0, defaultCutLines.Count);
        selectedCutLine = defaultCutLines[randomIndex];
        return true;
    }

    private void ShowCutLineForPlayer(CutLineOption selectedCutLine)
    {
        LineDrawingManagerAITutoria lineManager = Object.FindAnyObjectByType<LineDrawingManagerAITutoria>();
        if (lineManager == null)
        {
            Debug.LogWarning("PCPlayerAITutorial could not find LineDrawingManagerAITutoria");
            return;
        }

        if (activeCutLineRenderer != null)
        {
            Destroy(activeCutLineRenderer.gameObject);
            activeCutLineRenderer = null;
        }

        activeCutLineRenderer = lineManager.ShowLineForPlayer(selectedCutLine.startPoint, selectedCutLine.endPoint, pcPlayerNum);
    }

    /// <summary>
    /// Returns the PC default cut line count.
    /// </summary>
    public int GetDefaultCutLineCount()
    {
        return defaultCutLines.Count;
    }

    /// <summary>
    /// Executes PC choices locally after a short simulated AI thinking delay (1 second).
    /// If count == 1 (PC is Player 1), PC picks 1 random piece.
    /// If count == 2 (PC is Player 2), PC picks 2 random pieces.
    /// </summary>
    public void MakePCChoices(PlayerAITutorial playerTutorial, int count)
    {
        StartCoroutine(MakePCChoicesCoroutine(playerTutorial, count));
    }

    private IEnumerator MakePCChoicesCoroutine(PlayerAITutorial playerTutorial, int count)
    {
        yield return new WaitForSeconds(1.0f); // 1 second simulated thinking time

        if (playerTutorial == null)
        {
            playerTutorial = PlayerAITutorial.inst;
        }

        if (playerTutorial == null)
        {
            Debug.LogWarning("PCPlayerAITutorial cannot make choices because PlayerAITutorial is missing.");
            yield break;
        }

        for (int c = 0; c < count; c++)
        {
            List<int> availableIndices = playerTutorial.GetAvailableChoiceIndices();
            if (availableIndices.Count == 0)
            {
                break;
            }

            int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            playerTutorial.ExecuteLocalChoose(randomIndex, pcPlayerNum);

            if (c < count - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (pcPlayerNum == 1)
        {
            Manager.inst.setNote(-1, "Player 1 choose, please pick 2 parts", false, "שחקן 1 בחר, אנא בחרו שני חלקים");
        }

        // // When PC is Player 2 and completes its 2 choices (making 3 chosen total), wait 0.5s and assign 4th piece to Player 1
        // if (pcPlayerNum == 2 && playerTutorial.chooseNum >= 3)
        // {
        //     yield return new WaitForSeconds(0.5f);
        //     playerTutorial.ExecuteLocalGetValues();
        // }
    }
}



