// ─────────────────────────────────────────────────────────────────────────────
//  PlayerController.cs
//
//  Handles Kitty's movement, jumping, dodging and climbing.
//  Works alongside CameraController.cs (orbit camera).
//
//  REQUIRES on same GameObject:
//    - CharacterController
//    - InputReader
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to Kitty's root GameObject.
//  2. Kitty needs a CharacterController:
//       Height: 1.8  |  Radius: 0.3  |  Center Y: 0.9  |  Skin Width: 0.08
//  3. For climbing: tag climbable walls with the "Climbable" tag.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Movement")]
    public float walkSpeed        = 5f;
    public float sprintSpeed      = 9f;
    public float sprintStaminaCost = 15f;  // stamina per second while sprinting
    public float rotateSpeed = 12f;

    [Header("Jump")]
    public float jumpForce = 7f;

    [Tooltip("Stamina cost for a regular jump.")]
    public float jumpStaminaCost   = 10f;

    [Header("Stomp Attack")]
    [Tooltip("Damage dealt when Kitty lands on an enemy from above.")]
    public float stompDamage       = 25f;
    [Tooltip("Stamina cost for a stomp.")]
    public float stompStaminaCost  = 15f;
    [Tooltip("How fast Kitty must be falling to trigger a stomp.")]
    public float stompMinFallSpeed = 2f;
    [Tooltip("Radius around Kitty's feet to check for enemies on landing.")]
    public float stompRadius       = 0.8f;
    [Tooltip("Layer mask for enemies.")]
    public LayerMask enemyLayers;
    public float gravity   = -25f;

    [Header("Dodge")]
    public float dodgeSpeed    = 12f;
    public float dodgeDuration = 0.25f;

    [Header("Climb")]
    public float climbSpeed      = 3f;
    public float climbCheckDist  = 0.5f;
    public LayerMask climbMask;

    [Header("Animation")]
    public Animator animator;

    [Header("Footsteps")]
    [SerializeField] private float stepInterval = 0.38f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    CharacterController _cc;
    InputReader         _input;
    CameraController    _cam;
    Animator            _animator;
    bool                _wasGrounded = true;
    float               _stepTimer   = 0f;

    Vector3     _velocity;
    float       _fallSpeed    = 0f;   // tracked for stomp
    bool        _isSprinting = false;
    PlayerStats _stats;

    // Dodge
    bool    _isDodging;
    Vector3 _dodgeDir;

    // Climb
    bool    _isClimbing;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        _cc       = GetComponent<CharacterController>();
        _input    = GetComponent<InputReader>();
        _cam      = FindFirstObjectByType<CameraController>();
        // Use assigned animator or search children as fallback
        _animator = animator != null ? animator : GetComponentInChildren<Animator>();

        _stats    = GetComponent<PlayerStats>();

        if (_input == null)
            Debug.LogError("[PlayerController] InputReader not found on Kitty!");
    }

    void Update()
    {
        if (_isDodging) return;   // dodge coroutine takes over

        if (_isClimbing)
        {
            UpdateClimb();
            return;
        }

        CheckClimbStart();
        UpdateMovement();
        UpdateJump();
        UpdateDodge();

        _cc.Move(_velocity * Time.deltaTime);
        UpdateAnimator();
    }

    // ─────────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────────

    void UpdateAnimator()
    {
        if (_animator == null) return;

        // isRun — true whenever Kitty is moving horizontally
        float horizontalSpeed = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        _animator.SetBool("isRun",    horizontalSpeed > 0.1f);
        _animator.SetBool("isSprint", _isSprinting && horizontalSpeed > 0.1f);

        // Track fall speed for stomp detection
        if (!_cc.isGrounded)
            _fallSpeed = Mathf.Abs(Mathf.Min(_velocity.y, 0f));

        // isLand — fire trigger on the frame Kitty touches the ground
        bool grounded = _cc.isGrounded;
        if (grounded && !_wasGrounded)
        {
            _animator?.SetTrigger("isLand");
            AudioManager.instance.PlaySFX(AudioManager.instance.land, 0f);

            // Check for stomp attack on landing
            if (_fallSpeed >= stompMinFallSpeed)
                TryStompAttack();

            _fallSpeed = 0f;
        }
        _wasGrounded = grounded;
    }

    bool IsNearGround()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.2f,
                               Vector3.down, 0.4f);
    }

    void UpdateMovement()
    {
        Vector2 input = _input != null ? _input.Move : Vector2.zero;
        float   yaw   = _cam   != null ? _cam.CameraYaw : 0f;

        // Camera-relative direction
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f)
                    * new Vector3(input.x, 0f, input.y);

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // Sprint — hold Shift, costs stamina, must be moving
        bool wantsToSprint = _input.SprintHeld && dir.sqrMagnitude > 0.01f;
        bool hasStamina    = _stats != null && _stats.Stamina > 0f;
        _isSprinting       = wantsToSprint && hasStamina;

        if (_isSprinting && _stats != null)
            _stats.SpendStamina(sprintStaminaCost * Time.deltaTime);

        float speed = _isSprinting ? sprintSpeed : walkSpeed;

        // Apply horizontal velocity
        _velocity.x = dir.x * speed;
        _velocity.z = dir.z * speed;

        // Rotate Kitty to face movement direction
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotateSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  JUMP  —  single jump, grounded only
    // ─────────────────────────────────────────────

    void UpdateJump()
    {
        if (_cc.isGrounded)
        {
            // Small downward force keeps Kitty snapped to the ground
            if (_velocity.y < 0f) _velocity.y = -2f;

            if (_input != null && _input.JumpPressed)
            {
                // Spend stamina on jump — still jump even if out of stamina
                _stats?.SpendStamina(jumpStaminaCost);

                _velocity.y = jumpForce;
                _animator?.SetTrigger("isJump");
                AudioManager.instance.PlaySFX(AudioManager.instance.jump, 0f);
            }
        }
        else
        {
            // Apply gravity while airborne
            _velocity.y += gravity * Time.deltaTime;
        }
    }

    // ─────────────────────────────────────────────
    //  DODGE
    // ─────────────────────────────────────────────

    void UpdateDodge()
    {
        if (_input == null || !_input.DodgePressed) return;
        if (!_cc.isGrounded) return;

        // Dodge in movement direction, or backward if standing still
        Vector2 input = _input.Move;
        float   yaw   = _cam != null ? _cam.CameraYaw : 0f;

        Vector3 dir = Quaternion.Euler(0f, yaw, 0f)
                    * new Vector3(input.x, 0f, input.y);

        _dodgeDir = dir.sqrMagnitude > 0.1f
            ? dir.normalized
            : -transform.forward;

        StartCoroutine(DodgeRoutine());
    }

    IEnumerator DodgeRoutine()
    {
        _isDodging = true;
        float elapsed = 0f;

        while (elapsed < dodgeDuration)
        {
            // Curve the speed so it eases out
            float t     = elapsed / dodgeDuration;
            float speed = Mathf.Lerp(dodgeSpeed, 0f, t);

            Vector3 move = _dodgeDir * speed;
            move.y       = _velocity.y + gravity * Time.deltaTime;
            _velocity.y  = move.y;

            _cc.Move(move * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _isDodging = false;
    }

    // ─────────────────────────────────────────────
    //  CLIMB
    // ─────────────────────────────────────────────

    void CheckClimbStart()
    {
        if (_input == null || !_input.ClimbHeld) return;

        // Raycast forward to detect a climbable surface
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                             transform.forward, climbCheckDist, climbMask))
        {
            _isClimbing  = true;
            _velocity    = Vector3.zero;
        }
    }

    void UpdateClimb()
    {
        if (_input == null) return;

        // Stop climbing if button released or no wall detected
        bool wallAhead = Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            transform.forward, climbCheckDist + 0.1f, climbMask);

        if (!_input.ClimbHeld || !wallAhead)
        {
            _isClimbing = false;
            _velocity   = Vector3.zero;
            return;
        }

        // Move up/down using vertical input, strafe using horizontal
        Vector2 input = _input.Move;
        Vector3 climbMove = new Vector3(
            transform.right.x * input.x,
            input.y,
            transform.right.z * input.x) * climbSpeed;

        // Jump off wall
        if (_input.JumpPressed)
        {
            _isClimbing = false;
            _velocity   = -transform.forward * 3f;
            _velocity.y = jumpForce * 0.8f;
            _cc.Move(_velocity * Time.deltaTime);
            return;
        }

        _cc.Move(climbMove * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  PUBLIC
    // ─────────────────────────────────────────────

    public bool IsGrounded  => _cc.isGrounded;
    public bool IsDodging   => _isDodging;
    public bool IsClimbing  => _isClimbing;

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Climb detection ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.5f,
            transform.position + Vector3.up * 0.5f + transform.forward * climbCheckDist);

        // Stomp radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
    }

    // ─────────────────────────────────────────────
    //  STOMP ATTACK
    // ─────────────────────────────────────────────

    void TryStompAttack()
    {
        Vector3    feetPos = transform.position;
        Collider[] hits    = Physics.OverlapSphere(feetPos, stompRadius, enemyLayers);

        if (hits.Length == 0) return;

        bool hasStamina = _stats != null && _stats.SpendStamina(stompStaminaCost);

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>()
                          ?? hit.GetComponentInParent<IDamageable>();

            if (damageable == null) continue;

            float dmg = hasStamina ? stompDamage : stompDamage * 0.5f;
            damageable.TakeDamage(dmg, transform.position);

            // Bounce Kitty upward slightly
            _velocity.y = jumpForce * 0.5f;

            CombatFX.Instance?.OnHeavyHit(hit.transform.position);

            Debug.Log($"[Stomp] Hit {hit.gameObject.name} for {dmg} dmg | stamina={hasStamina}");
        }
    }
    private void TickFootsteps()
    {
        // Only play when grounded and actually moving
        float horizontalSpeed = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        bool moving = horizontalSpeed > 0.5f && IsGrounded;
        if (!moving)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= stepInterval)
        {
            _stepTimer = 0f;
            AudioManager.instance?.PlaySFX(AudioManager.instance.steps, 0f);
        }
    }

}