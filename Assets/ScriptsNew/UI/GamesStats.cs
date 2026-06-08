// Central tracker for game statistics — orbs collected, spiders killed.
// Listens to events from CheckpointMarker and EnemyStats automatically.
// Persists across scenes via DontDestroyOnLoad.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStats : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────

    public static GameStats Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  STATS
    // ─────────────────────────────────────────────

    public int OrbsCollected  { get; private set; } = 0;
    public int SpidersKilled  { get; private set; } = 0;
    public int TotalOrbs      { get; private set; } = 0;
    public int TotalSpiders   { get; private set; } = 0;

    // ─────────────────────────────────────────────
    //  EVENTS
    // ─────────────────────────────────────────────

    public event Action<int, int> OnOrbsChanged;      // (collected, total)
    public event Action<int, int> OnSpidersChanged;   // (killed, total)
    public event Action           OnAllOrbsCollected;
    public event Action           OnAllSpidersKilled;
    public event Action           OnPotionCollected;

    public bool PotionCollected { get; private set; } = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Listen for new orb collections
        CheckpointMarker.OnOrbCollected += HandleOrbCollected;

        // Broadcast initial totals so HUD gets correct values on first frame
        OnOrbsChanged?.Invoke(OrbsCollected, TotalOrbs);
        OnSpidersChanged?.Invoke(SpidersKilled, TotalSpiders);
    }

    void OnDestroy()
    {
        CheckpointMarker.OnOrbCollected -= HandleOrbCollected;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Reset counts first, then re-register fresh scene objects
        TotalOrbs     = 0;
        TotalSpiders  = 0;
        RegisterEnemies();
        RegisterOrbs();
        OnOrbsChanged?.Invoke(OrbsCollected, TotalOrbs);
        OnSpidersChanged?.Invoke(SpidersKilled, TotalSpiders);
    }

    // ─────────────────────────────────────────────
    //  REGISTRATION
    // ─────────────────────────────────────────────

    public void RegisterEnemies()
    {
        var enemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);

        foreach (var e in enemies)
        {
            // Only count and track enemies tagged "Spider" — excludes Spirit and others
            if (e.CompareTag("Spider"))
            {
                TotalSpiders++;
                e.OnDied += HandleSpiderKilled;
            }
        }
    }

    public void RegisterOrbs()
    {
        var orbs = FindObjectsByType<CheckpointMarker>(FindObjectsSortMode.None);
        TotalOrbs = orbs.Length;
    }

    // ─────────────────────────────────────────────
    //  HANDLERS
    // ─────────────────────────────────────────────

    void HandleOrbCollected()
    {
        OrbsCollected = Mathf.Min(OrbsCollected + 1, TotalOrbs);
        OnOrbsChanged?.Invoke(OrbsCollected, TotalOrbs);

        if (TotalOrbs > 0 && OrbsCollected >= TotalOrbs)
            OnAllOrbsCollected?.Invoke();
    }

    void HandleSpiderKilled()
    {
        SpidersKilled = Mathf.Min(SpidersKilled + 1, TotalSpiders);
        OnSpidersChanged?.Invoke(SpidersKilled, TotalSpiders);

        if (TotalSpiders > 0 && SpidersKilled >= TotalSpiders)
            OnAllSpidersKilled?.Invoke();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>Reset all stats — call on scene reload.</summary>
    public void Reset()
    {
        OrbsCollected   = 0;
        SpidersKilled   = 0;
        PotionCollected = false;
        MushroomSpawner.ResetPuzzleState();
        OnOrbsChanged?.Invoke(OrbsCollected, TotalOrbs);
        OnSpidersChanged?.Invoke(SpidersKilled, TotalSpiders);
    }

    /// <summary>Called by SpiritPotion when Kitty walks over it.</summary>
    public void RegisterPotionCollected()
    {
        if (PotionCollected) return;
        PotionCollected = true;
        OnPotionCollected?.Invoke();
    }
}