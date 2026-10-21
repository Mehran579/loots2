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
            new Upgrade { name = "LONG TRAILS\n+1.5s slash, +1s dash", apply = () => { attack.slashLifetime += 1.5f; attack.dashLifetime += 1f; } },
            new Upgrade { name = "WIDE SLASH\nbigger arc",              apply = () => { attack.arcLength += 1.5f; } },
            new Upgrade { name = "HOT CIRCUIT\n+0.5 loop damage",       apply = () => { attack.loopTickDamage += 0.5f; } },
            new Upgrade { name = "LONG CIRCUIT\nloops last +1s",        apply = () => { attack.loopDuration += 1f; } },
            new Upgrade { name = "RAPID PULSE\nloop ticks faster",      apply = () => { attack.loopTickRate = Mathf.Max(0.08f, attack.loopTickRate * 0.7f); } },
            new Upgrade { name = "QUICK SLASH\nfaster attack",          apply = () => { attack.attackCooldown = Mathf.Max(0.12f, attack.attackCooldown * 0.75f); } },
            new Upgrade { name = "FAST DASH\ncooldown -30%",            apply = () => { mover.dashCooldown *= 0.7f; } },
            new Upgrade { name = "SPEED\n+15% move speed",              apply = () => { mover.speed *= 1.15f; } },
            new Upgrade { name = "REPAIR\nrestore 50% of lost health",  apply = () => { health.health += (maxHealth - health.health) * 0.5f; } },
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