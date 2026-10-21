using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SFX
{
    Slash, Dash, LoopFormed, LoopTick,
    EnemyHit, EnemyDeath, EnemyLand, EnemyShoot,
    PlayerHit, PlayerDeath, ProjectileHit,
    UpgradeOpen, UpgradePick, WaveStart, Win
}

[System.Serializable]
public class SoundEntry
{
    public SFX id;
    public AudioClip[] clips;                    // more than one = random pick each play
    [Range(0f, 1f)] public float volume = 1f;
    public float pitchVariation = 0.08f;         // random +/- pitch so repeats don't sound identical
    public float minInterval = 0.04f;            // stops the same sound stacking in one frame
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;              // a child AudioSource, Loop ticked
    public AudioClip defaultMusic;               // plays on start
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    public float fadeTime = 1f;

    [Header("SFX")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public int voices = 12;                      // how many sounds can play at once
    public SoundEntry[] sounds;

    readonly Dictionary<SFX, SoundEntry> map = new Dictionary<SFX, SoundEntry>();
    readonly Dictionary<SFX, float> lastPlayed = new Dictionary<SFX, float>();
    AudioSource[] pool;
    int next;
    Coroutine musicRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }   // survives scene restart
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var s in sounds) map[s.id] = s;

        pool = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }

        if (musicSource) musicSource.loop = true;
    }

    void Start()
    {
        if (defaultMusic) PlayMusic(defaultMusic);
    }

    // ---------------- SFX ----------------
    public static void Play(SFX id)
    {
        if (Instance) Instance.PlayInternal(id);
    }

    void PlayInternal(SFX id)
    {
        if (!map.TryGetValue(id, out SoundEntry s) || s.clips == null || s.clips.Length == 0) return;

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < s.minInterval) return;
        lastPlayed[id] = now;

        AudioSource src = pool[next];
        next = (next + 1) % pool.Length;

        src.clip = s.clips[Random.Range(0, s.clips.Length)];
        src.volume = s.volume * sfxVolume;
        src.pitch = 1f + Random.Range(-s.pitchVariation, s.pitchVariation);
        src.Play();
    }

    // ---------------- MUSIC ----------------
    public void PlayMusic(AudioClip clip)
    {
        if (!musicSource || !clip) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(SwapMusic(clip));
    }

    public void StopMusic()
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(SwapMusic(null));
    }

    IEnumerator SwapMusic(AudioClip clip)
    {
        float t = 0f;
        float startVol = musicSource.volume;
        while (t < fadeTime && musicSource.isPlaying)          // fade out
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        if (!clip) { musicRoutine = null; yield break; }
        musicSource.Play();

        t = 0f;
        while (t < fadeTime)                                    // fade in
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = musicVolume;
        musicRoutine = null;
    }
}