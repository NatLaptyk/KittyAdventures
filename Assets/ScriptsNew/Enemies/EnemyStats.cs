

// ─────────────────────────────────────────────────────────────────────────────
//  EnemyStats.cs
//
//  Shared stats for all enemy types. Attach to any enemy GameObject.
//  Implements IDamageable so PlayerCombat can hit enemies directly.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to your enemy root GameObject.
//  2. Configure health, damage, speed in the Inspector.
//  3. Optionally assign a deathEffect particle prefab.
// ─────────────────────────────────────────────────────────────────────────────

using System;

using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Health")]
    public float maxHealth = 40f;

    [Header("Combat")]
    public float attackDamage  = 10f;
    public float attackRange   = 1.5f;
    public float attackCooldown = 1.5f;

    [Header("Movement")]
    public float moveSpeed    = 3.5f;
    public float chaseRange   = 12f;   // distance at which enemy starts chasing
    public float patrolRadius = 8f;    // how far enemy patrols from spawn

    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float      deathEffectDuration = 2f;

    // ─────────────────────────────────────────────
    //  PUBLIC STATE
    // ─────────────────────────────────────────────

    public float Health   { get; private set; }
    public bool  IsDead   { get; private set; }

    // ─────────────────────────────────────────────
    //  EVENTS
    // ─────────────────────────────────────────────

    public event Action<float, float> OnHealthChanged;  // (current, max)
    public event Action               OnDied;
    public event Action<float>        OnDamaged;        // (amount)

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        Health = maxHealth;
    }

    // ─────────────────────────────────────────────
    //  IDAMAGEABLE
    // ─────────────────────────────────────────────

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (IsDead) return;

        Health = Mathf.Max(0f, Health - amount);
        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(Health, maxHealth);

        // Flash red to show hit — works if enemy has a Renderer
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            StartCoroutine(HitFlash(renderer));

        if (Health <= 0f)
            Die(sourcePosition);
    }

    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────

    void Die(Vector3 sourcePosition)
    {
        IsDead = true;
        OnDied?.Invoke();

        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, deathEffectDuration);
        }

        // Disable enemy — Animator can play death anim before this
        // If you have a death animation, replace Destroy with a coroutine
        Destroy(gameObject, 0.1f);
    }

    // ─────────────────────────────────────────────
    //  HIT FLASH
    // ─────────────────────────────────────────────

    System.Collections.IEnumerator HitFlash(Renderer r)
    {
        var original = r.material.color;
        r.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        r.material.color = original;
    }
}
