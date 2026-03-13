// ─────────────────────────────────────────────────────────────────────────────
//  HouseTrigger.cs
//
//  Loads the Victory scene when Kitty enters the trigger zone near her house.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject near Kitty's house → name it "HouseTrigger"
//  2. Add a Collider (BoxCollider or SphereCollider) → check Is Trigger
//  3. Attach this script
//  4. Set Victory Scene Name to "Victory" in the Inspector
//  5. Make sure the Player GameObject (or its root) is on the "Player" layer
// ─────────────────────────────────────────────────────────────────────────────

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