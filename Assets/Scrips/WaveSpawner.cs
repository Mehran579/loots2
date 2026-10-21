using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public WaveData[] waves;
    public Transform player;
    public Transform[] targets;          // the 4 animated points under the player
    public float minSpawnDistance = 8f;
    public float maxSpawnDistance = 12f;
    public float timeBetweenWaves = 3f;
    public GameObject winScreen;         // optional, shown when all waves are cleared

    readonly List<EnemyManager> alive = new List<EnemyManager>();

    // Splitter calls this so the wave doesn't end while the small ones live
    public void Register(EnemyManager e) => alive.Add(e);

    public UpgradePicker picker;         // optional: upgrade screen between waves
    public int Wave { get; private set; }
    public int Alive { get { alive.RemoveAll(e => e == null); return alive.Count; } }

    IEnumerator Start()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            Wave = i + 1;
            yield return StartCoroutine(SpawnWave(waves[i]));
            yield return new WaitUntil(AllDead);
            if (picker && i < waves.Length - 1) yield return picker.Show();   // not after the last wave
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("all waves cleared");
        if (winScreen) winScreen.SetActive(true);
    }

    IEnumerator SpawnWave(WaveData wave)
    {
        foreach (EnemyGroup group in wave.groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                if (!player) yield break;

                Vector2 dir = Random.insideUnitCircle.normalized;
                float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector2 pos = (Vector2)player.position + dir * dist;

                EnemyManager e = Instantiate(group.enemyPrefab, pos, Quaternion.identity);
                e.Init(player, targets);
                alive.Add(e);

                var sp = e.GetComponent<Splitter>();
                if (sp) sp.spawner = this;

                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
    }

    bool AllDead()
    {
        alive.RemoveAll(e => e == null);
        return alive.Count == 0;
    }
}