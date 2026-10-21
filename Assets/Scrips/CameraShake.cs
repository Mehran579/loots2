using System.Collections;
using UnityEngine;

// Put this on the Main Camera. Then call from anywhere:
//   CameraShake.Shake(0.3f, 0.25f);   // strength (world units), duration (seconds)
//   CameraShake.HitStop(0.05f);       // freezes the game for that many real seconds
[DefaultExecutionOrder(-10000)]
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    float magnitude, duration, timeLeft;
    Vector3 applied;

    float stopEnd;
    Coroutine stopRoutine;

    void Awake() => instance = this;

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (stopRoutine != null) Time.timeScale = 1f;   // never leave the game frozen after a scene reload
    }

    public static void HitStop(float duration = 0.05f)
    {
        if (!instance || UpgradePicker.IsOpen) return;

        float end = Time.unscaledTime + duration;
        if (end <= instance.stopEnd) return;            // a longer stop is already running
        instance.stopEnd = end;
        if (instance.stopRoutine == null)
            instance.stopRoutine = instance.StartCoroutine(instance.StopRoutine());
    }

    IEnumerator StopRoutine()
    {
        Time.timeScale = 0f;
        while (Time.unscaledTime < stopEnd) yield return null;
        if (!UpgradePicker.IsOpen) Time.timeScale = 1f; // don't unpause the upgrade screen
        stopRoutine = null;
    }

    public static void Shake(float magnitude = 0.2f, float duration = 0.2f)
    {
        if (!instance) return;

        // a weak shake never cuts off a stronger one that's still going
        float current = instance.timeLeft > 0f
            ? instance.magnitude * instance.timeLeft / instance.duration
            : 0f;
        if (magnitude < current) return;

        instance.magnitude = magnitude;
        instance.duration = Mathf.Max(duration, 0.01f);
        instance.timeLeft = duration;
    }

    // undo last frame's shake before anything else moves the camera
    void Update()
    {
        if (applied == Vector3.zero) return;
        transform.position -= applied;
        applied = Vector3.zero;
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f) return;

        timeLeft -= Time.unscaledDeltaTime;        // unscaled, so it still works during hit stop
        float k = Mathf.Clamp01(timeLeft / duration);   // fades out
        Vector2 r = Random.insideUnitCircle * magnitude * k;

        applied = new Vector3(r.x, r.y, 0f);
        transform.position += applied;
    }
}