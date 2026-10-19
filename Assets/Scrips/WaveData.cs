using UnityEngine;

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