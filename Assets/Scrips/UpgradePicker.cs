using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Shows 3 random upgrade buttons between waves, pauses the game until one is picked.
// Juiced version: animated cards (UpgradeCard on each button), title slam, dark backdrop,
// keys 1/2/3 to pick, SFX hooks, short delay so the pick animation can play.
public class UpgradePicker : MonoBehaviour
{
    public GameObject panel;             // the upgrade panel (starts hidden)
    public Button[] buttons;             // 3 buttons
    public TMP_Text[] labels;            // the text on each button (same order)

    public PlayerAttack attack;
    public PlayerManager mover;
    public playerhealth health;

    [Header("Juice")]
    public TMP_Text title;               // optional big text, child of the panel
    public string titleText = "CHOOSE YOUR CURSE";
    public Color backdropColor = new Color(0f, 0f, 0.02f, 0.82f);
    public Color descriptionColor = new Color(1f, 0.42f, 0.54f, 1f);
    public float pickDelay = 0.55f;      // how long the pick animation plays before closing
    public float inputDelay = 0.35f;     // keys ignored this long after opening

    [Header("Debuff Values")]
    public float slashDebuff = -1.5f;
    public float dashDebuff = -1f;
    public float arcDebuff = -1.5f;
    public float loopDamageDebuff = -0.5f;
    public float loopDurationDebuff = -1f;
    public float loopTickRateMult = 1.4f; // Inverse of 0.7
    public float attackCooldownMult = 1.33f; // Inverse of 0.75
    public float dashCooldownMult = 1.43f; // Inverse of 0.7
    public float speedMult = 0.87f; // Inverse of 1.15

    class Upgrade { public string name; public System.Action apply; }
    public static bool IsOpen;           // read by CameraShake.HitStop
    List<Upgrade> pool;
    bool picked;
    float maxHealth;
    float openedAt;
    Image backdrop;
    Color titleBase = Color.white;
    UpgradeCard[] cards;

    static readonly Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3 };

    void Awake()
    {
        panel.SetActive(false);
        maxHealth = health.health;                  // starting health counts as the max

        backdrop = panel.GetComponent<Image>();
        if (title) { titleBase = title.color; titleBase.a = 1f; title.raycastTarget = false; }

        pool = new List<Upgrade>
        {
            new Upgrade { name = "SHORT TRAILS\n-1.5s slash, -1s dash", apply = () => { attack.slashLifetime = Mathf.Max(0.5f, attack.slashLifetime + slashDebuff); attack.dashLifetime = Mathf.Max(0.5f, attack.dashLifetime + dashDebuff); } },
            new Upgrade { name = "NARROW SLASH\nsmaller arc",              apply = () => { attack.arcLength = Mathf.Max(0.5f, attack.arcLength + arcDebuff); } },
            new Upgrade { name = "COLD CIRCUIT\n-0.5 loop damage",       apply = () => { attack.loopTickDamage = Mathf.Max(0.1f, attack.loopTickDamage + loopDamageDebuff); } },
            new Upgrade { name = "SHORT CIRCUIT\nloops last -1s",        apply = () => { attack.loopDuration = Mathf.Max(0.5f, attack.loopDuration + loopDurationDebuff); } },
            new Upgrade { name = "SLOW PULSE\nloop ticks slower",      apply = () => { attack.loopTickRate *= loopTickRateMult; } },
            new Upgrade { name = "HEAVY SLASH\nslower attack",          apply = () => { attack.attackCooldown *= attackCooldownMult; } },
            new Upgrade { name = "CLUMSY DASH\ncooldown +43%",            apply = () => { mover.dashCooldown *= dashCooldownMult; } },
            new Upgrade { name = "SLOWNESS\n-13% move speed",              apply = () => { mover.speed = Mathf.Max(2f, mover.speed * speedMult); } },
            new Upgrade { name = "INJURY\nlose 50% of current health",  apply = () => { health.health *= 0.5f; } },
        };
    }

    void Update()
    {
        if (!IsOpen || picked || Time.unscaledTime < openedAt + inputDelay) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        for (int i = 0; i < buttons.Length && i < keys.Length; i++)
            if (kb[keys[i]].wasPressedThisFrame) { buttons[i].onClick.Invoke(); return; }
    }

    // "NAME\ndescription" -> small [1] tag, big name, smaller colored description
    string Format(int index, string raw)
    {
        string name = raw, desc = "";
        int nl = raw.IndexOf('\n');
        if (nl >= 0) { name = raw.Substring(0, nl); desc = raw.Substring(nl + 1); }
        string hex = ColorUtility.ToHtmlStringRGB(descriptionColor);
        return "<size=45%><alpha=#66>[" + (index + 1) + "]</size><alpha=#FF>\n" + name +
               "\n<size=55%><color=#" + hex + ">" + desc + "</color></size>";
    }

    void Choose(int index, Upgrade u)
    {
        if (picked) return;
        u.apply();
        picked = true;
        AudioManager.Play(SFX.UpgradePick);

        for (int i = 0; i < cards.Length; i++)
        {
            if (!cards[i]) continue;
            if (i == index) cards[i].Chosen(); else cards[i].Dismiss();
        }
    }

    // WaveSpawner does: yield return picker.Show();
    public IEnumerator Show()
    {
        picked = false;
        var temp = new List<Upgrade>(pool);

        for (int i = 0; i < buttons.Length; i++)
        {
            int r = Random.Range(0, temp.Count);
            Upgrade u = temp[r];
            temp.RemoveAt(r);                       // no duplicates in one screen

            int idx = i;
            labels[i].text = Format(i, u.name);
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => Choose(idx, u));
        }

        panel.SetActive(true);                      // activate FIRST so the cards can wake up
        IsOpen = true;
        Time.timeScale = 0f;
        openedAt = Time.unscaledTime;

        cards = new UpgradeCard[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            cards[i] = buttons[i].GetComponent<UpgradeCard>();
            if (cards[i]) cards[i].Play(0.15f + 0.12f * i);   // staggered slide-in
        }

        AudioManager.Play(SFX.UpgradeOpen);
        StartCoroutine(Intro());

        yield return new WaitUntil(() => picked);   // WaitUntil ignores timescale
        yield return new WaitForSecondsRealtime(pickDelay);   // let the pick animation play

        IsOpen = false;
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    IEnumerator Intro()
    {
        Color bc = backdropColor;
        if (backdrop) backdrop.color = new Color(bc.r, bc.g, bc.b, 0f);
        if (title)
        {
            title.text = titleText;
            var c0 = titleBase; c0.a = 0f; title.color = c0;
        }

        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
            if (backdrop) backdrop.color = new Color(bc.r, bc.g, bc.b, bc.a * e);
            if (title)
            {
                title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, e);
                var c = titleBase; c.a = e; title.color = c;
            }
            yield return null;
        }
        if (backdrop) backdrop.color = bc;
        if (title) { title.rectTransform.localScale = Vector3.one; title.color = titleBase; }
    }
}