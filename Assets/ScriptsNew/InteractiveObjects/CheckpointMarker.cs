// ─────────────────────────────────────────────────────────────────────────────
//  CheckpointMarker.cs
//
//  A glowing interactable marker that Kitty must activate as part of the loop.
//  Pulses with an idle glow, changes colour when activated.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create a GameObject for each checkpoint (e.g. a glowing orb or rune)
//  2. Attach this script to it
//  3. Add a SphereCollider set to Is Trigger
//  4. Assign a Renderer (the glowing mesh) to the Glow Renderer field
//  5. Add all CheckpointMarkers to the LoopTracker's Checkpoints list
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using UnityEngine;

public class CheckpointMarker : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Visuals")]
    [Tooltip("The renderer that will glow and change colour.")]
    public Renderer glowRenderer;

    [Tooltip("Colour when idle/waiting to be activated.")]
    public Color idleColour      = new Color(0.2f, 0.8f, 0.2f, 1f);   // green

    [Tooltip("Colour when this checkpoint is the next one to activate.")]
    public Color activeColour    = new Color(1.0f, 0.9f, 0.1f, 1f);   // yellow

    [Tooltip("Colour when successfully activated.")]
    public Color completedColour = new Color(0.2f, 0.4f, 1.0f, 1f);   // blue

    [Tooltip("Colour when wrong order — brief red flash.")]
    public Color wrongColour     = new Color(1.0f, 0.1f, 0.1f, 1f);   // red

    [Header("Pulse")]
    public float pulseSpeed     = 2f;
    public float pulseMinAlpha  = 0.3f;
    public float pulseMaxAlpha  = 1.0f;

    [Header("Interaction")]
    [Tooltip("How close Kitty needs to be to activate this checkpoint.")]
    public float activationRadius = 2f;

    [Header("Particles")]
    [Tooltip("Optional particle effect to play when activated.")]
    public ParticleSystem activationEffect;

    [Header("Pickup")]
    [Tooltip("If true, the orb flies toward Kitty and gets collected when she enters the trigger.")]
    public bool pickupStyle       = true;

    [Tooltip("How fast the orb flies toward Kitty when picked up.")]
    public float flySpeed         = 8f;

    [Tooltip("How long the orb takes to shrink and disappear after being collected.")]
    public float vanishDuration   = 0.4f;

    [Tooltip("Optional sound to play on pickup.")]
    public AudioClip pickupSound;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    /// <summary>Fired when this orb is successfully collected.</summary>
    public static event System.Action OnOrbCollected;

    LoopTracker _tracker;
    bool        _activated  = false;
    bool        _isNext     = false;
    bool        _collecting = false;
    Material    _mat;
    AudioSource _audio;
    Transform   _kittyTransform;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (glowRenderer != null)
        {
            // Use instance material so we don't affect other objects
            _mat = glowRenderer.material;
            SetColour(idleColour);
        }

        _audio = GetComponent<AudioSource>();
        if (_audio == null && pickupSound != null)
            _audio = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (_activated || _mat == null) return;

        // Pulse the emission intensity
        float pulse = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                                 (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        Color baseColour = _isNext ? activeColour : idleColour;
        _mat.SetColor("_EmissionColor", baseColour * pulse * 2f);
    }

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (_activated || _collecting) return;
        if (!other.CompareTag("Player")) return;

        _kittyTransform = other.transform;

        if (pickupStyle)
            StartCoroutine(PickupFly());
        else
            Activate();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void SetTracker(LoopTracker tracker)
    {
        _tracker = tracker;
    }

    public void MarkAsNext(bool isNext)
    {
        _isNext = isNext;
    }

    public void ResetVisual()
    {
        _activated = false;
        _isNext    = false;
        SetColour(idleColour);
    }

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    void Activate()
    {
        if (_tracker == null) return;

        bool valid = _tracker.TryActivate(this);

        if (valid)
        {
            _activated = true;
            SetColour(completedColour);
            OnOrbCollected?.Invoke();

            // Spawn fish snack if on Path A
            if (_kittyTransform != null)
                BranchingManager.Instance?.SpawnFishSnack(
                    _kittyTransform.position,
                    _kittyTransform.forward);

            if (activationEffect != null)
                activationEffect.Play();

            StartCoroutine(ActivationEffect());
        }
        else
        {
            // Wrong order — flash red briefly
            StartCoroutine(WrongOrderFlash());
        }
    }

    IEnumerator PickupFly()
    {
        _collecting = true;

        // Play pickup sound
        if (pickupSound != null && _audio != null)
            _audio.PlayOneShot(pickupSound);

        // Fly toward Kitty
        while (_kittyTransform != null)
        {
            Vector3 dir  = (_kittyTransform.position + Vector3.up * 0.8f) - transform.position;
            float   dist = dir.magnitude;

            // Once close enough — collect
            if (dist < 0.3f)
                break;

            transform.position += dir.normalized * flySpeed * Time.deltaTime;
            yield return null;
        }

        // Detach particle children so they finish playing after orb vanishes
        foreach (Transform child in transform)
        {
            var ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                child.SetParent(null);           // detach from orb
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(child.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        // Also handle the activationEffect if assigned directly
        if (activationEffect != null)
        {
            activationEffect.transform.SetParent(null);
            activationEffect.Play();
            Destroy(activationEffect.gameObject, 
                    activationEffect.main.duration + activationEffect.main.startLifetime.constantMax);
        }

        // Shrink and vanish
        Vector3 originalScale = transform.localScale;
        float   t             = 0f;

        while (t < 1f)
        {
            t                    += Time.deltaTime / vanishDuration;
            transform.localScale  = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // Now register with tracker
        Activate();

        // Hide the mesh but keep the GameObject alive for the tracker
        if (glowRenderer != null) glowRenderer.enabled = false;
    }

    IEnumerator ActivationEffect()
    {
        // Scale up briefly for satisfying pop
        Vector3 original = transform.localScale;
        Vector3 big      = original * 1.4f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.localScale = Vector3.Lerp(original, big, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localScale = Vector3.Lerp(big, original, t);
            yield return null;
        }

        transform.localScale = original;
    }

    IEnumerator WrongOrderFlash()
    {
        SetColour(wrongColour);
        yield return new WaitForSeconds(0.4f);
        SetColour(idleColour);
    }

    void SetColour(Color col)
    {
        if (_mat == null) return;
        _mat.color = col;
        _mat.SetColor("_EmissionColor", col * 2f);
        _mat.EnableKeyword("_EMISSION");
    }
}