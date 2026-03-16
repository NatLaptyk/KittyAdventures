// ─────────────────────────────────────────────────────────────────────────────
//  SpiritAI.cs  —  Ghost Spirit Enemy
//
//  Completely overrides the base Update loop so the Spirit never gets frozen
//  by EnemyAI's UpdateAttack. The Spirit manages its own state entirely.
//
//  NORMAL PHASE  (health > 50%)
//    Orbits Kitty → dashes through her when cooldown ready
//
//  ENRAGED PHASE (health <= 50%)
//    Orbits faster → chains 2 dashes → retreats → repeats
//    Flashes white then glows red on phase transition
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpiritAI : EnemyAI
{
    [Header("Spirit — Float")]
    public float floatHeight   = 1.2f;
    public float bobAmplitude  = 0.5f;
    public float bobFrequency  = 1.5f;

    [Header("Spirit — Orbit")]
    public float preferredDist = 5f;
    public float minDistance   = 3f;
    public float strafeSpeed   = 8f;

    [Header("Spirit — Ghost Dash")]
    public float dashSpeed       = 20f;
    public float dashOvershoot   = 3f;
    public float dashWindup      = 0.5f;
    public float dashHitRadius   = 1.2f;
    public float dashCooldown    = 3f;

    [Header("Spirit — Enraged Phase")]
    [Range(0f, 1f)]
    public float enragedThreshold    = 0.5f;
    public float enragedSpeedMult    = 1.8f;
    public float enragedDashCooldown = 1.6f;
    public int   enragedMaxChain     = 2;
    public Color normalGlowColour    = new Color(0.4f, 0.6f, 1.0f);
    public Color enragedGlowColour   = new Color(1.0f, 0.15f, 0.1f);
    public Renderer glowRenderer;

    [Header("Spirit — Drop")]
    [Tooltip("The SpiritPotion prefab to spawn when the Spirit dies.")]
    public GameObject potionPrefab;
    [Tooltip("Height above Spirit position where the potion spawns.")]
    public float potionDropHeight = 1f;
    [Tooltip("If assigned, the potion spawns here instead of on the Spirit. " +
             "Place an empty GameObject somewhere visible in the arena.")]
    public Transform potionSpawnPoint;


    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    float    _strafeAngle  = 0f;
    float    _bobTimer     = 0f;
    float    _dashTimer    = 0f;
    bool     _isDashing   = false;
    bool     _isEnraged   = false;
    int      _dashChain   = 0;
    bool     _retreating  = false;
    bool     _isDead      = false;
    Material _mat;

    // Stuck detection
    Vector3  _lastPosition;
    float    _stuckTimer      = 0f;
    float    _stuckCheckDelay = 1f;   // check every second
    float    _stuckThreshold  = 0.3f; // must move this far per second or considered stuck

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        _agent.speed                 = _stats.moveSpeed;
        _agent.acceleration          = 20f;
        _agent.stoppingDistance      = 0.1f;
        _lastPosition                = transform.position;
        _strafeAngle                 = Random.Range(0f, 360f);

        if (glowRenderer != null)
        {
            _mat = glowRenderer.material;
            SetGlow(normalGlowColour);
        }

    }

    // ─────────────────────────────────────────────
    //  OVERRIDE Update ENTIRELY
    //  Bypass base EnemyAI state machine so UpdateAttack
    //  never freezes the Spirit
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        if (_isDead || _kitty == null) return;

        if (!_isDashing) ApplyFloat();
        CheckPhaseTransition();
        CheckIfStuck();

        // Simple two-state loop: patrol if far, orbit+attack if close
        float dist = Vector3.Distance(transform.position, _kitty.position);

        if (dist > _stats.chaseRange)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _animator?.SetBool("IsActive", false);
            Debug.Log($"[Spirit] Too far ({dist:F1}), chaseRange={_stats.chaseRange}");
            return;
        }

        _animator?.SetBool("IsActive", true);
        Debug.Log($"[Spirit] dist={dist:F1} isDashing={_isDashing} dashTimer={_dashTimer:F2} retreating={_retreating} enraged={_isEnraged}");

        if (!_isDashing)
        {
            Orbit();
            TickDash();
        }

        // Animator speed
        float spd = _agent.isOnNavMesh ? _agent.velocity.magnitude : 0f;
        _animator?.SetFloat("Speed", spd);
        _animator?.SetBool("isWalk", spd > 0.1f);
    }

    // ─────────────────────────────────────────────
    //  FLOAT
    // ─────────────────────────────────────────────

    void ApplyFloat()
    {
        _bobTimer += Time.deltaTime * bobFrequency;
        float bob  = Mathf.Sin(_bobTimer) * bobAmplitude;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            Vector3 pos    = transform.position;
            pos.y          = hit.position.y + floatHeight + bob;
            transform.position = pos;
        }
    }

    // ─────────────────────────────────────────────
    //  ORBIT
    // ─────────────────────────────────────────────

    void Orbit()
    {
        float dist          = Vector3.Distance(transform.position, _kitty.position);
        float currentStrafe = _isEnraged ? strafeSpeed * enragedSpeedMult : strafeSpeed;

        if (_retreating)
        {
            // Back away to preferred distance after dash chain
            Vector3 retreatDir    = (transform.position - _kitty.position).normalized;
            Vector3 retreatTarget = _kitty.position + retreatDir * preferredDist;

            if (_agent.isOnNavMesh &&
                NavMesh.SamplePosition(retreatTarget, out NavMeshHit rHit, 3f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(rHit.position);
            }

            if (dist >= preferredDist * 0.85f)
            {
                _retreating = false;
                _dashChain  = 0;
            }
            return;
        }

        // Circle orbit
        _strafeAngle += currentStrafe * Time.deltaTime * 20f;
        float   rad    = _strafeAngle * Mathf.Deg2Rad;
        Vector3 orbit  = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * preferredDist;
        Vector3 target = _kitty.position + orbit;

        // Back away if too close
        if (dist < minDistance)
        {
            Vector3 away = (transform.position - _kitty.position).normalized;
            target = transform.position + away * 2f;
        }

        if (_agent.isOnNavMesh &&
            NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
        }

        FaceKitty();
    }

    // ─────────────────────────────────────────────
    //  DASH TICK
    // ─────────────────────────────────────────────

    void TickDash()
    {
        _dashTimer -= Time.deltaTime;

        float cooldown = _isEnraged ? enragedDashCooldown : dashCooldown;
        if (_dashTimer <= 0f)
        {
            _dashTimer = cooldown;
            Debug.Log("[Spirit] Starting GhostDash!");
            StartCoroutine(GhostDash());
        }
    }

    // ─────────────────────────────────────────────
    //  GHOST DASH
    // ─────────────────────────────────────────────

    IEnumerator GhostDash()
    {
        _isDashing       = true;
        _agent.isStopped = true;
        Debug.Log("[Spirit] GhostDash coroutine running");
        _animator?.SetTrigger("isAttack");
        AudioManager.instance?.PlaySFX(AudioManager.instance.wispAttack, 0.5f);

        // Wind-up
        float t = dashWindup;
        while (t > 0f)
        {
            FaceKitty();
            t -= Time.deltaTime;
            yield return null;
        }

        // Dash through Kitty
        if (_kitty != null)
        {
            Vector3 dir   = (_kitty.position - transform.position).normalized;
            Vector3 dest  = _kitty.position + dir * dashOvershoot;
            dest.y        = transform.position.y;

            bool hitKitty = false;

            while (new Vector2(transform.position.x - dest.x, transform.position.z - dest.z).magnitude > 0.3f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, dest, dashSpeed * Time.deltaTime);

                if (!hitKitty)
                {
                    float d = Vector3.Distance(transform.position, _kitty.position);
                    if (d <= dashHitRadius)
                    {
                        hitKitty = true;
                        _kitty.GetComponent<IDamageable>()?.TakeDamage(
                            _stats.attackDamage, transform.position);
                        CombatFX.Instance?.OnKittyDamaged(transform.position);
                    }
                }

                yield return null;
            }
        }

        // Track chain for enraged phase
        _dashChain++;
        if (_isEnraged && _dashChain >= enragedMaxChain)
            _retreating = true;

        _isDashing       = false;
        _agent.isStopped = false;
    }

    // ─────────────────────────────────────────────
    //  PHASE TRANSITION
    // ─────────────────────────────────────────────

    void CheckPhaseTransition()
    {
        if (_isEnraged || _stats == null) return;
        if (_stats.Health / _stats.maxHealth <= enragedThreshold)
            EnterEnragedPhase();
    }

    void EnterEnragedPhase()
    {
        _isEnraged = true;
        _dashTimer = 0f;
        StartCoroutine(EnrageFlash());
    }

    IEnumerator EnrageFlash()
    {
        for (int i = 0; i < 5; i++)
        {
            SetGlow(Color.white);
            yield return new WaitForSeconds(0.08f);
            SetGlow(enragedGlowColour);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────

    protected override void OnStateChanged(State newState)
    {
        if (newState == State.Dead)
        {
            _isDead = true;
            StopAllCoroutines();
            _isDashing       = false;
            _agent.isStopped = true;
            _agent.enabled   = false;
            _animator?.SetTrigger("isDed");
            AudioManager.instance?.PlaySFX(AudioManager.instance.wispDed);

            // Drop the potion
            if (potionPrefab != null)
            {
                Vector3 spawnPos = potionSpawnPoint != null
                    ? potionSpawnPoint.position
                    : transform.position + Vector3.up * potionDropHeight;
                Instantiate(potionPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    void FaceKitty()
    {
        if (_kitty == null) return;
        Vector3 dir = (_kitty.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }

    void SetGlow(Color col)
    {
        if (_mat == null) return;
        _mat.SetColor("_EmissionColor", col * 2f);
        _mat.EnableKeyword("_EMISSION");
    }

    void CheckIfStuck()
    {
        if (_isDashing || !_agent.isOnNavMesh) return;

        _stuckTimer += Time.deltaTime;

        if (_stuckTimer >= _stuckCheckDelay)
        {
            float moved = Vector3.Distance(transform.position, _lastPosition);

            if (moved < _stuckThreshold && !_agent.isStopped)
            {
                // Spirit is stuck — pick a random nearby NavMesh point and go there
                Vector3 randomDir    = Random.insideUnitSphere * 4f;
                randomDir.y          = 0f;
                Vector3 escapeTarget = transform.position + randomDir;

                if (NavMesh.SamplePosition(escapeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                }
            }

            _lastPosition = transform.position;
            _stuckTimer   = 0f;
        }
    }
}