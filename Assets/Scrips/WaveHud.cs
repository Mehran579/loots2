using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Wave pips (one per wave), a big center banner when a wave starts, and a reactive enemy counter.
/// Works ALONGSIDE your Hud script: Hud still writes the text, this only animates it.
/// </summary>
public class WaveHud : MonoBehaviour
{
    public WaveSpawner spawner;

    [Header("Wave pips")]
    public RectTransform pipContainer;               // an empty object, pips are created inside it
    public Vector2 pipSize = new Vector2(22f, 8f);
    public float pipGap = 6f;
    public Color doneColor = new Color(0.10f, 1.00f, 1.00f, 1f);
    public Color currentColor = Color.white;
    public Color futureColor = new Color(0.10f, 1.00f, 1.00f, 0.15f);

    [Header("Wave banner")]
    public TMP_Text banner;                          // big text in the center of the screen
    public float bannerHold = 0.9f;

    [Header("Enemy counter")]
    public TMP_Text enemyText;                       // the same text your Hud writes to
    public Color killColor = new Color(0.10f, 1.00f, 1.00f, 1f);
    public Color clearColor = new Color(0.30f, 1.00f, 0.50f, 1f);

    struct Spring
    {
        public float x, v;
        public void Step(float dt, float k, float d) { v += (-k * x - d * v) * dt; x += v * dt; }
    }

    Image[] pips;
    Spring pipSpring, enemySpring;
    int lastWave = -1, lastAlive;
    float kill, clear;
    Color enemyBase = Color.white, bannerBase = Color.white;
    Coroutine bannerRoutine;

    void Awake()
    {
        if (banner)
        {
            bannerBase = banner.color; bannerBase.a = 1f;
            banner.raycastTarget = false;
            var c = bannerBase; c.a = 0f; banner.color = c;
        }
    }

    void Start()
    {
        if (!spawner) spawner = FindFirstObjectByType<WaveSpawner>();
        if (!spawner) { Debug.LogWarning("WaveHud: no WaveSpawner found."); enabled = false; return; }
        if (enemyText) enemyBase = enemyText.color;
        BuildPips();
    }

    void BuildPips()
    {
        if (!pipContainer) return;
        int n = spawner.waves.Length;
        pips = new Image[n];
        pipContainer.sizeDelta = new Vector2(n * (pipSize.x + pipGap) - pipGap, pipSize.y);

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Pip" + (i + 1), typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pipContainer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = pipSize;
            rt.anchoredPosition = new Vector2(i * (pipSize.x + pipGap), 0f);
            pips[i] = go.GetComponent<Image>();
            pips[i].raycastTarget = false;
            pips[i].color = futureColor;
        }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

        // ---- wave change ----
        int wave = spawner.Wave;
        if (wave != lastWave)
        {
            lastWave = wave;
            if (wave > 0)
            {
                pipSpring.v += 3f;
                if (banner)
                {
                    string msg = wave == spawner.waves.Length ? "FINAL WAVE" : "WAVE " + wave;
                    if (bannerRoutine != null) StopCoroutine(bannerRoutine);
                    bannerRoutine = StartCoroutine(ShowBanner(msg));
                }
            }
        }

        // ---- pips ----
        if (pips != null)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.5f + 0.5f;
            for (int i = 0; i < pips.Length; i++)
            {
                int n = i + 1;
                pips[i].color = n < wave ? doneColor
                              : n == wave ? Color.Lerp(doneColor, currentColor, pulse)
                              : futureColor;
            }
            pipSpring.Step(dt, 260f, 11f);
            pipContainer.localScale = Vector3.one * (1f + pipSpring.x);
        }

        // ---- enemy counter ----
        if (enemyText)
        {
            int alive = spawner.Alive;
            if (alive < lastAlive)
            {
                enemySpring.v += 2.5f;
                kill = 1f;
                if (alive == 0) clear = 1f;
            }
            lastAlive = alive;

            kill = Mathf.MoveTowards(kill, 0f, dt * 5f);
            clear = Mathf.MoveTowards(clear, 0f, dt * 0.8f);
            enemySpring.Step(dt, 260f, 11f);

            Color c = Color.Lerp(enemyBase, killColor, kill);
            c = Color.Lerp(c, clearColor, clear);
            enemyText.color = c;
            enemyText.rectTransform.localScale = Vector3.one * (1f + enemySpring.x);
        }
    }

    IEnumerator ShowBanner(string msg)
    {
        banner.text = msg;
        var rt = banner.rectTransform;

        // slam in
        float t = 0f, inTime = 0.25f;
        while (t < inTime)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / inTime), 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, e);
            var c = bannerBase; c.a = e; banner.color = c;
            yield return null;
        }
        rt.localScale = Vector3.one;
        banner.color = bannerBase;

        // hold
        float h = 0f;
        while (h < bannerHold) { h += Time.unscaledDeltaTime; yield return null; }

        // fade out
        float f = 0f, outTime = 0.4f;
        while (f < outTime)
        {
            f += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(f / outTime);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, p);
            var c = bannerBase; c.a = 1f - p; banner.color = c;
            yield return null;
        }
        var end = bannerBase; end.a = 0f; banner.color = end;
        rt.localScale = Vector3.one;
        bannerRoutine = null;
    }
}
