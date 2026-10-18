using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class EnemyGroup
{
    public EnemyManager enemyPrefab;
    public int count = 5;
    public float spawnInterval = 0.5f;   // seconds between each enemy in this group
}

[CreateAssetMenu(menuName = "Waves/Wave")]
public class WaveData : ScriptableObject
{
    public EnemyGroup[] groups;          // spawned one group after another
}

public class WaveSpawner : MonoBehaviour
{
    public WaveData[] waves;
    public Transform player;
    public Transform[] targets;   
    public float minSpawnDistance = 8f;
    public float maxSpawnDistance = 12f;
    public float timeBetweenWaves = 3f;
    public GameObject winScreen;         // optional, shown when all waves are cleared

    readonly List<EnemyManager> alive = new List<EnemyManager>();

    IEnumerator Start()
    {
        foreach (WaveData wave in waves)
        {
            yield return StartCoroutine(SpawnWave(wave));
            yield return new WaitUntil(AllDead);
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