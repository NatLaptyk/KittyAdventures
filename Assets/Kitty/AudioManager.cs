using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public AudioSource sfxSource;

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

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
