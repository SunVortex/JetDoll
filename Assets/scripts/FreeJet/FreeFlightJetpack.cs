using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlightJetpack : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("The Main Camera with the FreeFlightCamera script.")]
    public Transform cameraTransform;

    [Header("Boost Fuel Meter (UI Slider)")]
    [Tooltip("Drag your vertical UI Slider here.")]
    public Slider boostSlider;
    public float maxBoostDuration = 5.0f;
    public float fuelRechargeRate = 1.2f;
    public float rechargeStartDelay = 0.4f;

    [Header("Flight Speeds (Tunable in Inspector)")]
    public float cruiseSpeed = 15f;
    public float turboBoostSpeed = 42f;
    public float verticalClimbSpeed = 12f;
    public float acceleration = 25f;

    [Header("Movement-Facing Rotation (See Player Face!)")]
    [Tooltip("How fast the character turns to face their WASD movement direction.")]
    public float rotationTurnSpeed = 14f;
    [Tooltip("Forward leaning tilt while flying forward (degrees).")]
    public float forwardPitchAngle = 35f;

    [Header("Idle Hover Floating Bob (No Stale Static Freeze)")]
    public float hoverBobHeight = 0.14f;
    public float hoverBobSpeed = 2.0f;
    public float bobBlendSpeed = 5.0f;

    [Header("Thruster Particle Systems & Boost Surge")]
    [Tooltip("Drag all 4 particle systems here.")]
    public ParticleSystem[] thrusterParticles;
    public float idleHoverEmission = 15f;
    public float cruiseEmission = 40f;
    public float boostEmission = 95f;

    [Tooltip("How much longer the flame shoots backward during turbo boost (multiplier).")]
    public float boostFlameLengthMultiplier = 2.2f;

    [Tooltip("How much thicker/wider the flame expands during turbo boost (multiplier).")]
    public float boostFlameSizeMultiplier = 1.6f;

    [Tooltip("How smoothly the flame swells up and shrinks back down.")]
    public float throttleSpoolSpeed = 7f;

    // Public state for camera & effects
    public bool IsBoosting { get; private set; }
    public bool IsMoving { get; private set; }
    public float CurrentFuel { get; private set; }

    private Rigidbody jetRb;
    private Rigidbody[] pilotRagdollRbs;
    private float currentEmission = 0f;
    private float currentLengthMult = 1.0f;
    private float currentSizeMult = 1.0f;
    private float currentPitch = 0f;
    private float rechargeTimer = 0f;

    // Hover & Rotation state
    private float hoverBaseY = 0f;
    private float currentBobWeight = 0f;
    private float bobTimer = 0f;
    private Quaternion targetFacingRotation;

    // Cached baseline particle scales
    private float[] baseStartSpeeds;
    private float[] baseStartSizes;

    private void Awake()
    {
        jetRb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null && joint.connectedBody != null)
        {
            pilotRagdollRbs = joint.connectedBody.transform.root.GetComponentsInChildren<Rigidbody>();
        }

        // Clean static hover
        jetRb.useGravity = false;
        SetRagdollGravity(false);

        // Initialize fuel
        CurrentFuel = maxBoostDuration;
        if (boostSlider != null)
        {
            boostSlider.minValue = 0f;
            boostSlider.maxValue = 1f;
            boostSlider.value = 1f;
        }

        hoverBaseY = transform.position.y;
        targetFacingRotation = transform.rotation;

        // Cache base particle parameters so multipliers never compound/drift
        if (thrusterParticles != null && thrusterParticles.Length > 0)
        {
            baseStartSpeeds = new float[thrusterParticles.Length];
            baseStartSizes = new float[thrusterParticles.Length];

            for (int i = 0; i < thrusterParticles.Length; i++)
            {
                if (thrusterParticles[i] != null)
                {
                    baseStartSpeeds[i] = thrusterParticles[i].main.startSpeedMultiplier;
                    baseStartSizes[i] = thrusterParticles[i].main.startSizeMultiplier;

                    if (!thrusterParticles[i].isPlaying)
                    {
                        thrusterParticles[i].Play();
                    }
                }
            }
        }
    }

    private void Update()
    {
        HandleFuelAndBoostInput();
        HandleParticleThrottle();
    }

    private void FixedUpdate()
    {
        jetRb.angularVelocity = Vector3.zero;
        HandleFlightPhysics();
    }

    private void HandleFuelAndBoostInput()
    {
        bool rmbHeld = Input.GetMouseButton(1);

        if (rmbHeld && CurrentFuel > 0f)
        {
            IsBoosting = true;
            CurrentFuel = Mathf.Max(0f, CurrentFuel - Time.deltaTime);
            rechargeTimer = 0f;

            if (CurrentFuel <= 0f) IsBoosting = false;
        }
        else
        {
            IsBoosting = false;
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= rechargeStartDelay)
            {
                CurrentFuel = Mathf.Min(maxBoostDuration, CurrentFuel + (Time.deltaTime * fuelRechargeRate));
            }
        }

        if (boostSlider != null)
        {
            boostSlider.value = CurrentFuel / maxBoostDuration;
        }
    }

    private void HandleFlightPhysics()
    {
        // 1. GATHER INPUTS
        float hInput = 0f;
        if (Input.GetKey(KeyCode.A)) hInput -= 1f;
        if (Input.GetKey(KeyCode.D)) hInput += 1f;

        float vInput = 0f;
        if (Input.GetKey(KeyCode.S)) vInput -= 1f;
        if (Input.GetKey(KeyCode.W)) vInput += 1f;

        float upInput = 0f;
        if (Input.GetKey(KeyCode.Space)) upInput += 1f;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) upInput -= 1f;

        bool hasHorizontalInput = Mathf.Abs(hInput) > 0.05f || Mathf.Abs(vInput) > 0.05f;
        IsMoving = hasHorizontalInput || Mathf.Abs(upInput) > 0.05f;

        // 2. CAMERA-RELATIVE DIRECTION VECTORS
        Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;

        Vector3 flatCamForward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        Vector3 flatCamRight = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized;

        Vector3 horizontalMoveDir = (flatCamForward * vInput) + (flatCamRight * hInput);
        if (horizontalMoveDir.sqrMagnitude > 1f) horizontalMoveDir.Normalize();

        Vector3 totalMoveDirection = horizontalMoveDir + (Vector3.up * upInput);

        // 3. TARGET SPEED
        float targetSpeed = IsBoosting ? turboBoostSpeed : cruiseSpeed;
        Vector3 targetVelocity = totalMoveDirection * targetSpeed;

        // 4. DYNAMIC HOVER BOB
        if (IsMoving)
        {
            currentBobWeight = Mathf.Lerp(currentBobWeight, 0f, bobBlendSpeed * Time.fixedDeltaTime);
            hoverBaseY = transform.position.y;
        }
        else
        {
            currentBobWeight = Mathf.Lerp(currentBobWeight, 1f, bobBlendSpeed * Time.fixedDeltaTime);
            bobTimer += Time.fixedDeltaTime * hoverBobSpeed;

            float bobOffset = Mathf.Sin(bobTimer) * hoverBobHeight;
            float targetHoverY = hoverBaseY + bobOffset;
            float verticalError = targetHoverY - transform.position.y;

            targetVelocity.y += verticalError * 15f * currentBobWeight;
        }

        // 5. SMOOTH VELOCITY LERP
