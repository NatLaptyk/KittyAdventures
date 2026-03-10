using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenPerSec = 0f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenPerSec = 25f;

    public float Health { get; private set; }
    public float Stamina { get; private set; }
    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;

    public event Action<float, float> HealthChanged;  // current, max
    public event Action<float, float> StaminaChanged; // current, max
    public event Action Died;

    private bool isDead;

    private void Awake()
    {
        Health = maxHealth;
        Stamina = maxStamina;
        EmitAll();
    }

    private void Update()
    {
        if (isDead) return;

        if (healthRegenPerSec > 0f && Health < maxHealth)
        {
            Health = Mathf.Min(maxHealth, Health + healthRegenPerSec * Time.deltaTime);
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

    public void ResetFull()
    {
        isDead = false;
        Health = maxHealth;
        Stamina = maxStamina;
        EmitAll();
    }

    private void EmitAll()
    {
        HealthChanged?.Invoke(Health, maxHealth);
        StaminaChanged?.Invoke(Stamina, maxStamina);
    }
}