// ─────────────────────────────────────────────────────────────────────────────
//  SpiderAI.cs  —  Aggressive ground enemy
//
//  Spiders are aggressive melee enemies:
//  • Patrol their spawn area
//  • Charge at Kitty when she enters chase range
//  • Deal heavy melee damage up close
//  • High health, slow speed
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to your Spider enemy GameObject.
//  2. Also attach EnemyStats — set recommended values:
//       Max Health:    80
//       Attack Damage: 15
//       Attack Range:  1.8
//       Move Speed:    3.5
//       Chase Range:   10
//  3. Add NavMeshAgent component.
//  4. Add a Collider (CapsuleCollider works well).
//  5. Set GameObject layer to "Enemy".
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : EnemyAI
{
    [Header("Spider — Aggression")]
    [Tooltip("When this close, spider charges at full speed")]
    public float chargeRange    = 6f;
    public float chargeSpeed    = 7f;    // burst speed during charge
    public float normalSpeed    = 3.5f;

    [Tooltip("Spider pauses briefly before lunging")]
    public float lungeWindup    = 0.4f;

    bool  _isCharging   = false;
    bool  _inWindup     = false;
    float _windupTimer  = 0f;

    protected override void Awake()
    {
        base.Awake();
        _agent.speed        = normalSpeed;
        _agent.acceleration = 12f;
        _agent.stoppingDistance = _stats.attackRange * 0.8f;
    }

    protected override void Update()
    {
        base.Update();

        // Handle charge windup timer
        if (_inWindup)
        {
            _windupTimer -= Time.deltaTime;
            if (_windupTimer <= 0f)
            {
                _inWindup  = false;
                _isCharging = true;
                _agent.speed = chargeSpeed;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  CHASE  —  spiders charge when close enough
    // ─────────────────────────────────────────────

    protected override void ChaseTarget()
    {
        if (_kitty == null) return;

        float dist = Vector3.Distance(transform.position, _kitty.position);

        if (dist <= chargeRange && !_isCharging && !_inWindup)
        {
            // Begin windup before charge
            _inWindup    = true;
            _windupTimer = lungeWindup;
            _agent.isStopped = true;
            _animator?.SetTrigger("Windup");
        }
        else if (_isCharging || (!_inWindup && dist > chargeRange))
        {
            _agent.isStopped = false;
            _agent.SetDestination(_kitty.position);
        }
    }

    // ─────────────────────────────────────────────
    //  STATE CHANGES
    // ─────────────────────────────────────────────

    protected override void OnStateChanged(State newState)
    {
        _isCharging = false;
        _inWindup   = false;
        _agent.speed = normalSpeed;
        _agent.isStopped = false;

        if (newState == State.Chase)
            _animator?.SetBool("IsChasing", true);
        else
            _animator?.SetBool("IsChasing", false);
    }

    // ─────────────────────────────────────────────
    //  ATTACK  —  heavy hit with knockback feel
    // ─────────────────────────────────────────────

    protected override void PerformAttack()
    {
        _isCharging = false;
        _agent.speed = normalSpeed;

        base.PerformAttack();
    }
}