using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainAutoDetails : EditorWindow
{
    private List<Terrain> terrains = new List<Terrain>();
    private bool autoFindTerrains = false;

    [System.Serializable]
    private class DetailRule
    {
        public bool enabled = true;
        public string label = "Vegetation";
        public bool foldout = true;
        public int alphaLayerIndex = 0;
        public int detailLayerIndex = 0;
        public float threshold = 0.5f;
        public int density = 128;
    }

    private List<DetailRule> rules = new List<DetailRule>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Terrain Auto Details")]
    public static void ShowWindow()
    {
        GetWindow<TerrainAutoDetails>("Terrain Auto Details");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Terrain Auto Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- Terrain list ---
        GUILayout.Label("Terrains", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        autoFindTerrains = EditorGUILayout.Toggle("Auto-find all in scene", autoFindTerrains);
        if (GUILayout.Button("Find Now", GUILayout.Width(80)))
        {
            terrains.Clear();
            foreach (Terrain t in FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                terrains.Add(t);
        }
        EditorGUILayout.EndHorizontal();

        if (!autoFindTerrains)
        {
            int removeIdx = -1;
            for (int i = 0; i < terrains.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                terrains[i] = (Terrain)EditorGUILayout.ObjectField("Terrain " + i, terrains[i], typeof(Terrain), true);
                if (GUILayout.Button("-", GUILayout.Width(24)))
                    removeIdx = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIdx >= 0) terrains.RemoveAt(removeIdx);
            if (GUILayout.Button("+ Add Terrain"))
                terrains.Add(null);
        }
        else
        {
            EditorGUILayout.HelpBox("Will apply to ALL Terrain objects found in the scene.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Vegetation rules ---
        GUILayout.Label("Vegetation Rules", EditorStyles.boldLabel);
        GUILayout.Label("Each rule paints a detail mesh where a terrain layer is present.", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        int removeRule = -1;
        for (int i = 0; i < rules.Count; i++)
        {
            DetailRule rule = rules[i];
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            rule.foldout = EditorGUILayout.Foldout(rule.foldout, string.IsNullOrEmpty(rule.label) ? "Rule " + i : rule.label, true, EditorStyles.boldLabel);
            rule.enabled = EditorGUILayout.Toggle(rule.enabled, GUILayout.Width(20));
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                removeRule = i;
            EditorGUILayout.EndHorizontal();

            if (rule.foldout)
            {
                EditorGUI.BeginDisabledGroup(!rule.enabled);

                rule.label            = EditorGUILayout.TextField("Name", rule.label);
                rule.alphaLayerIndex  = EditorGUILayout.IntField("Alpha Layer Index (texture)", rule.alphaLayerIndex);
                rule.detailLayerIndex = EditorGUILayout.IntField("Detail Mesh Index", rule.detailLayerIndex);

                EditorGUILayout.Space();
                GUILayout.Label("Min alpha to paint (0 = always, 1 = only full coverage)", EditorStyles.miniLabel);
                rule.threshold = EditorGUILayout.Slider("Threshold", rule.threshold, 0f, 1f);

                GUILayout.Label("Density (1 = sparse, 255 = max)", EditorStyles.miniLabel);
                rule.density = EditorGUILayout.IntSlider("Density", rule.density, 1, 255);

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (removeRule >= 0) rules.RemoveAt(removeRule);

        if (GUILayout.Button("+ Add Vegetation Rule"))
        {
            rules.Add(new DetailRule
            {
                label = "Vegetation " + rules.Count,
                alphaLayerIndex = 0,
                detailLayerIndex = rules.Count,
                threshold = 0.5f,
                density = 128
            });
        }

        EditorGUILayout.Space();

        List<Terrain> targets = GetTargetTerrains();

        if (targets.Count == 0)
        {
            EditorGUILayout.HelpBox("No terrains selected.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (rules.Count == 0)
        {
            EditorGUILayout.HelpBox("No vegetation rules added.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.HelpBox("Will apply " + rules.Count + " rule(s) to " + targets.Count + " terrain(s).", MessageType.Info);

        if (GUILayout.Button("Apply Details to All"))
        {
            foreach (Terrain t in targets)
                if (t != null) ApplyDetails(t);
        }

        if (GUILayout.Button("Clear All Details"))
        {
            foreach (Terrain t in targets)
                if (t != null) ClearAllDetails(t);
        }

        EditorGUILayout.EndScrollView();
    }

    private List<Terrain> GetTargetTerrains()
    {
        if (autoFindTerrains)
            return new List<Terrain>(FindObjectsByType<Terrain>(FindObjectsSortMode.None));
        return terrains;
    }

    private void ApplyDetails(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int alphaW       = data.alphamapWidth;
        int alphaH       = data.alphamapHeight;
        int detailW      = data.detailWidth;
        int detailH      = data.detailHeight;
        int layerCount   = data.terrainLayers.Length;
        int detailCount  = data.detailPrototypes.Length;

        float[,,] alphaMaps = data.GetAlphamaps(0, 0, alphaW, alphaH);

        foreach (DetailRule rule in rules)
        {
            if (!rule.enabled) continue;

            if (rule.alphaLayerIndex >= layerCount)
            {
                Debug.LogWarning(terrain.name + " [" + rule.label + "]: Alpha Layer Index out of range. Skipping.");
                continue;
            }

            if (rule.detailLayerIndex >= detailCount)
            {
                Debug.LogWarning(terrain.name + " [" + rule.label + "]: Detail Mesh Index out of range. Skipping.");
                continue;
            }

            int[,] detailMap = data.GetDetailLayer(0, 0, detailW, detailH, rule.detailLayerIndex);

            for (int y = 0; y < detailH; y++)
            {
                for (int x = 0; x < detailW; x++)
                {
                    int ax = Mathf.Clamp(Mathf.RoundToInt((float)x / detailW * alphaW), 0, alphaW - 1);
                    int ay = Mathf.Clamp(Mathf.RoundToInt((float)y / detailH * alphaH), 0, alphaH - 1);

                    float alpha = alphaMaps[ay, ax, rule.alphaLayerIndex];
                    detailMap[y, x] = alpha >= rule.threshold ? rule.density : 0;
                }
            }

            data.SetDetailLayer(0, 0, rule.detailLayerIndex, detailMap);
            Debug.Log(terrain.name + ": Applied rule [" + rule.label + "]");
        }
    }

    private void ClearAllDetails(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int detailW      = data.detailWidth;
        int detailH      = data.detailHeight;
        int detailCount  = data.detailPrototypes.Length;

        int[,] empty = new int[detailH, detailW];
        for (int i = 0; i < detailCount; i++)
            data.SetDetailLayer(0, 0, i, empty);

        Debug.Log("All details cleared from: " + terrain.name);
    }
}