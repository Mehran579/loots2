using UnityEngine;

public class Splitter : MonoBehaviour
{
    public EnemyManager splitPrefab;     // the small version (no Splitter on it)
    public int count = 2;
    [HideInInspector] public WaveSpawner spawner;   // set by WaveSpawner

    EnemyManager em;

    void Awake() => em = GetComponent<EnemyManager>();

    void OnKilled()
    {
        if (!em || !em.Player || !splitPrefab) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
            var s = Instantiate(splitPrefab, pos, Quaternion.identity);
            s.Init(em.Player, null);                 // null targets = chases the player
            if (spawner) spawner.Register(s);
        }
    }
}