// ─────────────────────────────────────────────────────────────────────────────
//  RespawnCheckpoint.cs
//
//  When Kitty enters this trigger, it updates the DeathScreen's respawn point
//  so she respawns here if she dies. Perfect for the boss arena entrance.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject at the boss arena entrance
//  2. Add a Collider → check Is Trigger
//  3. Attach this script
//  4. The respawn position is this GameObject's position by default,
//     or assign a separate Respawn Position Transform in the Inspector
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class RespawnCheckpoint : MonoBehaviour
{
    [Tooltip("Where Kitty respawns if she dies after hitting this checkpoint. " +
             "Leave empty to use this GameObject's position.")]
    public Transform respawnPosition;

    [Tooltip("Optional particle effect when checkpoint is activated.")]
    public ParticleSystem activationEffect;

    [Tooltip("Optional audio clip when checkpoint is activated.")]
    public AudioClip activationSound;

    bool _activated = false;

    void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player") && other.transform.root.tag != "Player") return;

        _activated = true;

        // Find DeathScreen and update its respawn point
        DeathScreen deathScreen = FindFirstObjectByType<DeathScreen>();
        if (deathScreen != null)
        {
            deathScreen.respawnPoint = respawnPosition != null ? respawnPosition : transform;
            Debug.Log("[RespawnCheckpoint] Respawn point updated to: " + transform.name);
        }
        else
        {
            Debug.LogWarning("[RespawnCheckpoint] No DeathScreen found in scene!");
        }

        // Play effects
        if (activationEffect != null)
            activationEffect.Play();

        if (activationSound != null)
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
    }
}