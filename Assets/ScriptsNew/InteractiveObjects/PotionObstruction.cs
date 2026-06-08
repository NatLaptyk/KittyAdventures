// Potion bottles that slide apart like sliding doors when the Spirit is killed.
// Left bottles slide left, right bottles slide right, opening a path.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionObstruction : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Trigger")]
    [Tooltip("The Spirit's EnemyStats component — path opens when the Spirit dies.")]
    public EnemyStats spiritStats;

    [Header("Bottles")]
    [Tooltip("Bottles that will slide LEFT when the path opens.")]
    public List<Transform> leftBottles  = new List<Transform>();

    [Tooltip("Bottles that will slide RIGHT when the path opens.")]
    public List<Transform> rightBottles = new List<Transform>();

    [Header("Sliding")]
    [Tooltip("How far each side slides away from centre (world units).")]
    public float slideDistance = 5f;

    [Tooltip("The axis the bottles slide along. Default is right (X). " +
             "Change to Vector3.forward if your path runs along Z.")]
    public Vector3 slideDirection = Vector3.right;

    [Tooltip("How long the slide animation takes.")]
    public float slideDuration = 2f;

    [Tooltip("Delay between each bottle starting — creates a wave effect.")]
    public float staggerDelay = 0.12f;

    [Tooltip("Animation curve — ease in/out by default.")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Invisible Wall")]
    [Tooltip("Optional invisible wall to disable when the path opens.")]
    public GameObject invisibleWall;

    [Header("Effects")]
    [Tooltip("Optional particle effect played at centre when path opens.")]
    public ParticleSystem openEffect;

    [Tooltip("Optional audio clip played when bottles start sliding.")]
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

    readonly List<Vector3> _leftStart  = new List<Vector3>();
    readonly List<Vector3> _rightStart = new List<Vector3>();

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        _audio = GetComponent<AudioSource>();

        foreach (var t in leftBottles)
            _leftStart.Add(t != null ? t.position : Vector3.zero);

        foreach (var t in rightBottles)
            _rightStart.Add(t != null ? t.position : Vector3.zero);
    }

    void Start()
    {
        if (spiritStats != null)
            spiritStats.OnDied += OpenPath;
        else
            Debug.LogWarning("[PotionObstruction] No SpiritStats assigned — path won't open on Spirit death.");
    }

    void OnDestroy()
    {
        if (spiritStats != null)
            spiritStats.OnDied -= OpenPath;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

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

        for (int i = 0; i < leftBottles.Count; i++)
            if (leftBottles[i] != null && i < _leftStart.Count)
                leftBottles[i].position = _leftStart[i];

        for (int i = 0; i < rightBottles.Count; i++)
            if (rightBottles[i] != null && i < _rightStart.Count)
                rightBottles[i].position = _rightStart[i];
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

        int maxCount = Mathf.Max(leftBottles.Count, rightBottles.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < leftBottles.Count && leftBottles[i] != null)
                StartCoroutine(SlideBottle(leftBottles[i], _leftStart[i], _leftStart[i] - dir * slideDistance));

            if (i < rightBottles.Count && rightBottles[i] != null)
                StartCoroutine(SlideBottle(rightBottles[i], _rightStart[i], _rightStart[i] + dir * slideDistance));

            if (i < maxCount - 1)
                yield return new WaitForSeconds(staggerDelay);
        }

        yield return new WaitForSeconds(slideDuration);

        OnPathOpened?.Invoke();
        Debug.Log("[PotionObstruction] Path is now open!");
    }

    IEnumerator SlideBottle(Transform bottle, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t   = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            bottle.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        bottle.position = to;
    }
}