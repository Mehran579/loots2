using UnityEngine;
using TMPro;

public class Hud : MonoBehaviour
{
    public TMP_Text healthText;
    public TMP_Text waveText;
    public TMP_Text enemyText;

    public playerhealth player;
    public WaveSpawner spawner;

    void Update()
    {
        if (player && healthText) healthText.text = "HP " + Mathf.Max(0, Mathf.CeilToInt(player.health));
        if (spawner && waveText) waveText.text = "WAVE " + spawner.Wave + "/" + spawner.waves.Length;
        if (spawner && enemyText) enemyText.text = "ENEMIES " + spawner.Alive;
    }
}