// ─────────────────────────────────────────────────────────────────────────────
//  MushroomPathTrigger.cs
//
//  Place this trigger at the entrance to the mushroom puzzle area.
//  When Kitty enters WITHOUT having gone to the orb area first,
//  BranchingManager locks in Path B (orbs disappear, puzzle opens gate).
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject at the mushroom area entrance
//  2. Add a BoxCollider → Is Trigger ✓
//  3. Attach this script
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class MushroomPathTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.transform.root.tag != "Player") return;
        BranchingManager.Instance?.ChooseMushroomPath();
    }
}