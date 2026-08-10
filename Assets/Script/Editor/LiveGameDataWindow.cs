#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LiveGameDataWindow : EditorWindow
{
    private Vector2 scrollPos;
    private bool showRandArraysList = true;

    [MenuItem("Tools/Live Game Data Monitor")]
    public static void ShowWindow()
    {
        LiveGameDataWindow window = GetWindow<LiveGameDataWindow>("Live Game Data");
        window.minSize = new Vector2(350, 450);
        window.Show();
    }

    private void OnInspectorUpdate()
    {
        // Repaint constantly during Play Mode to keep values live
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Live Game Data Monitor", EditorStyles.boldLabel);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode in Unity Editor to view real-time values of Config, PlayerData, and RandArrays.", MessageType.Info);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawConfigSection();
        EditorGUILayout.Space(10);

        DrawPlayerDataSection();
        EditorGUILayout.Space(10);

        DrawRandArraysSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawConfigSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Config (Config.inst)", EditorStyles.boldLabel);

        if (Config.inst == null)
        {
            EditorGUILayout.HelpBox("Config.inst is not instantiated in current scene.", MessageType.Warning);
        }
        else
        {
            EditorGUI.indentLevel++;
            Config.inst.userID = EditorGUILayout.TextField("User ID", Config.inst.userID);
            Config.inst.surveyID = EditorGUILayout.TextField("Survey ID", Config.inst.surveyID);
            EditorGUILayout.LabelField("Round Number", Config.inst.getRoundNumber().ToString());
            EditorGUILayout.LabelField("Chat Enable", Config.inst.getChetEnable().ToString());
            EditorGUILayout.LabelField("See Other Enable", Config.inst.getSeeOtherEnable().ToString());
            EditorGUILayout.LabelField("Time For Help Page", Config.inst.getTimeForHelpPage().ToString());
            EditorGUILayout.LabelField("Scenario Type", $"{Config.inst.getScenarioType()} ({Config.inst.getScenarioTypeName()})");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPlayerDataSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Player Data (PlayerData.inst)", EditorStyles.boldLabel);

        if (PlayerData.inst == null)
        {
            EditorGUILayout.HelpBox("PlayerData.inst is not instantiated in current scene.", MessageType.Warning);
        }
        else
        {
            EditorGUI.indentLevel++;
            PlayerData.inst.Name = EditorGUILayout.TextField("Name", PlayerData.inst.Name);
            PlayerData.inst.Email = EditorGUILayout.TextField("Email", PlayerData.inst.Email);
            PlayerData.inst.PhoneNumber = EditorGUILayout.TextField("Phone Number", PlayerData.inst.PhoneNumber);
            PlayerData.inst.ID = EditorGUILayout.TextField("ID", PlayerData.inst.ID);
            EditorGUILayout.LabelField("Win Rand Index", PlayerData.inst.GetRandIndex().ToString());
            EditorGUILayout.LabelField("Winnings", PlayerData.inst.GetNormalizeWinning().ToString("F2"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRandArraysSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Rand Arrays (RandArrays.inst)", EditorStyles.boldLabel);

        if (RandArrays.inst == null)
        {
            EditorGUILayout.HelpBox("RandArrays.inst is not instantiated in current scene.", MessageType.Warning);
        }
        else
        {
            EditorGUI.indentLevel++;
            int count = RandArrays.inst.GetSize();
            EditorGUILayout.LabelField("Items Count", count.ToString());

            showRandArraysList = EditorGUILayout.Foldout(showRandArraysList, $"Float List ({count} items)");
            if (showRandArraysList && count > 0)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < count; i++)
                {
                    EditorGUILayout.LabelField($"Index [{i}]", RandArrays.inst.GetNumber(i).ToString("F4"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }
}
#endif