#if UNITY_6000_0_OR_NEWER
        Vector3 currentVel = jetRb.linearVelocity;
        jetRb.linearVelocity = Vector3.MoveTowards(currentVel, targetVelocity, acceleration * Time.fixedDeltaTime);
#else
        Vector3 currentVel = jetRb.velocity;
        jetRb.velocity = Vector3.MoveTowards(currentVel, targetVelocity, acceleration * Time.fixedDeltaTime);
#endif

        // 6. MOVEMENT-FACING ROTATION (Allows 360 camera orbit to see player's face!)
        if (hasHorizontalInput)
        {
            Quaternion movementLookRotation = Quaternion.LookRotation(horizontalMoveDir, Vector3.up);
            targetFacingRotation = movementLookRotation;

            currentPitch = Mathf.Lerp(currentPitch, forwardPitchAngle, 8f * Time.fixedDeltaTime);
        }
        else
        {
            currentPitch = Mathf.Lerp(currentPitch, 0f, 8f * Time.fixedDeltaTime);
        }

        Quaternion leanRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion finalTargetRotation = targetFacingRotation * leanRotation;

        jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, finalTargetRotation, rotationTurnSpeed * Time.fixedDeltaTime));
    }

    private void HandleParticleThrottle()
    {
        float targetEmission = 0f;
        float targetLength = 1.0f;
        float targetSize = 1.0f;

        if (IsBoosting)
        {
            // TURBO BOOST: Maximum emission + stretched length + expanded thickness!
            targetEmission = boostEmission;
            targetLength = boostFlameLengthMultiplier;
            targetSize = boostFlameSizeMultiplier;
        }
        else if (IsMoving)
        {
            // CRUISING: Standard flight flame
            targetEmission = cruiseEmission;
            targetLength = 1.0f;
            targetSize = 1.0f;
        }
        else
        {
            // IDLE HOVER: Soft, short idling flame
            targetEmission = idleHoverEmission;
            targetLength = 0.65f;
            targetSize = 0.8f;
        }

        // Smoothly interpolate all three parameters together
        currentEmission = Mathf.Lerp(currentEmission, targetEmission, throttleSpoolSpeed * Time.deltaTime);
        currentLengthMult = Mathf.Lerp(currentLengthMult, targetLength, throttleSpoolSpeed * Time.deltaTime);
        currentSizeMult = Mathf.Lerp(currentSizeMult, targetSize, throttleSpoolSpeed * Time.deltaTime);

        if (thrusterParticles != null)
        {
            for (int i = 0; i < thrusterParticles.Length; i++)
            {
                if (thrusterParticles[i] != null)
                {
                    // 1. Emission Count
                    var emission = thrusterParticles[i].emission;
                    emission.rateOverTime = currentEmission;

                    // 2. Flame Length (Start Speed) & Flame Thickness (Start Size)
                    var main = thrusterParticles[i].main;
                    if (baseStartSpeeds != null && i < baseStartSpeeds.Length)
                    {
                        main.startSpeedMultiplier = baseStartSpeeds[i] * currentLengthMult;
                    }
                    if (baseStartSizes != null && i < baseStartSizes.Length)
                    {
                        main.startSizeMultiplier = baseStartSizes[i] * currentSizeMult;
                    }
                }
            }
        }
    }

    private void SetRagdollGravity(bool useGravity)
    {
        if (pilotRagdollRbs == null) return;
        for (int i = 0; i < pilotRagdollRbs.Length; i++)
        {
            if (pilotRagdollRbs[i] != null && pilotRagdollRbs[i] != jetRb)
            {
                pilotRagdollRbs[i].useGravity = useGravity;
            }
        }
    }
}