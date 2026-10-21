using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Neon "juicy" health bar. It POLLS playerhealth.health, so you do NOT need to
/// change playerhealth or Hud. Works with Time.timeScale = 0 hitstop (uses unscaled time).
/// Features: instant front drop, ghost trail, heal-lead, white flash, shake, spring punch,
/// color by health, low-HP pulse, shine sweep, segment ticks, glow, red vignette, HP text punch.
/// </summary>
[DisallowMultipleComponent]
public class JuicyHealthBar : MonoBehaviour
{
    [Header("Source")]
    public playerhealth player;

    [Header("Layers")]
    [Tooltip("Object that shakes / punches. Usually this same object.")]
    public RectTransform root;
    public RectTransform fill;      // Image + RectMask2D
    public RectTransform ghost;     // Image
    public RectTransform shine;     // Image, child of Fill
    public Image glow;              // optional, soft glow behind bar
    public Image vignette;          // optional, full-screen stretch Image
    public TMP_Text hpText;         // optional, your existing HP text

    [Header("Colors")]
    public Color colorFull = new Color(0.10f, 1.00f, 1.00f);   // cyan (matches player)
    public Color colorMid  = new Color(1.00f, 0.85f, 0.20f);   // amber
    public Color colorLow  = new Color(1.00f, 0.10f, 0.25f);   // neon red
    public Color ghostDamageColor = new Color(1f, 1f, 1f, 0.85f);
    public Color ghostHealColor   = new Color(0.30f, 1f, 0.50f, 0.90f);

    [Header("Ghost / Fill")]
    public float ghostDelay = 0.45f;
    public float ghostDrainSpeed = 1.2f;   // bar-fractions per second
    public float healSmoothing = 8f;

    [Header("Juice")]
    public float shakeMax = 12f;           // pixels at full trauma
    public float shakeRotation = 3f;       // degrees at full trauma
    public float punchOnDamage = 3.2f;     // spring impulse
    public float punchOnHeal = 1.6f;
    public float springStiffness = 260f;
    public float springDamping = 11f;

    [Header("Low HP")]
    [Range(0f, 1f)] public float lowThreshold = 0.3f;
    public float lowPulseSpeed = 10f;

    [Header("Shine")]
    public float shineInterval = 3f;
    public float shineDuration = 0.6f;

    [Header("Segments")]
    [Tooltip("0 = no segment ticks")]
    public float hpPerSegment = 10f;

    // --- state ---
    float maxHealth, lastHealth;
    float display = 1f, ghostVal = 1f, delayTimer;
    bool healing;
    float flash, trauma, springX, springVel;
    float lowBlend, damagePulse, shineTimer;
    Vector2 basePos;
    Image fillImage, ghostImage;
    Color hpTextBase = Color.white;

    void Awake()
    {
        if (!root) root = (RectTransform)transform;
        if (fill)  fillImage  = fill.GetComponent<Image>();
        if (ghost) ghostImage = ghost.GetComponent<Image>();

        if (glow && !glow.sprite)         glow.sprite = MakeSoftSprite(false);
        if (vignette && !vignette.sprite) vignette.sprite = MakeSoftSprite(true);
        if (glow) glow.raycastTarget = false;
        if (vignette) { vignette.raycastTarget = false; vignette.color = new Color(1, 0.05f, 0.15f, 0); }

        if (shine)
        {
            shine.anchorMin = new Vector2(0, 0);
            shine.anchorMax = new Vector2(0, 1);
            shine.pivot = new Vector2(0.5f, 0.5f);
            shine.sizeDelta = new Vector2(14f, 16f);
            shine.localRotation = Quaternion.Euler(0, 0, 20f);
        }
    }

    void Start()
    {
        if (!player) player = FindFirstObjectByType<playerhealth>();
        if (!player) { Debug.LogWarning("JuicyHealthBar: no playerhealth found."); enabled = false; return; }

        maxHealth = Mathf.Max(1f, player.health);
        lastHealth = player.health;
        basePos = root.anchoredPosition;
        shineTimer = 0f;
        if (hpText) hpTextBase = hpText.color;

        BuildSegments();
    }

