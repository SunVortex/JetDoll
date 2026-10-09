using UnityEngine;

public class ProceduralStartWalk : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The JetpackFlightController on Jetpack_Vehicle.")]
    public JetpackFlightController flightController;
    [Tooltip("Assign mixamorig:LeftLeg (Left Knee / Calf).")]
    public Rigidbody leftKneeRb;
    [Tooltip("Assign mixamorig:RightLeg (Right Knee / Calf).")]
    public Rigidbody rightKneeRb;

    [Header("Walk & Knee Action Settings (Hold W)")]
    [Tooltip("Forward walk speed across the rooftop.")]
    public float walkSpeed = 4.8f;
    [Tooltip("How fast the legs pump back and forth.")]
    public float stepFrequency = 11f;
    [Tooltip("Forward kick strength of the knees.")]
    public float kneeKickPower = 35f;
    [Tooltip("Upward lift to bend knees and clear feet off the floor.")]
    public float footLiftPower = 15f;

    [Header("Juicy Step Bobbing (Up and Down Wobble)")]
    [Tooltip("Height of the vertical body bobbing with each step (meters).")]
    public float stepBobAmount = 0.07f;
    [Tooltip("Speed of the vertical bobbing return.")]
    public float bobSmoothSpeed = 14f;

    [Header("Ledge / Ground Detection")]
    [Tooltip("Downward distance to detect the rooftop floor.")]
    public float groundCheckDistance = 3.5f;
    [Tooltip("Assign the Ground layer here.")]
    public LayerMask groundLayer = ~0;

    // Public state
    public bool HasLeftRooftop { get; private set; } = false;

    private Rigidbody jetRb;
    private float baseStandingY = 0f;
    private float currentBobOffset = 0f;
    private float strideTimer = 0f;
    private int offLedgeFrames = 0;
    private bool isWalking = false;

    private void Awake()
    {
        jetRb = GetComponent<Rigidbody>();

        // Disable flight controller until we step off the ledge
        if (flightController != null)
        {
            flightController.enabled = false;
        }

        // Native Y-Freeze: locks height from gravity collapse
        jetRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        // Record starting standing elevation
        baseStandingY = transform.position.y;
    }

    private void Update()
    {
        if (HasLeftRooftop) return;

        // Walk only while holding W
        isWalking = Input.GetKey(KeyCode.W);
    }

    private void FixedUpdate()
    {
        if (HasLeftRooftop) return;

        // 1. CHECK IF STEPPED OFF THE LEDGE
        Ray ray = new Ray(transform.position, Vector3.down);
        bool groundDetected = Physics.Raycast(ray, groundCheckDistance, groundLayer);

        if (!groundDetected)
        {
            offLedgeFrames++;
            if (offLedgeFrames >= 3)
            {
                LaunchIntoFlight();
                return;
            }
        }
        else
        {
            offLedgeFrames = 0;
        }

        // 2. RUNWAY MOVEMENT, KNEE-DRIVE & STEP BOBBING
        HandleRooftopMovement();
    }

    private void HandleRooftopMovement()
    {
        if (isWalking)
        {
            // Advance stride cycle
            strideTimer += Time.fixedDeltaTime * stepFrequency;
            float stride = Mathf.Sin(strideTimer);

            // CALCULATE VERTICAL BOBBING (Bobs up on midstance, dips on plant)
            float targetBob = Mathf.Abs(Mathf.Sin(strideTimer)) * stepBobAmount;
            currentBobOffset = Mathf.Lerp(currentBobOffset, targetBob, bobSmoothSpeed * Time.fixedDeltaTime);

            // Forward movement along facing direction
            Vector3 forwardMove = transform.forward * walkSpeed;

            // KNEE-DRIVE ACTION (Bends knees and lifts feet)
            if (leftKneeRb != null)
            {
                float forwardForce = stride * kneeKickPower;
                float liftForce = Mathf.Max(0f, stride) * footLiftPower;
                Vector3 leftKick = (transform.forward * forwardForce) + (Vector3.up * liftForce);
                leftKneeRb.AddForce(leftKick, ForceMode.Acceleration);
            }

            if (rightKneeRb != null)
            {
                float forwardForce = -stride * kneeKickPower;
                float liftForce = Mathf.Max(0f, -stride) * footLiftPower;
                Vector3 rightKick = (transform.forward * forwardForce) + (Vector3.up * liftForce);
                rightKneeRb.AddForce(rightKick, ForceMode.Acceleration);
            }

            // Apply forward velocity
#if UNITY_6000_0_OR_NEWER
            Vector3 vel = jetRb.linearVelocity;
            vel.x = forwardMove.x;
            vel.z = forwardMove.z;
            jetRb.linearVelocity = vel;
#else
            Vector3 vel = jetRb.velocity;
            vel.x = forwardMove.x;
            vel.z = forwardMove.z;
            jetRb.velocity = vel;
#endif

            // Apply vertical bobbing height cleanly
            Vector3 bobbedPos = jetRb.position;
            bobbedPos.y = baseStandingY + currentBobOffset;
            jetRb.position = bobbedPos;
        }
        else
        {
            // IDLE: Settle smoothly to base standing height
            strideTimer = 0f;
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobSmoothSpeed * Time.fixedDeltaTime);

            Vector3 settledPos = jetRb.position;
            settledPos.y = baseStandingY + currentBobOffset;
            jetRb.position = settledPos;

            if (leftKneeRb != null) leftKneeRb.linearVelocity = Vector3.zero;
            if (rightKneeRb != null) rightKneeRb.linearVelocity = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
            Vector3 vel = jetRb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            jetRb.linearVelocity = vel;
#else
            Vector3 vel = jetRb.velocity;
            vel.x = 0f;
            vel.z = 0f;
            jetRb.velocity = vel;
#endif
        }
    }

    private void LaunchIntoFlight()
    {
        HasLeftRooftop = true;

        // UNFREEZE Y: Drops ragdoll naturally into freefall!
        jetRb.constraints = RigidbodyConstraints.None;

        // Turn on the flight controller
        if (flightController != null)
        {
            flightController.enabled = true;
        }

        // Destroy walk component cleanly so flight takes full control
        Destroy(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasLeftRooftop ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.down * groundCheckDistance));
    }
}