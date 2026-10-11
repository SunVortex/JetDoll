using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JetpackFlightController : MonoBehaviour
{
    public enum RollAxisType
    {
        Z_Axis_Roll,   // Classic airplane drill roll (Default)
        Y_Axis_Yaw,    // Flat spinning top / pirouette spin
        X_Axis_Pitch   // Flip / loop spin
    }

    [Header("References")]
    [Tooltip("The camera that defines forward aim.")]
    public Transform cameraTransform;

    [Header("Particle Thruster Effects")]
    [Tooltip("Drag all 4 particle systems here (flames, sparks, etc.).")]
    public ParticleSystem[] thrusterParticles;
    public float cruiseEmissionRate = 25f;
    public float boostEmissionRate = 75f;
    public float throttleSpoolSpeed = 6.0f;

    [Header("Direct Speed Controls (Tunable in Inspector)")]
    [Tooltip("Top flight speed (m/s) when holding RMB + W. Increase for faster flight!")]
    [Range(10f, 100f)]
    public float maxBoostSpeed = 35f;

    [Tooltip("Gentle cruise speed (m/s) when holding ONLY RMB.")]
    [Range(2f, 30f)]
    public float idleCruiseSpeed = 10f;

    [Tooltip("How fast the jetpack reaches top speed (responsiveness).")]
    public float boostAcceleration = 22f;

    [Header("Barrel Roll Settings (A & D Keys)")]
    [Tooltip("Choose which axis the jetpack spins around during a 360 roll!")]
    public RollAxisType rollAxis = RollAxisType.Z_Axis_Roll;

    [Tooltip("Speed of the 360-degree spin in degrees per second (720 = completes in 0.5s).")]
    public float rollSpinSpeed = 720f;

    [Header("Mouse Aim Steering (ONLY WAY TO STEER)")]
    [Tooltip("How fast the jetpack turns to follow the mouse aim.")]
    public float mouseTurnSpeed = 22f;
    [Tooltip("Forward flight pitch when flying forward toward Z.")]
    public float flightPitch = 70f;
    [Tooltip("Upright pitch when RMB is released.")]
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

        // Trigger Barrel Rolls with A & D
        if (rmb && !isRolling)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                // Left Spin
                TriggerBarrelRoll(360f);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                // Right Spin
                TriggerBarrelRoll(-360f);
            }
        }

        HandleSmoothParticleThrottle();
    }

    private void FixedUpdate()
    {
        jetRb.angularVelocity = Vector3.zero;
        HandleFlight();
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

        // 4. BARREL ROLL SPIN ANIMATION (ALONG CHOSEN AXIS)
        if (isRolling)
        {
            float step = Mathf.Sign(targetSpinAngle) * rollSpinSpeed * Time.fixedDeltaTime;
            currentSpinAngle += step;

            if (Mathf.Abs(currentSpinAngle) >= Mathf.Abs(targetSpinAngle))
            {
                currentSpinAngle = 0f;
                targetSpinAngle = 0f;
                isRolling = false; // Finished full 360 spin!
            }
        }
        else
        {
            currentSpinAngle = Mathf.Lerp(currentSpinAngle, 0f, 12f * Time.fixedDeltaTime);
        }

        // Build spin offset based on selected axis
        Vector3 spinEuler = Vector3.zero;
        switch (rollAxis)
        {
            case RollAxisType.Z_Axis_Roll:
                spinEuler = new Vector3(0f, 0f, currentSpinAngle);
                break;
            case RollAxisType.Y_Axis_Yaw:
                spinEuler = new Vector3(0f, currentSpinAngle, 0f);
                break;
            case RollAxisType.X_Axis_Pitch:
                spinEuler = new Vector3(currentSpinAngle, 0f, 0f);
                break;
        }

        // 5. MOUSE AIM DIRECTION (THE ONLY STEERING METHOD)
        Vector3 rawAim = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(rawAim, Vector3.up).normalized;

        Quaternion aimRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        Quaternion stanceRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Quaternion spinRotation = Quaternion.Euler(spinEuler);

        // Final combined rotation
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