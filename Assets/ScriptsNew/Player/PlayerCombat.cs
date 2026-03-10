// ─────────────────────────────────────────────────────────────────────────────
//  PlayerCombat.cs
//
//  Handles all of Kitty's combat: light attack, heavy attack, and parry.
//  Reads mouse input directly via the new Input System.
//
//  HOW ATTACKS WORK
//  ─────────────────────────────────────────────────────────────────────────
//  Rather than using raycasts or projectiles, attacks use an OverlapSphere —
//  an invisible sphere in front of Kitty that detects any enemy colliders
//  within range at the moment of impact. This is simple, reliable, and feels
//  good for a close-quarters action game.
//
//  PARRY WINDOW
//  ─────────────────────────────────────────────────────────────────────────
//  When the player holds Right Mouse and an enemy hits Kitty within the parry
//  window, the damage is negated and the attacker is briefly stunned.
//  Outside the parry window, Right Mouse performs a heavy attack instead.
//
//  REQUIRES on the same GameObject:
//    - PlayerStats  (for stamina checks)
//    - InputReader  (registered as a component but attacks bypass it —
//                   mouse is read directly for responsiveness)
//    - CameraController must exist in the scene (for shake)
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to Kitty's root GameObject.
//  2. Set enemyLayers to your Enemy layer mask in the Inspector.
//  3. Optionally assign an Animator.
//  4. Place Kitty on a layer that does NOT include enemyLayers.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR — LIGHT ATTACK
    // ─────────────────────────────────────────────

    [Header("Light Attack")]
    [Tooltip("Damage dealt per light hit")]
    public float lightDamage      = 10f;

    [Tooltip("Radius of the hit detection sphere in front of Kitty")]
    public float lightRange       = 1.4f;

    [Tooltip("Total duration of the light attack animation window")]
    public float lightDuration    = 0.35f;

    [Tooltip("How many light attacks can chain before resetting")]
    public int   maxCombo         = 3;

    [Tooltip("Time after last light attack before combo resets")]
    public float comboResetTime   = 0.7f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — HEAVY ATTACK
    // ─────────────────────────────────────────────

    [Header("Heavy Attack")]
    [Tooltip("Damage dealt by a heavy hit")]
    public float heavyDamage      = 28f;

    [Tooltip("Radius of heavy hit detection — wider arc than light")]
    public float heavyRange       = 1.8f;

    [Tooltip("Arc angle of the heavy attack sweep in degrees")]
    public float heavyArc         = 130f;

    [Tooltip("Force applied to knocked-back enemies")]
    public float knockbackForce   = 8f;

    [Tooltip("Total duration of the heavy attack animation window")]
    public float heavyDuration    = 0.6f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — PARRY
    // ─────────────────────────────────────────────

    [Header("Parry")]
    [Tooltip("How long the parry window stays active after pressing block")]
    public float parryWindow      = 0.4f;

    [Tooltip("How long a successfully parried enemy is stunned")]
    public float parryStunTime    = 2f;

    [Tooltip("Stamina cost to attempt a parry")]
    public float parryStaminaCost = 20f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — FEEL
    // ─────────────────────────────────────────────

    [Header("Hit Feel")]
    [Tooltip("Time scale during hit stop — lower = more dramatic freeze")]
    public float hitStopTimeScale  = 0.05f;

    [Tooltip("How long the hit stop lasts in real time")]
    public float hitStopDuration   = 0.06f;

    [Tooltip("Camera shake force on heavy hit")]
    public float heavyShakeForce   = 0.45f;

    // ─────────────────────────────────────────────
    //  INSPECTOR — REFERENCES
    // ─────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Layer mask for enemy detection — set to your Enemy layer")]
    public LayerMask enemyLayers;

    [Tooltip("Optional — assign if Kitty has an Animator")]
    public Animator animator;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    PlayerStats      _stats;
    CameraController _cam;

    // Combat state machine — prevents overlapping attacks
    enum CombatState { Free, Attacking, Parrying }
    CombatState _state = CombatState.Free;

    // Combo tracking
    int   _comboCount   = 0;
    float _comboTimer   = 0f;

    // Parry
    bool  _parryActive  = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        _stats = GetComponent<PlayerStats>();
        _cam   = FindFirstObjectByType<CameraController>();

        // Auto-find animator if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // TEMP DEBUG — remove once working
        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
            Debug.Log("[PlayerCombat] Update sees middle mouse!");

        TickComboTimer();
        ReadInput();
    }

    // ─────────────────────────────────────────────
    //  INPUT  —  read mouse directly each frame
    // ─────────────────────────────────────────────

    void ReadInput()
    {
        // Only accept new actions when free
        if (_state != CombatState.Free) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Left Mouse = light attack
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(LightAttack());
            AudioManager.instance.PlaySFX(AudioManager.instance.lightAttack, 0f);
        }

        // Right Mouse = heavy attack
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            StartCoroutine(HeavyAttack());
            AudioManager.instance.PlaySFX(AudioManager.instance.heavyAttack, 0f);
        }

        // Middle Mouse OR Q key = parry / block
        bool blockPressed = mouse.middleButton.wasPressedThisFrame
                         || Keyboard.current.qKey.wasPressedThisFrame;
        if (blockPressed)
        {
            Debug.Log($"[Combat] Block pressed | state={_state} | stamina={(_stats != null ? _stats.Stamina : -1)}");
            if (_state != CombatState.Free)
                Debug.Log("[Combat] Blocked by state: " + _state);
            else if (_stats == null)
                Debug.Log("[Combat] _stats is NULL — PlayerStats not found!");
            else if (!_stats.SpendStamina(parryStaminaCost))
                Debug.Log($"[Combat] Not enough stamina — need {parryStaminaCost}, have {_stats.Stamina}");
            else
                StartCoroutine(Parry());
        }
    }

    // ─────────────────────────────────────────────
    //  COMBO TIMER  —  resets combo if too slow
    // ─────────────────────────────────────────────

    void TickComboTimer()
    {
        if (_comboCount > 0 && _state == CombatState.Free)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
                _comboCount = 0;
        }
    }

    // ─────────────────────────────────────────────
    //  LIGHT ATTACK
    //
    //  Advances the combo on each press. The hit detection fires
    //  partway through the animation so it feels like the moment
    //  of impact rather than firing instantly on press.
    // ─────────────────────────────────────────────

    IEnumerator LightAttack()
    {
        _state = CombatState.Attacking;

        // Advance combo — clamp so it wraps back after maxCombo
        _comboCount = (_comboCount % maxCombo) + 1;
        _comboTimer = comboResetTime;

        animator?.SetTrigger("isLAttack");
        // ComboIndex not in existing animator — skipping

        // Wait for the "impact" moment — roughly 40% through the animation
        yield return new WaitForSeconds(lightDuration * 0.4f);

        // Detect enemies in front of Kitty
        HitEnemiesInSphere(lightRange, lightDamage, applyKnockback: false);

        // Wait for the recovery to finish
        yield return new WaitForSeconds(lightDuration * 0.6f);

        _state = CombatState.Free;
    }

    // ─────────────────────────────────────────────
    //  HEAVY ATTACK
    //
    //  Wide arc swing with knockback, hit stop and camera shake.
    //  Has a wind-up delay before the hit fires, which makes it
    //  feel weighty and telegraphed (enemies can dodge it).
    // ─────────────────────────────────────────────

    IEnumerator HeavyAttack()
    {
        _state = CombatState.Attacking;
        _comboCount = 0;  // heavy attack resets combo

        animator?.SetTrigger("isHAttack");

        // Wind-up — the player is committed once they press
        yield return new WaitForSeconds(heavyDuration * 0.4f);

        // Detect enemies in a wide arc
        bool hitAnything = HitEnemiesInArc(heavyRange, heavyArc,
                                            heavyDamage, applyKnockback: true);

        // Only apply dramatic effects if something was actually hit
        if (hitAnything)
        {
            _cam?.Shake(heavyShakeForce);
            StartCoroutine(HitStop());
        }

        // Recovery
        yield return new WaitForSeconds(heavyDuration * 0.6f);

        _state = CombatState.Free;
    }

    // ─────────────────────────────────────────────
    //  PARRY
    //
    //  Opens a brief parry window. If an enemy hits Kitty during
    //  this window (detected via the IDamageable.TakeDamage path
    //  being intercepted), the hit is negated and the enemy is stunned.
    //
    //  We implement this by temporarily making Kitty invincible AND
    //  flagging _parryActive so TakeDamage can check it.
    // ─────────────────────────────────────────────

    IEnumerator Parry()
    {
        _state = CombatState.Parrying;
        _parryActive = true;

        animator?.SetBool("isBlock", true);

        // Parry window
        yield return new WaitForSeconds(parryWindow);

        _parryActive = false;

        // Exit block animation
        animator?.SetBool("isBlock", false);

        // Recovery
        yield return new WaitForSeconds(0.2f);

        _state = CombatState.Free;
    }

    // ─────────────────────────────────────────────
    //  HIT DETECTION HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detects all enemies within a sphere directly in front of Kitty
    /// and applies damage to each. Returns true if anything was hit.
    /// </summary>
    bool HitEnemiesInSphere(float range, float damage, bool applyKnockback)
    {
        // The attack origin sits slightly in front of and above Kitty's feet
        Vector3 origin = transform.position
                       + transform.forward * (range * 0.5f)
                       + Vector3.up * 0.6f;

        Collider[] hits = Physics.OverlapSphere(origin, range, enemyLayers);

        foreach (var hit in hits)
            ApplyHit(hit, damage, applyKnockback);

        return hits.Length > 0;
    }

    /// <summary>
    /// Detects enemies within an arc sweep — used for heavy attacks
    /// so only enemies in front of Kitty are affected, not behind her.
    /// </summary>
    bool HitEnemiesInArc(float range, float arcDegrees, float damage, bool applyKnockback)
    {
        Vector3    origin = transform.position + Vector3.up * 0.6f;
        Collider[] hits   = Physics.OverlapSphere(origin, range, enemyLayers);

        bool hitAny = false;
        foreach (var hit in hits)
        {
            // Check if the enemy is within the arc angle
            Vector3 toEnemy = (hit.transform.position - origin).normalized;
            float   angle   = Vector3.Angle(transform.forward, toEnemy);

            if (angle <= arcDegrees * 0.5f)
            {
                ApplyHit(hit, damage, applyKnockback);
                hitAny = true;
            }
        }

        return hitAny;
    }

    /// <summary>
    /// Applies damage and optional knockback to a single enemy collider.
    /// </summary>
    void ApplyHit(Collider hit, float damage, bool applyKnockback)
    {
        // Check the hit collider first, then walk up to parent in case
        // EnemyStats is on the root but the collider is on a child object
        var damageable = hit.GetComponent<IDamageable>()
                      ?? hit.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage, transform.position);

            // Trigger combat FX — heavy attacks use the stronger effect
            bool isHeavy = damage >= heavyDamage;
            if (isHeavy)
                CombatFX.Instance?.OnHeavyHit(hit.transform.position);
            else
                CombatFX.Instance?.OnLightHit(hit.transform.position);
        }

        Debug.Log($"[Combat] Hit {hit.gameObject.name} damageable={damageable != null}");

        if (applyKnockback)
        {
            var rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  HIT STOP
    //
    //  Briefly slows time to near-zero on a heavy hit landing.
    //  Uses real time (unscaled) for the wait so the freeze
    //  ends even though Time.timeScale is near zero.
    // ─────────────────────────────────────────────

    IEnumerator HitStop()
    {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true if Kitty is currently in a parry window.
    /// Called by EnemyAI when it deals damage to check if the hit is parried.
    /// </summary>
    public bool IsParrying => _parryActive;

    /// <summary>Whether Kitty is currently busy with an attack or parry.</summary>
    public bool IsBusy => _state != CombatState.Free;

    // ─────────────────────────────────────────────
    //  GIZMOS  —  visualise hit ranges in editor
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position
                       + transform.forward * (lightRange * 0.5f)
                       + Vector3.up * 0.6f;

        // Light attack range — blue sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, lightRange);

        // Heavy attack range — orange sphere
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, heavyRange);
    }
}