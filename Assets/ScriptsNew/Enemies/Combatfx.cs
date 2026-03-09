// ─────────────────────────────────────────────────────────────────────────────
//  CombatFX.cs
//
//  Centralised combat visual effects manager:
//    • Hit Stop     — freezes time briefly on a heavy hit for satisfying impact
//    • Screen Flash — red/white UI overlay when Kitty takes damage
//    • Hit Sparks   — particle burst spawned at the point of contact
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create an empty GameObject, name it "CombatFX", attach this script
//
//  2. HIT SPARKS
//     a. GameObject → Effects → Particle System
//     b. Configure it (see recommended settings below)
//     c. Drag it into the Hit Spark Prefab field in the Inspector
//     d. Make sure "Play On Awake" is OFF and "Stop Action" = Destroy
//
//  3. SCREEN FLASH
//     a. In your scene's Canvas (Screen Space — Overlay):
//        Right-click → UI → Image
//        Name it "DamageFlash"
//        Set Rect Transform to stretch full screen (Alt+click the anchor preset → stretch all)
//        Set Image colour to red (R:255 G:0 B:0 A:0)
//        Make sure Raycast Target is OFF
//     b. Drag this Image into the Flash Image field
//
//  4. Wire up — in PlayerCombat's ApplyHit and PlayerStats' TakeDamage,
//     call CombatFX.Instance methods (examples shown at bottom of file)
//
//  RECOMMENDED PARTICLE SETTINGS FOR HIT SPARKS
//  ─────────────────────────────────────────────────────────────────────────────
//  Duration: 0.3   Loop: OFF   Start Lifetime: 0.2–0.4   Start Speed: 4–8
//  Start Size: 0.05–0.15   Start Colour: orange→yellow gradient
//  Emission: Burst — Count 12–20 at time 0
//  Shape: Sphere, Radius 0.1
//  Renderer: Billboard or Stretched Billboard
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CombatFX : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────

    public static CombatFX Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  INSPECTOR — HIT STOP
    // ─────────────────────────────────────────────

    [Header("Hit Stop")]
    [Tooltip("How long time freezes on a light attack hit (seconds).")]
    public float lightHitStopDuration  = 0.04f;

    [Tooltip("How long time freezes on a heavy attack hit (seconds).")]
    public float heavyHitStopDuration  = 0.08f;

    [Tooltip("How long time freezes when Kitty takes damage (seconds).")]
    public float damageHitStopDuration = 0.05f;

    [Tooltip("timescale during hit stop — 0 = full freeze, 0.05 = near freeze.")]
    [Range(0f, 0.1f)]
    public float hitStopTimeScale      = 0f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — SCREEN FLASH
    // ─────────────────────────────────────────────

    [Header("Screen Flash")]
    [Tooltip("The fullscreen UI Image used for the damage flash overlay.")]
    public Image flashImage;

    [Tooltip("Colour of the flash when Kitty takes damage.")]
    public Color damageFlashColour  = new Color(1f, 0f, 0f, 0.45f);

    [Tooltip("Colour of the flash when Kitty lands a heavy hit.")]
    public Color heavyHitFlashColour = new Color(1f, 1f, 1f, 0.2f);

    [Tooltip("How quickly the flash fades out.")]
    public float flashFadeSpeed     = 6f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — HIT SPARKS
    // ─────────────────────────────────────────────

    [Header("Hit Sparks")]
    [Tooltip("Particle prefab spawned when Kitty's attack lands on an enemy.")]
    public ParticleSystem hitSparkPrefab;

    [Tooltip("Particle prefab spawned when Kitty takes damage.")]
    public ParticleSystem damageSparkPrefab;

    [Tooltip("Offset upward from the hit position so sparks don't clip into ground.")]
    public float sparkHeightOffset = 0.5f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    Coroutine _hitStopCoroutine;
    Coroutine _flashCoroutine;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Start flash invisible
        if (flashImage != null)
            flashImage.color = Color.clear;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>Call this when Kitty lands a light attack on an enemy.</summary>
    public void OnLightHit(Vector3 hitPosition)
    {
        TriggerHitStop(lightHitStopDuration);
        SpawnSparks(hitSparkPrefab, hitPosition);
    }

    /// <summary>Call this when Kitty lands a heavy attack on an enemy.</summary>
    public void OnHeavyHit(Vector3 hitPosition)
    {
        TriggerHitStop(heavyHitStopDuration);
        SpawnSparks(hitSparkPrefab, hitPosition);
        TriggerFlash(heavyHitFlashColour);
    }

    /// <summary>Call this when Kitty takes damage from an enemy.</summary>
    public void OnKittyDamaged(Vector3 hitPosition)
    {
        TriggerHitStop(damageHitStopDuration);
        TriggerFlash(damageFlashColour);
        SpawnSparks(damageSparkPrefab, hitPosition);
    }

    /// <summary>Manual hit stop with custom duration.</summary>
    public void TriggerHitStop(float duration)
    {
        if (_hitStopCoroutine != null)
            StopCoroutine(_hitStopCoroutine);

        _hitStopCoroutine = StartCoroutine(HitStopCoroutine(duration));
    }

    /// <summary>Manual screen flash with custom colour.</summary>
    public void TriggerFlash(Color colour)
    {
        if (flashImage == null) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashCoroutine(colour));
    }

    /// <summary>Spawn hit sparks at a world position.</summary>
    public void SpawnSparks(ParticleSystem prefab, Vector3 worldPosition)
    {
        if (prefab == null) return;

        Vector3 spawnPos = worldPosition + Vector3.up * sparkHeightOffset;
        var     ps       = Instantiate(prefab, spawnPos, Quaternion.identity);
        ps.Play();
        // Auto-destroy handled by particle Stop Action = Destroy
        // Fallback destroy in case Stop Action isn't set
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
    }

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────

    IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale     = hitStopTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Use unscaled time since timeScale is near 0
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    IEnumerator FlashCoroutine(Color targetColour)
    {
        // Snap to full flash
        flashImage.color = targetColour;

        // Fade out
        float alpha = targetColour.a;
        while (alpha > 0f)
        {
            alpha            -= Time.unscaledDeltaTime * flashFadeSpeed;
            flashImage.color  = new Color(targetColour.r, targetColour.g, targetColour.b,
                                          Mathf.Max(alpha, 0f));
            yield return null;
        }

        flashImage.color = Color.clear;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  HOW TO CALL FROM OTHER SCRIPTS
// ─────────────────────────────────────────────────────────────────────────────
//
//  In PlayerCombat.cs → ApplyHit():
//
//      void ApplyHit(Collider hit, float damage, bool applyKnockback)
//      {
//          var damageable = hit.GetComponent<IDamageable>()
//                        ?? hit.GetComponentInParent<IDamageable>();
//
//          if (damageable != null)
//          {
//              damageable.TakeDamage(damage, transform.position);
//
//              // Trigger FX — use OnHeavyHit for heavy attacks
//              CombatFX.Instance?.OnLightHit(hit.transform.position);
//          }
//      }
//
//  In PlayerStats.cs → TakeDamage():
//
//      public void TakeDamage(float amount, Vector3 sourcePosition)
//      {
//          // ... existing damage logic ...
//          CombatFX.Instance?.OnKittyDamaged(sourcePosition);
//      }
// ─────────────────────────────────────────────────────────────────────────────