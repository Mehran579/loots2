using UnityEngine;
using UnityEngine.SceneManagement;

public class playerhealth : MonoBehaviour
{
    public int health = 5;

    [Header("Invulnerability")]
    public float invulnDuration = 1f;
    public float blinkInterval = 0.1f;

    //HitFlash flash;
    float invulnUntil;

    public bool IsInvulnerable => Time.time < invulnUntil;

    //void Awake() => flash = GetComponent<HitFlash>();

    public void TakeDamage(int amount)
    {
        if (IsInvulnerable) return;

        health -= amount;

        if (health <= 0)
        {
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            endscreen.SetActive(true);
            gameObject.SetActive(false);
            return;
        }

        invulnUntil = Time.time + invulnDuration;
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