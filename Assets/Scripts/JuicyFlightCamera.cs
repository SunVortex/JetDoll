using UnityEngine;

[RequireComponent(typeof(Camera))]
public class JuicyFlightCamera : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform target;
    public JetpackFlightController flightController;

    [Header("Mobile Swipe Area Reference")]
    [Tooltip("Drag Touch_Aim_Area from your Canvas here.")]
    public TouchAimZone touchAimZone;

    [Header("Centered Camera Alignment")]
    public float distance = 4.2f;
    public float height = 0.9f;
    public float followSpeed = 20f;

    [Header("Corridor Aim Cone Limits")]
    public float corridorBaseYaw = 0f;
    public float maxAimYawCone = 25f;
    public float maxAimPitchCone = 14f;

    [Header("Input Sensitivity (PC vs Mobile)")]
    public float mouseSensitivity = 2.5f;
    public float touchSensitivityX = 0.8f;
    public float touchSensitivityY = 0.7f;

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

        if (target == null)
        {
            JetpackFlightController jet = FindAnyObjectByType<JetpackFlightController>();
            if (jet != null)
            {
                target = jet.transform;
                flightController = jet;
            }
        }

#if !UNITY_ANDROID && !UNITY_IOS
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            return;
        }

#if !UNITY_ANDROID && !UNITY_IOS
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
#endif

        HandleAimInput();
        HandleDynamicFOV();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        UpdateCameraPosition();
    }

    private void HandleAimInput()
    {
        float deltaYaw = 0f;
        float deltaPitch = 0f;

        // 1. PC MOUSE INPUT
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            deltaYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            deltaPitch += Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        // 2. MOBILE TOUCH SWIPE INPUT
        if (touchAimZone != null)
        {
            deltaYaw += touchAimZone.TouchDelta.x * touchSensitivityX;
            deltaPitch += touchAimZone.TouchDelta.y * touchSensitivityY;
        }

        currentAimYaw = Mathf.Clamp(currentAimYaw + deltaYaw, -maxAimYawCone, maxAimYawCone);
        currentAimPitch = Mathf.Clamp(currentAimPitch - deltaPitch, -maxAimPitchCone, maxAimPitchCone);
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

        Vector3 targetCenter = target.position + (Vector3.up * height);
        Vector3 desiredPosition = targetCenter - (aimRotation * Vector3.forward * distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = aimRotation;
    }
}