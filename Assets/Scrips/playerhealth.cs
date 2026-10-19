using UnityEngine;
using UnityEngine.SceneManagement;
public interface IDamageable
{
    void TakeDamage(float amount);
}
public class playerhealth : MonoBehaviour, IDamageable
{
    public float health = 5f;

    [Header("Invulnerability")]
    public float invulnDuration = 1f;
    public float blinkInterval = 0.1f;

    //HitFlash flash;
    float invulnUntil;
    PlayerManager manager;

    public bool IsInvulnerable => Time.time < invulnUntil;

    void Awake() => manager = GetComponent<PlayerManager>();

    public void TakeDamage(float amount)
    {
        if (IsInvulnerable) return;

        health -= amount;
        //Debug.Log("got hit: " + health);
        if (health <= 0f)
        {

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            //endscreen.SetActive(true);
            //gameObject.SetActive(false);
            return;
        }

        invulnUntil = Time.time + invulnDuration;
        if (manager) manager.HitReaction(invulnDuration);
        //flash.Blink(invulnDuration, blinkInterval);

        //HitStop.Do(0.05f);
    }
    public GameObject endscreen;
    public void GrantInvuln(float seconds)
    {
        invulnUntil = Mathf.Max(invulnUntil, Time.time + seconds);
    }
    public void OnresterT()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}