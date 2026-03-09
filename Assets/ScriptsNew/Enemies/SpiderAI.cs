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

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : EnemyAI
{
    public enum BehaviourType { Charger, Circler }

    [Header("Spider — Behaviour")]
    public BehaviourType behaviour          = BehaviourType.Charger;
    public bool          randomiseVariation = true;

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

    [Header("Spider — Circler")]
    [Tooltip("Distance circler tries to maintain from Kitty")]
    public float orbitDistance  = 4f;
    [Tooltip("How fast it orbits around Kitty")]
    public float orbitSpeed     = 2.5f;
    [Tooltip("Circler dashes in to attack at this range")]
    public float dashInRange    = 2.5f;

    [Header("Spider — Separation")]
    [Tooltip("Spiders push away from each other within this radius.")]
    public float separationRadius = 2.5f;
    [Tooltip("How strongly spiders push away from each other.")]
    public float separationForce  = 1.8f;
    [Tooltip("Each spider approaches Kitty from a slightly different angle. " +
             "Higher = more spread out.")]
    public float approachAngleVariance = 45f;

    // Private state
    float _orbitAngle;
    float _variationOffset;
    float _approachAngle;       // unique angle offset for this spider's approach
    float _repositionTimer;     // periodically pick a new approach angle

    protected override void Awake()
    {
        base.Awake();
        _agent.speed            = normalSpeed;
        _agent.acceleration     = 12f;
        _agent.stoppingDistance = _stats.attackRange * 0.8f;

        // Each spider gets a unique random identity so they never move in sync
        _orbitAngle       = Random.Range(0f, 360f);
        _variationOffset  = Random.Range(0f, Mathf.PI * 2f);
        _approachAngle    = Random.Range(-approachAngleVariance, approachAngleVariance);
        _repositionTimer  = Random.Range(2f, 5f);

        // Circlers spread evenly — divide 360° by spider count approximation
        // by seeding orbit angle based on instance ID so no two start at same angle
        int seed = gameObject.GetInstanceID();
        Random.InitState(seed);
        _orbitAngle = Random.Range(0f, 360f);
        Random.InitState((int)Time.realtimeSinceStartup);
    }

    protected override void Update()
    {
        base.Update();

        if (behaviour == BehaviourType.Charger)
            UpdateCharge();
        else
            UpdateCircler();

        ApplySeparation();
        UpdateApproachAngle();
    }

    void ApplySeparation()
    {
        if (_kitty == null || !_agent.isOnNavMesh) return;

        // Find all other spiders nearby and push away from them
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationRadius,
                            LayerMask.GetMask("Enemy"));

        Vector3 separation = Vector3.zero;
        int     count      = 0;

        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<SpiderAI>() == null) continue;

            Vector3 away = transform.position - col.transform.position;
            float   dist = away.magnitude;
            if (dist < 0.01f) continue;

            // Closer spiders push harder
            separation += away.normalized * (1f - dist / separationRadius);
            count++;
        }

        if (count == 0) return;

        // Apply separation as a destination offset
        separation /= count;
        separation  = Vector3.ProjectOnPlane(separation, Vector3.up);

        Vector3 dest = _agent.destination + separation * separationForce;

        if (UnityEngine.AI.NavMesh.SamplePosition(dest, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    void UpdateApproachAngle()
    {
        // Periodically shift approach angle so spiders don't lock into the same spot
        _repositionTimer -= Time.deltaTime;
        if (_repositionTimer <= 0f)
        {
            _approachAngle   = Random.Range(-approachAngleVariance, approachAngleVariance);
            _repositionTimer = Random.Range(3f, 6f);
        }
    }

    void UpdateCircler()
    {
        if (_kitty == null || !_agent.isOnNavMesh) return;

        float distToKitty = Vector3.Distance(transform.position, _kitty.position);

        // Orbit around Kitty — direction flips per spider using variation offset
        float dir = Mathf.Sign(Mathf.Sin(_variationOffset));   // +1 or -1
        _orbitAngle += orbitSpeed * dir * Time.deltaTime * (1f + 0.4f * Mathf.Sin(Time.time * 0.7f + _variationOffset));

        Vector3 orbitPos = _kitty.position + new Vector3(
            Mathf.Cos(_orbitAngle * Mathf.Deg2Rad) * orbitDistance,
            0f,
            Mathf.Sin(_orbitAngle * Mathf.Deg2Rad) * orbitDistance);

        // Sample NavMesh to make sure orbit point is valid
        if (UnityEngine.AI.NavMesh.SamplePosition(orbitPos, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        // Dash in when close enough
        if (distToKitty <= dashInRange)
            _agent.SetDestination(_kitty.position);
    }

    void UpdateCharge()
    {
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
            // Windup — no parameter in animator
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
            _animator?.SetBool("isWalk", true);
        else
            _animator?.SetBool("isWalk", false);
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
    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            var result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}