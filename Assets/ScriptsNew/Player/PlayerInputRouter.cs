using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputRouter : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    public bool LightPressed   { get; private set; }
    public bool HeavyPressed   { get; private set; }
    public bool DodgePressed   { get; private set; }
    public bool ParryPressed   { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool JumpPressed    { get; private set; }

    public event Action PausePressed;

     // --- Unity Event callbacks (PlayerInput -> Events) ---
    public void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        Look = ctx.ReadValue<Vector2>();
    }

    public void OnLight(InputAction.CallbackContext ctx)
    {
        if (ctx.started) LightPressed = true;
    }

    public void OnHeavy(InputAction.CallbackContext ctx)
    {
        if (ctx.started) HeavyPressed = true;
    }

    public void OnDodge(InputAction.CallbackContext ctx)
    {
        if (ctx.started) DodgePressed = true;
    }

    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (ctx.started) ParryPressed = true;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started) InteractPressed = true;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) JumpPressed = true;
    }


    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.started) PausePressed?.Invoke();
    }
// Reset one-frame buttons
    void LateUpdate()
    {
        LightPressed    = false;
        HeavyPressed    = false;
        DodgePressed    = false;
        ParryPressed    = false;
        InteractPressed = false;
        JumpPressed     = false;
    }
}