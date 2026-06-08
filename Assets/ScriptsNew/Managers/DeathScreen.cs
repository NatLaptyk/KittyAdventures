// Shows a death overlay when Kitty's health reaches zero.
// Offers Respawn (restart from checkpoint or spawn point) and Exit to Menu.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private PlayerStats  playerStats;

    [Header("Death Panel")]
    [Tooltip("The root panel GameObject — set inactive in scene, activated on death.")]
    [SerializeField] private GameObject   deathPanel;
    [SerializeField] private CanvasGroup  deathCanvasGroup;
    [SerializeField] private TMP_Text     titleText;
    [SerializeField] private TMP_Text     subText;
    [SerializeField] private Button       respawnButton;
    [SerializeField] private Button       menuButton;

    [Header("Respawn")]
    [Tooltip("Where Kitty respawns. If empty, respawns at her death position.")]
    public Transform    respawnPoint;

    [Tooltip("Seconds after death before the death screen fades in.")]
    [SerializeField] private float        deathDelay     = 1.8f;

    [Tooltip("How fast the death screen fades in.")]
    [SerializeField] private float        fadeInDuration = 1.0f;

    [Header("Scenes")]
    [SerializeField] private string       menuSceneName  = "MainMenu";

    [Header("Slow Motion on Death")]
    [Tooltip("If true, time slows briefly when Kitty dies for dramatic effect.")]
    [SerializeField] private bool         slowMotionOnDeath = true;
    [SerializeField] private float        slowMotionScale   = 0.25f;
    [SerializeField] private float        slowMotionDuration = 1.2f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    Transform _kittyTransform;
    bool      _isDead = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Auto-find PlayerStats
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.Died += OnKittyDied;
            _kittyTransform   = playerStats.transform;
        }
        else
        {
            Debug.LogWarning("[DeathScreen] No PlayerStats found!");
        }

        // Wire buttons
        if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawn);
        if (menuButton    != null) menuButton.onClick.AddListener(OnMenu);

        // Make sure death panel starts hidden
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (playerStats != null) playerStats.Died -= OnKittyDied;
        if (respawnButton != null) respawnButton.onClick.RemoveListener(OnRespawn);
        if (menuButton    != null) menuButton.onClick.RemoveListener(OnMenu);
    }

    // ─────────────────────────────────────────────
    //  DEATH HANDLER
    // ─────────────────────────────────────────────

    void OnKittyDied()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Optional slow motion
        if (slowMotionOnDeath)
        {
            Time.timeScale      = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return new WaitForSecondsRealtime(slowMotionDuration);

            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        // Wait for death animation
        yield return new WaitForSecondsRealtime(deathDelay - slowMotionDuration);

        // Show death panel
        if (deathPanel != null)
            deathPanel.SetActive(true);

        // Fade in
        if (deathCanvasGroup != null)
        {
            deathCanvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeInDuration;
                deathCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            deathCanvasGroup.alpha = 1f;
        }
    }

    // ─────────────────────────────────────────────
    //  BUTTON HANDLERS
    // ─────────────────────────────────────────────

    void OnRespawn()
    {
        StartCoroutine(RespawnSequence());
    }

    void OnMenu()
    {
        Time.timeScale = 1f;
        SceneFader.Instance?.FadeTo(menuSceneName);
    }

    IEnumerator RespawnSequence()
    {
        // Fade out death panel
        if (deathCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.4f;
                deathCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
        }

        // Hide panel
        if (deathPanel != null)
            deathPanel.SetActive(false);

        // Teleport Kitty to respawn point
        if (_kittyTransform != null)
        {
            // Disable CharacterController briefly to allow teleport
            var cc = _kittyTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 spawnPos = respawnPoint != null
                ? respawnPoint.position
                : _kittyTransform.position;

            _kittyTransform.position = spawnPos;

            if (cc != null) cc.enabled = true;
        }

        // Reset Kitty's stats — health + stamina back to full
        if (playerStats != null)
            playerStats.ResetFull();

        // Re-enable Kitty's scripts
        var playerController = _kittyTransform?.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = true;

        var playerCombat = _kittyTransform?.GetComponent<PlayerCombat>();
        if (playerCombat != null) playerCombat.enabled = true;

        // Snap camera behind Kitty
        var cam = FindFirstObjectByType<CameraController>();
        cam?.SnapBehindKitty();

        _isDead = false;
    }
}