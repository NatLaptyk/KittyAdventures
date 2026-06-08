// A group of trees that split apart like sliding doors when enough spiders
// have been killed. Trees in the LEFT list slide left, trees in the RIGHT
// list slide right, opening a gap in the centre.
// Triggered automatically by GameStats.OnAllSpidersKilled.

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
    [SerializeField] private List<Transform> leftTrees  = new List<Transform>();

    [Tooltip("Trees that will slide RIGHT when the path opens.")]
    [SerializeField] private List<Transform> rightTrees = new List<Transform>();

    [Header("Sliding")]
    [Tooltip("How far each side slides away from centre (world units).")]
    [SerializeField] private float slideDistance = 5f;

    [Tooltip("The axis the trees slide along. Default is right (X). " +
             "Change to Vector3.forward if your path runs along Z.")]
    [SerializeField] private Vector3 slideDirection = Vector3.right;

    [Tooltip("How long the slide animation takes.")]
    [SerializeField] private float slideDuration = 2f;

    [Tooltip("Delay between each tree starting — creates a wave effect from centre outward.")]
    [SerializeField] private float staggerDelay = 0.12f;

    [Tooltip("Animation curve — ease in/out by default.")]
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Trigger")]
    [Tooltip("If assigned, path opens when the Spirit dies.")]
    [SerializeField] private EnemyStats spiritStats;
    [Tooltip("If true, path opens when all spiders are killed.")]
    [SerializeField] private bool openOnAllSpidersKilled = false;

    [Header("Effects")]
    [Tooltip("Optional particle effect at the centre when it opens.")]
    [SerializeField] private ParticleSystem openEffect;

    [Tooltip("Optional audio clip played when trees start sliding.")]
    [SerializeField] private AudioClip openSound;
    [Range(0f, 1f)]
    [SerializeField] private float openSoundVolume = 1f;

    [Header("State")]
    [SerializeField] private bool isOpen = false;

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
        TryGetComponent(out _audio);

        foreach (var t in leftTrees)
            _leftStart.Add(t != null ? t.position : Vector3.zero);

        foreach (var t in rightTrees)
            _rightStart.Add(t != null ? t.position : Vector3.zero);
    }

    void Start()
    {
        if (spiritStats != null)
            spiritStats.OnDied += OpenPath;

        if (openOnAllSpidersKilled && GameStats.Instance != null)
            GameStats.Instance.OnAllSpidersKilled += OpenPath;
        else if (!openOnAllSpidersKilled && spiritStats == null)
            Debug.LogWarning("[SlidingTreeObstruction] No trigger assigned — assign Spirit Stats or check Open On All Spiders Killed.");
    }

    void OnDestroy()
    {
        if (spiritStats != null)
            spiritStats.OnDied -= OpenPath;

        if (openOnAllSpidersKilled && GameStats.Instance != null)
            GameStats.Instance.OnAllSpidersKilled -= OpenPath;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    [Header("Invisible Wall")]
    [Tooltip("The invisible wall blocking spiders — disabled when the path opens.")]
    [SerializeField] private GameObject invisibleWall;

    public void OpenPath()
    {
        if (isOpen) return;
        isOpen = true;

        if (invisibleWall != null)
            invisibleWall.SetActive(false);

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
            _audio.PlayOneShot(openSound, openSoundVolume);
        else if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, transform.position, openSoundVolume);

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