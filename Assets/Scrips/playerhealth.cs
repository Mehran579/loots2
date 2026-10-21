using UnityEngine;
using UnityEngine.SceneManagement;
public interface IDamageable
{
    void TakeDamage(float amount);
}
public class playerhealth : MonoBehaviour, IDamageable
{
    public float health = 15f; // Buffed from 5f

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
            AudioManager.Play(SFX.PlayerDeath);   // SFX
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            endscreen.SetActive(true);
            //gameObject.SetActive(false);
            return;
        }

        invulnUntil = Time.time + invulnDuration;
        AudioManager.Play(SFX.PlayerHit);         // SFX
        CameraShake.Shake(0.35f, 0.25f);
        CameraShake.HitStop(0.08f);
        if (manager) manager.HitReaction(invulnDuration);
        //flash.Blink(invulnDuration, blinkInterval);
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