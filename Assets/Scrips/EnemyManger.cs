using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyManager : MonoBehaviour
{
    public int health = 3;
    public float moveSpeed = 3f;

    [Header("Targeting")]
    public float offsetRadius = 0.8f;
    public float repickMin = 2f;
    public float repickMax = 4f;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactCooldown = 0.5f;

    Rigidbody2D rb;
    Transform player;                 // fallback if no targets
    Transform[] targets;
    Transform target;
    Vector2 offset;
    float nextHitTime;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Init(Transform playerT, Transform[] targetPoints)
    {
        player = playerT;
        targets = targetPoints;
        offset = Random.insideUnitCircle * offsetRadius;
        StartCoroutine(RepickRoutine());
    }

    IEnumerator RepickRoutine()
    {
        while (true)
        {
            if (targets != null && targets.Length > 0)
                target = targets[Random.Range(0, targets.Length)];
            else
                target = player;

            yield return new WaitForSeconds(Random.Range(repickMin, repickMax));
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 goal = (Vector2)target.position + offset;
        rb.MovePosition(Vector2.MoveTowards(rb.position, goal, moveSpeed * Time.fixedDeltaTime));
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (Time.time < nextHitTime) return;
        if (col.collider.TryGetComponent<playerhealth>(out var ph))
        {
            ph.TakeDamage(contactDamage);
            nextHitTime = Time.time + contactCooldown;
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Destroy(gameObject);
    }
}