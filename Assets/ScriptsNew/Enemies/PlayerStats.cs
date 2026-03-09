// ─────────────────────────────────────────────────────────────────────────────
//  PlayerStats.cs
//
//  Kitty's health and stamina. Raises C# events for the HUD to listen to.
//  Implements IDamageable so enemies can damage Kitty directly.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  Attach to Kitty's root GameObject.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Health")]
    public float maxHealth          = 100f;

    [Header("Stamina")]
    public float maxStamina         = 100f;
    public float staminaRegenRate   = 20f;   // per second
    public float staminaRegenDelay  = 1.2f;  // seconds after use before regen starts

    [Header("Invincibility")]
    [Tooltip("Brief invincibility after being hit — prevents instant death from multiple hits")]
    public float invincibilityTime  = 0.5f;

    // ─────────────────────────────────────────────
    //  PUBLIC STATE
    // ─────────────────────────────────────────────

    public float Health  { get; private set; }
    public float Stamina { get; private set; }
    public bool  IsDead  { get; private set; }

    // ─────────────────────────────────────────────
    //  EVENTS  — subscribe in HUD, GameManager etc.
    // ─────────────────────────────────────────────

    public event Action<float, float> OnHealthChanged;   // (current, max)
    public event Action<float, float> OnStaminaChanged;  // (current, max)
    public event Action               OnDied;
    public event Action<float>        OnDamaged;         // (amount)

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    float _staminaRegenTimer  = 0f;
    float _invincibilityTimer = 0f;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        Health  = maxHealth;
        Stamina = maxStamina;
    }

    void Start()
    {
        // Fire initial values so HUD starts correctly
        OnHealthChanged?.Invoke(Health, maxHealth);
        OnStaminaChanged?.Invoke(Stamina, maxStamina);
    }

    void Update()
    {
        if (IsDead) return;
        TickInvincibility();
        RegenStamina();
    }

    // ─────────────────────────────────────────────
    //  IDAMAGEABLE — called by enemies
    // ─────────────────────────────────────────────

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (IsDead) return;
        if (_invincibilityTimer > 0f) return;  // invincible — ignore hit

        Health = Mathf.Max(0f, Health - amount);
        _invincibilityTimer = invincibilityTime;

        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(Health, maxHealth);

        if (Health <= 0f)
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void Heal(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        OnHealthChanged?.Invoke(Health, maxHealth);
    }

    /// <summary>Try to spend stamina. Returns false if not enough.</summary>
    public bool SpendStamina(float amount)
    {
        if (Stamina < amount) return false;
        Stamina = Mathf.Max(0f, Stamina - amount);
        _staminaRegenTimer = 0f;
        OnStaminaChanged?.Invoke(Stamina, maxStamina);
        return true;
    }

    /// <summary>Full reset — call on respawn.</summary>
    public void ResetStats()
    {
        IsDead  = false;
        Health  = maxHealth;
        Stamina = maxStamina;
        _invincibilityTimer = 0f;
        OnHealthChanged?.Invoke(Health, maxHealth);
        OnStaminaChanged?.Invoke(Stamina, maxStamina);
    }

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    void TickInvincibility()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    void RegenStamina()
    {
        if (Stamina >= maxStamina) return;

        _staminaRegenTimer += Time.deltaTime;
        if (_staminaRegenTimer < staminaRegenDelay) return;

        Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenRate * Time.deltaTime);
        OnStaminaChanged?.Invoke(Stamina, maxStamina);
    }
}
