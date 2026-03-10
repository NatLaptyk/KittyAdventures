// ─────────────────────────────────────────────────────────────────────────────
//  EnemyAI.cs  —  Base class for all enemies
//
//  Handles the shared state machine: Patrol → Chase → Attack → Dead
//  SpiderAI and SpiritAI inherit from this and override behaviour hooks.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Do NOT attach EnemyAI directly — use SpiderAI or SpiritAI instead.
//  2. Enemy GameObject needs:
//       - NavMeshAgent component
//       - EnemyStats component
//       - Collider (for player attacks to hit)
//  3. Bake a NavMesh on your terrain:
//       Window → AI → Navigation → Bake
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public abstract class EnemyAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  STATE MACHINE
    // ─────────────────────────────────────────────

    protected enum State { Patrol, Chase, Attack, Dead }
    protected State _state = State.Patrol;

    // ─────────────────────────────────────────────
    //  COMPONENTS
    // ─────────────────────────────────────────────

    protected NavMeshAgent _agent;
    protected EnemyStats   _stats;
    protected Transform    _kitty;
    protected Animator     _animator;

    // ─────────────────────────────────────────────
    //  PATROL
    // ─────────────────────────────────────────────

    Vector3 _spawnPoint;
    Vector3 _patrolTarget;
    float   _patrolWaitTimer;
    float   _patrolWaitDuration = 2f;
    bool    _waitingAtPatrolPoint;

    // ─────────────────────────────────────────────
    //  ATTACK
    // ─────────────────────────────────────────────

    float _attackTimer;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    protected virtual void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _stats    = GetComponent<EnemyStats>();
        _animator = GetComponentInChildren<Animator>();

        _spawnPoint   = transform.position;
        _patrolTarget = transform.position;

        // Apply stats to NavMeshAgent
        _agent.speed = _stats.moveSpeed;

        // Listen for death
        _stats.OnDied += OnDied;
    }

    protected virtual void Start()
    {
        // Find Kitty
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _kitty = go.transform;
        else Debug.LogWarning("[EnemyAI] No Player tag found in scene.");
    }

    protected virtual void Update()
    {
        if (_state == State.Dead) return;

        float distToKitty = _kitty != null
            ? Vector3.Distance(transform.position, _kitty.position)
            : float.MaxValue;

        switch (_state)
        {
            case State.Patrol: UpdatePatrol(distToKitty); break;
            case State.Chase:  UpdateChase(distToKitty);  break;
            case State.Attack: UpdateAttack(distToKitty); break;
        }

        // Update animator speed
        float spd = _agent.isOnNavMesh ? _agent.velocity.magnitude : 0f;
        _animator?.SetFloat("Speed", spd);
        _animator?.SetBool("isWalk", spd > 0.1f);
    }

    // ─────────────────────────────────────────────
    //  PATROL
    // ─────────────────────────────────────────────

    void UpdatePatrol(float distToKitty)
    {
        // Transition to chase
        if (distToKitty <= _stats.chaseRange)
        {
            TransitionTo(State.Chase);
            return;
        }

        if (_waitingAtPatrolPoint)
        {
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= _patrolWaitDuration)
            {
                _waitingAtPatrolPoint = false;
                SetNewPatrolTarget();
            }
            return;
        }

        // Move toward patrol target
        if (!_agent.pathPending && _agent.isOnNavMesh
            && _agent.remainingDistance < 0.5f)
        {
            _waitingAtPatrolPoint = true;
            _patrolWaitTimer      = 0f;
        }
    }

    void SetNewPatrolTarget()
    {
        // Pick a random point within patrol radius of spawn
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = _spawnPoint + Random.insideUnitSphere * _stats.patrolRadius;
            randomPoint.y       = _spawnPoint.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _patrolTarget = hit.position;
                if (_agent.isOnNavMesh)
                    _agent.SetDestination(_patrolTarget);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  CHASE
    // ─────────────────────────────────────────────

    void UpdateChase(float distToKitty)
    {
        // Lost Kitty — return to patrol
        if (distToKitty > _stats.chaseRange * 1.5f)
        {
            TransitionTo(State.Patrol);
            return;
        }

        // Close enough to attack
        if (distToKitty <= _stats.attackRange)
        {
            TransitionTo(State.Attack);
            return;
        }

        ChaseTarget();
    }

    // Override in subclasses for different chase behaviours
    protected virtual void ChaseTarget()
    {
        if (_kitty != null && _agent.isOnNavMesh)
            _agent.SetDestination(_kitty.position);
    }

    // ─────────────────────────────────────────────
    //  ATTACK
    // ─────────────────────────────────────────────

    void UpdateAttack(float distToKitty)
    {
        // Kitty moved away — chase again
        if (distToKitty > _stats.attackRange * 1.2f)
        {
            TransitionTo(State.Chase);
            return;
        }

        // Stop moving AND disable NavMeshAgent rotation during attack
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped           = true;
            _agent.updateRotation      = false;  // prevent agent from rotating us
            _agent.SetDestination(transform.position);
        }

        // Snap to face Kitty every frame during attack
        if (_kitty != null)
        {
            Vector3 dir = (_kitty.position - transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                Debug.Log($"[Attack] forward={transform.forward} dirToKitty={dir} rotation={transform.eulerAngles}");
            }
        }

        // Attack cooldown
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            _attackTimer = _stats.attackCooldown;
            PerformAttack();
        }
    }

    protected virtual void PerformAttack()
    {
        _animator?.SetTrigger("isAttack");
        AudioManager.instance.PlaySFX(AudioManager.instance.spiderAttack, 0.8f);
        // Deal damage if Kitty is still in range
        if (_kitty == null) return;
        float dist = Vector3.Distance(transform.position, _kitty.position);
        if (dist <= _stats.attackRange)
            _kitty.GetComponent<IDamageable>()?.TakeDamage(
                _stats.attackDamage, transform.position);
        AudioManager.instance.PlaySFX(AudioManager.instance.playerDamaged, 0.8f);
    }

    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────

    void OnDied()
    {
        TransitionTo(State.Dead);
        _agent.isStopped = true;
        _agent.enabled   = false;
        _animator?.SetTrigger("isDed");
        AudioManager.instance.PlaySFX(AudioManager.instance.spiderDed);
    }

    // ─────────────────────────────────────────────
    //  STATE TRANSITION
    // ─────────────────────────────────────────────

    protected void TransitionTo(State newState)
    {
        if (_state == newState) return;
        _state = newState;
        OnStateChanged(newState);
    }

    protected virtual void OnStateChanged(State newState)
    {
        // Re-enable NavMeshAgent rotation when leaving attack state
        if (newState != State.Attack)
        {
            _agent.updateRotation = true;
            _agent.isStopped      = false;
        }
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Chase range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position,
            GetComponent<EnemyStats>()?.chaseRange ?? 12f);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,
            GetComponent<EnemyStats>()?.attackRange ?? 1.5f);

        // Patrol radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? _spawnPoint : transform.position,
            GetComponent<EnemyStats>()?.patrolRadius ?? 8f);
    }
}
