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

    private InputAction moveAction;
    private Vector3 moveForce;
    private bool isDrifting;
    private bool canMove;

    public bool IsConfigured => body != null && playerInput != null;

    public void Initialize()
    {
        body.constraints = RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        body.centerOfMass = new Vector3(0f, -0.5f, 0f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        moveForce = Vector3.zero;
        moveAction = null;
        isDrifting = false;
        canMove = true;
    }

    public void TickPhysics(float deltaTime)
    {
        if (!IsConfigured || !body.gameObject.activeInHierarchy) return;
        if (!canMove)
        {
            body.velocity = Vector3.zero;
            return;
        }

        if (moveAction == null) moveAction = playerInput.actions.FindAction("Move", true);
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
    }

    public void Break(InputAction.CallbackContext context)
    {
        if (context.performed) isDrifting = true;
        else if (context.canceled) isDrifting = false;
    }

    public void SetMovementEnabled(bool value) => canMove = value;
}
