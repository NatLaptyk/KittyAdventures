// Blocking trees that transform into small mushrooms when the number puzzle
// is solved correctly. Each tree scales down, swaps its model for a mushroom
// prefab, then scales back up — in a randomised order.
// Listens to NumberTrigger.OnPuzzleSolved automatically — no manual wiring needed.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomObstruction : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Objects")]
    [Tooltip("All tree GameObjects that will transform into mushrooms.")]
    [SerializeField] private List<GameObject> trees = new List<GameObject>();

    [Tooltip("The mushroom prefab to spawn in place of each tree.")]
    [SerializeField] private GameObject mushroomPrefab;

    [Header("Animation")]
    [Tooltip("How long each tree takes to scale down to nothing.")]
    [SerializeField] private float shrinkDuration  = 0.5f;

    [Tooltip("How long each mushroom takes to scale up from nothing.")]
    [SerializeField] private float growDuration    = 0.6f;

    [Tooltip("Pause between a tree disappearing and its mushroom appearing.")]
    [SerializeField] private float swapPause       = 0.1f;

    [Tooltip("Delay between each tree starting its transformation.")]
    [SerializeField] private float staggerDelay    = 0.18f;

    [Tooltip("Final scale of each mushroom. Match to your prefab's intended size.")]
    [SerializeField] private Vector3 mushroomScale = Vector3.one;

    [Tooltip("Animation curve for shrink and grow.")]
    [SerializeField] private AnimationCurve transformCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Effects")]
    [Tooltip("Optional particle effect spawned at each tree's position when it transforms.")]
    [SerializeField] private ParticleSystem transformFX;

    [Tooltip("Optional audio clip played when the transformation begins.")]
    [SerializeField] private AudioClip transformSound;

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

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        TryGetComponent(out _audio);
    }

    void Start()
    {
        NumberTrigger.OnPuzzleSolved += OnPuzzleSolved;
    }

    void OnDestroy()
    {
        NumberTrigger.OnPuzzleSolved -= OnPuzzleSolved;
    }

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────

    void OnPuzzleSolved()
    {
        if (!isOpen)
            OpenPath();
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

        StartCoroutine(TransformAll());
    }

    // ─────────────────────────────────────────────
    //  ANIMATION
    // ─────────────────────────────────────────────

    IEnumerator TransformAll()
    {
        if (transformSound != null)
        {
            if (_audio != null) _audio.PlayOneShot(transformSound);
            else AudioSource.PlayClipAtPoint(transformSound, transform.position);
        }

        // Shuffle tree indices for random order
        List<int> indices = new List<int>();
        for (int i = 0; i < trees.Count; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Kick off each transform coroutine staggered
        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            if (trees[idx] != null)
                StartCoroutine(TransformTree(trees[idx]));

            if (i < indices.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }

        // Wait for the last tree to finish its full animation
        float totalDuration = shrinkDuration + swapPause + growDuration;
        yield return new WaitForSeconds(totalDuration);

        OnPathOpened?.Invoke();
        Debug.Log("[MushroomObstruction] Path is now open!");
    }

    IEnumerator TransformTree(GameObject tree)
    {
        Vector3 originalScale = tree.transform.localScale;
        Vector3 treePos       = tree.transform.position;
        Quaternion treeRot    = tree.transform.rotation;

        // ── Phase 1: Shrink tree to zero ──────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = transformCurve.Evaluate(Mathf.Clamp01(elapsed / shrinkDuration));
            tree.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }
        tree.transform.localScale = Vector3.zero;
        tree.SetActive(false);

        // Spawn FX at tree position
        if (transformFX != null)
        {
            var fx = Instantiate(transformFX, treePos, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + 0.5f);
        }

        // ── Brief pause before mushroom appears ───────────────────────────────
        yield return new WaitForSeconds(swapPause);

        // ── Phase 2: Grow mushroom from zero ──────────────────────────────────
        if (mushroomPrefab != null)
        {
            GameObject mushroom = Instantiate(mushroomPrefab, treePos, treeRot);
            mushroom.transform.localScale = Vector3.zero;

            elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = transformCurve.Evaluate(Mathf.Clamp01(elapsed / growDuration));
                mushroom.transform.localScale = Vector3.Lerp(Vector3.zero, mushroomScale, t);
                yield return null;
            }
            mushroom.transform.localScale = mushroomScale;
        }
    }
}