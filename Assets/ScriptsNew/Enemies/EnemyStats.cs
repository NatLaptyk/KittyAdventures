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
using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Health")]
    public float maxHealth = 40f;

    [Header("Combat")]
    public float attackDamage  = 5f;
    public float attackRange   = 1.5f;
    public float attackCooldown = 1.5f;

    [Header("Movement")]
    public float moveSpeed    = 3.5f;
    public float chaseRange   = 12f;   // distance at which enemy starts chasing
    public float patrolRadius = 8f;    // how far enemy patrols from spawn

    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float      deathEffectDuration = 2f;

    [Header("Hit Effect")]
    [Tooltip("Particle System on this GameObject that plays on every hit. " +
             "Add a Particle System component to the enemy and set Play On Awake to off.")]
    public ParticleSystem hitEffect;
    [Tooltip("Optional prefab to instantiate on every hit — assign the same prefab as Death Effect for Spirit.")]
    public GameObject hitEffectPrefab;
    [Tooltip("How long before the hit effect prefab is destroyed.")]
    public float hitEffectDuration = 0.5f;

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

    Animator _animator;

    void Awake()
    {
        Health    = maxHealth;
        _animator = GetComponentInChildren<Animator>();
        if (hitEffect == null)
            hitEffect = GetComponentInChildren<ParticleSystem>();
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

        // Play hit animation
        _animator?.SetTrigger("TakeDamage");

        // Flash red to show hit
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            StartCoroutine(HitFlash(renderer));

        // Play hit particle effect
        if (hitEffect != null)
        {
            hitEffect.transform.position = transform.position;
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitEffect.Play();
        }

        // Instantiate hit effect prefab at hit position
        if (hitEffectPrefab != null)
        {
            var fx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(fx, hitEffectDuration);
        }

        if (Health <= 0f)
            Die(sourcePosition);
    }

    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────

    [Header("Death Timing")]
    [Tooltip("How long to wait after death before destroying the GameObject. " +
             "Set this to match the length of your death animation clip.")]
    public float destroyDelay = 3f;

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

        // Wait for death animation to finish before destroying
        StartCoroutine(DestroyAfterDeathAnim());
    }

    IEnumerator DestroyAfterDeathAnim()
    {
        // Disable collider and agent immediately so enemy stops interacting
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Wait for animation to play
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
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