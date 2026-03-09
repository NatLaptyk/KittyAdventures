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
    public float walkSpeed   = 5f;
    public float rotateSpeed = 12f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravity   = -25f;

    [Header("Dodge")]
    public float dodgeSpeed    = 12f;
    public float dodgeDuration = 0.25f;

    [Header("Climb")]
    public float climbSpeed      = 3f;
    public float climbCheckDist  = 0.5f;
    public LayerMask climbMask;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    CharacterController _cc;
    InputReader         _input;
    CameraController    _cam;

    Vector3 _velocity;

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
        _cc    = GetComponent<CharacterController>();
        _input = GetComponent<InputReader>();
        _cam   = FindFirstObjectByType<CameraController>();

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
    }

    // ─────────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────────

    void UpdateMovement()
    {
        Vector2 input = _input != null ? _input.Move : Vector2.zero;
        float   yaw   = _cam   != null ? _cam.CameraYaw : 0f;

        // Camera-relative direction
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f)
                    * new Vector3(input.x, 0f, input.y);

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // Apply horizontal velocity
        _velocity.x = dir.x * walkSpeed;
        _velocity.z = dir.z * walkSpeed;

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
                _velocity.y = jumpForce;
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
    }
}
