using UnityEngine;

public class DynamicJetPart : MonoBehaviour
{
    public enum MotionType
    {
        ContinuousSpin,     // Fans, turbines, spinning gears
        EngineRumbleShake,  // Exhaust nozzles, booster pipes vibrating under heat
        TailFinSteering,    // Aerodynamic tails/fins reacting to A & D
        SineSwayWobble      // Antennas, pressure gauges, idle breathing sway
    }

    public enum MotionAxis { X, Y, Z }

    [Header("Motion Mode")]
    [Tooltip("Pick the type of dynamic motion for this specific part.")]
    public MotionType motionType = MotionType.TailFinSteering;

    [Tooltip("Axis along which the part rotates, tilts, or swings.")]
    public MotionAxis axis = MotionAxis.Z;

    [Header("1. Continuous Spin Settings")]
    [Tooltip("Base rotation speed in degrees per second.")]
    public float baseSpinSpeed = 400f;
    [Tooltip("Speed multiplier when boosting with RMB + W.")]
    public float boostSpinMultiplier = 2.5f;
    [Tooltip("Reverse spin direction (great for twin contra-rotating engines!).")]
    public bool reverseRotation = false;

    [Header("2. Engine Rumble Shake Settings")]
    [Tooltip("Positional jitter distance.")]
    public float shakeIntensity = 0.05f;
    [Tooltip("Angular rotational twitch (makes vibration clearly visible!).")]
    public float angularTwitchIntensity = 3.0f;
    [Tooltip("Speed of the rumble vibration.")]
    public float shakeFrequency = 50f;
    [Tooltip("Only shake when thrusters are actively firing (RMB held).")]
    public bool shakeOnlyWhenFiring = true;

    [Header("3. Dynamic Tail Fin Settings (A & D)")]
    [Tooltip("Maximum deflection/swing angle in degrees when steering.")]
    public float tailTiltAngle = 35f;
    [Tooltip("How smoothly the fin snaps into and out of the turn.")]
    public float tailSmoothSpeed = 12f;
    [Tooltip("Check this on one of the tails if you want them to mirror symmetrically!")]
    public bool invertTailDirection = false;
    [Tooltip("Wind drag effect: pushes fins opposite to the turn direction.")]
    public bool slipstreamDragMode = true;

    [Header("4. Sine Sway Wobble Settings")]
    [Tooltip("Wobble angle in degrees.")]
    public float swayAngle = 12f;
    [Tooltip("Frequency of the wobble wave.")]
    public float swaySpeed = 3.5f;

    [Header("Jetpack Integration")]
    [Tooltip("Automatically reacts to flight and boost states.")]
    public bool reactToJetpack = true;
    [Tooltip("Optional direct reference to flight controller (auto-finds if empty).")]
    public JetpackFlightController flightController;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private float currentTailAngle = 0f;

    private void Awake()
    {
        // Cache original resting transform positions
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        if (reactToJetpack && flightController == null)
        {
            flightController = GetComponentInParent<JetpackFlightController>();
        }
    }

    private void Update()
    {
        bool isFiring = flightController != null && flightController.IsIgnited;
        bool isBoosting = flightController != null && flightController.IsFullBurning;

        switch (motionType)
        {
            case MotionType.ContinuousSpin:
                HandleSpin(isBoosting);
                break;

            case MotionType.EngineRumbleShake:
                HandleRumble(isFiring, isBoosting);
                break;

            case MotionType.TailFinSteering:
                HandleTailFin();
                break;

            case MotionType.SineSwayWobble:
                HandleSway();
                break;
        }
    }

    private void HandleSpin(bool isBoosting)
    {
        float dir = reverseRotation ? -1f : 1f;
        float speed = baseSpinSpeed * dir;

        if (reactToJetpack && isBoosting)
        {
            speed *= boostSpinMultiplier;
        }

        Vector3 axisVector = GetAxisVector(axis);
        transform.Rotate(axisVector * (speed * Time.deltaTime), Space.Self);
    }

    private void HandleRumble(bool isFiring, bool isBoosting)
    {
        bool shouldShake = !shakeOnlyWhenFiring || isFiring;

        if (shouldShake)
        {
            float posMult = isBoosting ? 1.8f : 1.0f;
            float rotMult = isBoosting ? 1.8f : 1.0f;

            // 1. High-frequency positional rumble
            Vector3 posJitter = Random.insideUnitSphere * (shakeIntensity * posMult);

            // 2. Visible angular micro-twitching (makes the nozzle look like real roaring steel)
            float twitchAngle = angularTwitchIntensity * rotMult;
            Vector3 rotJitter = new Vector3(
                Random.Range(-twitchAngle, twitchAngle),
                Random.Range(-twitchAngle, twitchAngle),
                Random.Range(-twitchAngle, twitchAngle)
            );

            transform.localPosition = initialLocalPos + posJitter;
            transform.localRotation = initialLocalRot * Quaternion.Euler(rotJitter);
        }
        else
        {
            // Settle smoothly back to original resting pose
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos, 15f * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot, 15f * Time.deltaTime);
        }
    }

    private void HandleTailFin()
    {
        float hInput = 0f;
        if (Input.GetKey(KeyCode.A)) hInput -= 1f;
        if (Input.GetKey(KeyCode.D)) hInput += 1f;

        // Wind drag pushes the tail opposite to movement
        if (slipstreamDragMode) hInput = -hInput;
        if (invertTailDirection) hInput = -hInput;

        float targetAngle = hInput * tailTiltAngle;
        currentTailAngle = Mathf.Lerp(currentTailAngle, targetAngle, tailSmoothSpeed * Time.deltaTime);

        Vector3 axisVector = GetAxisVector(axis);
        transform.localRotation = initialLocalRot * Quaternion.AngleAxis(currentTailAngle, axisVector);
    }

    private void HandleSway()
    {
        float angle = Mathf.Sin(Time.time * swaySpeed) * swayAngle;
        Vector3 axisVector = GetAxisVector(axis);
        transform.localRotation = initialLocalRot * Quaternion.AngleAxis(angle, axisVector);
    }

    private Vector3 GetAxisVector(MotionAxis selectedAxis)
    {
        switch (selectedAxis)
        {
            case MotionAxis.X: return Vector3.right;
            case MotionAxis.Y: return Vector3.up;
            case MotionAxis.Z: return Vector3.forward;
            default: return Vector3.forward;
        }
    }
}