using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerManager : MonoBehaviour
{
    public static bool IsDashing { get; private set; }   // PlayerAttack reads this

    public float speed;
    [Range(0f, 1f)] public float attackMoveMultiplier = 0.3f;

    [Header("dash setting")]
    public float dashSheepd;
    public float dahsDuearion;
    public float invulTIme;
    public float dashCooldown;
    public LayerMask dashPassLayers;      // layers you can pass through (dash AND after a hit)

    [Header("Dash Juice")]
    public Transform visual;              // child with the player sprite (NOT the root, so the collider isn't scaled)
    public float stretch = 1.5f;          // size along the dash direction
    public float squash = 0.7f;           // size across the dash direction
    public float stretchInTime = 0.04f;   // how fast it snaps into the stretch
    public float recoverTime = 0.3f;      // spring back, with a little wobble
    public float wobbleSpeed = 9f;

    [Header("Walk Spin")]
    public Transform walkVisual;          // the "Sprite" child under Visual/Spin
    public float walkSpinSpeed = 60f;     // degrees per second while moving (negative = other direction)

    [Header("Hit Reaction")]
    public float hitSpinSpeed = 1440f;    // degrees per second at the start, slows to 0 by the end of the i-frames

    Vector2 moveInput;
    Vector2 lastdir = Vector2.down;
    Vector2 dashDir;
    float nextDashTime;
    float hitUntil;
    float hitDuration;
    Rigidbody2D rb;
    playerhealth health;
    Coroutine visualRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<playerhealth>();
    }

    void OnDisable()
    {
        IsDashing = false;
        rb.excludeLayers = 0;
        if (visual) visual.localScale = Vector3.one;
    }

    // called by playerhealth when the player takes a hit
    public void HitReaction(float duration)
    {
        if (duration <= 0f) return;
        hitDuration = duration;
        hitUntil = Time.time + duration;
    }

    void Update()
    {
        if (!walkVisual) return;

        if (Time.time < hitUntil)
        {
            float k = (hitUntil - Time.time) / hitDuration;   // 1 -> 0, so the spin eases to a stop
            walkVisual.Rotate(0f, 0f, -hitSpinSpeed * k * Time.deltaTime);
            return;
        }

        if (IsDashing) return;
        if (moveInput != Vector2.zero)
            walkVisual.Rotate(0f, 0f, -walkSpinSpeed * moveInput.magnitude * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput != Vector2.zero) lastdir = moveInput.normalized;
    }

    public void OnDash(InputAction.CallbackContext ctx) => TryDash(ctx, false);        // Shift
    public void OnDashToMouse(InputAction.CallbackContext ctx) => TryDash(ctx, true);  // Ctrl

    void TryDash(InputAction.CallbackContext ctx, bool toMouse)
    {
        if (!ctx.performed || IsDashing || Time.time < nextDashTime) return;
        StartCoroutine(DashRoutine(toMouse));
    }

    IEnumerator DashRoutine(bool toMouse)
    {
        PlayerAttack.CancelAttack();
        nextDashTime = Time.time + dashCooldown;

        if (toMouse)
            dashDir = Aim.DirFrom(rb.position, lastdir);        // always toward the mouse
        else
            dashDir = moveInput != Vector2.zero ? moveInput.normalized : lastdir;

        Vector2 start = rb.position;

        IsDashing = true;
        if (health) health.GrantInvuln(invulTIme);

        if (visual)
        {
            if (visualRoutine != null) StopCoroutine(visualRoutine);
            visualRoutine = StartCoroutine(DashVisual(dashDir));
        }

        yield return new WaitForSeconds(dahsDuearion);

        IsDashing = false;

        if (PlayerAttack.Instance)
            PlayerAttack.Instance.AddDashMark(start, rb.position);
    }

    // ================= DASH JUICE =================
    // stretch along the dash direction, squash across it, then spring back with a wobble
    Vector3 DashScale(Vector2 dir)
    {
        float dx = Mathf.Abs(dir.x), dy = Mathf.Abs(dir.y);
        float sx = 1f + (stretch - 1f) * dx + (squash - 1f) * dy;
        float sy = 1f + (stretch - 1f) * dy + (squash - 1f) * dx;
        return new Vector3(sx, sy, 1f);
    }

    IEnumerator DashVisual(Vector2 dir)
    {
        Vector3 target = DashScale(dir);

        // 1. snap into the stretch, and hold it while dashing
        float t = 0f;
        while (IsDashing)
        {
            t += Time.deltaTime;
            float p = stretchInTime > 0f ? Mathf.Clamp01(t / stretchInTime) : 1f;
            visual.localScale = Vector3.Lerp(Vector3.one, target, p);
            yield return null;
        }

        // 2. spring back: damped wobble around 1 (overshoots into the opposite squash once)
        Vector3 offset = target - Vector3.one;
        t = 0f;
        while (t < recoverTime)
        {
            t += Time.deltaTime;
            float p = t / recoverTime;
            float f = Mathf.Exp(-4f * p) * Mathf.Cos(p * wobbleSpeed);
            visual.localScale = Vector3.one + offset * f;
            yield return null;
        }
        visual.localScale = Vector3.one;
        visualRoutine = null;
    }

    // the ONLY place that writes velocity and excluded layers
    void FixedUpdate()
    {
        rb.excludeLayers = (IsDashing || Time.time < hitUntil) ? dashPassLayers : 0;

        if (IsDashing)
            rb.linearVelocity = dashDir * dashSheepd;
        else if (PlayerAttack.IsAttacking)
            rb.linearVelocity = moveInput * speed * attackMoveMultiplier;
        else
            rb.linearVelocity = moveInput * speed;
    }
}