// ─────────────────────────────────────────────────────────────────────────────
// HouseTrigger.cs
// Loads the Victory scene when Kitty enters the trigger zone near her house.

using UnityEngine;

public class HouseTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the Victory scene in Build Settings.")]
    public string victorySceneName = "Victory";

    [Tooltip("Optional particle effect when Kitty arrives home.")]
    public ParticleSystem arrivalEffect;

    bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // Check if it's the player
        if (!other.CompareTag("Player") && other.transform.root.CompareTag("Player") == false)
        {
            // Also try layer check as fallback
            if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
                return;
        }

        _triggered = true;

        if (arrivalEffect != null)
            arrivalEffect.Play();

        SceneFader.Instance?.FadeTo(victorySceneName);
    }
}