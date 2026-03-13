// ─────────────────────────────────────────────────────────────────────────────
//  MainMenuManager.cs
//
//  Handles the Main Menu scene — plays music, handles Play and Exit buttons,
//  and fades in/out on load.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create a new scene: File → New Scene → name it "MainMenu"
//  2. Add to Build Settings as scene 0 (drag it to the top)
//     New order: 0 MainMenu → 1 Intro → 2 MainScene → 3 Victory → 4 End
//  3. Create a Canvas (Screen Space - Overlay) in MainMenu
//  4. Attach this script to an empty GameObject named "MainMenuManager"
//
//  UI HIERARCHY
//  ─────────────────────────────────────────────────────────────────────────────
//  Canvas
//    ├── Background     (UI Image — assign your background sprite)
//    └── Panel
//          ├── TitleText      (TextMeshPro — "Whisker in the Woods")
//          ├── SubtitleText   (TextMeshPro — "A cat's adventure...")
//          ├── PlayButton     (Button + TextMeshPro child — "Play")
//          └── ExitButton     (Button + TextMeshPro child — "Exit")
//
//  Wire in Inspector:
//    - Fade Image    → a full-screen black UI Image (put it last in hierarchy so it's on top)
//    - Play Button   → drag the Button here
//    - Exit Button   → drag the Button here
//    - Level Music   → drag your music AudioClip here
// ─────────────────────────────────────────────────────────────────────────────

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
    public string introSceneName = "Intro";

    [Header("Buttons")]
    public Button playButton;
    public Button exitButton;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    [Header("Music")]
    [Tooltip("Level music clip to play on the main menu.")]
    public AudioClip levelMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;


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