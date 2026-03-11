// ─────────────────────────────────────────────────────────────────────────────
//  SlidingTreeObstruction.cs
//
//  A group of trees that split apart like sliding doors when enough spiders
//  have been killed. Trees in the LEFT list slide left, trees in the RIGHT
//  list slide right, opening a gap in the centre.
//
//  Triggered automatically by GameStats.OnAllSpidersKilled.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Place your blocking trees in the scene.
//  2. Create an empty GameObject → name it "SlidingTreeObstruction" → attach this script.
//  3. Assign trees to Left Trees or Right Trees lists in the Inspector.
//     - Left Trees  → will slide in the -X (or custom) direction
//     - Right Trees → will slide in the +X (or custom) direction
//  4. Set Slide Distance to how far each side slides (world units).
//  5. Set Slide Direction to match your path orientation
//     (default Vector3.right — override if your path runs along Z axis).
//  6. Set Required Kills to 15 (or however many spiders trigger this).
//  7. No need to wire to LoopTracker — this listens to GameStats directly.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingTreeObstruction : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Trees")]
    [Tooltip("Trees that will slide LEFT when the path opens.")]
    public List<Transform> leftTrees  = new List<Transform>();

    [Tooltip("Trees that will slide RIGHT when the path opens.")]
    public List<Transform> rightTrees = new List<Transform>();

    [Header("Sliding")]
    [Tooltip("How far each side slides away from centre (world units).")]
    public float slideDistance = 5f;

    [Tooltip("The axis the trees slide along. Default is right (X). " +
             "Change to Vector3.forward if your path runs along Z.")]
    public Vector3 slideDirection = Vector3.right;

    [Tooltip("How long the slide animation takes.")]
    public float slideDuration = 2f;

    [Tooltip("Delay between each tree starting — creates a wave effect from centre outward.")]
    public float staggerDelay = 0.12f;

    [Tooltip("Animation curve — ease in/out by default.")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Trigger")]
    [Tooltip("How many spiders must be killed to open this obstruction.")]
    public int requiredKills = 15;

    [Header("Effects")]
    [Tooltip("Optional particle effect at the centre when it opens.")]
    public ParticleSystem openEffect;

    [Tooltip("Optional audio clip played when trees start sliding.")]
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

    // Stored start positions
    readonly List<Vector3> _leftStart  = new List<Vector3>();
    readonly List<Vector3> _rightStart = new List<Vector3>();

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        _audio = GetComponent<AudioSource>();

        foreach (var t in leftTrees)
            _leftStart.Add(t != null ? t.position : Vector3.zero);

        foreach (var t in rightTrees)
            _rightStart.Add(t != null ? t.position : Vector3.zero);
    }

    void Start()
    {
        if (GameStats.Instance != null)
            GameStats.Instance.OnSpidersChanged += OnSpidersChanged;
        else
            Debug.LogWarning("[SlidingTreeObstruction] No GameStats found in scene.");
    }

    void OnDestroy()
    {
        if (GameStats.Instance != null)
            GameStats.Instance.OnSpidersChanged -= OnSpidersChanged;
    }

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────

    void OnSpidersChanged(int killed, int total)
    {
        if (!isOpen && killed >= requiredKills)
            OpenPath();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void OpenPath()
    {
        if (isOpen) return;
        isOpen = true;
        StartCoroutine(AnimateOpen());
    }

    public void ClosePath()
    {
        isOpen = false;
        StopAllCoroutines();

        for (int i = 0; i < leftTrees.Count; i++)
            if (leftTrees[i] != null && i < _leftStart.Count)
                leftTrees[i].position = _leftStart[i];

        for (int i = 0; i < rightTrees.Count; i++)
            if (rightTrees[i] != null && i < _rightStart.Count)
                rightTrees[i].position = _rightStart[i];
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    IEnumerator AnimateOpen()
    {
        if (openSound != null && _audio != null)
            _audio.PlayOneShot(openSound);
        else if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, transform.position);

        if (openEffect != null)
            openEffect.Play();

        Vector3 dir = slideDirection.normalized;

        // Stagger from centre outward — interleave left and right trees
        int maxCount = Mathf.Max(leftTrees.Count, rightTrees.Count);
        for (int i = 0; i < maxCount; i++)
        {
            // Start left tree i sliding
            if (i < leftTrees.Count && leftTrees[i] != null)
                StartCoroutine(SlideTree(leftTrees[i], _leftStart[i], _leftStart[i] - dir * slideDistance));

            // Start right tree i sliding
            if (i < rightTrees.Count && rightTrees[i] != null)
                StartCoroutine(SlideTree(rightTrees[i], _rightStart[i], _rightStart[i] + dir * slideDistance));

            if (i < maxCount - 1)
                yield return new WaitForSeconds(staggerDelay);
        }

        // Wait for all slides to finish
        yield return new WaitForSeconds(slideDuration);

        OnPathOpened?.Invoke();
        Debug.Log("[SlidingTreeObstruction] Path is now open!");
    }

    IEnumerator SlideTree(Transform tree, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t  = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            tree.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        tree.position = to;
    }
}