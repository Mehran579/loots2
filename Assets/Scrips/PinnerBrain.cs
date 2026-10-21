using UnityEngine;

// Put on the Pinner prefab (next to EnemyManager) and untick "Chase" on EnemyManager.
[RequireComponent(typeof(EnemyManager))]
public class PinnerBrain : MonoBehaviour
{
    public float speed = 3f;
    public float minDist = 6f, maxDist = 9f;   // how far from the player it stands
    public float shootInterval = 1.2f;
    public int shotsPerSpot = 3;
    public Projectile projectile;              // drag the projectile prefab here

    EnemyManager em;
    Rigidbody2D rb;
    Vector2 spot;
    int shots;
    float nextShot;
    bool moving;
    bool hasSpot;

    void Awake()
    {
        em = GetComponent<EnemyManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    void PickSpot()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        spot = (Vector2)em.Player.position + dir * Random.Range(minDist, maxDist);
        moving = true;
        hasSpot = true;
        shots = 0;
    }

    void FixedUpdate()
    {
        if (!em.Player) return;

        rb.linearVelocity = Vector2.zero;      // cancel knockback from collisions
        rb.angularVelocity = 0f;

        if (em.IsFalling) return;
        if (!hasSpot) PickSpot();

        if (moving)
        {
            Vector2 to = spot - rb.position;
            if (to.sqrMagnitude < 0.1f) { moving = false; nextShot = Time.time + 0.4f; }
            else rb.MovePosition(rb.position + to.normalized * speed * Time.fixedDeltaTime);
        }
        else if (Time.time >= nextShot)
        {
            Vector2 dir = ((Vector2)em.Player.position - rb.position).normalized;
            AudioManager.Play(SFX.EnemyShoot);   // SFX
            Instantiate(projectile, rb.position, Quaternion.identity).Launch(dir);
            nextShot = Time.time + (shootInterval + Random.Range(-1f, 1f));
            if (++shots >= shotsPerSpot) PickSpot();
        }
    }
}