using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerManager : MonoBehaviour
{
    public float speed;
    Vector2 moveInput;
    Vector2 lastdir;
    Rigidbody2D rb;

    [Header("dash setting")]
    public float dashSheepd;
    public float dahsDuearion;
    public float invulTIme;
    public float dashCooldown;

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if(moveInput != Vector2.zero) lastdir = moveInput.normalized;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }
}
