// ─────────────────────────────────────────────────────────────────────────────
//  BranchingManager.cs
//
//  Tracks which branching path the player has chosen:
//    Path A — Orbs first (orbs active, fish snacks spawn on collection)
//    Path B — Mushroom puzzle first (orbs disappear, puzzle opens TreeObstruction)
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject → name it "BranchingManager" → attach this script
//  2. Assign the Orb Area root GameObject (parent of all orb GameObjects)
//  3. Assign the TreeObstruction that orbs normally open
//  4. Place OrbPathTrigger and MushroomPathTrigger in the scene (separate scripts)
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class BranchingManager : MonoBehaviour
{
    public static BranchingManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Path A — Orbs")]
    [Tooltip("Parent GameObject containing all orb collectibles.")]
    public GameObject orbAreaRoot;

    [Tooltip("The OrbGate used to open the TreeObstruction on Path A.")]
    public OrbGate orbGate;

    [Header("Path B — Mushroom")]
    [Tooltip("The TreeObstruction that should open when mushroom puzzle is solved on Path B.")]
    public TreeObstruction treeObstruction;

    [Tooltip("The MushroomObstruction that opens on puzzle solve.")]
    public MushroomObstruction mushroomObstruction;

    [Header("Fish Snack")]
    [Tooltip("Fish snack prefab spawned in front of Kitty on each orb collect (Path A).")]
    public GameObject fishSnackPrefab;

    [Tooltip("How far in front of Kitty the snack spawns.")]
    public float snackSpawnOffset = 1.5f;

    // ─────────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────────

    public enum Branch { Undecided, OrbPath, MushroomPath }
    public Branch CurrentBranch { get; private set; } = Branch.Undecided;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    //  PATH SELECTION
    // ─────────────────────────────────────────────

    /// <summary>Called by OrbPathTrigger when Kitty enters the orb area.</summary>
    public void ChooseOrbPath()
    {
        if (CurrentBranch != Branch.Undecided) return;
        CurrentBranch = Branch.OrbPath;
        Debug.Log("[BranchingManager] Path A chosen — Orbs");

        // Mushroom puzzle now opens nothing extra (MushroomObstruction handles itself)
        // OrbGate remains active
    }

    /// <summary>Called by MushroomPathTrigger when Kitty enters the mushroom area first.</summary>
    public void ChooseMushroomPath()
    {
        if (CurrentBranch != Branch.Undecided) return;
        CurrentBranch = Branch.MushroomPath;
        Debug.Log("[BranchingManager] Path B chosen — Mushroom Puzzle");

        // Disable all orbs
        if (orbAreaRoot != null)
            orbAreaRoot.SetActive(false);

        // Disable OrbGate
        if (orbGate != null)
            orbGate.gameObject.SetActive(false);

        // Wire mushroom puzzle to also open the TreeObstruction
        if (mushroomObstruction != null && treeObstruction != null)
            mushroomObstruction.OnPathOpened += () => treeObstruction.OpenPath();
    }

    /// <summary>Called by CheckpointMarker on Path A — spawns a fish snack.</summary>
    public void SpawnFishSnack(Vector3 kittyPosition, Vector3 kittyForward)
    {
        if (CurrentBranch != Branch.OrbPath)
        {
            Debug.Log($"[BranchingManager] SpawnFishSnack skipped — branch is {CurrentBranch}");
            return;
        }
        if (fishSnackPrefab == null)
        {
            Debug.LogWarning("[BranchingManager] Fish Snack Prefab is not assigned!");
            return;
        }

        Vector3 spawnPos = kittyPosition + kittyForward * snackSpawnOffset + Vector3.up * 0.5f;
        Debug.Log($"[BranchingManager] Spawning fish snack at {spawnPos}");
        Instantiate(fishSnackPrefab, spawnPos, Quaternion.identity);
    }

    /// <summary>Reset on Play Again.</summary>
    public static void ResetBranch()
    {
        if (Instance != null)
            Instance.CurrentBranch = Branch.Undecided;
    }
}