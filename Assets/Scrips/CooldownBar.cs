using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One script for BOTH cooldown bars. Pick Dash or Attack in the Inspector.
/// Fills while the ability recharges, flashes white and pops the moment it is ready,
/// and dips a little when you use it.
/// Needs DashReady01 (PlayerManager) and AttackReady01 (PlayerAttack), see setup steps.
/// </summary>
public class CooldownBar : MonoBehaviour
{
    public enum Source { Dash, Attack }
    public Source source = Source.Dash;

    [Header("Layers")]
    public RectTransform fill;          // the stretched Fill Image
    public TMP_Text label;              // optional "DASH" / "ATK" text

    [Header("Colors")]
    public Color readyColor = new Color(0.10f, 1.00f, 1.00f, 1f);
    public Color chargingColor = new Color(0.10f, 1.00f, 1.00f, 0.30f);

    [Header("Juice")]
    public float readyPunch = 2.5f;     // pop when it becomes ready
    public float usedDip = 1.2f;        // squish when you use it
    public float springStiffness = 260f;
    public float springDamping = 11f;

    RectTransform root;
    Image fillImage;
    PlayerManager dash;
    float flash, springX, springVel;
    bool wasReady = true;
    Color labelBase = Color.white;
    [Range(0f, 1f)] public float labelDimAlpha = 0.45f;   // how faded the label is while charging
    void Awake()
    {
        root = (RectTransform)transform;
        if (fill) fillImage = fill.GetComponent<Image>();
        if (label) labelBase = label.color;
    }

    void Start()
    {
        dash = FindFirstObjectByType<PlayerManager>();
    }

    float ReadProgress()
    {
        if (source == Source.Dash)
            return dash ? dash.DashReady01 : 1f;
        return PlayerAttack.Instance ? PlayerAttack.Instance.AttackReady01 : 1f;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        float p = ReadProgress();
        bool ready = p >= 0.999f;

        if (ready && !wasReady) { flash = 1f; springVel += readyPunch; }
        else if (!ready && wasReady) { springVel -= usedDip; }
        wasReady = ready;

        // spring + flash decay
        springVel += (-springStiffness * springX - springDamping * springVel) * dt;
        springX += springVel * dt;
        flash = Mathf.MoveTowards(flash, 0f, dt * 6f);

        // fill amount
        if (fill)
        {
            var m = fill.anchorMax; m.x = p; fill.anchorMax = m;
        }

        Color c = Color.Lerp(chargingColor, readyColor, p * p);
        c = Color.Lerp(c, Color.white, flash);
        if (fillImage) fillImage.color = c;
        if (label)
        {
            Color lc = labelBase;   // your color, untouched
            lc.a = labelBase.a * Mathf.Lerp(labelDimAlpha, 1f, ready ? 1f : p * 0.5f);
            label.color = lc;
        }

        root.localScale = Vector3.one * (1f + springX);
    }
}
