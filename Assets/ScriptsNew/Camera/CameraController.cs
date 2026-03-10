// ─────────────────────────────────────────────────────────────────────────────
//  CameraController.cs  —  True Orbit Camera
//
//  The camera orbits around Kitty like a ball on a string.
//  No matter where you move the mouse, the camera ALWAYS points at Kitty.
//
//  HOW IT WORKS
//  ─────────────────────────────────────────────────────────────────────────
//  We don't use CinemachinePanTilt (that rotates the camera itself).
//  Instead:
//    1. Kitty's position is the orbit centre
//    2. Mouse X/Y builds a yaw + pitch rotation
//    3. Camera is placed at: centre + rotation * (0, 0, -distance)
//    4. Camera always calls LookAt(Kitty) so it faces inward
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach this script to your Main Camera GameObject.
//     (Not to Kitty — to the camera itself.)
//
//  2. Tag Kitty's root GameObject as "Player".
//
//  3. Make sure InputReader.cs is on Kitty.
//
//  4. Remove CinemachineBrain from the Main Camera if present.
//     Remove any CinemachineCamera objects from the scene.
//     This script drives the camera directly — no Cinemachine needed.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Target")]
    [Tooltip("Auto-found via Player tag. Drag Kitty in if needed.")]
    public Transform target;

    [Tooltip("Camera looks at this height above Kitty's feet. 1.2 = chest height.")]
    public float targetHeightOffset = 1.2f;

    [Tooltip("Shifts the orbit centre left/right relative to Kitty. " +
             "Positive = camera sits to Kitty's left (over right shoulder). " +
             "Try 0.4 to 0.6 for a classic over-shoulder feel. 0 = dead centre.")]
    public float horizontalOffset = 0f;

    [Header("Orbit")]
    public float distance    = 5f;
    public float sensitivityX = 3f;
    public float sensitivityY = 2f;
    public float pitchMin    = -10f;
    public float pitchMax    =  70f;

    [Header("Zoom")]
    public float zoomMin     = 2f;
    public float zoomMax     = 10f;
    public float zoomSpeed   = 4f;
    public float zoomDamping = 8f;

    [Header("Snap-Back Behind Kitty")]
    [Tooltip("Seconds of mouse inactivity before snapping back. 0 = disabled.")]
    public float snapBackDelay          = 2.5f;
    public float snapBackSpeed          = 80f;
    public float snapBackSpeedMoving    = 200f;

    [Header("Smoothing")]
    [Tooltip("How smoothly camera follows Kitty. Lower = snappier.")]
    public float followDamping  = 8f;
    [Range(0f, 0.9f)]
    public float mouseSmoothing = 0.08f;

    [Header("Collision")]
    [Tooltip("Pulls camera forward when terrain/walls block the view.")]
    public bool      enableCollision = true;
    public float     collisionRadius = 0.2f;
    [Tooltip("Set this to everything EXCEPT your Player layer to avoid camera snapping when Kitty jumps.")]
    public LayerMask collisionMask   = ~0;

    [Header("Camera Shake")]
    public float defaultShakeForce = 0.3f;
    public float shakeDecay        = 5f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    InputReader _input;

    float _yaw   = 0f;
    float _pitch = 15f;

    float   _targetDistance;
    float   _currentDistance;

    Vector2 _smoothedDelta;
    float   _idleTimer  = 0f;
    bool    _mouseMoved = false;

    Vector3 _smoothedOrbitCentre;
    bool    _initialized = false;

    float   _shakeIntensity = 0f;
    Vector3 _shakeOffset;

    // ─────────────────────────────────────────────
    //  PUBLIC
    // ─────────────────────────────────────────────

    /// <summary>Current camera yaw in world degrees.
    /// PlayerController uses this for camera-relative movement.</summary>
    public float CameraYaw => _yaw;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        // Auto-find Kitty
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
            else Debug.LogError("[CameraController] No GameObject tagged 'Player'!");
        }

        // Auto-find InputReader on Kitty
        if (target != null)
            _input = target.GetComponentInChildren<InputReader>();

        // Seed yaw to Kitty's facing so camera starts behind her
        if (target != null)
            _yaw = target.eulerAngles.y;

        _targetDistance  = distance;
        _currentDistance = distance;

        // Lock cursor for runtime mouse orbit
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // LateUpdate so camera moves AFTER Kitty has moved this frame
    void LateUpdate()
    {
        if (target == null) return;

        HandleMouseInput();
        HandleZoom();
        HandleSnapBack();
        //UpdateShake();
        ApplyOrbit();
    }

    // ─────────────────────────────────────────────
    //  MOUSE INPUT
    // ─────────────────────────────────────────────

    void HandleMouseInput()
    {
        // Press Escape to unlock cursor (e.g. to access menus)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        Vector2 raw = _input != null ? _input.Look : Vector2.zero;

        // Smooth input to remove jitter
        _smoothedDelta = Vector2.Lerp(_smoothedDelta, raw, 1f - mouseSmoothing);

        if (raw.sqrMagnitude > 0.01f)
        {
            _yaw   += _smoothedDelta.x * sensitivityX * Time.deltaTime * 60f;
            _pitch -= _smoothedDelta.y * sensitivityY * Time.deltaTime * 60f;
            _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            _idleTimer  = 0f;
            _mouseMoved = true;
        }
    }

    // ─────────────────────────────────────────────
    //  ZOOM
    // ─────────────────────────────────────────────

    void HandleZoom()
    {
        float scroll = _input != null ? _input.Zoom : 0f;

        if (Mathf.Abs(scroll) > 0.01f)
            _targetDistance = Mathf.Clamp(
                _targetDistance - scroll * zoomSpeed * 0.01f,
                zoomMin, zoomMax);

        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance,
                                        zoomDamping * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  SNAP-BACK
    // ─────────────────────────────────────────────

    void HandleSnapBack()
    {
        if (!_mouseMoved || snapBackDelay <= 0f) return;

        Vector2 raw        = _input != null ? _input.Look : Vector2.zero;
        bool    mouseActive = raw.sqrMagnitude > 0.01f;

        if (!mouseActive)
        {
            _idleTimer += Time.deltaTime;

            if (_idleTimer >= snapBackDelay)
            {
                float kittyYaw    = target.eulerAngles.y;
                bool  kittyMoving = _input != null &&
                                    _input.Move.sqrMagnitude > 0.05f;
                float speed       = kittyMoving ? snapBackSpeedMoving : snapBackSpeed;

                _yaw = Mathf.MoveTowardsAngle(_yaw, kittyYaw, speed * Time.deltaTime);

                if (Mathf.Abs(Mathf.DeltaAngle(_yaw, kittyYaw)) < 0.5f)
                {
                    _yaw        = kittyYaw;
                    _mouseMoved = false;
                    _idleTimer  = 0f;
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  APPLY ORBIT  —  the core of the script
    //
    //  Positions the camera at a point on a sphere
    //  centred on Kitty, then looks inward at her.
    // ─────────────────────────────────────────────

    void ApplyOrbit()
    {
        // Orbit centre = Kitty's position + height offset
        // Orbit centre — height offset + horizontal shoulder offset
        // The horizontal offset is relative to Kitty's facing direction
        Vector3 orbitCentre = target.position
                            + Vector3.up * targetHeightOffset
                            + target.right * horizontalOffset;

        // Smooth the centre so camera doesn't jitter when Kitty jumps
        if (!_initialized)
        {
            _smoothedOrbitCentre = orbitCentre;
            _initialized         = true;
        }
        _smoothedOrbitCentre = Vector3.Lerp(_smoothedOrbitCentre, orbitCentre,
                                             followDamping * Time.deltaTime);

        // Build rotation from yaw (horizontal) + pitch (vertical)
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Check collision — pull camera in if something blocks the view
        float dist = GetCollisionAdjustedDistance(rotation);

        // Place camera behind Kitty along the orbit direction
        // rotation * Vector3.back = the "behind" direction after applying yaw+pitch
        Vector3 camPos = _smoothedOrbitCentre + rotation * new Vector3(0f, 0f, -dist);

        // Apply position
        transform.position = camPos + _shakeOffset;

        // ALWAYS look at Kitty — this is what keeps her centred
        transform.LookAt(_smoothedOrbitCentre);
    }

    // ─────────────────────────────────────────────
    //  COLLISION
    // ─────────────────────────────────────────────

    float GetCollisionAdjustedDistance(Quaternion rotation)
    {
        if (!enableCollision) return _currentDistance;

        Vector3 direction = rotation * Vector3.back;

        // Ignore the Player layer entirely so Kitty's own collider
        // never triggers the pull-in during jumps
        int mask = collisionMask & ~(1 << LayerMask.NameToLayer("Player"));

        if (Physics.SphereCast(
                _smoothedOrbitCentre,
                collisionRadius,
                direction,
                out RaycastHit hit,
                _currentDistance,
                mask,
                QueryTriggerInteraction.Ignore))
        {
            // Smooth the pull-in so it doesn't snap
            float targetDist = Mathf.Max(hit.distance - 0.1f, zoomMin);
            return Mathf.Lerp(_currentDistance, targetDist, 15f * Time.deltaTime);
        }

        // Smoothly restore distance when no longer occluded
        return Mathf.Lerp(_currentDistance, _targetDistance, 6f * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  SHAKE
    // ─────────────────────────────────────────────

   void UpdateShake()
    {
        if (_shakeIntensity > 0.005f)
        {
            _shakeOffset    = Random.insideUnitSphere * _shakeIntensity;
            _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f,
                                          shakeDecay * Time.deltaTime);
        }
        else
        {
            _shakeIntensity = 0f;
            _shakeOffset    = Vector3.zero;
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>Trigger camera shake. Call from PlayerCombat on heavy hits.</summary>
    public void Shake(float force = -1f)
    {
        _shakeIntensity = force < 0f ? defaultShakeForce : force;
    }

    /// <summary>Instantly snap camera behind Kitty (after respawn, cutscene etc).</summary>
    public void SnapBehindKitty()
    {
        if (target != null) _yaw = target.eulerAngles.y;
        _mouseMoved = false;
        _idleTimer  = 0f;
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 centre = target.position + Vector3.up * targetHeightOffset;

        // Orbit centre
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centre, 0.15f);

        // Line from centre to camera
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(centre, transform.position);
    }
}
