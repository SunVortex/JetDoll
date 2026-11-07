using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JetpackFlightController : MonoBehaviour
{
    public enum RollAxisType
    {
        Z_Axis_Roll,
        Y_Axis_Yaw,
        X_Axis_Pitch
    }

    [Header("References")]
    public Transform cameraTransform;

    [Header("Streamlined Touch Controls (Canvas References)")]
    public TouchAimZone touchAimBoostZone;
    public MobileTouchButton touchRollLeftButton;
    public MobileTouchButton touchRollRightButton;

    [Header("Flight Speed")]
    [Tooltip("Base boost speed (m/s) when holding RMB or Right Touch Screen.")]
    public float boostFlightSpeed = 38f;
    public float boostAcceleration = 26f;

    [Header("Window Gate Slingshot Boost (Tunable in Inspector)")]
    [Tooltip("Extra bonus speed (m/s) added on top when flying cleanly through a window gate!")]
    public float gateBoostBonusSpeed = 18f;

    [Tooltip("Duration (seconds) the slingshot speed surge lasts before smoothly decaying.")]
    public float gateBoostDuration = 1.4f;

    [Header("Particle Thruster Effects")]
    public ParticleSystem[] thrusterParticles;
    public float boostEmissionRate = 85f;
    public float throttleSpoolSpeed = 6.0f;

    [Header("Barrel Roll Settings")]
    public RollAxisType rollAxis = RollAxisType.Z_Axis_Roll;
    public float rollSpinSpeed = 720f;

    [Header("Aim Steering & Stance")]
    public float turnSpeed = 22f;
    public float flightPitch = 70f;
    public float uprightPitch = 0f;

    [Header("Corridor Altitude Lock")]
    public float cruiseAltitude = 3.8f;
    public float maxCeilingHeight = 7.5f;
    public float climbSpeed = 6.0f;

    [Header("Air Drag & Descent")]
    public float brakeDrag = 3.2f;
    public float flightDrag = 0.8f;

    [Header("Hazard Damage Timers")]
    public float safeBridgeZOffset = 20f;
    public float streetDangerHeightY = 1.2f;
    public float roadDamageInterval = 1.5f;
    public float buildingHitCooldown = 1.0f;

    public bool IsFullBurning { get; private set; }
    public bool IsIgnited { get; private set; }

    private Rigidbody jetRb;
    private Rigidbody[] pilotRagdollRbs;
    private float currentPitch = 0f;
    private float startingFloorY = 0f;
    private float currentEmissionRate = 0f;

    private float currentSpinAngle = 0f;
    private float targetSpinAngle = 0f;
    private bool isRolling = false;

    private float roadContactTimer = 0f;
    private float buildingCooldownTimer = 0f;

    // Window Gate Slingshot state
    private float currentGateBonus = 0f;
    private float gateBonusTimer = 0f;

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

        startingFloorY = transform.position.y;

        if (thrusterParticles != null)
        {
            for (int i = 0; i < thrusterParticles.Length; i++)
            {
                if (thrusterParticles[i] != null)
                {
                    var emission = thrusterParticles[i].emission;
                    emission.rateOverTime = 0f;
                    if (!thrusterParticles[i].isPlaying) thrusterParticles[i].Play();
                }
            }
        }
    }

    private void Update()
    {
        // 1. UNIFIED IGNITION & BOOST
        bool pcIgnite = Input.GetMouseButton(1);
        bool mobileIgnite = touchAimBoostZone != null && touchAimBoostZone.IsHolding;

        IsIgnited = pcIgnite || mobileIgnite;
        IsFullBurning = IsIgnited;

        // 2. UNIFIED BARREL ROLLS
        if (IsIgnited && !isRolling)
        {
            bool rollLeft = Input.GetKeyDown(KeyCode.A) || (touchRollLeftButton != null && touchRollLeftButton.ConsumeTap());
            bool rollRight = Input.GetKeyDown(KeyCode.D) || (touchRollRightButton != null && touchRollRightButton.ConsumeTap());

            if (rollLeft) TriggerBarrelRoll(360f);
            else if (rollRight) TriggerBarrelRoll(-360f);
        }

        HandleGateBoostTimer();
        HandleSmoothParticleThrottle();
        HandleStreetHazardAndBuildings();
    }

    private void FixedUpdate()
    {
        jetRb.angularVelocity = Vector3.zero;
        HandleFlight();
    }

    /// <summary>
    /// Called automatically whenever passing through a window glass target!
    /// </summary>
    public void TriggerGateBoost()
    {
        currentGateBonus = gateBoostBonusSpeed;
        gateBonusTimer = gateBoostDuration;
    }

    private void HandleGateBoostTimer()
    {
        if (gateBonusTimer > 0f)
        {
            gateBonusTimer -= Time.deltaTime;
            // Smooth exponential decay back down
            currentGateBonus = Mathf.Lerp(0f, gateBoostBonusSpeed, gateBonusTimer / gateBoostDuration);
        }
        else
        {
            currentGateBonus = 0f;
        }
    }

    private void HandleStreetHazardAndBuildings()
    {
        if (buildingCooldownTimer > 0f)
        {
            buildingCooldownTimer -= Time.deltaTime;
        }

        bool hasLeftBridge = transform.position.z > safeBridgeZOffset;
        bool isFallenOnStreet = transform.position.y <= streetDangerHeightY;

        if (hasLeftBridge && isFallenOnStreet)
        {
            roadContactTimer += Time.deltaTime;
            if (roadContactTimer >= roadDamageInterval)
            {
                roadContactTimer = 0f;
                if (GameManager.Instance != null) GameManager.Instance.TakeDamage("Fell onto the Street!");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayGoalMiss();
            }
        }
        else
        {
            roadContactTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Building") || collision.transform.root.CompareTag("Building"))
        {
            if (buildingCooldownTimer <= 0f)
            {
                buildingCooldownTimer = buildingHitCooldown;
                if (GameManager.Instance != null) GameManager.Instance.TakeDamage("Crashed into a Building!");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayGoalMiss();
            }
        }
    }

    private void TriggerBarrelRoll(float degrees)
    {
        isRolling = true;
        currentSpinAngle = 0f;
        targetSpinAngle = degrees;
    }

    private void HandleSmoothParticleThrottle()
    {
        // Flames flare up even stronger during a gate slingshot boost!
        bool isAnyBoostActive = IsFullBurning || currentGateBonus > 1.0f;
        float targetRate = isAnyBoostActive ? boostEmissionRate : 0f;
        currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetRate, throttleSpoolSpeed * Time.deltaTime);

        if (thrusterParticles != null)
        {
            for (int i = 0; i < thrusterParticles.Length; i++)
            {
                if (thrusterParticles[i] != null)
                {
                    var emission = thrusterParticles[i].emission;
                    emission.rateOverTime = currentEmissionRate;
                }
            }
        }

        if (AudioManager.Instance != null)
        {
            float volumeFraction = currentEmissionRate / Mathf.Max(1f, boostEmissionRate);
            AudioManager.Instance.SetThrusterVolume(volumeFraction);
        }
    }

    private void HandleFlight()
    {
        // 1. ENGINE OFF / BRAKE (unless a gate boost slingshots you forward!)
        if (!IsIgnited && currentGateBonus <= 0.1f)
        {
            SetRagdollGravity(true);
            jetRb.useGravity = true;
            ApplyDrag(brakeDrag);

            currentPitch = Mathf.Lerp(currentPitch, uprightPitch, 8f * Time.fixedDeltaTime);
            currentSpinAngle = Mathf.Lerp(currentSpinAngle, 0f, 10f * Time.fixedDeltaTime);
            isRolling = false;

            Quaternion landRot = Quaternion.Euler(currentPitch, transform.eulerAngles.y, 0f);
            jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, landRot, turnSpeed * Time.fixedDeltaTime));
            return;
        }

        // 2. ENGINE ACTIVE / GATE BOOST
        SetRagdollGravity(false);
        jetRb.useGravity = false;
        ApplyDrag(flightDrag);

        currentPitch = Mathf.Lerp(currentPitch, flightPitch, 8f * Time.fixedDeltaTime);

        if (isRolling)
        {
            float step = Mathf.Sign(targetSpinAngle) * rollSpinSpeed * Time.fixedDeltaTime;
            currentSpinAngle += step;

            if (Mathf.Abs(currentSpinAngle) >= Mathf.Abs(targetSpinAngle))
            {
                currentSpinAngle = 0f;
                targetSpinAngle = 0f;
                isRolling = false;
            }
        }
        else
        {
            currentSpinAngle = Mathf.Lerp(currentSpinAngle, 0f, 12f * Time.fixedDeltaTime);
        }

        Vector3 spinEuler = Vector3.zero;
        switch (rollAxis)
        {
            case RollAxisType.Z_Axis_Roll: spinEuler = new Vector3(0f, 0f, currentSpinAngle); break;
            case RollAxisType.Y_Axis_Yaw: spinEuler = new Vector3(0f, currentSpinAngle, 0f); break;
            case RollAxisType.X_Axis_Pitch: spinEuler = new Vector3(currentSpinAngle, 0f, 0f); break;
        }

        Vector3 rawAim = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(rawAim, Vector3.up).normalized;

        Quaternion aimRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        Quaternion stanceRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion spinRotation = Quaternion.Euler(spinEuler);
        Quaternion finalTargetRot = aimRotation * stanceRotation * spinRotation;

        jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, finalTargetRot, turnSpeed * Time.fixedDeltaTime));

        // Altitude lock
        float currentHeightAboveFloor = transform.position.y - startingFloorY;
        float targetYVelocity = 0f;

        if (currentHeightAboveFloor < cruiseAltitude) targetYVelocity = climbSpeed;
        else if (currentHeightAboveFloor > maxCeilingHeight) targetYVelocity = -climbSpeed;
        else targetYVelocity = 0f;

        // COMBINED SPEED: Base forward boost + Gate slingshot bonus!
        float activeBaseSpeed = IsFullBurning ? boostFlightSpeed : 0f;
        float targetSpeed = activeBaseSpeed + currentGateBonus;
        Vector3 targetHorizontalVelocity = horizontalForward * targetSpeed;

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
            if (pilotRagdollRbs[i] != null) pilotRagdollRbs[i].useGravity = useGravity;
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