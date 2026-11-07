using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlightJetpack : MonoBehaviour
{
    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Mobile Touch Controls (Assign from Canvas)")]
    [Tooltip("The parent GameObject holding all mobile buttons & joystick (auto-hides on PC!).")]
    public GameObject mobileUIRoot;
    [Tooltip("Drag the Dynamic Joystick prefab from your Canvas here.")]
    public Joystick dynamicJoystick;
    [Tooltip("Drag the touch Boost button here.")]
    public MobileTouchButton touchBoostButton;
    [Tooltip("Drag the touch Up elevation button here.")]
    public MobileTouchButton touchUpButton;
    [Tooltip("Drag the touch Down elevation button here.")]
    public MobileTouchButton touchDownButton;

    [Header("Boost Fuel Meter (UI Slider)")]
    public Slider boostSlider;
    public float maxBoostDuration = 5.0f;
    public float fuelRechargeRate = 1.2f;
    public float rechargeStartDelay = 0.4f;

    [Header("Flight Speeds (Tunable in Inspector)")]
    public float cruiseSpeed = 15f;
    public float turboBoostSpeed = 42f;
    public float verticalClimbSpeed = 12f;
    public float acceleration = 25f;

    [Header("Movement-Facing Rotation")]
    public float rotationTurnSpeed = 14f;
    public float forwardPitchAngle = 35f;

    [Header("Idle Hover Floating Bob")]
    public float hoverBobHeight = 0.14f;
    public float hoverBobSpeed = 2.0f;
    public float bobBlendSpeed = 5.0f;

    [Header("Thruster Particle Systems & Boost Surge")]
    public ParticleSystem[] thrusterParticles;
    public float idleHoverEmission = 15f;
    public float cruiseEmission = 40f;
    public float boostEmission = 95f;
    public float boostFlameLengthMultiplier = 2.2f;
    public float boostFlameSizeMultiplier = 1.6f;
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

    private float hoverBaseY = 0f;
    private float currentBobWeight = 0f;
    private float bobTimer = 0f;
    private Quaternion targetFacingRotation;

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

        jetRb.useGravity = false;
        SetRagdollGravity(false);

        CurrentFuel = maxBoostDuration;
        if (boostSlider != null)
        {
            boostSlider.minValue = 0f;
            boostSlider.maxValue = 1f;
            boostSlider.value = 1f;
        }

        hoverBaseY = transform.position.y;
        targetFacingRotation = transform.rotation;

        // Auto-configure Mobile UI: show on mobile/tablets, hide on PC standalone builds
        if (mobileUIRoot != null)
        {
#if UNITY_ANDROID || UNITY_IOS
            mobileUIRoot.SetActive(true);
#else
            // In Unity Editor, keep active if you want to test with mouse clicks on touch buttons!
            mobileUIRoot.SetActive(Application.isMobilePlatform);
#endif
        }

        // Cache baseline particle parameters
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

                    if (!thrusterParticles[i].isPlaying) thrusterParticles[i].Play();
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
        // UNIFIED BOOST: Reads Right Mouse Click on PC OR Mobile Touch Button on Android!
        bool pcBoost = Input.GetMouseButton(1);
        bool mobileBoost = touchBoostButton != null && touchBoostButton.IsPressed;
        bool wantsBoost = pcBoost || mobileBoost;

        if (wantsBoost && CurrentFuel > 0f)
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
        // 1. UNIFIED HORIZONTAL INPUT (Keyboard WASD + Dynamic Joystick)
        float hInput = 0f;
        if (Input.GetKey(KeyCode.A)) hInput -= 1f;
        if (Input.GetKey(KeyCode.D)) hInput += 1f;
        if (dynamicJoystick != null) hInput += dynamicJoystick.Horizontal;
        hInput = Mathf.Clamp(hInput, -1f, 1f);

        float vInput = 0f;
        if (Input.GetKey(KeyCode.S)) vInput -= 1f;
        if (Input.GetKey(KeyCode.W)) vInput += 1f;
        if (dynamicJoystick != null) vInput += dynamicJoystick.Vertical;
        vInput = Mathf.Clamp(vInput, -1f, 1f);

        // 2. UNIFIED ELEVATION INPUT (Keyboard Space/Ctrl + Mobile Up/Down Buttons)
        float upInput = 0f;
        if (Input.GetKey(KeyCode.Space)) upInput += 1f;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) upInput -= 1f;
        if (touchUpButton != null && touchUpButton.IsPressed) upInput += 1f;
        if (touchDownButton != null && touchDownButton.IsPressed) upInput -= 1f;
        upInput = Mathf.Clamp(upInput, -1f, 1f);

        bool hasHorizontalInput = Mathf.Abs(hInput) > 0.05f || Mathf.Abs(vInput) > 0.05f;
        IsMoving = hasHorizontalInput || Mathf.Abs(upInput) > 0.05f;

        // 3. CAMERA-RELATIVE DIRECTION VECTORS
        Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;

        Vector3 flatCamForward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        Vector3 flatCamRight = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized;

        Vector3 horizontalMoveDir = (flatCamForward * vInput) + (flatCamRight * hInput);
        if (horizontalMoveDir.sqrMagnitude > 1f) horizontalMoveDir.Normalize();

        Vector3 totalMoveDirection = horizontalMoveDir + (Vector3.up * upInput);

        // 4. TARGET SPEED
        float targetSpeed = IsBoosting ? turboBoostSpeed : cruiseSpeed;
        Vector3 targetVelocity = totalMoveDirection * targetSpeed;

        // 5. DYNAMIC HOVER BOB
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

        // 6. VELOCITY LERP
#if UNITY_6000_0_OR_NEWER
        Vector3 currentVel = jetRb.linearVelocity;
        jetRb.linearVelocity = Vector3.MoveTowards(currentVel, targetVelocity, acceleration * Time.fixedDeltaTime);
#else
        Vector3 currentVel = jetRb.velocity;
        jetRb.velocity = Vector3.MoveTowards(currentVel, targetVelocity, acceleration * Time.fixedDeltaTime);
#endif

        // 7. MOVEMENT-FACING ROTATION
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
            targetEmission = boostEmission;
            targetLength = boostFlameLengthMultiplier;
            targetSize = boostFlameSizeMultiplier;
        }
        else if (IsMoving)
        {
            targetEmission = cruiseEmission;
            targetLength = 1.0f;
            targetSize = 1.0f;
        }
        else
        {
            targetEmission = idleHoverEmission;
            targetLength = 0.65f;
            targetSize = 0.8f;
        }

        currentEmission = Mathf.Lerp(currentEmission, targetEmission, throttleSpoolSpeed * Time.deltaTime);
        currentLengthMult = Mathf.Lerp(currentLengthMult, targetLength, throttleSpoolSpeed * Time.deltaTime);
        currentSizeMult = Mathf.Lerp(currentSizeMult, targetSize, throttleSpoolSpeed * Time.deltaTime);

        if (thrusterParticles != null)
        {
            for (int i = 0; i < thrusterParticles.Length; i++)
            {
                if (thrusterParticles[i] != null)
                {
                    var emission = thrusterParticles[i].emission;
                    emission.rateOverTime = currentEmission;

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