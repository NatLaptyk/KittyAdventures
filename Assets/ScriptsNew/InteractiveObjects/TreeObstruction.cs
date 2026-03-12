// ─────────────────────────────────────────────────────────────────────────────
//  TreeObstruction.cs
//
//  Controls a group of trees that shift aside to reveal a hidden path
//  when the loop is completed. Trees animate smoothly to open positions.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Place your blocking trees in the scene
//  2. Create an empty GameObject, name it "TreeObstruction"
//  3. Attach this script to it
//  4. For each blocking tree, add a TreeEntry in the Inspector:
//     - Tree Transform: drag the tree GameObject
//     - Open Position: where the tree should move to (offset from current pos)
//       e.g. set to (5, 0, 0) to slide 5 units to the right
//     - Open Rotation: optional rotation when open (e.g. tilt the tree)
//  5. Assign this to the LoopTracker's Obstruction field
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeObstruction : MonoBehaviour
{
    public static TreeObstruction Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  DATA
    // ─────────────────────────────────────────────

    [Serializable]
    public class TreeEntry
    {
        [Tooltip("The tree GameObject to move.")]
        public Transform tree;

        [Tooltip("World-space offset from the tree's original position when path is open. " +
                 "e.g. (4, 0, 0) slides the tree 4 units to the right.")]
        public Vector3 openPositionOffset = new Vector3(4f, 0f, 0f);

        [Tooltip("Optional rotation when the tree is in its open position.")]
        public Vector3 openRotation = Vector3.zero;

        [Tooltip("Use original rotation when closed.")]
        [HideInInspector] public Vector3 closedRotation;
        [HideInInspector] public Vector3 closedPosition;
        [HideInInspector] public bool    initialised = false;
    }

    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Trees")]
    public List<TreeEntry> trees = new List<TreeEntry>();

    [Header("Animation")]
    [Tooltip("How long the trees take to shift aside.")]
    public float openDuration   = 2f;

    [Tooltip("Delay between each tree starting to move — creates a wave effect.")]
    public float staggerDelay   = 0.15f;

    [Tooltip("Animation curve for tree movement — ease in/out by default.")]
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Effects")]
    [Tooltip("Optional particle effect played at the path entrance when it opens.")]
    public ParticleSystem openEffect;

    [Tooltip("Optional audio clip played when the path opens.")]
    public AudioClip openSound;

    [Header("State")]
    public bool isOpen = false;

    // ─────────────────────────────────────────────
    //  EVENTS
    // ─────────────────────────────────────────────

    public event Action OnPathOpened;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    AudioSource _audio;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        _audio = GetComponent<AudioSource>();

        // Store original positions and rotations
        foreach (var entry in trees)
        {
            if (entry.tree == null) continue;
            entry.closedPosition = entry.tree.position;
            entry.closedRotation = entry.tree.eulerAngles;
            entry.initialised    = true;
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>Opens the path — called by LoopTracker when loop is complete.</summary>
    public void OpenPath()
    {
        if (isOpen) return;
        isOpen = true;

        StartCoroutine(AnimateOpen());
    }

    /// <summary>Closes the path instantly (for resetting the puzzle).</summary>
    public void ClosePath()
    {
        isOpen = false;
        StopAllCoroutines();

        foreach (var entry in trees)
        {
            if (entry.tree == null || !entry.initialised) continue;
            entry.tree.position    = entry.closedPosition;
            entry.tree.eulerAngles = entry.closedRotation;
        }
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    IEnumerator AnimateOpen()
    {
        // Play open sound
        if (openSound != null && _audio != null)
            _audio.PlayOneShot(openSound);
        AudioManager.instance?.PlaySFX(AudioManager.instance.treesMoving, 0f);

        // Play open effect
        if (openEffect != null)
            openEffect.Play();

        // Stagger each tree's animation for a wave effect
        for (int i = 0; i < trees.Count; i++)
        {
            var entry = trees[i];
            if (entry.tree == null || !entry.initialised) continue;

            StartCoroutine(AnimateTree(entry));

            if (i < trees.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }

        // Wait for all trees to finish
        yield return new WaitForSeconds(openDuration);

        OnPathOpened?.Invoke();
        Debug.Log("[TreeObstruction] Path is now open!");
    }

    IEnumerator AnimateTree(TreeEntry entry)
    {
        Vector3 startPos = entry.closedPosition;
        Vector3 endPos   = entry.closedPosition + entry.openPositionOffset;
        Vector3 startRot = entry.closedRotation;
        Vector3 endRot   = entry.openRotation;

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t  = openCurve.Evaluate(Mathf.Clamp01(elapsed / openDuration));

            entry.tree.position    = Vector3.Lerp(startPos, endPos, t);
            entry.tree.eulerAngles = Vector3.Lerp(startRot, endRot, t);

            yield return null;
        }

        // Snap to final position
        entry.tree.position    = endPos;
        entry.tree.eulerAngles = endRot;
    }
}