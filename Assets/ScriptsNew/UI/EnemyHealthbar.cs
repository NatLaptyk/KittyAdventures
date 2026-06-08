// A world-space health bar that floats above an enemy's head.
// Hides when the enemy is at full health, appears on first hit,
// and fades out after the enemy hasn't taken damage for a while.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The EnemyStats this bar tracks. Auto-found from parent if not set.")]
    [SerializeField] private EnemyStats stats;

    [Tooltip("The fill image showing current health.")]
    [SerializeField] private Image fillImage;

    [Tooltip("Optional ghost/delayed fill image — lags behind for visual feedback.")]
    [SerializeField] private Image delayedFillImage;

    [Header("Behaviour")]
    [Tooltip("Bar is hidden at full health and only appears after first hit.")]
    [SerializeField] private bool hideWhenFull       = true;

    [Tooltip("Seconds after last damage before bar fades out.")]
    [SerializeField] private float fadeOutDelay      = 3f;

    [Tooltip("How fast the bar fades in/out.")]
    [SerializeField] private float fadeSpeed         = 4f;

    [Tooltip("How fast the ghost bar catches up.")]
    [SerializeField] private float delayedLerpSpeed  = 2.5f;

    [Tooltip("Seconds before ghost bar starts moving.")]
    [SerializeField] private float delayedBarDelay   = 0.5f;

    [Header("Billboard")]
    [Tooltip("If true, the bar always faces the camera.")]
    [SerializeField] private bool faceCamera         = true;

    [Header("Colours")]
    [SerializeField] private Color highHealthColour  = new Color(0.3f, 0.85f, 0.3f);   // green
    [SerializeField] private Color midHealthColour   = new Color(1.0f, 0.75f, 0.0f);   // yellow
    [SerializeField] private Color lowHealthColour   = new Color(0.9f, 0.15f, 0.15f);  // red

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    float         _targetFill        = 1f;
    float         _delayedFill       = 1f;
    float         _delayTimer        = 0f;
    bool          _delayActive       = false;
    float         _fadeOutTimer      = 0f;
    bool          _hasBeenHit        = false;
    CanvasGroup   _canvasGroup;
    Camera        _cam;
    bool          _dead              = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        TryGetComponent(out _canvasGroup);
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start hidden if hideWhenFull
        _canvasGroup.alpha = hideWhenFull ? 0f : 1f;
    }

    void Start()
    {
        _cam = Camera.main;

        // Auto-find EnemyStats from parent
        if (stats == null)
            stats = GetComponentInParent<EnemyStats>();

        if (stats == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] No EnemyStats found on {gameObject.name} or its parents.");
            return;
        }

        stats.OnHealthChanged += OnHealthChanged;
        stats.OnDied          += OnDied;

        // Initialise to full
        SetFill(1f);
        _delayedFill = 1f;
        if (delayedFillImage != null)
            delayedFillImage.fillAmount = 1f;
    }

    void OnDestroy()
    {
        if (stats == null) return;
        stats.OnHealthChanged -= OnHealthChanged;
        stats.OnDied          -= OnDied;
    }

    void Update()
    {
        if (_dead) return;

        HandleBillboard();
        AnimateDelayedBar();
        AnimateFade();
    }

    // ─────────────────────────────────────────────
    //  EVENT HANDLERS
    // ─────────────────────────────────────────────

    void OnHealthChanged(float current, float max)
    {
        _targetFill   = max > 0f ? current / max : 0f;
        _hasBeenHit   = true;
        _fadeOutTimer = fadeOutDelay;

        // Snap fill immediately
        SetFill(_targetFill);

        // Start delayed bar countdown
        _delayTimer  = delayedBarDelay;
        _delayActive = true;

        // Show bar
        if (hideWhenFull)
            _canvasGroup.alpha = 1f;
    }

    void OnDied()
    {
        _dead = true;
        StartCoroutine(FadeOutAndDisable());
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    void HandleBillboard()
    {
        if (!faceCamera || _cam == null) return;

        // Face the camera so bar is always readable
        transform.forward = _cam.transform.forward;
    }

    void AnimateDelayedBar()
    {
        if (delayedFillImage == null) return;

        if (_delayActive)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0f)
                _delayActive = false;
        }
        else
        {
            _delayedFill = Mathf.Lerp(_delayedFill, _targetFill,
                                       Time.deltaTime * delayedLerpSpeed);
            delayedFillImage.fillAmount = _delayedFill;
        }
    }

    void AnimateFade()
    {
        if (!hideWhenFull || !_hasBeenHit) return;

        if (_fadeOutTimer > 0f)
        {
            _fadeOutTimer -= Time.deltaTime;
        }
        else
        {
            // Fade out gently
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0f,
                                             Time.deltaTime * fadeSpeed);
        }
    }

    IEnumerator FadeOutAndDisable()
    {
        while (_canvasGroup.alpha > 0.01f)
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0f,
                                             Time.deltaTime * fadeSpeed * 2f);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    void SetFill(float value)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = value;

        // Colour shifts green → yellow → red as health drops
        if (value > 0.5f)
            fillImage.color = Color.Lerp(midHealthColour, highHealthColour, (value - 0.5f) * 2f);
        else
            fillImage.color = Color.Lerp(lowHealthColour, midHealthColour, value * 2f);
    }
}