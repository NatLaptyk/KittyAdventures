// A persistent singleton that fades the screen to/from black on every scene
// transition. Lives across all scenes via DontDestroyOnLoad.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────

    public static SceneFader Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Tooltip("The full-screen black Image used for fading.")]
    [SerializeField] private Image fadeImage;

    CanvasGroup _canvasGroup;

    [Tooltip("Default duration for fade out (to black).")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("Default duration for fade in (from black).")]
    [SerializeField] private float fadeInDuration  = 0.8f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    bool _isFading = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        // Singleton — destroy duplicate if another scene already has one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Set up CanvasGroup on FadePanel for raycast control
        if (fadeImage != null)
        {
            _canvasGroup = fadeImage.gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable   = false;
        }

        // Subscribe to scene loaded event to fade in on every scene
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start fully black then fade in
        SetAlpha(1f);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ─────────────────────────────────────────────
    //  SCENE LOADED — fade in automatically
    // ─────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        StartCoroutine(FadeIn(fadeInDuration));
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>Debug helper — returns current blocksRaycasts state.</summary>
    public bool GetBlocksRaycasts() => _canvasGroup != null && _canvasGroup.blocksRaycasts;

    /// <summary>Force raycasts unblocked — call if buttons are stuck unclickable.</summary>
    public void ForceUnblock()
    {
        SetBlockRaycasts(false);
    }

    /// <summary>Fade to black then load the named scene.</summary>
    public void FadeTo(string sceneName, float fadeDuration = -1f)
    {
        if (_isFading) return;
        float duration = fadeDuration > 0f ? fadeDuration : fadeOutDuration;
        StartCoroutine(FadeOutAndLoad(sceneName, duration));
    }

    /// <summary>Fade to black then quit the application.</summary>
    public void FadeToQuit(float fadeDuration = -1f)
    {
        if (_isFading) return;
        float duration = fadeDuration > 0f ? fadeDuration : fadeOutDuration;
        StartCoroutine(FadeOutAndQuit(duration));
    }

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────

    IEnumerator FadeIn(float duration)
    {
        // Wait one frame for the scene to fully initialize
        yield return null;

        SetBlockRaycasts(true);
        _isFading = true;
        SetAlpha(1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            SetAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        SetAlpha(0f);
        SetBlockRaycasts(false);
        _isFading = false;
    }

    IEnumerator FadeOutAndLoad(string sceneName, float duration)
    {
        _isFading = true;
        SetBlockRaycasts(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            SetAlpha(Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        SetAlpha(1f);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOutAndQuit(float duration)
    {
        _isFading = true;
        SetBlockRaycasts(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            SetAlpha(Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    void SetAlpha(float alpha)
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
    }

    void SetBlockRaycasts(bool block)
    {
        if (_canvasGroup != null)
            _canvasGroup.blocksRaycasts = block;
    }
}