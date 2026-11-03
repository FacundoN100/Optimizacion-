using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class CarController
{
    [SerializeField] private float MoveSpeed = 50f;
    [SerializeField] private float MaxSpeed = 15f;
    [SerializeField] private float Drag = 0.98f;
    [SerializeField] private float DriftSteerAngle = 20f;
    [SerializeField] private float Traction = 1f;
    [SerializeField] private float Downforce = 5f;
    [SerializeField] private Rigidbody body;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private ParticleSystem[] driftSmoke;
    [SerializeField, Min(0f)] private float smokeMinSpeed = 3f;

    private InputAction moveAction;
    private InputAction brakeAction;
    private Vector3 moveForce;
    private bool isDrifting;
    private bool canMove;
    private bool smokeEnabled;
    private bool smokeEmitting;
    private float smokeGraceRemaining;

    public bool IsConfigured => body != null && playerInput != null;
    public Rigidbody Body => body;

    public void Initialize()
    {
        body.constraints = RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        body.centerOfMass = new Vector3(0f, -0.5f, 0f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        moveForce = Vector3.zero;
        moveAction = null;
        brakeAction = null;
        isDrifting = false;
        canMove = true;
        smokeEnabled = true;
        smokeEmitting = false;
        smokeGraceRemaining = 0f;
        if (driftSmoke != null)
            foreach (ParticleSystem smoke in driftSmoke)
                if (smoke != null) smoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void TickPhysics(float deltaTime)
    {
        if (!IsConfigured || !body.gameObject.activeInHierarchy)
        {
            StopDriftSmoke();
            return;
        }
        if (!canMove)
        {
            body.velocity = Vector3.zero;
            StopDriftSmoke();
            return;
        }

        InputActionAsset actions = playerInput.actions;
        if (actions == null)
        {
            StopDriftSmoke();
            return;
        }
        if (moveAction == null || moveAction.actionMap.asset != actions)
        {
            moveAction = actions.FindAction("Move", true);
            brakeAction = actions.FindAction("Break");
        }
        if (brakeAction != null) isDrifting = brakeAction.enabled && brakeAction.IsPressed();
        Vector2 input = moveAction.ReadValue<Vector2>();
        Transform carTransform = body.transform;
        float steerAngle = isDrifting ? DriftSteerAngle : DriftSteerAngle / 2f;
        moveForce += carTransform.forward * MoveSpeed * input.y * deltaTime;
        moveForce *= Drag;
        moveForce = Vector3.ClampMagnitude(moveForce, MaxSpeed);
        body.AddForce(moveForce - body.velocity, ForceMode.VelocityChange);
        float signedSpeed = Vector3.Dot(moveForce, carTransform.forward);
        float yaw = input.x * signedSpeed * steerAngle * deltaTime;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));
        body.AddForce(-carTransform.up * Downforce, ForceMode.Acceleration);
        Vector3 planarVelocity = Vector3.ProjectOnPlane(body.velocity, carTransform.up);
        UpdateDriftSmoke(input.x, planarVelocity, carTransform.right, deltaTime);
    }

    public void Break(InputAction.CallbackContext context)
    {
        if (context.performed) isDrifting = true;
        else if (context.canceled)
        {
            isDrifting = context.action != null && context.action.enabled && context.action.IsPressed();
            if (!isDrifting) StopDriftSmoke();
        }
    }

    public void SetMovementEnabled(bool value)
    {
        canMove = value;
        if (!value) StopDriftSmoke();
    }

    public void SetDriftSmokeEnabled(bool value)
    {
        smokeEnabled = value;
        if (!value) StopDriftSmoke();
    }

    public void StopDriftSmoke()
    {
        smokeGraceRemaining = 0f;
        SetSmokeEmission(false);
    }

    private void UpdateDriftSmoke(float steering, Vector3 velocity, Vector3 right, float deltaTime)
    {
        float speedSquared = velocity.sqrMagnitude;
        float minSpeed = smokeMinSpeed * (smokeEmitting ? 0.7f : 1f);
        if (!smokeEnabled || !isDrifting || speedSquared < Mathf.Max(0.01f, minSpeed * minSpeed))
        {
            StopDriftSmoke();
            return;
        }

        float sidewaysSpeed = Vector3.Dot(velocity, right);
        float steeringThreshold = smokeEmitting ? 0.05f : 0.1f;
        bool sliding = sidewaysSpeed * sidewaysSpeed >= speedSquared * 0.0144f;
        if (Mathf.Abs(steering) >= steeringThreshold || sliding) smokeGraceRemaining = 0.12f;
        else smokeGraceRemaining = Mathf.Max(0f, smokeGraceRemaining - deltaTime);
        SetSmokeEmission(smokeGraceRemaining > 0f);
    }

    private void SetSmokeEmission(bool emitting)
    {
        smokeEmitting = emitting;
        if (driftSmoke == null) return;
        foreach (ParticleSystem smoke in driftSmoke)
        {
            if (smoke == null || !smoke.gameObject.activeInHierarchy) continue;
            if (emitting)
            {
                if (smoke.isEmitting && !smoke.isPaused) continue;
                var emission = smoke.emission;
                emission.enabled = true;
                smoke.Play(false);
                if (smoke.particleCount == 0) smoke.Emit(1);
            }
            else if (smoke.isEmitting || smoke.isPaused)
                smoke.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
