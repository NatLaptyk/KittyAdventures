// Displays orb and spider counts. Shows completion prompts when all collected
// or all killed, then hides the relevant counter.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryHUD : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Orbs")]
    [SerializeField] private GameObject orbRow;
    [SerializeField] private TMP_Text orbText;
    [SerializeField] private Image orbIcon;

    [Header("Spiders")]
    [SerializeField] private GameObject spiderRow;
    [SerializeField] private TMP_Text spiderText;
    [SerializeField] private Image spiderIcon;

    [Header("Announcement")]
    [Tooltip("Large centred TMP text for completion messages. Set inactive in scene.")]
    [SerializeField] private TMP_Text announcementText;

    [Header("Timing")]
    [Tooltip("How long each announcement line stays on screen.")]
    [SerializeField] private float lineDisplayTime  = 2.5f;
    [Tooltip("How long the counter row stays visible after completion before hiding.")]
    [SerializeField] private float rowHideDelay     = 4f;

    [Header("Pop Animation")]
    [SerializeField] private float popScale    = 1.4f;
    [SerializeField] private float popDuration = 0.2f;

    [Header("Colours")]
    [SerializeField] private Color defaultColour   = Color.white;
    [SerializeField] private Color completedColour = new Color(0.4f, 1f, 0.4f);



    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (GameStats.Instance == null)
        {
            Debug.LogWarning("[InventoryHUD] No GameStats found.");
            return;
        }

        GameStats.Instance.OnOrbsChanged       += UpdateOrbs;
        GameStats.Instance.OnSpidersChanged    += UpdateSpiders;
        GameStats.Instance.OnAllOrbsCollected  += OnAllOrbsCollected;
        GameStats.Instance.OnAllSpidersKilled  += OnAllSpidersKilled;
        GameStats.Instance.OnPotionCollected   += OnPotionCollected;

        if (announcementText != null)
            announcementText.gameObject.SetActive(false);

        UpdateOrbs(GameStats.Instance.OrbsCollected,    GameStats.Instance.TotalOrbs);
        UpdateSpiders(GameStats.Instance.SpidersKilled, GameStats.Instance.TotalSpiders);
    }

    void OnDestroy()
    {
        if (GameStats.Instance == null) return;
        GameStats.Instance.OnOrbsChanged      -= UpdateOrbs;
        GameStats.Instance.OnSpidersChanged   -= UpdateSpiders;
        GameStats.Instance.OnAllOrbsCollected -= OnAllOrbsCollected;
        GameStats.Instance.OnAllSpidersKilled -= OnAllSpidersKilled;
        GameStats.Instance.OnPotionCollected  -= OnPotionCollected;
    }

    // ─────────────────────────────────────────────
    //  COUNTER UPDATES
    // ─────────────────────────────────────────────

    void UpdateOrbs(int collected, int total)
    {
        if (orbText != null)
        {
            orbText.text  = $"{collected} / {total}";
            orbText.color = (collected >= total && total > 0) ? completedColour : defaultColour;
        }
        if (collected > 0)
            StartCoroutine(PopAnimation(orbIcon != null ? orbIcon.transform : orbText?.transform));
    }

    void UpdateSpiders(int killed, int total)
    {
        if (spiderText != null)
        {
            spiderText.text  = $"{killed} / {total}";
            spiderText.color = (killed >= total && total > 0) ? completedColour : defaultColour;
        }
        if (killed > 0)
            StartCoroutine(PopAnimation(spiderIcon != null ? spiderIcon.transform : spiderText?.transform));
    }

    // ─────────────────────────────────────────────
    //  COMPLETION SEQUENCES
    // ─────────────────────────────────────────────

    void OnAllOrbsCollected()
    {
        StartCoroutine(OrbCompletionSequence());
    }

    void OnAllSpidersKilled()
    {
        StartCoroutine(SpiderCompletionSequence());
    }

    IEnumerator OrbCompletionSequence()
    {
        yield return StartCoroutine(ShowAnnouncement("All orbs collected!"));
        yield return StartCoroutine(ShowAnnouncement("The Forest has granted you an additional Snack!"));

        yield return new WaitForSeconds(rowHideDelay * 0.3f);
        yield return StartCoroutine(FadeOutRow(orbRow));
    }

    IEnumerator SpiderCompletionSequence()
    {
        yield return StartCoroutine(ShowAnnouncement("You defeated all the spiders!"));
        yield return StartCoroutine(ShowAnnouncement("The path is now clear."));

        yield return new WaitForSeconds(rowHideDelay * 0.3f);
        yield return StartCoroutine(FadeOutRow(spiderRow));
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    IEnumerator ShowAnnouncement(string message)
    {
        if (announcementText == null) yield break;

        announcementText.gameObject.SetActive(true);
        announcementText.text = message;

        Color col = announcementText.color;
        col.a = 0f;
        announcementText.color = col;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            col.a = Mathf.Lerp(0f, 1f, t);
            announcementText.color = col;
            yield return null;
        }

        yield return new WaitForSeconds(lineDisplayTime);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            col.a = Mathf.Lerp(1f, 0f, t);
            announcementText.color = col;
            yield return null;
        }

        announcementText.gameObject.SetActive(false);
    }

    IEnumerator FadeOutRow(GameObject row)
    {
        if (row == null) yield break;

        CanvasGroup cg = row.GetComponent<CanvasGroup>();
        if (cg == null) cg = row.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.6f;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        row.SetActive(false);
    }

    IEnumerator PopAnimation(Transform target)
    {
        if (target == null) yield break;

        Vector3 original = target.localScale;
        Vector3 big      = original * popScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / popDuration;
            target.localScale = Vector3.Lerp(original, big, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / popDuration;
            target.localScale = Vector3.Lerp(big, original, t);
            yield return null;
        }
        target.localScale = original;
    }

    void OnPotionCollected()
    {
        StartCoroutine(PotionSequence());
    }

    IEnumerator PotionSequence()
    {
        yield return StartCoroutine(ShowAnnouncement("You retrieved the Spirit Potion!"));
        yield return StartCoroutine(ShowAnnouncement("Now bring it back home!"));
    }
}