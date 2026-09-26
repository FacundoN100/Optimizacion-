using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    // Settings
    [SerializeField] private float MoveSpeed = 50f;
    [SerializeField] private float MaxSpeed = 15f;
    [SerializeField] private float Drag = 0.98f;
    [SerializeField] private float DriftSteerAngle = 20f;
    [SerializeField] private float Traction = 1f;
    [SerializeField] private float Downforce = 5f; // nuevo: intensidad del downforce
    private float SteerAngle = 20f;
    private PlayerInput playerInput;
    private Vector2 input;
    private bool isDrifting;

    // Variables
    private Vector3 MoveForce;
    private Rigidbody rb;
    private bool canMove;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        playerInput = GetComponent<PlayerInput>();
        isDrifting = false;
        canMove = true;
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            input = playerInput.actions["Move"].ReadValue<Vector2>();
            if (isDrifting)
            {
                SteerAngle = DriftSteerAngle;
            }
            else
            {
                SteerAngle = DriftSteerAngle / 2;
            }
            MoveForce += transform.forward * MoveSpeed * input.y * Time.fixedDeltaTime;
            MoveForce *= Drag;
            MoveForce = Vector3.ClampMagnitude(MoveForce, MaxSpeed);
            MoveForce = Vector3.Lerp(MoveForce.normalized, transform.forward, Traction * Time.fixedDeltaTime) * MoveForce.magnitude;
            Vector3 desiredVelocity = MoveForce;
            Vector3 velChange = desiredVelocity - rb.velocity;
            rb.AddForce(velChange, ForceMode.VelocityChange);
            float yaw = input.x * MoveForce.magnitude * SteerAngle * Time.fixedDeltaTime;
            Quaternion deltaRot = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(rb.rotation * deltaRot);
            rb.AddForce(-transform.up * Downforce, ForceMode.Acceleration);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    public void Break(InputAction.CallbackContext callbackContext)
    {
        if(callbackContext.performed)
        {
            isDrifting = true;
        }
        else if(callbackContext.canceled)
        {
            isDrifting = false;
        }
    }

    public void ImDead(bool state)
    {
        canMove = state;
    }
}