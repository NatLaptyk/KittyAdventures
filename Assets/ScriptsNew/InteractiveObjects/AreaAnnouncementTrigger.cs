// ─────────────────────────────────────────────────────────────────────────────
//  AreaAnnouncementTrigger.cs
//
//  Shows an announcement message via InventoryHUD when Kitty enters the trigger.
//  Can optionally only show if a referenced SlidingTreeObstruction is already open.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject near the MushroomObstruction area
//  2. Add a Collider → check Is Trigger → size it to cover the approach area
//  3. Attach this script
//  4. Assign the SlidingTreeObstruction (boss arena gate) to the
//     Required Open Obstruction field
//  5. Set your message in the Inspector
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;

public class AreaAnnouncementTrigger : MonoBehaviour
{
    [Header("Message")]
    [Tooltip("The message to display when Kitty enters this area.")]
    public string message = "Find the Potion Spirit at the Potion entrance!";

    [Tooltip("How long the message stays on screen.")]
    public float displayDuration = 3f;

    [Header("Condition")]
    [Tooltip("If assigned, the message only shows when this obstruction is already open.")]
    public SlidingTreeObstruction requiredOpenObstruction;

    [Tooltip("If true, the message only shows once per session.")]
    public bool showOnce = true;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    bool _shown = false;

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.transform.root.tag != "Player") return;
        if (showOnce && _shown) return;

        // Check condition — only show if required obstruction is open
        if (requiredOpenObstruction != null && !requiredOpenObstruction.isOpen) return;

        _shown = true;
        StartCoroutine(ShowMessage());
    }

    IEnumerator ShowMessage()
    {
        // Find InventoryHUD and use its ShowAnnouncement
        var hud = FindFirstObjectByType<InventoryHUD>();
        if (hud == null) yield break;

        // Use reflection to call the private ShowAnnouncement coroutine
        yield return StartCoroutine(hud.ShowAnnouncementPublic(message, displayDuration));
    }
}