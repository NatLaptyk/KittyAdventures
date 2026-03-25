// ─────────────────────────────────────────────────────────────────────────────
//  SpiderCountPrompt.cs
//
//  An interactable object that shows the spider kill progress to the player.
//  Displays "Defeat X more spiders to clear the path!" until all are killed,
//  then shows a completion message.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Attach to any GameObject (a sign, stone, totem, etc.)
//  2. Add a Collider → check Is Trigger
//  3. Make sure the Interactor can raycast to it
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class SpiderCountPrompt : MonoBehaviour, IInteractable
{
    [Header("Prompts")]
    [Tooltip("Message shown while spiders remain. {0} = remaining, {1} = total.")]
    public string notDonePrompt = "Defeat {0} more spiders to clear the path to the Potion Spirit!";

    [Tooltip("Message shown when all spiders are defeated.")]
    public string donePrompt = "The path is now clear!";

    // ─────────────────────────────────────────────
    //  IInteractable
    // ─────────────────────────────────────────────

    public string Prompt
    {
        get
        {
            if (GameStats.Instance == null) return "";

            int killed = GameStats.Instance.SpidersKilled;
            int total  = GameStats.Instance.TotalSpiders;

            if (killed >= total && total > 0)
                return donePrompt;

            int remaining = total - killed;
            return string.Format(notDonePrompt, remaining, total);
        }
    }

    public bool CanInteract(GameObject interactor) => true;

    public void Interact(GameObject interactor)
    {
        // No action needed — prompt displays the info
    }
}