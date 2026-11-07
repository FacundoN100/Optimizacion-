using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public sealed class DrivingControlsFeedback : IDisposable
{
    private readonly PlayerInput playerInput;
    private readonly Image wheelImage;
    private readonly Image brakeImage;
    private readonly Color wheelColor;
    private readonly Color brakeColor;
    private readonly RectTransform wheelVisual;
    private InputAction move;
    private InputAction brake;
    private bool wasPressed;
    private float angle;

    public DrivingControlsFeedback(PlayerInput playerInput)
    {
        this.playerInput = playerInput;
        foreach (OnScreenStick stick in UnityEngine.Object.FindObjectsByType<OnScreenStick>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (stick.gameObject.scene == playerInput.gameObject.scene && stick.controlPath == "<Gamepad>/leftStick")
            {
                wheelImage = stick.GetComponent<Image>();
                break;
            }
        foreach (OnScreenButton button in UnityEngine.Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (button.gameObject.scene == playerInput.gameObject.scene && button.controlPath == "<Gamepad>/buttonSouth")
            {
                brakeImage = button.GetComponent<Image>();
                break;
            }
        if (wheelImage != null)
        {
            wheelColor = wheelImage.color;
            var visual = new GameObject("Steering Wheel Visual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            wheelVisual = (RectTransform)visual.transform;
            wheelVisual.SetParent(wheelImage.transform, false);
            wheelVisual.anchorMin = Vector2.zero;
            wheelVisual.anchorMax = Vector2.one;
            wheelVisual.sizeDelta = Vector2.zero;
            var image = visual.GetComponent<Image>();
            image.sprite = wheelImage.overrideSprite;
            image.type = wheelImage.type;
            image.preserveAspect = wheelImage.preserveAspect;
            image.material = wheelImage.material;
            image.color = wheelColor;
            image.raycastTarget = false;
            Color transparent = wheelColor;
            transparent.a = 0.001f;
            wheelImage.color = transparent;
        }
        if (brakeImage != null)
        {
            brakeColor = brakeImage.color;
            Color released = brakeColor;
            released.a *= 0.35f;
            brakeImage.color = released;
        }
    }

    public void Tick(float unscaledDeltaTime)
    {
        if (playerInput == null || playerInput.actions == null) return;
        if (move == null || move.actionMap.asset != playerInput.actions)
        {
            move = playerInput.actions.FindAction("Move");
            brake = playerInput.actions.FindAction("Break");
        }
        float steering = move != null && move.enabled ? move.ReadValue<Vector2>().x : 0f;
        angle = Mathf.LerpAngle(angle, -steering * 35f, 1f - Mathf.Exp(-14f * unscaledDeltaTime));
        if (wheelVisual != null) wheelVisual.localRotation = Quaternion.Euler(0f, 0f, angle);
        bool pressed = brake != null && brake.enabled && brake.IsPressed();
        if (brakeImage != null)
        {
            Color color = brakeColor;
            color.a = Mathf.Lerp(brakeImage.color.a, brakeColor.a * (pressed ? 1f : 0.35f), 1f - Mathf.Exp(-18f * unscaledDeltaTime));
            brakeImage.color = color;
        }
        if (pressed && !wasPressed && ServiceLocator.TryGet<IGameAudio>(out var audio)) audio.PlayButtonClick();
        wasPressed = pressed;
    }

    public void Dispose()
    {
        if (wheelImage != null) wheelImage.color = wheelColor;
        if (brakeImage != null) brakeImage.color = brakeColor;
        if (wheelVisual != null) UnityEngine.Object.Destroy(wheelVisual.gameObject);
    }
}
