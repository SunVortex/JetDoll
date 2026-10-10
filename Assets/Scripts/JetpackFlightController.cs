using UnityEngine;

public class JetpackFlightController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Direct Speed Controls (Tunable in Inspector)")]
    public float maxBoostSpeed = 50.8f;
    public float idleCruiseSpeed = 10f;
    public float boostAcceleration = 22f;
    public float strafeSpeed = 14f;

    [Header("Corridor Altitude Lock")]
    public float cruiseAltitude = 3.8f;
    public float maxCeilingHeight = 7.5f;
    public float climbSpeed = 5.5f;

    [Header("Snap A/D Banking & Mouse Steering")]
    public float mouseTurnSpeed = 22f;
    public float bankAngle = 30f;
    public float bankSnapSpeed = 14f;
    public float flightPitch = 70f;
    public float uprightPitch = 0f;

    [Header("Air Drag & Descent")]
    public float brakeDrag = 2.5f;
    public float flightDrag = 0.8f;

    // Properties read by DynamicJetPart.cs
    public bool IsIgnited { get; private set; } = false;
    public bool IsFullBurning { get; private set; } = false;

    private Rigidbody rb;
    private float currentSpeed;
    private float targetBankZ = 0f;
    private float currentBankZ = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = idleCruiseSpeed;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // 1. FREEZE COMPLETELY WHEN IN MAIN MENU OR GAME OVER
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            IsIgnited = false;
            IsFullBurning = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

        // 2. Flight input reading
        HandleFlightInput();
    }

    private void FixedUpdate()
    {
        // Do not apply flight physics when menu is open
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            return;
        }

        ApplyFlightPhysics();
    }

    private void HandleFlightInput()
    {
        // Left Shift or RMB triggers full thruster burn
        bool boostKey = Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(1);
        IsFullBurning = boostKey;
        IsIgnited = true; // Always firing forward during active corridor run

        float targetSpeed = IsFullBurning ? maxBoostSpeed : idleCruiseSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, boostAcceleration * Time.deltaTime);

        // A / D Banking
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;

        targetBankZ = -horizontal * bankAngle;
        currentBankZ = Mathf.Lerp(currentBankZ, targetBankZ, bankSnapSpeed * Time.deltaTime);
    }

    private void ApplyFlightPhysics()
    {
        Vector3 forwardMove = Vector3.forward * currentSpeed;

        // Strafe Left/Right
        float hInput = 0f;
        if (Input.GetKey(KeyCode.A)) hInput -= 1f;
        if (Input.GetKey(KeyCode.D)) hInput += 1f;
        Vector3 strafeMove = Vector3.right * (hInput * strafeSpeed);

        // Climb / Descend (W / S or Space / Ctrl)
        float vInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space)) vInput += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.LeftControl)) vInput -= 1f;
        Vector3 verticalMove = Vector3.up * (vInput * climbSpeed);

        Vector3 targetVelocity = forwardMove + strafeMove + verticalMove;

        if (rb != null)
        {
            rb.linearVelocity = targetVelocity;

            // Height clamp
            Vector3 clampedPos = rb.position;
            clampedPos.y = Mathf.Clamp(clampedPos.y, 0.5f, maxCeilingHeight);
            rb.position = clampedPos;

            // Apply forward flight pitch + banking roll
            Quaternion targetRotation = Quaternion.Euler(flightPitch, 0f, currentBankZ);
            rb.MoveRotation(targetRotation);
        }
        else
        {
            transform.position += targetVelocity * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(flightPitch, 0f, currentBankZ);
        }
    }
}