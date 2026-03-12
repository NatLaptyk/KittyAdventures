// ─────────────────────────────────────────────────────────────────────────────
//  PlayerHUD.cs
//
//  Draws Kitty's health and stamina bars on screen.
//  Listens to PlayerStats events — no polling needed.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create a Canvas (Screen Space — Overlay) if you don't have one
//
//  2. BUILD THE HEALTH BAR
//     a. Right-click Canvas → UI → Image → name it "HealthBarBG"
//        Colour: dark red (30, 0, 0, 200)
//     b. Right-click HealthBarBG → UI → Image → name it "HealthBarFill"
//        Set Image Type to "Filled", Fill Method "Horizontal", Fill Origin "Left"
//        Colour: bright red (220, 50, 50, 255)
//     c. Right-click HealthBarBG → UI → Image → name it "HealthBarDelayed"
//        Same Filled settings — this is the ghost bar that lags behind
//        Colour: orange (255, 160, 0, 200)
//        Place this BETWEEN BG and Fill in the hierarchy so Fill draws on top
//
//  3. BUILD THE STAMINA BAR — same structure, different colours
//     Background: dark blue (0, 10, 40, 200)
//     Fill:       bright yellow (240, 220, 40, 255)
//     Delayed:    not needed for stamina (optional)
//
//  4. Attach PlayerHUD.cs to the Canvas or any persistent GameObject
//     Drag the Fill images into the Inspector slots
//     Assign the PlayerStats component (drag Kitty root)
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Drag Kitty's PlayerStats component here.")]
    public PlayerStats playerStats;

    [Header("Health Bar")]
    [Tooltip("The filled Image that shows current health.")]
    public Image healthFill;

    [Header("Regen Indicator")]
    [Tooltip("Colour the health bar glows when regenerating passively.")]
    public Color regenColour         = new Color(0.3f, 1f, 0.5f);
    [Tooltip("Colour when regenerating fast (out of combat).")]
    public Color fastRegenColour     = new Color(0f, 1f, 0.3f);
    [Tooltip("How fast the regen glow pulses.")]
    public float regenPulseSpeed     = 2f;

    [Header("Snack UI")]
    [Tooltip("Image showing the snack icon — hide when count is 0.")]
    public Image   snackIcon;
    [Tooltip("TMP text showing snack count e.g. 'x1'.")]
    public TMPro.TextMeshProUGUI snackText;
    [Tooltip("Key to eat the snack.")]
    public KeyCode eatKey = KeyCode.F;

    [Tooltip("The ghost bar that lags behind health loss for dramatic effect.")]
    public Image healthDelayedFill;

    [Tooltip("Seconds before the ghost bar starts catching up.")]
    public float healthDelayTime   = 0.6f;

    [Tooltip("How fast the ghost bar catches up.")]
    public float healthLerpSpeed   = 3f;

    [Header("Stamina Bar")]
    [Tooltip("The filled Image that shows current stamina.")]
    public Image staminaFill;

    [Tooltip("How fast the stamina bar animates.")]
    public float staminaLerpSpeed  = 8f;

    [Header("Low Health Warning")]
    [Tooltip("Health percentage below which the bar pulses red.")]
    [Range(0f, 0.5f)]
    public float lowHealthThreshold = 0.25f;

    [Tooltip("How fast the low-health pulse animates.")]
    public float lowHealthPulseSpeed = 3f;

    public Color lowHealthColour    = new Color(1f, 0.1f, 0.1f);
    public Color normalHealthColour = new Color(0.86f, 0.2f, 0.2f);

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    float _targetHealth   = 1f;
    float _displayHealth  = 1f;   // ghost bar value
    float _targetStamina  = 1f;
    float _delayTimer     = 0f;
    bool  _delayActive    = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Auto-find PlayerStats if not assigned
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogWarning("[PlayerHUD] No PlayerStats found — HUD won't work.");
            return;
        }

        // Subscribe to events
        playerStats.HealthChanged  += OnHealthChanged;
        playerStats.StaminaChanged += OnStaminaChanged;
        playerStats.SnacksChanged  += OnSnacksChanged;

        // Initialise bars to full
        SetBarImmediate(healthFill,        1f);
        SetBarImmediate(healthDelayedFill, 1f);
        SetBarImmediate(staminaFill,       1f);
        OnSnacksChanged(playerStats.Snacks);
    }

    void OnDestroy()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged  -= OnHealthChanged;
        playerStats.StaminaChanged -= OnStaminaChanged;
        playerStats.SnacksChanged  -= OnSnacksChanged;
    }

    void Update()
    {
        AnimateHealthBar();
        AnimateStaminaBar();
        AnimateLowHealthPulse();
        AnimateRegenGlow();
        CheckSnackInput();
    }

    // ─────────────────────────────────────────────
    //  EVENT HANDLERS
    // ─────────────────────────────────────────────

    void OnHealthChanged(float current, float max)
    {
        float newTarget = max > 0f ? current / max : 0f;

        // If health dropped, start the delay before ghost bar catches up
        if (newTarget < _targetHealth)
        {
            _delayTimer  = healthDelayTime;
            _delayActive = true;
        }

        _targetHealth = newTarget;

        // Health fill snaps immediately
        if (healthFill != null)
            healthFill.fillAmount = _targetHealth;
    }

    void OnStaminaChanged(float current, float max)
    {
        _targetStamina = max > 0f ? current / max : 0f;
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    void AnimateHealthBar()
    {
        if (healthDelayedFill == null) return;

        if (_delayActive)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0f)
                _delayActive = false;
        }
        else
        {
            // Ghost bar lerps down to catch up with real health
            _displayHealth = Mathf.Lerp(_displayHealth, _targetHealth,
                                        Time.deltaTime * healthLerpSpeed);
            healthDelayedFill.fillAmount = _displayHealth;
        }
    }

    void AnimateStaminaBar()
    {
        if (staminaFill == null) return;

        staminaFill.fillAmount = Mathf.Lerp(staminaFill.fillAmount, _targetStamina,
                                             Time.deltaTime * staminaLerpSpeed);
    }

    void AnimateLowHealthPulse()
    {
        if (healthFill == null) return;
        if (_targetHealth > lowHealthThreshold) 
        {
            healthFill.color = normalHealthColour;
            return;
        }

        // Pulse between normal and low health colour
        float t = (Mathf.Sin(Time.time * lowHealthPulseSpeed) + 1f) * 0.5f;
        healthFill.color = Color.Lerp(normalHealthColour, lowHealthColour, t);
    }

    void SetBarImmediate(Image bar, float value)
    {
        if (bar != null) bar.fillAmount = value;
    }
    // ─────────────────────────────────────────────
    //  SNACK
    // ─────────────────────────────────────────────

    void CheckSnackInput()
    {
        if (playerStats == null) return;
        if (Input.GetKeyDown(eatKey))
            playerStats.UseSnack();
    }

    void OnSnacksChanged(int count)
    {
        if (snackIcon != null)
            snackIcon.gameObject.SetActive(count > 0);
        if (snackText != null)
            snackText.text = count > 0 ? $"x{count}  F to snack" : "";
    }

    // ─────────────────────────────────────────────
    //  REGEN GLOW
    // ─────────────────────────────────────────────

    void AnimateRegenGlow()
    {
        if (healthFill == null || playerStats == null) return;
        if (!playerStats.IsRegenerating) return;

        // Don't override low-health pulse
        if (playerStats.Health / playerStats.MaxHealth <= lowHealthThreshold) return;

        bool outOfCombat = playerStats.Health < playerStats.MaxHealth;
        Color target = outOfCombat ? fastRegenColour : regenColour;
        float t = (Mathf.Sin(Time.time * regenPulseSpeed) + 1f) * 0.5f;
        healthFill.color = Color.Lerp(normalHealthColour, target, t * 0.6f);
    }

}