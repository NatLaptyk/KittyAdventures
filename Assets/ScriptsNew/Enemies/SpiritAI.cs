// ─────────────────────────────────────────────────────────────────────────────
//  SpiritAI.cs  —  Ranged/distance-keeping spirit enemy
//
//  Corrupted spirits keep their distance and circle around Kitty:
//  • Float above ground (Y offset applied)
//  • Keep a preferred distance from Kitty — not too close, not too far
//  • Circle strafe around Kitty while attacking
//  • Phase through obstacles (NavMeshAgent obstacle avoidance off)
//  • Faster than spiders, lower health
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to your Spirit enemy GameObject.
//  2. Also attach EnemyStats — set recommended values:
//       Max Health:    40
//       Attack Damage: 8
//       Attack Range:  5      (ranged attack)
//       Move Speed:    5
//       Chase Range:   15
//  3. Add NavMeshAgent — set Obstacle Avoidance to None (spirits phase through)
//  4. Set GameObject layer to "Enemy".
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.AI;

public class SpiritAI : EnemyAI
{
    [Header("Spirit — Behaviour")]
    [Tooltip("Spirit floats this high above the ground")]
    public float floatHeight     = 1.2f;

    [Tooltip("Spirit tries to stay at this distance from Kitty")]
    public float preferredDist   = 5f;

    [Tooltip("If Kitty gets closer than this, spirit backs away")]
    public float minDistance     = 3f;

    [Tooltip("Speed of circling around Kitty")]
    public float strafeSpeed     = 2.5f;

    [Tooltip("Hover bob amplitude")]
    public float bobAmplitude    = 0.2f;
    public float bobFrequency    = 1.5f;

    float _strafeAngle  = 0f;
    float _bobTimer     = 0f;
    Vector3 _basePos;

    protected override void Awake()
    {
        base.Awake();

        // Spirits phase through obstacles — disable avoidance
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        _agent.speed        = _stats.moveSpeed;
        _agent.acceleration = 20f;   // snappy direction changes
        _agent.stoppingDistance = 0.1f;

        _strafeAngle = Random.Range(0f, 360f);  // start at random angle
    }

    protected override void Update()
    {
        base.Update();
        ApplyFloat();
    }

    // ─────────────────────────────────────────────
    //  FLOAT  —  hover above ground with gentle bob
    // ─────────────────────────────────────────────

    void ApplyFloat()
    {
        _bobTimer += Time.deltaTime * bobFrequency;
        float bob = Mathf.Sin(_bobTimer) * bobAmplitude;

        // Sample NavMesh height at current position
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            Vector3 pos = transform.position;
            pos.y = hit.position.y + floatHeight + bob;
            transform.position = pos;
        }
    }

    // ─────────────────────────────────────────────
    //  CHASE  —  spirits keep preferred distance
    //            and circle strafe around Kitty
    // ─────────────────────────────────────────────

    protected override void ChaseTarget()
    {
        if (_kitty == null) return;

        float dist = Vector3.Distance(transform.position, _kitty.position);

        // Strafe angle increases over time — circles around Kitty
        _strafeAngle += strafeSpeed * Time.deltaTime * 30f;

        // Position on a circle around Kitty at preferred distance
        float rad     = _strafeAngle * Mathf.Deg2Rad;
        Vector3 orbit = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * preferredDist;
        Vector3 target = _kitty.position + orbit;

        // Back away if Kitty is too close
        if (dist < minDistance)
        {
            Vector3 away = (transform.position - _kitty.position).normalized;
            target = transform.position + away * 2f;
        }

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        // Always face Kitty
        Vector3 lookDir = (_kitty.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), 5f * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  ATTACK  —  spirit fires from a distance
    // ─────────────────────────────────────────────

    protected override void PerformAttack()
    {
        if (_kitty == null) return;

        _animator?.SetTrigger("Attack");

        // Spirits attack at range — check line of sight
        float dist = Vector3.Distance(transform.position, _kitty.position);
        if (dist <= _stats.attackRange)
            _kitty.GetComponent<IDamageable>()?.TakeDamage(
                _stats.attackDamage, transform.position);
    }

    // ─────────────────────────────────────────────
    //  STATE CHANGES
    // ─────────────────────────────────────────────

    protected override void OnStateChanged(State newState)
    {
        if (newState == State.Chase || newState == State.Attack)
            _animator?.SetBool("IsActive", true);
        else
            _animator?.SetBool("IsActive", false);
    }
}