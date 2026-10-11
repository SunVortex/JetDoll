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

    [Header("Smooth Step Bobbing (Zero Jitter)")]
    [Tooltip("Height of the vertical body bobbing with each step.")]
    public float stepBobAmount = 0.06f;
    [Tooltip("Stiffness of the vertical standing suspension.")]
    public float standSuspensionStrength = 22f;

    [Header("Ledge / Ground Detection")]
    [Tooltip("Downward distance to detect the rooftop floor.")]
    public float groundCheckDistance = 3.5f;
    [Tooltip("Assign the Ground layer here.")]
    public LayerMask groundLayer = ~0;

    public bool HasLeftRooftop { get; private set; } = false;

    private Rigidbody jetRb;
    private float baseStandingY = 0f;
    private float strideTimer = 0f;
    private int offLedgeFrames = 0;
    private bool isWalking = false;

    private void Awake()
    {
        jetRb = GetComponent<Rigidbody>();

        if (flightController != null)
        {
            flightController.enabled = false;
        }

        jetRb.constraints = RigidbodyConstraints.FreezeRotation;
        jetRb.useGravity = false;
    }

    private void Start()
    {
        baseStandingY = transform.position.y;
    }

    private void Update()
    {
        if (HasLeftRooftop) return;

        // WALKING LOCKED DURING MAIN MENU
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            isWalking = false;
            return;
        }

        isWalking = Input.GetKey(KeyCode.W);
    }

    private void FixedUpdate()
    {
        if (HasLeftRooftop) return;

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

        HandleRooftopMovement();
    }

    private void HandleRooftopMovement()
    {
        float targetBob = 0f;
        Vector3 forwardMove = Vector3.zero;

        if (isWalking)
        {
            strideTimer += Time.fixedDeltaTime * stepFrequency;
            float stride = Mathf.Sin(strideTimer);

            targetBob = Mathf.Abs(stride) * stepBobAmount;
            forwardMove = transform.forward * walkSpeed;

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
        }
        else
        {
            strideTimer = 0f;
            targetBob = 0f;

            if (leftKneeRb != null) leftKneeRb.linearVelocity = Vector3.zero;
            if (rightKneeRb != null) rightKneeRb.linearVelocity = Vector3.zero;
        }

        float targetY = baseStandingY + targetBob;
        float heightDifference = targetY - transform.position.y;
        float smoothVerticalVelocity = Mathf.Clamp(heightDifference * standSuspensionStrength, -4f, 4f);

#if UNITY_6000_0_OR_NEWER
        Vector3 vel = jetRb.linearVelocity;
        vel.x = forwardMove.x;
        vel.z = forwardMove.z;
        vel.y = smoothVerticalVelocity;
        jetRb.linearVelocity = vel;
#else
        Vector3 vel = jetRb.velocity;
        vel.x = forwardMove.x;
        vel.z = forwardMove.z;
        vel.y = smoothVerticalVelocity;
        jetRb.velocity = vel;
#endif
    }

    private void LaunchIntoFlight()
    {
        HasLeftRooftop = true;
        jetRb.constraints = RigidbodyConstraints.None;

        if (flightController != null)
        {
            flightController.enabled = true;
        }

        Destroy(this);
    }
}