// ─────────────────────────────────────────────────────────────────────────────
//  GameStats.cs
//
//  Central tracker for game statistics — orbs collected, spiders killed.
//  Listens to events from CheckpointMarker and EnemyStats automatically.
//  Persists across scenes via DontDestroyOnLoad.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject → name it "GameStats" → attach this script
//  2. That's it — it finds enemies and orbs automatically at runtime
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;

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
    }

    void Start()
    {
        RegisterEnemies();
        RegisterOrbs();

        // Listen for new orb collections
        CheckpointMarker.OnOrbCollected += HandleOrbCollected;
    }

    void OnDestroy()
    {
        CheckpointMarker.OnOrbCollected -= HandleOrbCollected;
    }

    // ─────────────────────────────────────────────
    //  REGISTRATION
    // ─────────────────────────────────────────────

    void RegisterEnemies()
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

    void RegisterOrbs()
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