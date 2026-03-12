using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth           = 100f;
    [SerializeField] private float passiveRegenPerSec  = 1f;    // always on
    [SerializeField] private float combatRegenPerSec   = 4f;    // out of combat
    [SerializeField] private float outOfCombatDelay    = 5f;    // seconds after last hit

    [Header("Snack")]
    [SerializeField] private int startingSnacks = 1;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenPerSec = 25f;

    public float Health     { get; private set; }
    public float Stamina    { get; private set; }
    public float MaxHealth  => maxHealth;
    public float MaxStamina => maxStamina;
    public int   Snacks     { get; private set; }
    public bool  IsRegenerating => Health < maxHealth && !isDead;

    public event Action<float, float> HealthChanged;   // current, max
    public event Action<float, float> StaminaChanged;  // current, max
    public event Action<int>          SnacksChanged;   // current count
    public event Action               Died;

    private bool  isDead;
    private float _lastDamageTime = -999f;

    private void Awake()
    {
        Health  = maxHealth;
        Stamina = maxStamina;
        Snacks  = startingSnacks;
        EmitAll();
    }

    private void Update()
    {
        if (isDead) return;

        if (Health < maxHealth)
        {
            bool outOfCombat = (Time.time - _lastDamageTime) >= outOfCombatDelay;
            float regen = passiveRegenPerSec + (outOfCombat ? combatRegenPerSec : 0f);
            Health = Mathf.Min(maxHealth, Health + regen * Time.deltaTime);
            HealthChanged?.Invoke(Health, maxHealth);
        }

        if (staminaRegenPerSec > 0f && Stamina < maxStamina)
        {
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSec * Time.deltaTime);
            StaminaChanged?.Invoke(Stamina, maxStamina);
        }
    }

    public bool SpendStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (Stamina < amount) return false;

        Stamina -= amount;
        StaminaChanged?.Invoke(Stamina, maxStamina);
        return true;
    }

    public void RestoreStamina(float amount)
    {
        if (isDead) return;
        Stamina = Mathf.Min(maxStamina, Stamina + Mathf.Abs(amount));
        StaminaChanged?.Invoke(Stamina, maxStamina);
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        Health = Mathf.Min(maxHealth, Health + Mathf.Abs(amount));
        HealthChanged?.Invoke(Health, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    // IDamageable overload — called by enemies
    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (isDead) return;

        _lastDamageTime = Time.time;
        Health -= Mathf.Abs(amount);
        HealthChanged?.Invoke(Health, maxHealth);

        // Camera shake + combat FX
        var cam = FindFirstObjectByType<CameraController>();
        cam?.Shake(0.4f);
        CombatFX.Instance?.OnKittyDamaged(sourcePosition);

        if (Health <= 0f)
        {
            Health = 0f;
            isDead = true;
            Died?.Invoke();
        }
    }

    public void UseSnack()
    {
        if (isDead || Snacks <= 0) return;
        Snacks--;
        Health = maxHealth;
        HealthChanged?.Invoke(Health, maxHealth);
        SnacksChanged?.Invoke(Snacks);
    }

    public void AddSnack(int amount = 1)
    {
        Snacks += amount;
        SnacksChanged?.Invoke(Snacks);
    }

    public void ResetFull()
    {
        isDead = false;
        Health  = maxHealth;
        Stamina = maxStamina;
        Snacks  = startingSnacks;
        EmitAll();
    }

    private void EmitAll()
    {
        HealthChanged?.Invoke(Health, maxHealth);
        StaminaChanged?.Invoke(Stamina, maxStamina);
        SnacksChanged?.Invoke(Snacks);
    }
}