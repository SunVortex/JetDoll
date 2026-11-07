using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeFlightCamera : MonoBehaviour
{
    [Header("Tracking Target")]
    public Transform target;
    public FreeFlightJetpack jetpackController;

    [Header("Mobile Swipe Area Reference")]
    [Tooltip("Drag the right-side TouchCameraSwipeArea from Canvas here.")]
    public TouchCameraSwipeArea touchSwipeArea;

    [Header("Camera Rig Positioning")]
    public float distance = 4.5f;
    public float height = 1.0f;
    public float followSpeed = 18f;

    [Header("360 Orbit Sensitivity")]
    public float mouseSensitivityX = 3.0f;
    public float mouseSensitivityY = 2.5f;
    public float touchSensitivityX = 1.2f;
    public float touchSensitivityY = 1.0f;
    public float minPitchAngle = -75f;
    public float maxPitchAngle = 75f;

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

        // Lock cursor on PC standalone; keep free on Mobile
#if !UNITY_ANDROID && !UNITY_IOS
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
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

        HandleCameraLook();
        HandleDynamicFOV();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        UpdateCameraTransform();
    }

    private void HandleCameraLook()
    {
        float lookX = 0f;
        float lookY = 0f;

        // 1. PC MOUSE INPUT
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            lookX += Input.GetAxis("Mouse X") * mouseSensitivityX;
            lookY += Input.GetAxis("Mouse Y") * mouseSensitivityY;
        }

        // 2. MOBILE TOUCH SWIPE INPUT
        if (touchSwipeArea != null)
        {
            lookX += touchSwipeArea.TouchDelta.x * touchSensitivityX;
            lookY += touchSwipeArea.TouchDelta.y * touchSensitivityY;
        }

        currentYaw += lookX;
        currentPitch = Mathf.Clamp(currentPitch - lookY, minPitchAngle, maxPitchAngle);
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