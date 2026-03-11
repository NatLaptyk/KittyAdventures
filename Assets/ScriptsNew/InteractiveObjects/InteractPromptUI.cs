// ─────────────────────────────────────────────────────────────────────────────
//  InteractPromptUI.cs
//
//  Attach to a GameObject inside your Canvas that contains a TMP text element.
//  Interactor calls Set() to show/hide the prompt automatically.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. In your Canvas, create an empty GameObject → name it "InteractPrompt"
//  2. Add a TextMeshProUGUI component to it (or a child of it)
//  3. Attach this script to the same GameObject
//  4. Wire the Text field in the Inspector to your TMP text
//  5. DISABLE the GameObject in the scene — the script will show/hide it
//  6. Drag this GameObject into Interactor's Prompt UI field on Kitty
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    public void Set(string prompt)
    {
        if (text == null) return;
        bool show = !string.IsNullOrWhiteSpace(prompt);
        text.text = prompt;
        gameObject.SetActive(show);
    }
}