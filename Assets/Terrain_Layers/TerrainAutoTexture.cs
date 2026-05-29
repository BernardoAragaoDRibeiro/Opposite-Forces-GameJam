using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainAutoTexture : EditorWindow
{
    private List<Terrain> terrains = new List<Terrain>();
    private bool autoFindTerrains = false;

    private int grassLayerIndex = 0;
    private int rockLayerIndex = 1;
    private float flatAngle = 30f;
    private float steepAngle = 45f;

    // Smooth settings
    private bool smoothFoldout = true;
    private float smoothStrength = 1f;
    private int smoothPasses = 1;

    private enum BlendMode { Uniform, Noise }

    [System.Serializable]
    private class LayerEntry
    {
        public int layerIndex = 0;
        public float weight = 1f;
    }

    [System.Serializable]
    private class HeightBand
    {
        public bool enabled = true;
        public string label = "Band";
        public float heightMin = 0f;
        public float heightMax = 1.5f;
        public bool foldout = true;
        public BlendMode blendMode = BlendMode.Uniform;
        public float noiseScale = 20f;
        public float noiseSharpness = 2f;
        public Vector2 noiseOffset = Vector2.zero;
        public List<LayerEntry> layers = new List<LayerEntry>()
        {
            new LayerEntry(),
            new LayerEntry { layerIndex = 1 }
        };
    }

    private List<HeightBand> heightBands = new List<HeightBand>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Terrain Auto Texture")]
    public static void ShowWindow()
    {
        GetWindow<TerrainAutoTexture>("Terrain Auto Texture");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Terrain Auto Texture", EditorStyles.boldLabel);
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

        // --- Smooth section ---
        smoothFoldout = EditorGUILayout.Foldout(smoothFoldout, "Smooth Heightmap", true, EditorStyles.boldLabel);
        if (smoothFoldout)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.HelpBox("Smooths the terrain heightmap independently from texturing. Apply this first, then apply textures.", MessageType.None);
            EditorGUILayout.Space();

            GUILayout.Label("Strength (1 = very subtle, 10 = strong)", EditorStyles.miniLabel);
            smoothStrength = EditorGUILayout.Slider("Strength", smoothStrength, 1f, 10f);

            GUILayout.Label("Passes (more passes = smoother result)", EditorStyles.miniLabel);
            smoothPasses = EditorGUILayout.IntSlider("Passes", smoothPasses, 1, 20);

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Smooth"))
            {
                List<Terrain> targets = GetTargetTerrains();
                if (targets.Count == 0)
                    EditorUtility.DisplayDialog("No Terrains", "No terrains selected.", "OK");
                else
                    foreach (Terrain t in targets)
                        if (t != null) SmoothTerrain(t);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Slope ---
        GUILayout.Label("Slope Settings", EditorStyles.boldLabel);
        GUILayout.Label("Layer Indexes (check your Terrain Inspector)", EditorStyles.miniLabel);
        grassLayerIndex = EditorGUILayout.IntField("Grass Layer Index", grassLayerIndex);
        rockLayerIndex  = EditorGUILayout.IntField("Rock Layer Index",  rockLayerIndex);
        EditorGUILayout.Space();
        GUILayout.Label("Angle thresholds (degrees)", EditorStyles.miniLabel);
        flatAngle  = EditorGUILayout.Slider("Flat below this angle",  flatAngle,  0f, 90f);
        steepAngle = EditorGUILayout.Slider("Rock above this angle",  steepAngle, 0f, 90f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Height bands ---
        GUILayout.Label("Height Bands", EditorStyles.boldLabel);
        GUILayout.Label("Each band overrides slope rules in its height range.", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        int removeIndex = -1;
        for (int i = 0; i < heightBands.Count; i++)
        {
            HeightBand band = heightBands[i];
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            band.foldout = EditorGUILayout.Foldout(band.foldout, string.IsNullOrEmpty(band.label) ? "Band " + i : band.label, true, EditorStyles.boldLabel);
            band.enabled = EditorGUILayout.Toggle(band.enabled, GUILayout.Width(20));
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                removeIndex = i;
            EditorGUILayout.EndHorizontal();

            if (band.foldout)
            {
                EditorGUI.BeginDisabledGroup(!band.enabled);
                band.label     = EditorGUILayout.TextField("Name", band.label);
                band.heightMin = EditorGUILayout.FloatField("Height min", band.heightMin);
                band.heightMax = EditorGUILayout.FloatField("Height max", band.heightMax);
                EditorGUILayout.Space();
                band.blendMode = (BlendMode)EditorGUILayout.EnumPopup("Blend Mode", band.blendMode);

                if (band.blendMode == BlendMode.Noise)
                {
                    EditorGUILayout.HelpBox("Noise mode: each point gets one dominant texture based on Perlin Noise.", MessageType.None);
                    band.noiseScale     = EditorGUILayout.FloatField("Noise Scale", band.noiseScale);
                    band.noiseSharpness = EditorGUILayout.Slider("Sharpness", band.noiseSharpness, 1f, 10f);
                    band.noiseOffset    = EditorGUILayout.Vector2Field("Noise Offset", band.noiseOffset);
                }

                EditorGUILayout.Space();
                GUILayout.Label("Layers (Index | Weight)", EditorStyles.miniLabel);

                int removeLayer = -1;
                for (int j = 0; j < band.layers.Count; j++)
                {
                    LayerEntry entry = band.layers[j];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Layer", GUILayout.Width(38));
                    entry.layerIndex = EditorGUILayout.IntField(entry.layerIndex, GUILayout.Width(36));
                    GUILayout.Label("Weight", GUILayout.Width(44));
                    entry.weight = Mathf.Max(0f, EditorGUILayout.FloatField(entry.weight, GUILayout.Width(46)));
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                        removeLayer = j;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeLayer >= 0) band.layers.RemoveAt(removeLayer);
                if (GUILayout.Button("+ Add Layer"))
                    band.layers.Add(new LayerEntry { layerIndex = 0, weight = 1f });

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (removeIndex >= 0) heightBands.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Height Band"))
        {
            heightBands.Add(new HeightBand
            {
                label = "Band " + heightBands.Count,
                heightMin = 0f,
                heightMax = 1.5f
            });
        }

        EditorGUILayout.Space();

        List<Terrain> applyTargets = GetTargetTerrains();
        if (applyTargets.Count == 0)
        {
            EditorGUILayout.HelpBox("No terrains selected.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.HelpBox("Will apply to " + applyTargets.Count + " terrain(s).", MessageType.Info);

        if (GUILayout.Button("Apply Auto Texture to All"))
            foreach (Terrain t in applyTargets)
                if (t != null) ApplyTexture(t);

        EditorGUILayout.EndScrollView();
    }

    private List<Terrain> GetTargetTerrains()
    {
        if (autoFindTerrains)
            return new List<Terrain>(FindObjectsByType<Terrain>(FindObjectsSortMode.None));
        return terrains;
    }

    private void SmoothTerrain(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;

        float t = smoothStrength / 10f;

        for (int pass = 0; pass < smoothPasses; pass++)
        {
            float[,] heights = data.GetHeights(0, 0, w, h);
            float[,] smoothed = new float[w, h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float avg = 0f;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, w - 1);
                            int ny = Mathf.Clamp(y + dy, 0, h - 1);
                            avg += heights[ny, nx];
                            count++;
                        }
                    }
                    avg /= count;
                    smoothed[y, x] = Mathf.Lerp(heights[y, x], avg, t);
                }
            }

            data.SetHeights(0, 0, smoothed);
        }

        Debug.Log("Smoothed: " + terrain.name + " | Strength: " + smoothStrength + " | Passes: " + smoothPasses);
    }

    private void ApplyTexture(Terrain terrain)
    {
        TerrainData data    = terrain.terrainData;
        int alphaWidth      = data.alphamapWidth;
        int alphaHeight     = data.alphamapHeight;
        int layerCount      = data.terrainLayers.Length;
        Vector3 terrainSize = data.size;

        float[,,] alphaMaps = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

        for (int y = 0; y < alphaHeight; y++)
        {
            for (int x = 0; x < alphaWidth; x++)
            {
                float normX = (float)x / alphaWidth;
                float normY = (float)y / alphaHeight;

                float worldHeight = data.GetInterpolatedHeight(normX, normY);
                float steepness   = data.GetSteepness(normX, normY);
                float worldX      = normX * terrainSize.x;
                float worldZ      = normY * terrainSize.z;

                for (int l = 0; l < layerCount; l++)
                    alphaMaps[y, x, l] = 0f;

                HeightBand activeBand = null;
                foreach (HeightBand band in heightBands)
                {
                    if (band.enabled && worldHeight >= band.heightMin && worldHeight <= band.heightMax)
                    {
                        activeBand = band;
                        break;
                    }
                }

                if (activeBand != null && activeBand.layers.Count > 0)
                {
                    float totalWeight = 0f;
                    foreach (LayerEntry e in activeBand.layers)
                        totalWeight += e.weight;

                    if (totalWeight > 0f)
                    {
                        if (activeBand.blendMode == BlendMode.Uniform)
                        {
                            foreach (LayerEntry e in activeBand.layers)
                                if (e.layerIndex < layerCount)
                                    alphaMaps[y, x, e.layerIndex] += e.weight / totalWeight;
                        }
                        else
                        {
                            float nx = (worldX + activeBand.noiseOffset.x) / activeBand.noiseScale;
                            float nz = (worldZ + activeBand.noiseOffset.y) / activeBand.noiseScale;
                            float noiseVal = Mathf.PerlinNoise(nx, nz);

                            float cursor = 0f;
                            int chosenLayer = activeBand.layers[0].layerIndex;
                            foreach (LayerEntry e in activeBand.layers)
                            {
                                float slice = e.weight / totalWeight;
                                if (noiseVal <= cursor + slice)
                                {
                                    chosenLayer = e.layerIndex;
                                    break;
                                }
                                cursor += slice;
                            }

                            if (chosenLayer < layerCount)
                                alphaMaps[y, x, chosenLayer] = 1f;
                        }
                    }
                }
                else
                {
                    float grassBlend = 1f;
                    float rockBlend  = 0f;

                    if (steepness >= steepAngle)
                    {
                        grassBlend = 0f;
                        rockBlend  = 1f;
                    }
                    else if (steepness > flatAngle)
                    {
                        float t = (steepness - flatAngle) / (steepAngle - flatAngle);
                        grassBlend = 1f - t;
                        rockBlend  = t;
                    }

                    if (grassLayerIndex < layerCount) alphaMaps[y, x, grassLayerIndex] = grassBlend;
                    if (rockLayerIndex  < layerCount) alphaMaps[y, x, rockLayerIndex]  = rockBlend;
                }
            }
        }

        data.SetAlphamaps(0, 0, alphaMaps);
        Debug.Log("Applied textures to: " + terrain.name);
    }
}