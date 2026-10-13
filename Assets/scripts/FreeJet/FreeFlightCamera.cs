using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeFlightCamera : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("Drag Jetpack_Vehicle here.")]
    public Transform target;
    [Tooltip("Reference to the flight controller (auto-finds if empty).")]
    public FreeFlightJetpack jetpackController;

    [Header("Camera Rig Positioning")]
    [Tooltip("Distance behind the jetpack.")]
    public float distance = 4.5f;
    [Tooltip("Height offset above the jetpack.")]
    public float height = 1.0f;
    [Tooltip("Smooth follow catch-up speed.")]
    public float followSpeed = 18f;

    [Header("Free 360 Mouse Orbit")]
    public float mouseSensitivityX = 3.0f;
    public float mouseSensitivityY = 2.5f;
    public float minPitchAngle = -75f; // Look straight up into sky
    public float maxPitchAngle = 75f;  // Look straight down at ground

    [Header("Speed Juice (Dynamic FOV)")]
    public float normalFOV = 60f;
    public float turboFOV = 78f;
    public float fovTransitionSpeed = 6f;

    private Camera cam;
    private float currentYaw = 0f;
    private float currentPitch = 15f;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (target == null)
        {
            FreeFlightJetpack jet = FindAnyObjectByType<FreeFlightJetpack>();
            if (jet != null)
            {
                target = jet.transform;
                jetpackController = jet;
            }
        }

        // Lock mouse into center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }
    }

    private void Update()
    {
        // Toggle cursor lock with Escape / Left Click
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleDynamicFOV();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraTransform();
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // Unbounded 360 Yaw
        currentYaw += mouseX;

        // Clamped Pitch
        currentPitch = Mathf.Clamp(currentPitch - mouseY, minPitchAngle, maxPitchAngle);
    }

    private void HandleDynamicFOV()
    {
        bool isBoosting = jetpackController != null && jetpackController.IsBoosting;
        float targetFOV = isBoosting ? turboFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    private void UpdateCameraTransform()
    {
        Quaternion orbitRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        Vector3 targetPivot = target.position + (Vector3.up * height);
        Vector3 desiredPosition = targetPivot - (orbitRotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = orbitRotation;
    }
}