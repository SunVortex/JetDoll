using UnityEngine;

[RequireComponent(typeof(Camera))]
public class JuicyFlightCamera : MonoBehaviour
{
    [Header("Target Tracking (Dead-Center Behind Jet)")]
    [Tooltip("Assign Jetpack_Vehicle here.")]
    public Transform target;
    [Tooltip("Assign Jetpack_Vehicle here.")]
    public JetpackFlightController flightController;

    [Header("Centered Camera Alignment (No Offset)")]
    [Tooltip("Distance directly behind the jetpack.")]
    public float distance = 4.2f;
    [Tooltip("Height aligned right with the jetpack center.")]
    public float height = 0.9f;
    [Tooltip("Follow speed: 20 gives tight, razor-sharp tracking.")]
    public float followSpeed = 20f;

    [Header("Forward Corridor Aim Cone")]
    [Tooltip("Base track heading (0 = Facing Z-axis forward).")]
    public float corridorBaseYaw = 0f;
    [Tooltip("Max degrees to steer left/right.")]
    public float maxAimYawCone = 25f;
    [Tooltip("Max degrees to steer up/down.")]
    public float maxAimPitchCone = 14f;
    public float mouseSensitivity = 2.5f;

    [Header("Speed Juice (Dynamic FOV)")]
    public float normalFOV = 60f;
    public float boostFOV = 76f;
    public float fovSpeed = 7f;

    private Camera cam;
    private float currentAimYaw = 0f;
    private float currentAimPitch = 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
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

        HandleMouseAim();
        HandleDynamicFOV();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraPosition();
    }

    private void HandleMouseAim()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentAimYaw = Mathf.Clamp(currentAimYaw + mouseX, -maxAimYawCone, maxAimYawCone);
        currentAimPitch = Mathf.Clamp(currentAimPitch - mouseY, -maxAimPitchCone, maxAimPitchCone);
    }

    private void HandleDynamicFOV()
    {
        bool isBoosting = flightController != null && flightController.IsFullBurning;
        float targetFOV = isBoosting ? boostFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }

    private void UpdateCameraPosition()
    {
        // Pure forward aim rotation with zero horizontal offset
        Quaternion aimRotation = Quaternion.Euler(currentAimPitch, corridorBaseYaw + currentAimYaw, 0f);

        // Position directly behind the jetpack center
        Vector3 targetCenter = target.position + (Vector3.up * height);
        Vector3 desiredPosition = targetCenter - (aimRotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = aimRotation;
    }
}