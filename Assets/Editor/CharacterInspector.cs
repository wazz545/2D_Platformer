#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(Character))]
public class CharacterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var ch = (Character)target;
        serializedObject.Update();

        // --- CONFIG ---
        var configProp = serializedObject.FindProperty("configuration");
        var configGuids = AssetDatabase.FindAssets($"t:{nameof(CharacterConfigurationSO)}");
        var configs = configGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<CharacterConfigurationSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .OrderBy(a => string.IsNullOrEmpty(a.displayName) ? a.name : a.displayName)
            .ToArray();

        string[] configNames = configs.Length == 0
            ? new[] { "(No CharacterConfiguration assets found)" }
            : configs.Select(a => string.IsNullOrEmpty(a.displayName) ? a.name : a.displayName).ToArray();

        int configIndex = 0;
        if (configProp.objectReferenceValue != null)
        {
            var cur = (CharacterConfigurationSO)configProp.objectReferenceValue;
            for (int i = 0; i < configs.Length; i++)
                if (configs[i] == cur) { configIndex = i; break; }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Configuration (Dropdown)", EditorStyles.boldLabel);
        int newConfigIndex = EditorGUILayout.Popup("Character Configuration", configIndex, configNames);
        if (configs.Length > 0 && newConfigIndex >= 0 && newConfigIndex < configs.Length)
        {
            if (configProp.objectReferenceValue != configs[newConfigIndex])
            {
                configProp.objectReferenceValue = configs[newConfigIndex];
                serializedObject.ApplyModifiedProperties();
                ch.SetConfiguration(configs[newConfigIndex]); // ensures components attached correctly
                EditorUtility.SetDirty(ch);
            }
        }

        // --- STATS ---
        var statsProp = serializedObject.FindProperty("stats");
        var statsGuids = AssetDatabase.FindAssets($"t:{nameof(CharacterStatsSO)}");
        var stats = statsGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<CharacterStatsSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .OrderBy(a => a.isPlayer ? 0 : 1)
            .ThenBy(a => a.name)
            .ToArray();

        string[] statNames = stats.Length == 0
            ? new[] { "(No CharacterStats assets found)" }
            : stats.Select(a => (a.isPlayer ? "[Player] " : "[AI] ") + a.name).ToArray();

        int statsIndex = 0;
        if (statsProp.objectReferenceValue != null)
        {
            var cur = (CharacterStatsSO)statsProp.objectReferenceValue;
            for (int i = 0; i < stats.Length; i++)
                if (stats[i] == cur) { statsIndex = i; break; }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Stats (Dropdown)", EditorStyles.boldLabel);
        int newStatsIndex = EditorGUILayout.Popup("Character Stats", statsIndex, statNames);
        if (stats.Length > 0 && newStatsIndex >= 0 && newStatsIndex < stats.Length)
        {
            if (statsProp.objectReferenceValue != stats[newStatsIndex])
            {
                statsProp.objectReferenceValue = stats[newStatsIndex];
                serializedObject.ApplyModifiedProperties();
                ch.SetStats(stats[newStatsIndex]);
                EditorUtility.SetDirty(ch);
            }
        }

        EditorGUILayout.Space(10);
        DrawPropertiesExcluding(serializedObject, "m_Script", "configuration", "stats");
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
