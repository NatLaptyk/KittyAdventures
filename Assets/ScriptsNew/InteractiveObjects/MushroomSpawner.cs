// Randomly spawns a number of mushrooms each playthrough within defined
// spawn points. Tells NumberTrigger the correct answer (= mushroom count).
// If the puzzle was already solved this session, mushrooms stay as they are.

using System.Collections.Generic;
using UnityEngine;

public class MushroomSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Mushrooms")]
    [Tooltip("Mushroom prefab to spawn.")]
    [SerializeField] private GameObject mushroomPrefab;

    [Tooltip("All possible spawn point Transforms. Mushrooms will be placed at a random subset of these.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("Minimum number of mushrooms to spawn.")]
    [SerializeField] private int minMushrooms = 5;

    [Tooltip("Maximum number of mushrooms to spawn.")]
    [SerializeField] private int maxMushrooms = 12;

    [Header("Puzzle")]
    [Tooltip("The NumberTrigger whose correct answer will be set to the mushroom count.")]
    [SerializeField] private NumberTrigger numberTrigger;

    [Header("State")]
    [Tooltip("Read-only — how many mushrooms were spawned this session.")]
    [SerializeField] private int spawnedCount = 0;

    // ─────────────────────────────────────────────
    //  STATIC STATE — survives respawn
    // ─────────────────────────────────────────────

    public static bool PuzzleSolved  = false;
    public static int  LastSpawnCount = -1;   // -1 means not yet spawned this session

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    readonly List<GameObject> _spawnedMushrooms = new List<GameObject>();

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Subscribe to puzzle solved event to remember state across respawns
        NumberTrigger.OnPuzzleSolved += OnPuzzleSolved;

        if (PuzzleSolved)
        {
            // Puzzle already solved this session — don't respawn mushrooms
            // but re-spawn them in their solved state so the scene looks right
            if (LastSpawnCount > 0)
                SpawnMushrooms(LastSpawnCount, updateTrigger: false);
            return;
        }

        if (LastSpawnCount < 0)
        {
            // First time this session — pick a random count
            int count = Random.Range(minMushrooms, maxMushrooms + 1);
            LastSpawnCount = count;
        }

        SpawnMushrooms(LastSpawnCount, updateTrigger: true);
    }

    void OnDestroy()
    {
        NumberTrigger.OnPuzzleSolved -= OnPuzzleSolved;
    }

    // ─────────────────────────────────────────────
    //  SPAWN
    // ─────────────────────────────────────────────

    void SpawnMushrooms(int count, bool updateTrigger)
    {
        if (mushroomPrefab == null || spawnPoints.Count == 0) return;

        count = Mathf.Clamp(count, 0, spawnPoints.Count);
        spawnedCount = count;

        // Shuffle spawn points
        List<Transform> shuffled = new List<Transform>(spawnPoints);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Spawn mushrooms at first 'count' shuffled points
        for (int i = 0; i < count; i++)
        {
            var mushroom = Instantiate(mushroomPrefab, shuffled[i].position, shuffled[i].rotation);
            _spawnedMushrooms.Add(mushroom);
        }

        // Tell NumberTrigger the correct answer
        if (updateTrigger && numberTrigger != null)
        {
            numberTrigger.correctNumber = count;
            Debug.Log($"[MushroomSpawner] Spawned {count} mushrooms. Correct answer set to {count}.");
        }
    }

    // ─────────────────────────────────────────────
    //  PUZZLE SOLVED
    // ─────────────────────────────────────────────

    void OnPuzzleSolved()
    {
        PuzzleSolved = true;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC — call from GameStats.Reset() on Play Again
    // ─────────────────────────────────────────────

    public static void ResetPuzzleState()
    {
        PuzzleSolved   = false;
        LastSpawnCount = -1;
    }
}