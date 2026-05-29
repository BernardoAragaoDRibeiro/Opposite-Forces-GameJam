using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class TerrainTextureBlender : EditorWindow
{
    private Terrain targetTerrain;
    private int     targetLayerIndex = 0;
    private float   globalSmoothness = 0.5f;
    private Vector2 scrollPos;

    [System.Serializable]
    private class BlendLayer
    {
        public bool   enabled      = true;
        public int    layerIndex   = 2;
        public float  weight       = 1f;
        public float  strength     = 1f;
        public float  noiseScale   = 3f;
        public float  noiseOffsetX = 0f;
        public float  noiseOffsetY = 0f;
        public bool   foldout      = true;
    }

    private List<BlendLayer> blendLayers = new List<BlendLayer>();

    [MenuItem("Tools/Terrain Texture Blender")]
    public static void ShowWindow()
    {
        GetWindow<TerrainTextureBlender>("Terrain Texture Blender");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Terrain Texture Blender", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define uma layer alvo e adicione layers para misturar com ela via Perlin Noise.\n" +
            "Cada layer ocupa seu próprio território — sem sobreposição em gosma.",
            MessageType.Info);
        EditorGUILayout.Space(5);

        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Terreno Alvo", targetTerrain, typeof(Terrain), true);

        if (targetTerrain == null)
        {
            EditorGUILayout.HelpBox("Selecione um Terrain na cena.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        int layerCount = targetTerrain.terrainData.terrainLayers.Length;
        if (layerCount == 0)
        {
            EditorGUILayout.HelpBox("O terreno não tem texturas configuradas.", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }

        string[] layerNames = GetLayerNames();

        EditorGUILayout.Space(10);

        // --- Layer alvo ---
        GUILayout.Label("Layer Alvo", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Esta é a layer base. As outras layers vão se misturar com ela.", MessageType.None);
        targetLayerIndex = EditorGUILayout.Popup("Layer Alvo", targetLayerIndex, layerNames);
        DrawLayerPreview(targetLayerIndex);

        EditorGUILayout.Space(10);

        // --- Suavidade global ---
        GUILayout.Label("Configurações Globais", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Suavidade 0 = bordas duras entre territórios.\n" +
            "Suavidade 1 = transição gradual entre territórios.",
            MessageType.None);
        globalSmoothness = EditorGUILayout.Slider("Suavidade das Bordas", globalSmoothness, 0f, 1f);

        EditorGUILayout.Space(10);

        // --- Blend layers ---
        GUILayout.Label("Layers de Blend", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Cada layer tem seu próprio noise e ocupa regiões distintas da layer alvo.\n" +
            "Weight: quanto território essa layer reivindica (maior = mais área).\n" +
            "Noise Scale: tamanho das manchas (maior = manchas maiores).",
            MessageType.None);

        EditorGUILayout.Space(4);

        int removeIndex = -1;
        for (int i = 0; i < blendLayers.Count; i++)
        {
            BlendLayer layer = blendLayers[i];
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            string layerLabel = (layer.layerIndex >= 0 && layer.layerIndex < layerNames.Length)
                ? layerNames[layer.layerIndex] : $"Layer {i}";
            layer.foldout = EditorGUILayout.Foldout(layer.foldout, layerLabel, true, EditorStyles.boldLabel);
            layer.enabled = EditorGUILayout.Toggle(layer.enabled, GUILayout.Width(20));
            if (GUILayout.Button("✕", GUILayout.Width(24))) { removeIndex = i; }
            EditorGUILayout.EndHorizontal();

            if (layer.foldout)
            {
                EditorGUI.BeginDisabledGroup(!layer.enabled);

                layer.layerIndex = EditorGUILayout.Popup("Textura", layer.layerIndex, layerNames);
                DrawLayerPreview(layer.layerIndex);

                if (layer.layerIndex == targetLayerIndex)
                    EditorGUILayout.HelpBox("Esta layer é igual à layer alvo — será ignorada.", MessageType.Warning);

                EditorGUILayout.Space(4);
                layer.weight       = Mathf.Max(0f, EditorGUILayout.FloatField("Weight", layer.weight));
                layer.strength     = EditorGUILayout.Slider("Força de Aplicação", layer.strength, 0f, 1f);
                layer.noiseScale   = EditorGUILayout.Slider("Noise Scale",  layer.noiseScale,   0.1f, 10f);
                layer.noiseOffsetX = EditorGUILayout.FloatField("Offset X", layer.noiseOffsetX);
                layer.noiseOffsetY = EditorGUILayout.FloatField("Offset Y", layer.noiseOffsetY);

                if (GUILayout.Button("Randomizar Offset"))
                {
                    layer.noiseOffsetX = Random.Range(0f, 9999f);
                    layer.noiseOffsetY = Random.Range(0f, 9999f);
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        if (removeIndex >= 0) blendLayers.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Layer"))
            blendLayers.Add(new BlendLayer
            {
                layerIndex   = Mathf.Clamp(targetLayerIndex + 1, 0, layerCount - 1),
                noiseOffsetX = Random.Range(0f, 9999f),
                noiseOffsetY = Random.Range(0f, 9999f)
            });

        EditorGUILayout.Space(15);

        EditorGUI.BeginDisabledGroup(blendLayers.Count == 0);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Aplicar Blend no Terreno", GUILayout.Height(40)))
            ApplyBlend();
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
        if (GUILayout.Button("Restaurar Backup", GUILayout.Height(30)))
            RestoreBackup();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void DrawLayerPreview(int layerIndex)
    {
        if (targetTerrain == null) return;
        var layers = targetTerrain.terrainData.terrainLayers;
        if (layerIndex >= 0 && layerIndex < layers.Length && layers[layerIndex] != null && layers[layerIndex].diffuseTexture != null)
        {
            var tex = layers[layerIndex].diffuseTexture;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label(AssetPreview.GetAssetPreview(tex) ?? tex, GUILayout.Width(48), GUILayout.Height(48));
            GUILayout.Label(layers[layerIndex].name, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    // -----------------------------------------------------------------------
    // Backup em disco
    // -----------------------------------------------------------------------

    private string GetBackupPath()
    {
        if (targetTerrain == null) return null;
        string dataPath = AssetDatabase.GetAssetPath(targetTerrain.terrainData);
        return dataPath.Replace(".asset", "_blender_backup.bytes");
    }

    private void SaveBackup(float[,,] alphas)
    {
        string path = GetBackupPath();
        if (path == null) return;

        int h = alphas.GetLength(0);
        int w = alphas.GetLength(1);
        int l = alphas.GetLength(2);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(h); bw.Write(w); bw.Write(l);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    for (int i = 0; i < l; i++)
                        bw.Write(alphas[y, x, i]);
            File.WriteAllBytes(path, ms.ToArray());
        }

        Debug.Log($"[TerrainTextureBlender] Backup salvo em: {path}");
    }

    private void RestoreBackup()
    {
        if (targetTerrain == null) { Debug.LogWarning("Nenhum terreno selecionado."); return; }

        string path = GetBackupPath();
        if (path == null || !File.Exists(path))
        {
            EditorUtility.DisplayDialog("Sem Backup",
                "Nenhum backup encontrado para este terreno.\nAplique o blend ao menos uma vez para criar um.", "OK");
            return;
        }

        using (var ms = new MemoryStream(File.ReadAllBytes(path)))
        using (var br = new BinaryReader(ms))
        {
            int h = br.ReadInt32();
            int w = br.ReadInt32();
            int l = br.ReadInt32();
            float[,,] alphas = new float[h, w, l];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    for (int i = 0; i < l; i++)
                        alphas[y, x, i] = br.ReadSingle();

            Undo.RecordObject(targetTerrain.terrainData, "Restore Terrain Backup");
            targetTerrain.terrainData.SetAlphamaps(0, 0, alphas);
            EditorUtility.SetDirty(targetTerrain);
            Debug.Log("[TerrainTextureBlender] Backup restaurado com sucesso!");
        }
    }

    // -----------------------------------------------------------------------
    // Apply
    // -----------------------------------------------------------------------

    private void ApplyBlend()
    {
        if (targetTerrain == null) return;

        TerrainData data = targetTerrain.terrainData;
        int alphaW       = data.alphamapWidth;
        int alphaH       = data.alphamapHeight;
        int layerCount   = data.terrainLayers.Length;
        Vector3 size     = data.size;

        float[,,] alphas = data.GetAlphamaps(0, 0, alphaW, alphaH);

        // Salva backup ANTES de qualquer modificação
        SaveBackup(alphas);

        // Filtra layers válidas e ativas
        var validLayers = new List<BlendLayer>();
        foreach (var layer in blendLayers)
            if (layer.enabled && layer.layerIndex != targetLayerIndex && layer.layerIndex < layerCount)
                validLayers.Add(layer);

        if (validLayers.Count == 0)
        {
            Debug.LogWarning("[TerrainTextureBlender] Nenhuma layer de blend válida.");
            return;
        }

        // sharpness: controla dureza das bordas entre territórios
        // smoothness=0 -> sharpness alto -> bordas duras
        // smoothness=1 -> sharpness baixo -> bordas suaves
        float sharpness = Mathf.Lerp(8f, 1f, globalSmoothness);

        for (int y = 0; y < alphaH; y++)
        {
            for (int x = 0; x < alphaW; x++)
            {
                float targetWeight = alphas[y, x, targetLayerIndex];
                if (targetWeight <= 0.001f) continue;

                float normX = (float)x / alphaW;
                float normY = (float)y / alphaH;
                float worldX = normX * size.x;
                float worldZ = normY * size.z;

                // Calcula score via softmax para cada layer
                // Layer alvo tem score fixo 0 → exp(0*sharpness) = 1
                float expTarget = 1f;
                float totalExp  = expTarget;

                float[] expScores = new float[validLayers.Count];
                for (int i = 0; i < validLayers.Count; i++)
                {
                    BlendLayer layer = validLayers[i];
                    float nx    = worldX / layer.noiseScale + layer.noiseOffsetX;
                    float nz    = worldZ / layer.noiseScale + layer.noiseOffsetY;
                    float noise = Mathf.PerlinNoise(nx, nz); // 0..1
                    // Remapeia para [-1..1] e escala pelo weight
                    float score  = (noise - 0.5f) * 2f * layer.weight;
                    expScores[i] = Mathf.Exp(score * sharpness);
                    totalExp    += expScores[i];
                }

                // Redistribui o peso da layer alvo proporcionalmente aos scores
                // Cada layer secundária só recebe até (score/total * targetWeight * strength)
                // O restante fica na layer alvo
                alphas[y, x, targetLayerIndex] = targetWeight; // começa com tudo na alvo
                for (int i = 0; i < validLayers.Count; i++)
                {
                    float share = (expScores[i] / totalExp) * targetWeight * validLayers[i].strength;
                    alphas[y, x, validLayers[i].layerIndex] += share;
                    alphas[y, x, targetLayerIndex]          -= share;
                }

                // Normaliza linha para soma = 1
                float sum = 0f;
                for (int l = 0; l < layerCount; l++) sum += alphas[y, x, l];
                if (sum > 0f)
                    for (int l = 0; l < layerCount; l++) alphas[y, x, l] /= sum;
            }
        }

        Undo.RecordObject(data, "Terrain Texture Blend");
        data.SetAlphamaps(0, 0, alphas);
        EditorUtility.SetDirty(targetTerrain);
        Debug.Log("[TerrainTextureBlender] Blend aplicado com sucesso!");
    }

    private string[] GetLayerNames()
    {
        var layers = targetTerrain.terrainData.terrainLayers;
        string[] names = new string[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            names[i] = $"{i}: {(layers[i] != null ? layers[i].name : "null")}";
        return names;
    }
}