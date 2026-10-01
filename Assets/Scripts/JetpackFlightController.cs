using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JetpackFlightController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera that defines forward aim.")]
    public Transform cameraTransform;

    [Header("Direct Speed Controls (Tunable in Inspector)")]
    [Tooltip("Top flight speed (m/s) when holding RMB + W.")]
    [Range(10f, 80f)]
    public float maxBoostSpeed = 35f;

    [Tooltip("Gentle cruise speed (m/s) when holding ONLY RMB.")]
    [Range(2f, 25f)]
    public float idleCruiseSpeed = 10f;

    [Tooltip("How fast the jetpack reaches top speed (responsiveness).")]
    public float boostAcceleration = 22f;

    [Tooltip("Sideways strafe speed (m/s) with A & D.")]
    [Range(5f, 35f)]
    public float strafeSpeed = 14f;

    [Header("Corridor Altitude Lock")]
    [Tooltip("Target cruise height above the floor (meters).")]
    public float cruiseAltitude = 3.0f;
    [Tooltip("Maximum ceiling (stops you from flying over corridor walls).")]
    public float maxCeilingHeight = 4.5f;
    [Tooltip("Climb speed to reach cruise altitude.")]
    public float climbSpeed = 6.0f;

    [Header("Snap A/D Banking & Mouse Steering")]
    [Tooltip("How fast the jetpack snaps to face your mouse crosshair.")]
    public float mouseTurnSpeed = 22f;
    [Tooltip("Fighter-jet roll bank angle when pressing A or D (degrees).")]
    public float bankAngle = 35f;
    [Tooltip("How snappy the banking tilt is when pressing/releasing A and D.")]
    public float bankSnapSpeed = 14f;
    [Tooltip("Horizontal flight pitch when flying forward toward Z.")]
    public float flightPitch = 70f;
    [Tooltip("Upright pitch when RMB is released.")]
    public float uprightPitch = 0f;

    [Header("Air Drag & Descent")]
    public float brakeDrag = 3.0f;
    public float flightDrag = 0.8f;

    // Public state for Camera
    public bool IsFullBurning { get; private set; }
    public bool IsIgnited { get; private set; }

    private Rigidbody jetRb;
    private Rigidbody[] pilotRagdollRbs;
    private float currentBank = 0f;
    private float currentPitch = 0f;
    private float startingFloorY = 0f;

    private void Awake()
    {
        jetRb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Cache all ragdoll bones connected via harness
        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null && joint.connectedBody != null)
        {
            pilotRagdollRbs = joint.connectedBody.transform.root.GetComponentsInChildren<Rigidbody>();
        }

        startingFloorY = transform.position.y;
    }

    private void Update()
    {
        bool rmb = Input.GetMouseButton(1);
        bool wKey = Input.GetKey(KeyCode.W);
        IsFullBurning = rmb && wKey;
        IsIgnited = rmb;
    }

    private void FixedUpdate()
    {
        // VIRTUAL GYROSCOPE: Cancels all unwanted physics spins from the ragdoll
        jetRb.angularVelocity = Vector3.zero;

        HandleFlight();
    }

    private void HandleFlight()
    {
        bool rmb = Input.GetMouseButton(1);
        bool wKey = Input.GetKey(KeyCode.W);

        float hInput = 0f;
        if (Input.GetKey(KeyCode.A)) hInput -= 1f;
        if (Input.GetKey(KeyCode.D)) hInput += 1f;

        // 1. ENGINE OFF / DESCENT (RMB Released)
        if (!rmb)
        {
            SetRagdollGravity(true);
            jetRb.useGravity = true;
            ApplyDrag(brakeDrag);

            // Return upright to land on feet
            currentPitch = Mathf.Lerp(currentPitch, uprightPitch, 8f * Time.fixedDeltaTime);
            currentBank = Mathf.Lerp(currentBank, 0f, bankSnapSpeed * Time.fixedDeltaTime);

            Quaternion landRot = Quaternion.Euler(currentPitch, transform.eulerAngles.y, currentBank);
            jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, landRot, mouseTurnSpeed * Time.fixedDeltaTime));
            return;
        }

        // 2. ENGINE ACTIVE (Anti-Gravity enabled)
        SetRagdollGravity(false);
        jetRb.useGravity = false;
        ApplyDrag(flightDrag);

        // 3. TARGET PITCH & SNAP BANKING (A & D)
        float targetPitch = wKey ? flightPitch : 25f;
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, 8f * Time.fixedDeltaTime);

        float targetBank = -hInput * bankAngle;
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSnapSpeed * Time.fixedDeltaTime);

        // 4. RESPONSIVE MOUSE AIM DIRECTION
        Vector3 rawAim = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(rawAim, Vector3.up).normalized;
        Vector3 horizontalRight = Vector3.ProjectOnPlane(cameraTransform != null ? cameraTransform.right : Vector3.right, Vector3.up).normalized;

        Quaternion aimRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        Quaternion stanceRotation = Quaternion.Euler(currentPitch, 0f, currentBank);
        Quaternion finalTargetRot = aimRotation * stanceRotation;

        jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, finalTargetRot, mouseTurnSpeed * Time.fixedDeltaTime));

        // 5. CORRIDOR ALTITUDE LOCK
        float currentHeightAboveFloor = transform.position.y - startingFloorY;
        float targetYVelocity = 0f;

        if (currentHeightAboveFloor < cruiseAltitude)
        {
            targetYVelocity = climbSpeed;
        }
        else if (currentHeightAboveFloor > maxCeilingHeight)
        {
            targetYVelocity = -climbSpeed;
        }
        else
        {
            targetYVelocity = 0f; // Firm level hover
        }

        // 6. DIRECT VELOCITY SPEED CONTROL (Predictable & Tunable!)
        float targetSpeed = wKey ? maxBoostSpeed : idleCruiseSpeed;
        Vector3 targetHorizontalVelocity = (horizontalForward * targetSpeed) + (horizontalRight * (hInput * strafeSpeed));

#if UNITY_6000_0_OR_NEWER
        Vector3 currentVel = jetRb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 smoothedHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontalVelocity, boostAcceleration * Time.fixedDeltaTime);

        currentVel.x = smoothedHorizontal.x;
        currentVel.z = smoothedHorizontal.z;
        currentVel.y = Mathf.MoveTowards(currentVel.y, targetYVelocity, 35f * Time.fixedDeltaTime);
        jetRb.linearVelocity = currentVel;
#else
        Vector3 currentVel = jetRb.velocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 smoothedHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontalVelocity, boostAcceleration * Time.fixedDeltaTime);

        currentVel.x = smoothedHorizontal.x;
        currentVel.z = smoothedHorizontal.z;
        currentVel.y = Mathf.MoveTowards(currentVel.y, targetYVelocity, 35f * Time.fixedDeltaTime);
        jetRb.velocity = currentVel;
#endif
    }

    private void SetRagdollGravity(bool useGravity)
    {
        if (pilotRagdollRbs == null) return;

        for (int i = 0; i < pilotRagdollRbs.Length; i++)
        {
            if (pilotRagdollRbs[i] != null)
            {
                pilotRagdollRbs[i].useGravity = useGravity;
            }
        }
    }

    private void ApplyDrag(float value)
    {
#if UNITY_6000_0_OR_NEWER
        jetRb.linearDamping = value;
#else
        jetRb.drag = value;
#endif
    }
}