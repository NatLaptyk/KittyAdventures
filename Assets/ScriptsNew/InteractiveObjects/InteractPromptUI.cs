// ─────────────────────────────────────────────────────────────────────────────
// InteractPromptUI.cs
// Attach to a GameObject inside your Canvas that contains a TMP text element.
// Interactor calls Set() to show/hide the prompt automatically.

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