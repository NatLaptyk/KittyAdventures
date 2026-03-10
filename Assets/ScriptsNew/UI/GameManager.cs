// ─────────────────────────────────────────────────────────────────────────────
//  GameManager.cs
//
//  Listens for Kitty's death and loads the EndScene after a short delay.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject in your main game scene, name it "GameManager"
//  2. Attach this script to it
//  3. Assign Kitty's PlayerStats to the Player Stats field
//  4. Make sure "EndScene" is added to Build Settings
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag Kitty's PlayerStats here.")]
    public PlayerStats playerStats;

    [Header("Scene")]
    [Tooltip("Exact name of your end scene as it appears in Build Settings.")]
    public string endSceneName = "EndScene";

    [Header("Timing")]
    [Tooltip("Seconds to wait after Kitty dies before loading the end scene.")]
    public float deathDelay = 3f;

    [Header("Death Fade (optional)")]
    [Tooltip("Optional fullscreen Image to fade to black on death.")]
    public Image fadeImage;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
            playerStats.Died += OnKittyDied;
        else
            Debug.LogWarning("[GameManager] No PlayerStats found — death trigger won't work.");
    }

    void OnDestroy()
    {
        if (playerStats != null)
            playerStats.Died -= OnKittyDied;
    }

    // ─────────────────────────────────────────────
    //  DEATH HANDLER
    // ─────────────────────────────────────────────

    void OnKittyDied()
    {
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(deathDelay);

        // Fade to black if image assigned
        if (fadeImage != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
        }

        SceneManager.LoadScene(endSceneName);
    }
}