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
    public LayerMask dashPassLayers;      // layers you can dash through (leave empty if none)

    Vector2 moveInput;
    Vector2 lastdir = Vector2.down;
    Vector2 dashDir;
    float nextDashTime;
    Rigidbody2D rb;
    playerhealth health;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<playerhealth>();
    }

    void OnDisable()
    {
        IsDashing = false;
        rb.excludeLayers = 0;
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
        rb.excludeLayers = dashPassLayers;

        yield return new WaitForSeconds(dahsDuearion);

        IsDashing = false;
        rb.excludeLayers = 0;

        if (PlayerAttack.Instance)
            PlayerAttack.Instance.AddDashMark(start, rb.position);
    }

    // the ONLY place that writes velocity
    void FixedUpdate()
    {
        if (IsDashing)
            rb.linearVelocity = dashDir * dashSheepd;
        else if (PlayerAttack.IsAttacking)
            rb.linearVelocity = moveInput * speed * attackMoveMultiplier;
        else
            rb.linearVelocity = moveInput * speed;
    }
}