// ─────────────────────────────────────────────────────────────────────────────
//  InputReader.cs
//
//  Receives input from PlayerInput (Invoke Unity Events mode) and exposes
//  clean properties for PlayerController and CameraController to read.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────
//  1. Attach to Kitty's root GameObject.
//  2. Add a PlayerInput component to Kitty.
//     - Actions      → your Input Actions asset
//     - Behavior     → Invoke Unity Events
//     - Default Map  → your action map name
//  3. Wire each action to the matching method below (see wiring guide).
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    // ── Read by CameraController ──────────────────────────────────────────────
    public Vector2 Move  { get; private set; }
    public Vector2 Look  { get; private set; }
    public float   Zoom  { get; private set; }

    // ── Read by PlayerController ──────────────────────────────────────────────
    public bool JumpPressed    { get; private set; }
    public bool DodgePressed   { get; private set; }
    public bool ClimbHeld      { get; private set; }
    public bool SprintHeld { get; private set; }

    // ── Read by PlayerCombat ──────────────────────────────────────────────────
    public bool LightPressed   { get; private set; }
    public bool HeavyPressed   { get; private set; }
    public bool ParryPressed   { get; private set; }

    // ── Read by Interactor ────────────────────────────────────────────────────
    public bool InteractPressed { get; private set; }

    // ─────────────────────────────────────────────
    //  PLAYERINPUT WIRING  (Invoke Unity Events)
    //
    //  In the PlayerInput component on Kitty:
    //  Events → [your map name] → wire each action:
    //
    //  Move        → InputReader.OnMove
    //  Look        → InputReader.OnLook
    //  Zoom        → InputReader.OnZoom
    //  Jump        → InputReader.OnJump
    //  Dodge       → InputReader.OnDodge
    //  Climb       → InputReader.OnClimb
    //  Light       → InputReader.OnLight
    //  Heavy       → InputReader.OnHeavy
    //  Parry       → InputReader.OnParry
    //  Interact    → InputReader.OnInteract
    // ─────────────────────────────────────────────

    public void OnMove(InputAction.CallbackContext ctx)
        => Move = ctx.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext ctx)
        => Look = ctx.ReadValue<Vector2>();

    public void OnZoom(InputAction.CallbackContext ctx)
        => Zoom = ctx.ReadValue<float>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        // performed fires exactly once on press
        if (ctx.performed) JumpPressed = true;
    }

    public void OnDodge(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) DodgePressed = true;
    }

    public void OnClimb(InputAction.CallbackContext ctx)
    {
        // held while pressing climb button
        ClimbHeld = ctx.performed || ctx.started;
        if (ctx.canceled) ClimbHeld = false;
    }

    public void OnLight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) LightPressed = true;
    }

    public void OnHeavy(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) HeavyPressed = true;
    }

    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) ParryPressed = true;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) InteractPressed = true;
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        SprintHeld = ctx.performed || ctx.started;
        if (ctx.canceled) SprintHeld = false;
    }

    // Clear one-frame flags after every frame
    void LateUpdate()
    {
        JumpPressed     = false;
        DodgePressed    = false;
        LightPressed    = false;
        HeavyPressed    = false;
        ParryPressed    = false;
        InteractPressed = false;
    }
}