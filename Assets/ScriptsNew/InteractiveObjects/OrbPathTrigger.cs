// ─────────────────────────────────────────────────────────────────────────────
//  OrbPathTrigger.cs
//
//  Place this trigger at the entrance to the orb area.
//  When Kitty enters, BranchingManager locks in Path A (orbs active).
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject at the orb area entrance
//  2. Add a BoxCollider → Is Trigger ✓
//  3. Attach this script
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class OrbPathTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.transform.root.tag != "Player") return;
        BranchingManager.Instance?.ChooseOrbPath();
    }
}