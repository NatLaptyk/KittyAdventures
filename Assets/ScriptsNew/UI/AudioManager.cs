using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    Dictionary<AudioClip, float> lastPlayedTime = new Dictionary<AudioClip, float>();

    public static AudioManager instance;

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource levelMusicSource;
    public AudioSource bossMusicSource;

    [Header("Settings")]
    public float sfxCooldown = 0.1f;
    public float sfxVolume = 0.1f;
    public float musicFadeTime = 2f;

    [Header("Music")]
    public AudioClip levelMusic;
    public AudioClip bossMusic;

    [Header("Player SFX")]
    public AudioClip jump;
    public AudioClip land;
    public AudioClip lightAttack;
    public AudioClip heavyAttack;
    public AudioClip playerDamaged;
    public AudioClip steps;
    public AudioClip stomps;

    [Header("Environment SFX")]
    public AudioClip treesMoving;
    public AudioClip signRight;
    public AudioClip signWrong;

    [Header("Spider SFX")]
    public AudioClip spiderAttack;
    public AudioClip spiderDed;

    [Header("Wisp SFX")]
    public AudioClip wispAttack;
    public AudioClip wispDed;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        PlayLevelMusic();
    }

    public void PlaySFX(AudioClip clip, float cooldown = 0.1f)
    {
        if (clip == null) return;

        if (lastPlayedTime.TryGetValue(clip, out float lastTime))
        {
            if (Time.time - lastTime < cooldown)
                return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
        lastPlayedTime[clip] = Time.time;
    }

    public void PlayLevelMusic()
    {
        levelMusicSource.clip = levelMusic;
        levelMusicSource.loop = true;
        levelMusicSource.Play();
    }

    public void StartBossMusic()
    {
        StartCoroutine(FadeMusic(levelMusicSource, bossMusicSource, bossMusic));
    }

    public void ReturnToLevelMusic()
    {
        StartCoroutine(FadeMusic(bossMusicSource, levelMusicSource, levelMusic));
    }

    IEnumerator FadeMusic(AudioSource from, AudioSource to, AudioClip newClip)
    {
        if (!to.isPlaying)
        {
            to.clip = newClip;
            to.loop = true;
            to.volume = 0;
            to.Play();
        }

        float t = 0;

        while (t < musicFadeTime)
        {
            t += Time.deltaTime;

            from.volume = Mathf.Lerp(1, 0, t / musicFadeTime);
            to.volume = Mathf.Lerp(0, 1, t / musicFadeTime);

            yield return null;
        }

        from.Stop();
        from.volume = 1;
        to.volume = 1;
    }
}