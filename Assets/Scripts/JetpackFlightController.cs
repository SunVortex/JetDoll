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
    [Tooltip("The camera that defines forward aim.")]
    public Transform cameraTransform;

    [Header("Street Hazard (Altitude Danger Zone)")]
    [Tooltip("Z distance where the safe Starting_Bridge ends and street danger begins.")]
    public float safeBridgeZOffset = 20f;

    [Tooltip("World Y height threshold. Falling below this on the street triggers damage.")]
    public float streetDangerHeightY = 1.2f;

    [Tooltip("Seconds spent fallen on the street before losing 1 heart.")]
    public float roadDamageInterval = 1.5f;

    [Tooltip("Grace period (seconds) after crashing into a building before another hit can register.")]
    public float buildingHitCooldown = 1.0f;

    [Header("Particle Thruster Effects")]
    [Tooltip("Drag all 4 particle systems here (flames, sparks, etc.).")]
    public ParticleSystem[] thrusterParticles;
    public float cruiseEmissionRate = 25f;
    public float boostEmissionRate = 75f;
    public float throttleSpoolSpeed = 6.0f;

    [Header("Direct Speed Controls (Tunable in Inspector)")]
    [Tooltip("Top flight speed (m/s) when holding RMB + W.")]
    [Range(10f, 100f)]
    public float maxBoostSpeed = 35f;

    [Tooltip("Gentle cruise speed (m/s) when holding ONLY RMB.")]
    [Range(2f, 30f)]
    public float idleCruiseSpeed = 10f;

    [Tooltip("How fast the jetpack reaches top speed (responsiveness).")]
    public float boostAcceleration = 22f;

    [Header("Barrel Roll Settings (A & D Keys)")]
    public RollAxisType rollAxis = RollAxisType.Z_Axis_Roll;
    public float rollSpinSpeed = 720f;

    [Header("Mouse Aim Steering (ONLY WAY TO STEER)")]
    public float mouseTurnSpeed = 22f;
    public float flightPitch = 70f;
    public float uprightPitch = 0f;

    [Header("Corridor Altitude Lock")]
    public float cruiseAltitude = 3.8f;
    public float maxCeilingHeight = 7.5f;
    public float climbSpeed = 6.0f;

    [Header("Air Drag & Descent")]
    public float brakeDrag = 3.0f;
    public float flightDrag = 0.8f;

    // Public state for Camera
    public bool IsFullBurning { get; private set; }
    public bool IsIgnited { get; private set; }

    private Rigidbody jetRb;
    private Rigidbody[] pilotRagdollRbs;
    private float currentPitch = 0f;
    private float startingFloorY = 0f;
    private float currentEmissionRate = 0f;

    // Barrel roll spin state
    private float currentSpinAngle = 0f;
    private float targetSpinAngle = 0f;
    private bool isRolling = false;

    // Hazard timers
    private float roadContactTimer = 0f;
    private float buildingCooldownTimer = 0f;

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
        bool rmb = Input.GetMouseButton(1);
        bool wKey = Input.GetKey(KeyCode.W);
        IsFullBurning = rmb && wKey;
        IsIgnited = rmb;

        // Barrel Rolls with A & D
        if (rmb && !isRolling)
        {
            if (Input.GetKeyDown(KeyCode.A)) TriggerBarrelRoll(360f);
            else if (Input.GetKeyDown(KeyCode.D)) TriggerBarrelRoll(-360f);
        }

        HandleSmoothParticleThrottle();
        HandleStreetHazardAndBuildings();
    }

    private void FixedUpdate()
    {
        jetRb.angularVelocity = Vector3.zero;
        HandleFlight();
    }

    // ==========================================
    // HAZARD TIMERS (ROAD & BUILDINGS)
    // ==========================================

    private void HandleStreetHazardAndBuildings()
    {
        // 1. Building cooldown recovery
        if (buildingCooldownTimer > 0f)
        {
            buildingCooldownTimer -= Time.deltaTime;
        }

        // 2. Road Hazard (Active ONLY once past the safe Starting_Bridge!)
        bool hasLeftBridge = transform.position.z > safeBridgeZOffset;
        bool isFallenOnStreet = transform.position.y <= streetDangerHeightY;

        if (hasLeftBridge && isFallenOnStreet)
        {
            roadContactTimer += Time.deltaTime;

            if (roadContactTimer >= roadDamageInterval)
            {
                roadContactTimer = 0f; // Reset ticker for next 1.5s interval

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage("Fell onto the Street!");
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayGoalMiss();
                }
            }
        }
        else
        {
            // Reset timer immediately when safely in the air or on the bridge!
            roadContactTimer = 0f;
        }
    }

    // ==========================================
    // BUILDING CRASH COLLISION LOGIC
    // ==========================================

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Building") || collision.transform.root.CompareTag("Building"))
        {
            if (buildingCooldownTimer <= 0f)
            {
                buildingCooldownTimer = buildingHitCooldown;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage("Crashed into a Building!");
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayGoalMiss();
                }
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
        float targetRate = 0f;

        if (IsFullBurning) targetRate = boostEmissionRate;
        else if (IsIgnited) targetRate = cruiseEmissionRate;
        else targetRate = 0f;

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
        bool rmb = Input.GetMouseButton(1);
        bool wKey = Input.GetKey(KeyCode.W);

        // 1. ENGINE OFF / DESCENT
        if (!rmb)
        {
            SetRagdollGravity(true);
            jetRb.useGravity = true;
            ApplyDrag(brakeDrag);

            currentPitch = Mathf.Lerp(currentPitch, uprightPitch, 8f * Time.fixedDeltaTime);
            currentSpinAngle = Mathf.Lerp(currentSpinAngle, 0f, 10f * Time.fixedDeltaTime);
            isRolling = false;

            Quaternion landRot = Quaternion.Euler(currentPitch, transform.eulerAngles.y, 0f);
            jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, landRot, mouseTurnSpeed * Time.fixedDeltaTime));
            return;
        }

        // 2. ENGINE ACTIVE
        SetRagdollGravity(false);
        jetRb.useGravity = false;
        ApplyDrag(flightDrag);

        // 3. PITCH STANCE
        float targetPitch = wKey ? flightPitch : 25f;
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, 8f * Time.fixedDeltaTime);

        // 4. BARREL ROLL SPIN ANIMATION
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

        // 5. MOUSE AIM DIRECTION
        Vector3 rawAim = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(rawAim, Vector3.up).normalized;

        Quaternion aimRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        Quaternion stanceRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion spinRotation = Quaternion.Euler(spinEuler);
        Quaternion finalTargetRot = aimRotation * stanceRotation * spinRotation;

        jetRb.MoveRotation(Quaternion.Slerp(jetRb.rotation, finalTargetRot, mouseTurnSpeed * Time.fixedDeltaTime));

        // 6. ALTITUDE LOCK
        float currentHeightAboveFloor = transform.position.y - startingFloorY;
        float targetYVelocity = 0f;

        if (currentHeightAboveFloor < cruiseAltitude) targetYVelocity = climbSpeed;
        else if (currentHeightAboveFloor > maxCeilingHeight) targetYVelocity = -climbSpeed;
        else targetYVelocity = 0f;

        // 7. DIRECT VELOCITY FORWARD
        float targetSpeed = wKey ? maxBoostSpeed : idleCruiseSpeed;
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