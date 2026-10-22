using UnityEngine;
using System.Collections;

public class Bulwark : MonoBehaviour
{
    [Header("Settings")]
    public float minTimeUntilChase = 5f;
    public float maxTimeUntilChase = 10f;

    private EnemyManager enemyManager;
    private bool isChasingPlayer = false;

    void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
    }

    void Start()
    {
        StartCoroutine(WaitAndChaseRoutine());
    }

    IEnumerator WaitAndChaseRoutine()
    {
        // Wait for the specified random duration
        float waitTime = Random.Range(minTimeUntilChase, maxTimeUntilChase);
        yield return new WaitForSeconds(waitTime);

        if (enemyManager != null)
        {
            enemyManager.stopRepicking = true;
        }
    }
}
