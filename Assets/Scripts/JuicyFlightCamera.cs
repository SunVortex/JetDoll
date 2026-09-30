using UnityEngine;

[RequireComponent(typeof(Camera))]
public class JuicyFlightCamera : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("Assign Jetpack_Vehicle here.")]
    public Transform target;
    [Tooltip("Assign Jetpack_Vehicle here.")]
    public JetpackFlightController flightController;

    [Header("Positioning Behind Jet")]
    [Tooltip("Distance behind the jetpack.")]
    public float distance = 4.8f;
    [Tooltip("Height above the jetpack.")]
    public float height = 1.6f;
    [Tooltip("Smooth follow catch-up speed.")]
    public float followSpeed = 16f;

    [Header("Mouse Aim Steering Cone")]
    [Tooltip("Base track heading (0 = Facing Z-axis forward).")]
    public float corridorBaseYaw = 0f;
    [Tooltip("Max degrees you can aim left/right.")]
    public float maxAimYawCone = 30f;
    [Tooltip("Max degrees you can aim up/down.")]
    public float maxAimPitchCone = 20f;
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
        Quaternion aimRotation = Quaternion.Euler(currentAimPitch, corridorBaseYaw + currentAimYaw, 0f);

        Vector3 targetPivot = target.position + (Vector3.up * height);
        Vector3 desiredPosition = targetPivot - (aimRotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = aimRotation;
    }
}