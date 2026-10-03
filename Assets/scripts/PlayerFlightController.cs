using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerFlightController : MonoBehaviour
{
    public enum GameState { WaitingToStart, Flying }
    [Header("Current State")]
    public GameState currentState = GameState.WaitingToStart;

    [Header("Flight Speed")]
    public float forwardSpeed = 26f;

    [Header("Direct Mouse Flight Box")]
    // Exact positions matching the 3x3 Frame Grid
    public float minX = -2.2f;
    public float maxX = 2.2f;
    public float minY = 1.0f;  // Puts the player directly level with bottom frame
    public float maxY = 6.2f;  // Top frame level
    public float steeringSpeed = 18f;

    [Header("Camera Rig")]
    public Transform cameraTransform;
    // Lower camera offset directly behind the player
    public Vector3 cameraOffset = new Vector3(0f, 1.8f, -4.5f);
    public float cameraFollowSharpness = 20f;

    private float targetX = 0f;
    private float targetY = 2.0f;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Snap camera right behind player at start
        if (cameraTransform != null)
        {
            cameraTransform.position = transform.position + cameraOffset;
            cameraTransform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }

    void Update()
    {
        if (currentState == GameState.WaitingToStart)
        {
            if (WasTapPressed())
            {
                currentState = GameState.Flying;
            }
            return;
        }

        HandleFlight();
    }

    void LateUpdate()
    {
        if (cameraTransform != null)
        {
            Vector3 targetCamPos = transform.position + cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCamPos, cameraFollowSharpness * Time.deltaTime);
            cameraTransform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }

    void HandleFlight()
    {
        // 1. Move forward
        transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;

        // 2. Read mouse position normalized across the screen (0 to 1)
        Vector3 mouse = GetCurrentMousePosition();
        float normX = Mathf.Clamp01(mouse.x / Screen.width);
        float normY = Mathf.Clamp01(mouse.y / Screen.height);

        // 3. Map directly to our corridor boundaries
        targetX = Mathf.Lerp(minX, maxX, normX);
        targetY = Mathf.Lerp(minY, maxY, normY);

        // 4. Smooth movement
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, steeringSpeed * Time.deltaTime);
        pos.y = Mathf.Lerp(pos.y, targetY, steeringSpeed * Time.deltaTime);
        transform.position = pos;

        // 5. Visual tilt
        float tiltZ = (pos.x - targetX) * 15f;
        float pitchX = (targetY - pos.y) * 10f;
        transform.rotation = Quaternion.Euler(pitchX, 0f, tiltZ);
    }

    bool WasTapPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    Vector3 GetCurrentMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
        return Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    void OnGUI()
    {
        if (currentState == GameState.WaitingToStart)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 32;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;

            Rect rect = new Rect((Screen.width - 400) * 0.5f, Screen.height * 0.65f, 400, 80);
            GUI.Label(rect, "TAP TO FLY", labelStyle);
        }
    }
}