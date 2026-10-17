using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Temp : MonoBehaviour
{
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public Sprite sprite;
    public Collider2D colld;
    public WindZone widnzone;
    public LineRenderer lr;
    // public 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("hehe");
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = Vector2.up;
    }
}
