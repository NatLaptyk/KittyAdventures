using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
}
