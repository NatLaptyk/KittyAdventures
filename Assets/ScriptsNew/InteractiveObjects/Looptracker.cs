// Manages an ordered sequence of checkpoints. When Kitty activates all
// checkpoints in order, fires OnLoopComplete which triggers the TreeObstruction.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopTracker : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Checkpoints — add in order")]
    [Tooltip("Drag CheckpointMarker GameObjects here in the order Kitty must visit them.")]
    [SerializeField] private List<CheckpointMarker> checkpoints = new List<CheckpointMarker>();

    [Header("Obstruction")]
    [Tooltip("The TreeObstruction to open when the loop is complete.")]
    [SerializeField] private TreeObstruction obstruction;

    [Header("Settings")]
    [Tooltip("If true, checkpoints must be visited in strict order. " +
             "If false, any order works.")]
    [SerializeField] private bool strictOrder = true;

    [Tooltip("Show debug logs in Console.")]
    [SerializeField] private bool debugLogs = true;

    // ─────────────────────────────────────────────
    //  EVENTS
    // ─────────────────────────────────────────────

    /// <summary>Fired when all checkpoints have been activated.</summary>
    [SerializeField] private PlayerStats playerStats;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    int  _nextIndex    = 0;
    int  _activatedCount = 0;
    bool _complete     = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Register this tracker with each checkpoint
        foreach (var cp in checkpoints)
        {
            if (cp != null)
                cp.SetTracker(this);
        }

        if (debugLogs)
            Debug.Log($"[LoopTracker] Initialised with {checkpoints.Count} checkpoints.");

    }

    // ─────────────────────────────────────────────
    //  PUBLIC API — called by CheckpointMarker
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called by a CheckpointMarker when Kitty activates it.
    /// Returns true if the activation was valid (correct order).
    /// </summary>
    public bool TryActivate(CheckpointMarker marker)
    {
        if (_complete) return false;

        int index = checkpoints.IndexOf(marker);
        if (index < 0) return false;

        if (strictOrder)
        {
            // Must activate in exact order
            if (index != _nextIndex)
            {
                if (debugLogs)
                    Debug.Log($"[LoopTracker] Wrong order! Expected checkpoint {_nextIndex}, got {index}. Resetting.");

                ResetProgress();
                return false;
            }
        }

        _nextIndex = index + 1;
        _activatedCount++;

        if (debugLogs)
            Debug.Log($"[LoopTracker] Checkpoint {index + 1}/{checkpoints.Count} activated.");

        if (_activatedCount >= checkpoints.Count)
            StartCoroutine(CompleteLoop());

        return true;
    }

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    IEnumerator CompleteLoop()
    {
        _complete = true;

        if (debugLogs)
            Debug.Log("[LoopTracker] Loop complete! Opening path...");

        // Small delay for dramatic effect
        yield return new WaitForSeconds(0.5f);

        playerStats.AddSnack(1);

        // Path is now opened manually by OrbGate when Kitty interacts with it.
        // obstruction.OpenPath() is no longer called here.
    }

    void ResetProgress()
    {
        _nextIndex      = 0;
        _activatedCount = 0;

        // Reset all checkpoint visuals
        foreach (var cp in checkpoints)
            if (cp != null) cp.ResetVisual();
    }
}