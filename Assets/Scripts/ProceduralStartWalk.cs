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

    [Header("Mobile Walk Button")]
    [Tooltip("Drag the on-screen UP button from Canvas here (Auto-hides in the air!).")]
    public MobileTouchButton walkUpButton;

    [Header("Launch Pad Detection")]
    public LayerMask launchPadLayer = ~0;
    public float padCheckDistance = 8.0f;

    [Header("Walk & Knee Action Settings")]
    public float walkSpeed = 5.0f;
    public float stepFrequency = 11f;
    public float kneeKickPower = 35f;
    public float footLiftPower = 15f;

    [Header("Smooth Step Bobbing")]
    public float stepBobAmount = 0.06f;
    public float standSuspensionStrength = 22f;

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

        // Block walking while on Main Menu
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            isWalking = false;
            return;
        }

        // Direct flight takeover: holding RMB on PC or right screen on mobile launches flight
        bool wantsFlight = Input.GetMouseButton(1);
        if (flightController != null && flightController.touchAimBoostZone != null && flightController.touchAimBoostZone.IsHolding)
        {
            wantsFlight = true;
        }

        if (wantsFlight)
        {
            LaunchIntoFlight();
            return;
        }

        // UNIFIED WALK INPUT: W on PC -OR- UP Button on Mobile
        bool pcWalk = Input.GetKey(KeyCode.W);
        bool mobileWalk = walkUpButton != null && walkUpButton.IsPressed;
        isWalking = pcWalk || mobileWalk;
    }

    private void FixedUpdate()
    {
        if (HasLeftRooftop) return;

        Ray ray = new Ray(transform.position, Vector3.down);
        bool padDetected = Physics.Raycast(ray, padCheckDistance, launchPadLayer);

        if (!padDetected)
        {
            offLedgeFrames++;
            if (offLedgeFrames >= 6)
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
        if (HasLeftRooftop) return;
        HasLeftRooftop = true;

        // AUTO-HIDE: Turn OFF the UP walk button so it vanishes in the air!
        if (walkUpButton != null)
        {
            walkUpButton.gameObject.SetActive(false);
        }

        jetRb.constraints = RigidbodyConstraints.None;

        if (flightController != null)
        {
            flightController.enabled = true;
        }

        Destroy(this);
    }
}