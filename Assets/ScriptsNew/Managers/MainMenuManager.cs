using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Scenes")]
    [Tooltip("Name of the Intro scene to load when Play is pressed.")]
    [SerializeField] private string introSceneName = "Intro";

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Music")]
    [Tooltip("Level music clip to play on the main menu.")]
    [SerializeField] private AudioClip levelMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.3f;


    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    AudioSource _musicSource;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        Time.timeScale = 1f;

        // Wire buttons
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);

        // Start music
        StartMusic();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
    }

    // ─────────────────────────────────────────────
    //  MUSIC
    // ─────────────────────────────────────────────

    void StartMusic()
    {
        if (levelMusic == null) return;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.clip   = levelMusic;
        _musicSource.loop   = true;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    // ─────────────────────────────────────────────
    //  BUTTON HANDLERS
    // ─────────────────────────────────────────────

    public void OnPlay()
    {
        SceneFader.Instance?.FadeTo(introSceneName);
    }

    public void OnExit()
    {
        SceneFader.Instance?.FadeToQuit();
    }

}