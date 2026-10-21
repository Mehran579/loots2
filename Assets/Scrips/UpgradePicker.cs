using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows 3 random upgrade buttons between waves, pauses the game until one is picked.
public class UpgradePicker : MonoBehaviour
{
    public GameObject panel;             // the upgrade panel (starts hidden)
    public Button[] buttons;             // 3 buttons
    public TMP_Text[] labels;            // the text on each button (same order)

    public PlayerAttack attack;
    public PlayerManager mover;
    public playerhealth health;

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

    void Awake()
    {
        panel.SetActive(false);
        maxHealth = health.health;                  // starting health counts as the max

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

            labels[i].text = u.name;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => { u.apply(); picked = true; });
        }

        panel.SetActive(true);
        IsOpen = true;
        Time.timeScale = 0f;
        yield return new WaitUntil(() => picked);   // WaitUntil ignores timescale
        IsOpen = false;
        Time.timeScale = 1f;
        panel.SetActive(false);
    }
}