    void BuildSegments()
    {
        if (hpPerSegment <= 0f) return;
        int count = Mathf.RoundToInt(maxHealth / hpPerSegment);
        for (int i = 1; i < count; i++)
        {
            var go = new GameObject("Seg" + i, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            float x = i / (float)count;
            rt.anchorMin = new Vector2(x, 0);
            rt.anchorMax = new Vector2(x, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(3f, 0f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.9f);
            img.raycastTarget = false;
        }
    }

    void Update()
    {
        if (!player) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

        float cur = Mathf.Max(0f, player.health);
        if (cur > maxHealth) maxHealth = cur;

        if (cur < lastHealth - 0.001f)      OnDamage((lastHealth - cur) / maxHealth);
        else if (cur > lastHealth + 0.001f) OnHeal();
        lastHealth = cur;

        float target = cur / maxHealth;

        // Front bar: drops instantly, rises smoothly
        if (target < display) display = target;
        else display = Mathf.Lerp(display, target, 1f - Mathf.Exp(-healSmoothing * dt));
        if (Mathf.Abs(display - target) < 0.0005f) display = target;

        // Ghost bar: trails after damage, leads on heal
        if (ghostVal < target) ghostVal = target;
        else if (ghostVal > target)
        {
            healing = false;
            delayTimer -= dt;
            if (delayTimer <= 0f) ghostVal = Mathf.MoveTowards(ghostVal, target, ghostDrainSpeed * dt);
        }

        // Spring (punch scale)
        float accel = -springStiffness * springX - springDamping * springVel;
        springVel += accel * dt;
        springX += springVel * dt;

        // Decays
        flash = Mathf.MoveTowards(flash, 0f, dt * 7f);
        trauma = Mathf.MoveTowards(trauma, 0f, dt * 2.5f);
        damagePulse = Mathf.MoveTowards(damagePulse, 0f, dt * 2.5f);

        // Low HP pulse
        float lowTarget = (display < lowThreshold && cur > 0f) ? 1f : 0f;
        lowBlend = Mathf.MoveTowards(lowBlend, lowTarget, dt * 3f);
        float pulse = (Mathf.Sin(Time.unscaledTime * lowPulseSpeed) * 0.5f + 0.5f) * lowBlend;

        // ---- Apply ----
        SetFill(fill, display);
        SetFill(ghost, ghostVal);

        Color c = display > 0.5f
            ? Color.Lerp(colorMid, colorFull, (display - 0.5f) * 2f)
            : Color.Lerp(colorLow, colorMid, display * 2f);
        c = Color.Lerp(c, Color.white, pulse * 0.5f);
        c = Color.Lerp(c, Color.white, flash);
        if (fillImage) fillImage.color = c;

        if (ghostImage)
            ghostImage.color = Color.Lerp(ghostImage.color, healing ? ghostHealColor : ghostDamageColor, dt * 20f);

        if (glow)
        {
            Color g = c; g.a = Mathf.Clamp01(0.30f + flash * 0.5f + pulse * 0.35f);
            glow.color = g;
        }

        if (vignette)
        {
            var v = vignette.color; v.a = damagePulse * 0.35f + pulse * 0.22f;
            vignette.color = v;
        }

        // Shake + punch on root
        float s = trauma * trauma;
        Vector2 off = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (s * shakeMax);
        root.anchoredPosition = basePos + off;
        root.localRotation = Quaternion.Euler(0, 0, Random.Range(-1f, 1f) * s * shakeRotation);
        root.localScale = Vector3.one * (1f + springX);

        // HP text
        if (hpText)
        {
            hpText.rectTransform.localScale = Vector3.one * (1f + springX * 0.8f);
            Color t = Color.Lerp(hpTextBase, colorLow, Mathf.Max(flash, lowBlend * 0.6f));
            t = Color.Lerp(t, Color.white, pulse * 0.4f);
            hpText.color = t;
        }

        UpdateShine(dt);
    }

    void OnDamage(float fraction)
    {
        delayTimer = ghostDelay;
        healing = false;
        flash = 1f;
        trauma = Mathf.Clamp01(trauma + 0.45f + fraction * 3f);
        springVel += punchOnDamage * (0.7f + Mathf.Clamp01(fraction * 4f));
        damagePulse = 1f;
    }

    void OnHeal()
    {
        healing = true;
        springVel += punchOnHeal;
        shineTimer = shineInterval;   // trigger a shine sweep right away
    }

    void UpdateShine(float dt)
    {
        if (!shine) return;
        shineTimer += dt;
        float p = (shineTimer - shineInterval) / Mathf.Max(0.01f, shineDuration);
        var img = shine.GetComponent<Image>();

        if (p < 0f) { if (img) img.color = new Color(1, 1, 1, 0); return; }
        if (p > 1f) { shineTimer = 0f; return; }

        float w = root.rect.width;
        shine.anchoredPosition = new Vector2(Mathf.Lerp(-30f, w + 30f, p), 0f);
        if (img) img.color = new Color(1, 1, 1, Mathf.Sin(p * Mathf.PI) * 0.55f);
    }

    static void SetFill(RectTransform rt, float v)
    {
        if (!rt) return;
        var m = rt.anchorMax; m.x = Mathf.Clamp01(v); rt.anchorMax = m;
    }

    // Procedural soft sprite: box falloff for the glow, radial edge-heavy for the vignette
    static Sprite MakeSoftSprite(bool vignetteShape)
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f;
                float v = (y + 0.5f) / N * 2f - 1f;
                float a;
                if (vignetteShape)
                {
                    float d = Mathf.Sqrt(u * u + v * v);
                    a = Mathf.Clamp01((d - 0.5f) / 0.9f);
                    a *= a;
                }
                else
                {
                    float d = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                    a = Mathf.Clamp01(1f - d);
                    a *= a;
                }
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }
}
