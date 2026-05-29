using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainHeightTexture : EditorWindow
{
    private List<Terrain> terrains = new List<Terrain>();
    private bool autoFindTerrains = false;

    private enum BlendMode { Uniform, Noise }

    [System.Serializable]
    private class LayerEntry
    {
        public int   layerIndex  = 0;
        public float weight      = 1f;
        public float noiseScale  = 20f;
        public float noiseOffsetX = 0f;
        public float noiseOffsetY = 0f;
    }

    [System.Serializable]
    private class HeightBand
    {
        public bool      enabled          = true;
        public string    label            = "Band";
        public bool      foldout          = true;
        public float     heightMin        = 0f;
        public float     heightMax        = 10f;
        public BlendMode blendMode        = BlendMode.Noise;
        public float     globalSmoothness = 0.5f;
        public List<LayerEntry> layers    = new List<LayerEntry>()
        {
            new LayerEntry { layerIndex = 0, weight = 1f }
        };
    }

    private List<HeightBand> bands = new List<HeightBand>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Terrain Height Texture")]
    public static void ShowWindow()
    {
        GetWindow<TerrainHeightTexture>("Terrain Height Texture");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Terrain Height Texture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define faixas de altura e aplica texturas nelas.\n" +
            "Pixels fora de qualquer banda ficam sem textura (weight 0) — garanta que as bandas cubram toda a altura do terreno.",
            MessageType.Info);
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
                if (GUILayout.Button("-", GUILayout.Width(24))) removeIdx = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIdx >= 0) terrains.RemoveAt(removeIdx);
            if (GUILayout.Button("+ Add Terrain")) terrains.Add(null);
        }
        else
        {
            EditorGUILayout.HelpBox("Will apply to ALL Terrain objects found in the scene.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // --- Height bands ---
        GUILayout.Label("Height Bands", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int removeIndex = -1;
        for (int i = 0; i < bands.Count; i++)
        {
            HeightBand band = bands[i];
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            band.foldout = EditorGUILayout.Foldout(band.foldout,
                string.IsNullOrEmpty(band.label) ? "Band " + i : band.label, true, EditorStyles.boldLabel);
            band.enabled = EditorGUILayout.Toggle(band.enabled, GUILayout.Width(20));
            if (GUILayout.Button("✕", GUILayout.Width(24))) removeIndex = i;
            EditorGUILayout.EndHorizontal();

            if (band.foldout)
            {
                EditorGUI.BeginDisabledGroup(!band.enabled);

                band.label     = EditorGUILayout.TextField("Nome",       band.label);
                band.heightMin = EditorGUILayout.FloatField("Altura Mínima (m)", band.heightMin);
                band.heightMax = EditorGUILayout.FloatField("Altura Máxima (m)", band.heightMax);

                EditorGUILayout.Space();
                band.blendMode = (BlendMode)EditorGUILayout.EnumPopup("Blend Mode", band.blendMode);

                if (band.blendMode == BlendMode.Noise)
                {
                    EditorGUILayout.HelpBox(
                        "Noise: cada layer tem seu próprio noise e ocupa territórios separados.\n" +
                        "Suavidade 0 = corte seco. Suavidade 1 = bordas muito suaves.\n" +
                        "Weight controla quanto território cada layer reivindica.",
                        MessageType.None);
                    band.globalSmoothness = EditorGUILayout.Slider("Suavidade das Bordas", band.globalSmoothness, 0f, 1f);
                }
                else
                {
                    EditorGUILayout.HelpBox("Uniform: todas as layers mescladas proporcionalmente ao weight.", MessageType.None);
                }

                EditorGUILayout.Space();
                GUILayout.Label("Layers", EditorStyles.miniBoldLabel);

                int removeLayer = -1;
                for (int j = 0; j < band.layers.Count; j++)
                {
                    LayerEntry entry = band.layers[j];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Layer {j}", EditorStyles.boldLabel);
                    if (GUILayout.Button("-", GUILayout.Width(22))) removeLayer = j;
                    EditorGUILayout.EndHorizontal();

                    entry.layerIndex = EditorGUILayout.IntField("Layer Index", entry.layerIndex);
                    entry.weight     = Mathf.Max(0f, EditorGUILayout.FloatField("Weight", entry.weight));

                    if (band.blendMode == BlendMode.Noise)
                    {
                        entry.noiseScale   = EditorGUILayout.Slider("Noise Scale",  entry.noiseScale,   0.1f, 500f);
                        entry.noiseOffsetX = EditorGUILayout.FloatField("Offset X", entry.noiseOffsetX);
                        entry.noiseOffsetY = EditorGUILayout.FloatField("Offset Y", entry.noiseOffsetY);
                        if (GUILayout.Button("Randomizar Offset"))
                        {
                            entry.noiseOffsetX = Random.Range(0f, 9999f);
                            entry.noiseOffsetY = Random.Range(0f, 9999f);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (removeLayer >= 0) band.layers.RemoveAt(removeLayer);
                if (GUILayout.Button("+ Add Layer"))
                    band.layers.Add(new LayerEntry
                    {
                        layerIndex   = 0,
                        weight       = 1f,
                        noiseOffsetX = Random.Range(0f, 9999f),
                        noiseOffsetY = Random.Range(0f, 9999f)
                    });

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (removeIndex >= 0) bands.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Height Band"))
            bands.Add(new HeightBand { label = "Band " + bands.Count });

        EditorGUILayout.Space();

        List<Terrain> applyTargets = GetTargetTerrains();
        if (applyTargets.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhum terrain selecionado.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (bands.Count == 0)
        {
            EditorGUILayout.HelpBox("Adicione pelo menos uma Height Band.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.HelpBox("Vai aplicar em " + applyTargets.Count + " terrain(s).", MessageType.Info);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Aplicar Height Texture", GUILayout.Height(40)))
            foreach (Terrain t in applyTargets)
                if (t != null) ApplyHeightTexture(t);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private List<Terrain> GetTargetTerrains()
    {
        if (autoFindTerrains)
            return new List<Terrain>(FindObjectsByType<Terrain>(FindObjectsSortMode.None));
        return terrains;
    }

    private void ApplyHeightTexture(Terrain terrain)
    {
        TerrainData data    = terrain.terrainData;
        int alphaW          = data.alphamapWidth;
        int alphaH          = data.alphamapHeight;
        int layerCount      = data.terrainLayers.Length;
        Vector3 terrainSize = data.size;

        float[,,] alphaMaps = data.GetAlphamaps(0, 0, alphaW, alphaH);

        for (int y = 0; y < alphaH; y++)
        {
            for (int x = 0; x < alphaW; x++)
            {
                float normX  = (float)x / alphaW;
                float normY  = (float)y / alphaH;
                float worldH = data.GetInterpolatedHeight(normX, normY);
                float worldX = normX * terrainSize.x;
                float worldZ = normY * terrainSize.z;

                // Encontra a banda ativa para esta altura
                HeightBand activeBand = null;
                foreach (HeightBand band in bands)
                    if (band.enabled && worldH >= band.heightMin && worldH <= band.heightMax)
                    { activeBand = band; break; }

                // Sem banda ativa: não modifica este pixel
                if (activeBand == null || activeBand.layers.Count == 0) continue;

                // Zera apenas as layers envolvidas nesta banda
                foreach (LayerEntry e in activeBand.layers)
                    if (e.layerIndex < layerCount) alphaMaps[y, x, e.layerIndex] = 0f;

                if (activeBand.blendMode == BlendMode.Uniform)
                {
                    float totalW = 0f;
                    foreach (LayerEntry e in activeBand.layers) totalW += e.weight;
                    if (totalW > 0f)
                        foreach (LayerEntry e in activeBand.layers)
                            if (e.layerIndex < layerCount)
                                alphaMaps[y, x, e.layerIndex] = e.weight / totalW;
                }
                else
                {
                    // Noise: territórios separados por softmax
                    float sharpness = Mathf.Lerp(8f, 1f, activeBand.globalSmoothness);

                    float[] expScores = new float[activeBand.layers.Count];
                    float totalExp = 0f;

                    for (int i = 0; i < activeBand.layers.Count; i++)
                    {
                        LayerEntry e = activeBand.layers[i];
                        float nx    = worldX / e.noiseScale + e.noiseOffsetX;
                        float nz    = worldZ / e.noiseScale + e.noiseOffsetY;
                        float noise = Mathf.PerlinNoise(nx, nz);
                        float score = (noise - 0.5f) * 2f * e.weight;
                        expScores[i] = Mathf.Exp(score * sharpness);
                        totalExp += expScores[i];
                    }

                    if (totalExp > 0f)
                        for (int i = 0; i < activeBand.layers.Count; i++)
                        {
                            LayerEntry e = activeBand.layers[i];
                            if (e.layerIndex < layerCount)
                                alphaMaps[y, x, e.layerIndex] = expScores[i] / totalExp;
                        }
                }

                // Normaliza a linha inteira para garantir soma = 1
                float sum = 0f;
                for (int l = 0; l < layerCount; l++) sum += alphaMaps[y, x, l];
                if (sum > 0f)
                    for (int l = 0; l < layerCount; l++) alphaMaps[y, x, l] /= sum;
            }
        }

        Undo.RecordObject(data, "Terrain Height Texture");
        data.SetAlphamaps(0, 0, alphaMaps);
        EditorUtility.SetDirty(terrain);
        Debug.Log("[TerrainHeightTexture] Aplicado em: " + terrain.name);
    }
}