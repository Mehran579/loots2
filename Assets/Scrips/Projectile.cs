using UnityEngine;


public class Projectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 1f;
    public float life = 4f;

    Vector2 dir;

    public void Launch(Vector2 d)
    {
        dir = d;
        transform.up = d;
        Destroy(gameObject, life);
    }

    void Update() => transform.position += (Vector3)(dir * speed * Time.deltaTime);

    void OnTriggerEnter2D(Collider2D c)
    {
        if (c.GetComponentInParent<EnemyManager>()) return;   // ignore enemies
        var d = c.GetComponentInParent<IDamageable>();
        if (d != null)
        {
            d.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}