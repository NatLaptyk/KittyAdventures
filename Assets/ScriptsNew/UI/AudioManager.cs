using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    Dictionary<AudioClip, float> lastPlayedTime = new Dictionary<AudioClip, float>();
    public static AudioManager instance;
    public AudioSource sfxSource;
    public float sfxCooldown = 0.1f;
    public float sfxVolume = 0.1f;

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

    [Header("Music")]
    public AudioClip levelMusic;
    public AudioClip bossMusic;
    public AudioSource musicSource;
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop   = true;
        musicSource.volume = musicVolume;

        if (levelMusic != null)
        {
            musicSource.clip = levelMusic;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float cooldown = 0.1f)
    {
        if (clip == null || sfxSource == null) return;

        if (lastPlayedTime.TryGetValue(clip, out float lastTime))
        {
            if (Time.time - lastTime < cooldown)
                return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
        lastPlayedTime[clip] = Time.time;
    }
    public void StartBossMusic()
    {
        if (bossMusic == null || musicSource == null) return;
        musicSource.clip = bossMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void ReturnToLevelMusic()
    {
        if (levelMusic == null || musicSource == null) return;
        musicSource.clip = levelMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

}