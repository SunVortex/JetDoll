using UnityEngine;

public class PlayerFlightController : MonoBehaviour
{
    [Header("Flight Speed")]
    public float forwardSpeed = 25f;

    [Header("Mouse Steer Sensitivity")]
    public float horizontalSensitivity = 16f;
    public float verticalSensitivity = 16f;

    [Header("Flight Boundaries")]
    public float minX = -2.2f;
    public float maxX = 2.2f;
    public float minY = 0.8f;
    public float maxY = 6.2f;

    [Header("Visual Tilt")]
    public float tiltAmount = 25f;
    public float pitchAmount = 15f;
    public float rotationSmoothSpeed = 12f;

    [Header("Camera Rig")]
    public Transform cameraTransform;
    public Vector3 cameraOffset = new Vector3(0f, 1.8f, -4.5f);
    public float cameraFollowSharpness = 20f;

    private float currentRoll = 0f;
    private float currentPitch = 0f;
    private bool isFlying = false;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + cameraOffset;
            cameraTransform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }

    // Called directly by GameManager when the manual PLAY button is clicked
    public void StartFlight()
    {
        isFlying = true;
    }

    void Update()
    {
        // Don't steer or move forward until Play button is clicked
        if (!isFlying || (GameManager.Instance != null && !GameManager.Instance.isPlaying))
        {
            return;
        }

        HandleFlight();

        // Optional mouse escape key for testing
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void LateUpdate()
    {
        if (!isFlying || (GameManager.Instance != null && !GameManager.Instance.isPlaying))
        {
            return;
        }

        if (cameraTransform != null)
        {
            Vector3 targetCamPos = transform.position + cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCamPos, cameraFollowSharpness * Time.unscaledDeltaTime);
            cameraTransform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }

    void HandleFlight()
    {
        // 1. Forward movement
        transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;

        // 2. Mouse steering
        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;

        // 3. Move and clamp within corridor bounds
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + mouseX, minX, maxX);
        pos.y = Mathf.Clamp(pos.y + mouseY, minY, maxY);
        transform.position = pos;

        // 4. Tilt visuals
        float targetRoll = Mathf.Clamp(-mouseX * tiltAmount * 15f, -tiltAmount, tiltAmount);
        float targetPitch = Mathf.Clamp(-mouseY * pitchAmount * 15f, -pitchAmount, pitchAmount);

        currentRoll = Mathf.Lerp(currentRoll, targetRoll, rotationSmoothSpeed * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationSmoothSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
    }
}