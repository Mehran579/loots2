using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyManager : MonoBehaviour, IDamageable
{
    public float health = 3f;
    public float moveSpeed = 3f;
    public bool chase = true;             // untick for Pinner (PinnerBrain moves it instead)

    [Header("Targeting")]
    public float offsetRadius = 1.5f;     // bigger = more spread around each point
    public float repickMin = 2f;
    public float repickMax = 4f;

    [Header("Spacing")]
    public float separationRadius = 1.2f; // enemies closer than this push apart
    public float separationStrength = 1.5f;

    [Header("Contact Damage")]
    public float contactDamage = 1f;
    public float contactCooldown = 0.5f;

    [Header("Spawn Fall")]
    public Transform visual;              // child that holds the sprite (this is what "falls")
    public Transform shadow;              // optional child shadow, stays on the ground
    public float fallHeight = 12f;        // how high above the ground it starts (world units)
    public float fallGravity = 35f;       // bigger = faster drop
    public float startScale = 1.4f;       // looks bigger/closer while high up
    public float landSquashTime = 0.12f;
    public float landSquash = 0.7f;       // y-scale at the moment of impact
    public float shadowFadeTime = 0.1f;   // shadow fades out on impact, then is removed (0 = instant)

    [Header("Hit Feedback")]
    public DamagePopup damagePopup;

    [Header("Facing")]
    public float turnSpeed = 720f;       // degrees per second, 0 = snap instantly
    public float facingOffset = -90f;    // -90 if the sprite points up, 0 if it points right, 90 if it points left

    static readonly List<EnemyManager> all = new List<EnemyManager>();

    Rigidbody2D rb;
    Collider2D col;
    HitEffect hitEffect;
    Transform player;                     // fallback if no targets
    Transform[] targets;
    Transform target;
    Vector2 offset;
    float nextHitTime;
    bool falling;
    bool dead;

    // read by Splitter / PinnerBrain
    public Transform Player => player;
    public bool IsFalling => falling;

    void Awake()
    {
        hitEffect = GetComponent<HitEffect>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public void Init(Transform playerT, Transform[] targetPoints)
    {
        player = playerT;
        targets = targetPoints;
        StartCoroutine(FallRoutine());
    }

    // fake height: the real body stays on the ground, only the sprite is lifted up the screen
    IEnumerator FallRoutine()
    {
        falling = true;
        if (col) col.enabled = false;               // can't be hit or hit anyone while in the air

        Vector3 visualRest = visual ? visual.localPosition : Vector3.zero;
        Vector3 shadowScale = shadow ? shadow.localScale : Vector3.one;
        SpriteRenderer shadowSr = shadow ? shadow.GetComponent<SpriteRenderer>() : null;
        Color shadowCol = shadowSr ? shadowSr.color : Color.white;

        float height = fallHeight;
        float speed = 0f;

        while (height > 0f)
        {
            speed += fallGravity * Time.deltaTime;
            height -= speed * Time.deltaTime;
            float h = Mathf.Max(height, 0f);
            float k = 1f - h / fallHeight;          // 0 = high up, 1 = on the ground

            if (visual)
            {
                visual.localPosition = visualRest + Vector3.up * h;
                visual.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, k);
            }
            if (shadow)
            {
                shadow.localScale = shadowScale * Mathf.Lerp(0.3f, 1f, k);   // shadow grows as it gets closer
                if (shadowSr) shadowSr.color = new Color(shadowCol.r, shadowCol.g, shadowCol.b, shadowCol.a * k);
            }
            yield return null;
        }

        // the shadow is only for the fall: remove it on impact
        AudioManager.Play(SFX.EnemyLand);           // SFX
        if (shadow) StartCoroutine(RemoveShadow(shadow, shadowSr));

        // landing squash
        if (visual)
        {
            visual.localPosition = visualRest;
            float t = 0f;
            while (t < landSquashTime)
            {
                t += Time.deltaTime;
                float p = t / landSquashTime;
                float sy = Mathf.Lerp(landSquash, 1f, p);
                float sx = Mathf.Lerp(2f - landSquash, 1f, p);
                visual.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }
            visual.localScale = Vector3.one;
        }

        // now it's a normal enemy
        falling = false;
        if (col) col.enabled = true;
        StartCoroutine(RepickRoutine());
    }

    IEnumerator RemoveShadow(Transform sh, SpriteRenderer sr)
    {
        if (sr && shadowFadeTime > 0f)
        {
            Color c = sr.color;
            float startA = c.a;
            float t = 0f;
            while (t < shadowFadeTime && sr)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(startA, 0f, t / shadowFadeTime);
                sr.color = c;
                yield return null;
            }
        }
        if (sh) Destroy(sh.gameObject);
    }

    IEnumerator RepickRoutine()
    {
        while (true)
        {
            target = (targets != null && targets.Length > 0)
                ? targets[Random.Range(0, targets.Length)]
                : player;

            offset = Random.insideUnitCircle * offsetRadius;   // new spot around the point each time
            yield return new WaitForSeconds(Random.Range(repickMin, repickMax));
        }
    }

    void FixedUpdate()
    {
        if (!chase || falling || target == null) return;

        // 1. toward the goal
        Vector2 goal = (Vector2)target.position + offset;
        Vector2 toGoal = goal - rb.position;
        Vector2 move = toGoal.sqrMagnitude > 0.04f ? toGoal.normalized : Vector2.zero;

        // 2. away from nearby enemies
        Vector2 push = Vector2.zero;
        foreach (var other in all)
        {
            if (other == this || other.falling) continue;
            Vector2 away = rb.position - other.rb.position;
            float d = away.magnitude;
            if (d < separationRadius && d > 0.001f)
                push += away / d * (1f - d / separationRadius);
        }

        Vector2 velocity = move + push * separationStrength;
        velocity = Vector2.ClampMagnitude(velocity, 1f) * moveSpeed;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (falling || Time.time < nextHitTime) return;
        if (collision.collider.TryGetComponent<IDamageable>(out var hit))
        {
            if (hit is EnemyManager) return;         // enemies don't hurt each other
            hit.TakeDamage(contactDamage);
            nextHitTime = Time.time + contactCooldown;
        }
    }

    public void TakeDamage(float amount)
    {
        if (falling || dead) return;
        if (damagePopup) DamagePopup.Spawn(damagePopup, transform.position + Vector3.up * 0.5f, amount);
        health -= amount;
        if (health <= 0f)
        {
            dead = true;                              // loop ticks can hit twice in one frame
            AudioManager.Play(SFX.EnemyDeath);        // SFX
            CameraShake.Shake(0.1f, 0.12f);
            CameraShake.HitStop(0.04f);

            // Notify BackgroundFader if it exists
            var fader = GameObject.FindFirstObjectByType<BackgroundFader>();
            if (fader != null) fader.OnEnemyKilled();

            SendMessage("OnKilled", SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }
        AudioManager.Play(SFX.EnemyHit);              // SFX
        if (hitEffect) hitEffect.TakeHit();
    }

    void Update()
    {
        if (!player || !visual) return;

        Vector2 dir = (Vector2)player.position - rb.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + facingOffset;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

        visual.rotation = turnSpeed > 0f
            ? Quaternion.RotateTowards(visual.rotation, targetRot, turnSpeed * Time.deltaTime)
            : targetRot;
    }
}