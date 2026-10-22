using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on each of the 3 upgrade BUTTONS. UpgradePicker drives it.
/// Neon frame (made automatically), slide-in, idle bob, hover grow + tilt, pick flash.
/// Uses unscaled time, so it animates while the game is paused (timeScale 0).
/// </summary>
[RequireComponent(typeof(Button))]
public class UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Colors")]
    public Color bodyColor = new Color(0.03f, 0.04f, 0.07f, 0.95f);
    public Color bodyHoverColor = new Color(0.10f, 0.03f, 0.06f, 0.98f);
    public Color frameIdle = new Color(0.10f, 1.00f, 1.00f, 0.55f);
    public Color frameHover = new Color(1.00f, 0.20f, 0.45f, 1.00f);

    [Header("Frame")]
    public float frameThickness = 2f;
    public float frameThicknessHover = 4f;

    [Header("Motion")]
    public float slideDistance = 260f;
    public float hoverScale = 0.08f;
    public float tiltDegrees = 5f;
    public float bobAmount = 4f;
    [Tooltip("Seconds after appearing before the card can be clicked (stops accidental picks)")]
    public float clickDelay = 0.35f;

    RectTransform rt;
    Image body;
    CanvasGroup group;
    Canvas canvas;
    Button button;
    readonly Image[] frame = new Image[4];   // top, bottom, left, right

    Vector2 basePos;
    float phase;
    float startTime = -1f;
    float slide = 1f, slideVel;
    float punch, punchVel;
    float hover, flash, tilt, dismiss, chosenT;
    bool hovered, chosen, dismissed, playing;

    void Awake()
    {
        rt = (RectTransform)transform;
        body = GetComponent<Image>();
        button = GetComponent<Button>();
        canvas = GetComponentInParent<Canvas>();
        group = GetComponent<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();

        basePos = rt.anchoredPosition;
        button.transition = Selectable.Transition.None;   // we do our own hover look

        MakeEdge(0, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f));   // top
        MakeEdge(1, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f));   // bottom
        MakeEdge(2, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f));   // left
        MakeEdge(3, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f));   // right
    }

    void MakeEdge(int i, Vector2 aMin, Vector2 aMax, Vector2 pivot)
    {
        var go = new GameObject("Frame" + i, typeof(RectTransform), typeof(Image));
        var r = (RectTransform)go.transform;
        r.SetParent(rt, false);
        r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot;
        r.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        frame[i] = img;
    }

    // ---- called by UpgradePicker ----
    public void Play(float delay)
    {
        playing = true;
        startTime = Time.unscaledTime + delay;
        slide = 1f; slideVel = 0f;
        punch = 0f; punchVel = 0f;
        hover = 0f; flash = 0f; tilt = 0f; dismiss = 0f; chosenT = 0f;
        hovered = chosen = dismissed = false;
        phase = transform.GetSiblingIndex() * 1.7f;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    public void Chosen()    { chosen = true; punchVel += 7f; flash = 1f; }
    public void Dismiss()   { dismissed = true; }

    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e)  { hovered = false; }

    void Update()
    {
        if (!playing) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

        if (Time.unscaledTime < startTime) { group.alpha = 0f; return; }

        // springs
        slideVel += (-170f * slide - 13f * slideVel) * dt;
        slide += slideVel * dt;
        punchVel += (-260f * punch - 11f * punchVel) * dt;
        punch += punchVel * dt;
        flash = Mathf.MoveTowards(flash, 0f, dt * 3f);

        // fade in / pick fade / dismiss fade
        float pickK = 0f;
        if (chosen)
        {
            chosenT += dt;
            pickK = Mathf.InverseLerp(0.25f, 0.55f, chosenT);
            group.alpha = 1f - pickK;
        }
        else if (dismissed)
        {
            dismiss = Mathf.MoveTowards(dismiss, 1f, dt * 4f);
            group.alpha = 1f - dismiss;
        }
        else
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, dt * 6f);
        }

        bool clickable = !chosen && !dismissed && Time.unscaledTime >= startTime + clickDelay;
        group.blocksRaycasts = clickable;
        group.interactable = clickable;

        // hover
        bool wantHover = hovered && clickable;
        hover = Mathf.MoveTowards(hover, wantHover ? 1f : 0f, dt * 9f);
        float h = Mathf.SmoothStep(0f, 1f, hover);

        // tilt toward the mouse
        float tiltTarget = 0f;
        if (wantHover && Mouse.current != null)
        {
            Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Mouse.current.position.ReadValue(), cam, out Vector2 lp))
                tiltTarget = -Mathf.Clamp(lp.x / (rt.rect.width * 0.5f), -1f, 1f) * tiltDegrees;
        }
        tilt = Mathf.Lerp(tilt, tiltTarget, 1f - Mathf.Exp(-12f * dt));

        // idle bob
        float bob = (!chosen && !dismissed) ? Mathf.Sin(Time.unscaledTime * 1.6f + phase) * bobAmount * (1f - h) : 0f;

        // apply transform
        rt.anchoredPosition = basePos + new Vector2(0f, -slide * slideDistance + bob - dismiss * 40f);
        float s = (1f + hoverScale * h + punch + pickK * 0.25f) * (1f - 0.1f * dismiss);
        rt.localScale = Vector3.one * s;
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // apply colors
        if (body) body.color = Color.Lerp(bodyColor, bodyHoverColor, h);
        Color fc = Color.Lerp(frameIdle, frameHover, h);
        fc = Color.Lerp(fc, Color.white, flash);
        float t = Mathf.Lerp(frameThickness, frameThicknessHover, h) + flash * 3f;
        for (int i = 0; i < 4; i++)
        {
            frame[i].color = fc;
            frame[i].rectTransform.sizeDelta = i < 2 ? new Vector2(0f, t) : new Vector2(t, 0f);
        }
    }
}
