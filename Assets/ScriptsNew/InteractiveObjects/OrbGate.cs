// An interactable object (e.g. a glowing stone, ancient door, totem) that
// Kitty can activate after collecting enough orbs. On interaction it calls
// TreeObstruction.OpenPath(), making the trees sink into the ground.
// Implements IInteractable — works with the existing Interactor / raycast system.

using System.Collections;
using UnityEngine;

public class OrbGate : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Refs")]
    [Tooltip("The TreeObstruction this gate controls.")]
    public TreeObstruction obstruction;

    [Header("Condition")]
    [Tooltip("How many orbs Kitty must have collected before she can open this gate.")]
    public int requiredOrbs = 5;

    [Header("Prompts")]
    [Tooltip("Shown when Kitty has enough orbs.")]
    public string readyPrompt    = "Press E to open the path";

    [Tooltip("Shown when Kitty doesn't have enough orbs yet.")]
    public string notReadyPrompt = "You need {0}/{1} orbs";  // {0} = collected, {1} = required

    [Header("Effects")]
    [Tooltip("Optional particle effect played on the gate when activated.")]
    public ParticleSystem activateFX;

    [Tooltip("Optional audio clip played on activation.")]
    public AudioClip activateSound;

    [Header("Animation")]
    [Tooltip("Optional Transform to animate (e.g. spin or pulse) while locked.")]
    public Transform idleAnimTarget;
    [Tooltip("Rotation speed while locked (degrees per second).")]
    public float idleSpinSpeed = 45f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    bool        _activated = false;
    AudioSource _audio;

    // ─────────────────────────────────────────────
    //  IInteractable
    // ─────────────────────────────────────────────

    public string Prompt
    {
        get
        {
            if (_activated) return "";

            int collected = GameStats.Instance != null ? GameStats.Instance.OrbsCollected : 0;

            if (collected >= requiredOrbs)
                return readyPrompt;

            return string.Format(notReadyPrompt, collected, requiredOrbs);
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        // Always true so the prompt is always visible when looking at the gate
        return !_activated;
    }

    public void Interact(GameObject interactor)
    {
        if (_activated) return;

        int collected = GameStats.Instance != null ? GameStats.Instance.OrbsCollected : 0;
        if (collected < requiredOrbs)
        {
            // Not enough orbs — do nothing, prompt already shows the count
            return;
        }

        _activated = true;
        StartCoroutine(Activate());
    }

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Gently spin the idle anim target while not yet activated
        if (!_activated && idleAnimTarget != null)
            idleAnimTarget.Rotate(Vector3.up, idleSpinSpeed * Time.deltaTime, Space.World);
    }

    // ─────────────────────────────────────────────
    //  ACTIVATION
    // ─────────────────────────────────────────────

    IEnumerator Activate()
    {
        // Play FX
        if (activateFX != null)
            activateFX.Play();

        if (activateSound != null)
        {
            if (_audio != null) _audio.PlayOneShot(activateSound);
            else AudioSource.PlayClipAtPoint(activateSound, transform.position);
        }

        // Brief pause for effect before trees move
        yield return new WaitForSeconds(0.4f);

        // Open the path
        if (obstruction != null)
            obstruction.OpenPath();
        else
            Debug.LogWarning("[OrbGate] No TreeObstruction assigned!");

        // Stop idle spin
        if (idleAnimTarget != null)
            idleAnimTarget.gameObject.SetActive(false);
    }
}