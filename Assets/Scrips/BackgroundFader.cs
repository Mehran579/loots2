using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundFader : MonoBehaviour
{
    [Header("Settings")]
    public string treeObjectName = "TREE";
    public float minFadeDelay = 1f;
    public float maxFadeDelay = 10f;
    public float fadeDuration = 2f;

    [Header("References")]
    public WaveSpawner spawner;
    public GameObject[] treeParents; // Input the 3 tree parent objects here

    private List<SpriteRenderer[]> treeLayers = new List<SpriteRenderer[]>();
    private int totalEnemiesInGame = 0;
    private int enemiesKilled = 0;
    private int totalBranchLayers = 0;

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<WaveSpawner>();

        // 1. Count total enemies across all waves
        if (spawner != null && spawner.waves != null)
        {
            foreach (var wave in spawner.waves)
            {
                foreach (var group in wave.groups)
                {
                    totalEnemiesInGame += group.count;
                }
            }
        }

        // 2. Map layers from the provided tree parents
        if (treeParents != null && treeParents.Length > 0)
        {
            foreach (var tree in treeParents)
            {
                if (tree == null) continue;

                Transform treeTransform = tree.transform;
                int childCount = treeTransform.childCount;
                SpriteRenderer[] layers = new SpriteRenderer[childCount];

                for (int i = 0; i < childCount; i++)
                {
                    layers[i] = treeTransform.GetChild(i).GetComponentInChildren<SpriteRenderer>();
                }
                treeLayers.Add(layers);
            }
        }

        // Total number of unique layer indices across the trees
        if (treeLayers.Count > 0)
        {
            totalBranchLayers = treeLayers[0].Length;
        }

        Debug.Log($"BackgroundFader: Total Enemies: {totalEnemiesInGame}, Total Layers: {totalBranchLayers}");
    }

    IEnumerator FadeEntireTree(GameObject tree)
    {
        // Get all sprite renderers in the tree hierarchy
        SpriteRenderer[] allSprites = tree.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSprites)
        {
            StartCoroutine(FadeSprite(sr));
        }
        yield return null;
    }

    void Update()
    {
        // Start fading after 45 seconds, complete by 4 minutes (240s)
        float startTime = 45f;
        float endTime = 240f;
        float currentTime = Time.timeSinceLevelLoad;

        if (currentTime < startTime) return;

        float fadeWindow = endTime - startTime;
        float progress = (currentTime - startTime) / fadeWindow;

        // Calculate how many layers should have started fading by now
        int layersToFadeByNow = Mathf.FloorToInt(progress * totalBranchLayers);

        // Trigger the fade for the specific layer that just crossed the threshold
        if (layersToFadeByNow >= 0 && layersToFadeByNow < totalBranchLayers && layersToFadeByNow == enemiesKilled)
        {
            // We reuse enemiesKilled as a counter for which layer index we are on
            OnEnemyKilled();
        }
    }

    // Public method to be called when an enemy dies (kept for backward compatibility)
    public void OnEnemyKilled()
    {
        int layerIndexToFade = enemiesKilled;
        enemiesKilled++;

        if (totalBranchLayers <= 0) return;

        layerIndexToFade = Mathf.Clamp(layerIndexToFade, 0, totalBranchLayers - 1);

        StartCoroutine(FadeLayerWithDelay(layerIndexToFade));
    }

    IEnumerator FadeLayerWithDelay(int layerIndex)
    {
        float delay = Random.Range(minFadeDelay, maxFadeDelay);
        yield return new WaitForSeconds(delay);

        // Fade this specific layer index across all identified tree objects
        foreach (var layers in treeLayers)
        {
            if (layers != null && layerIndex < layers.Length)
            {
                SpriteRenderer sr = layers[layerIndex];
                if (sr != null)
                {
                    StartCoroutine(FadeSprite(sr));
                }
            }
        }
    }

    IEnumerator FadeSprite(SpriteRenderer sr)
    {
        if (sr == null) yield break;

        Material instMaterial = new Material(sr.material);
        sr.material = instMaterial;

        // Identify all potential color/emission properties
        string colorProp = instMaterial.HasProperty("_Color") ? "_Color" :
                           instMaterial.HasProperty("_BaseColor") ? "_BaseColor" : null;

        string emissionProp = instMaterial.HasProperty("_EmissionColor") ? "_EmissionColor" :
                              instMaterial.HasProperty("_Emission") ? "_Emission" : null;

        Color c = sr.color;
        float startA = c.a;
        float t = 0f;

        while (t < fadeDuration && sr != null)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startA, 0f, t / fadeDuration);
            c.a = alpha;

            // 1. Update SpriteRenderer (standard)
            sr.color = c;

            // 2. Update Base Color in Material
            if (colorProp != null)
            {
                instMaterial.SetColor(colorProp, c);
            }

            // 3. Update Emission Color
            // Emission usually ignores alpha, so we multiply the RGB by the alpha to actually "fade" the glow
            if (emissionProp != null)
            {
                Color emissionCol = instMaterial.GetColor(emissionProp);
                Color fadedEmission = new Color(emissionCol.r * alpha, emissionCol.g * alpha, emissionCol.b * alpha, emissionCol.a);
                instMaterial.SetColor(emissionProp, fadedEmission);
            }

            yield return null;
        }

        if (sr != null)
        {
            c.a = 0f;
            sr.color = c;
            if (colorProp != null) instMaterial.SetColor(colorProp, c);
            if (emissionProp != null)
            {
                // Set emission to black/transparent
                instMaterial.SetColor(emissionProp, Color.black);
            }
        }
    }
}