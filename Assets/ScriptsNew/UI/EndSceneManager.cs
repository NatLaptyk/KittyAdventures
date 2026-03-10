// ─────────────────────────────────────────────────────────────────────────────
//  EndSceneManager.cs
//
//  Manages the end screen — fades in, handles Replay and Exit buttons.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create a new Unity Scene: File → New Scene → name it "EndScene"
//  2. Add it to Build Settings: File → Build Settings → Add Open Scenes
//     Make sure your main game scene is index 0, EndScene is index 1
//  3. Create a Canvas (Screen Space Overlay) in EndScene
//  4. Attach this script to an empty GameObject named "EndSceneManager"
//  5. Build the UI (see below) and wire the buttons in the Inspector
//
//  UI HIERARCHY
//  ─────────────────────────────────────────────────────────────────────────────
//  Canvas
//    └── Panel (full screen dark overlay — Image, colour 0,0,0,255)
//          ├── TitleText    (TextMeshPro — "Thanks for playing!")
//          ├── SubtitleText (TextMeshPro — "Whisker in the Woods")
//          ├── ReplayButton (Button + TextMeshPro child — "Play Again")
//          └── ExitButton   (Button + TextMeshPro child — "Exit Game")
//
//  Wire in Inspector:
//    - Replay Button → drag the Button component into Replay Button field
//    - Exit Button   → drag the Button component into Exit Button field
//    - Panel         → drag the Panel Image into Panel Image field
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndSceneManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Buttons")]
    public Button replayButton;
    public Button exitButton;

    [Header("UI Elements")]
    [Tooltip("The full-screen panel image — used for fade in.")]
    public Image    panelImage;
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    [Header("Scenes")]
    [Tooltip("Name of your main game scene to reload on Replay.")]
    public string gameSceneName = "MainScene";

    [Header("Timing")]
    public float fadeInDuration  = 1.5f;
    public float textRevealDelay = 0.8f;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Make sure time is running (in case it was paused)
        Time.timeScale = 1f;

        // Wire buttons
        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplay);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);

        // Start hidden then fade in
        SetUIAlpha(0f);
        StartCoroutine(FadeIn());
    }

    void OnDestroy()
    {
        if (replayButton != null) replayButton.onClick.RemoveListener(OnReplay);
        if (exitButton   != null) exitButton.onClick.RemoveListener(OnExit);
    }

    // ─────────────────────────────────────────────
    //  BUTTON HANDLERS
    // ─────────────────────────────────────────────

    public void OnReplay()
    {
        StartCoroutine(FadeOutAndLoad(gameSceneName));
    }

    public void OnExit()
    {
        StartCoroutine(FadeOutAndQuit());
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    IEnumerator FadeIn()
    {
        // Fade panel from black to transparent
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            if (panelImage != null)
                panelImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        // Wait then reveal text and buttons
        yield return new WaitForSeconds(textRevealDelay);
        StartCoroutine(RevealUI());
    }

    IEnumerator RevealUI()
    {
        // Fade in title
        yield return StartCoroutine(FadeText(titleText, 0f, 1f, 0.6f));
        yield return new WaitForSeconds(0.2f);

        // Fade in subtitle
        yield return StartCoroutine(FadeText(subtitleText, 0f, 1f, 0.5f));
        yield return new WaitForSeconds(0.3f);

        // Fade in buttons
        SetUIAlpha(1f);
    }

    IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null) yield break;

        float t = 0f;
        Color col = text.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            col.a    = Mathf.Lerp(from, to, t);
            text.color = col;
            yield return null;
        }
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeToBlack());
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOutAndQuit()
    {
        yield return StartCoroutine(FadeToBlack());

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeToBlack()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.6f;
            if (panelImage != null)
                panelImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
    }

    void SetUIAlpha(float alpha)
    {
        // Set button alpha via CanvasGroup if present
        var groups = GetComponentsInChildren<CanvasGroup>();
        foreach (var g in groups) g.alpha = alpha;
    }
}