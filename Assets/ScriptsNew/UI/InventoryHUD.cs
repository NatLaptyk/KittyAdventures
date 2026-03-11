// ─────────────────────────────────────────────────────────────────────────────
//  InventoryHUD.cs
//
//  Displays orb and spider counts. Shows completion prompts when all collected
//  or all killed, then hides the relevant counter.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  Canvas
//    └── InventoryPanel
//          ├── OrbRow          ← set active, hides after all orbs collected
//          │     ├── OrbIcon   (Image)
//          │     └── OrbText   (TMP — "✦ 0 / 3")
//          ├── SpiderRow       ← set active, hides after all spiders killed
//          │     ├── SpiderIcon (Image)
//          │     └── SpiderText (TMP — "✕ 0 / 2")
//          └── AnnouncementText (TMP — large centred text, set INACTIVE by default)
//
//  Attach InventoryHUD.cs to InventoryPanel and wire all fields.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class InventoryHUD : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Orbs")]
    public GameObject orbRow;
    public TMP_Text   orbText;
    public Image      orbIcon;

    [Header("Spiders")]
    public GameObject spiderRow;
    public TMP_Text   spiderText;
    public Image      spiderIcon;

    [Header("Announcement")]
    [Tooltip("Large centred TMP text for completion messages. Set inactive in scene.")]
    public TMP_Text announcementText;

    [Header("Format")]
    public string orbFormat    = "✦ {0} / {1}";
    public string spiderFormat = "✕ {0} / {1}";

    [Header("Timing")]
    [Tooltip("How long each announcement line stays on screen.")]
    public float lineDisplayTime  = 2.5f;
    [Tooltip("How long the counter row stays visible after completion before hiding.")]
    public float rowHideDelay     = 4f;

    [Header("Pop Animation")]
    public float popScale    = 1.4f;
    public float popDuration = 0.2f;

    [Header("Colours")]
    public Color defaultColour   = Color.white;
    public Color completedColour = new Color(0.4f, 1f, 0.4f);

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    [Header("Game Completion")]
    [SerializeField] private string endSceneName = "EndScene";
    [SerializeField] private float  endSceneDelay = 3f;

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
            orbText.text  = string.Format(orbFormat, collected, total);
            orbText.color = (collected >= total && total > 0) ? completedColour : defaultColour;
        }
        if (collected > 0)
            StartCoroutine(PopAnimation(orbIcon != null ? orbIcon.transform : orbText?.transform));
    }

    void UpdateSpiders(int killed, int total)
    {
        if (spiderText != null)
        {
            spiderText.text  = string.Format(spiderFormat, killed, total);
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
        // Line 1
        yield return StartCoroutine(ShowAnnouncement("All orbs collected!"));

        // Line 2 — waits for TreeObstruction to start opening
        yield return StartCoroutine(ShowAnnouncement("The path is opening..."));

        // Listen for path fully open — meanwhile show waiting message
        bool pathOpen = false;
        if (TreeObstruction.Instance != null)
            TreeObstruction.Instance.OnPathOpened += () => pathOpen = true;

        // Wait until path is open or timeout after 5s
        float waited = 0f;
        while (!pathOpen && waited < 5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // Line 3
        yield return StartCoroutine(ShowAnnouncement("The path is open!"));

        // Hide orb row
        yield return new WaitForSeconds(rowHideDelay * 0.3f);
        yield return StartCoroutine(FadeOutRow(orbRow));
    }

    IEnumerator SpiderCompletionSequence()
    {
        yield return StartCoroutine(ShowAnnouncement("You killed all the spiders!"));
        yield return StartCoroutine(ShowAnnouncement("The path is now clear."));

        // Hide spider row
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

        // Fade in
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

        // Fade out
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
        yield return new WaitForSeconds(endSceneDelay);
        SceneManager.LoadScene(endSceneName);
    }

}