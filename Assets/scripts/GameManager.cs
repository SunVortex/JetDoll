using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { MainMenu, Transitioning, Playing, GameOver }

    [Header("Current State")]
    public GameState currentState = GameState.MainMenu;

    public bool isPlaying => currentState == GameState.Playing;
    public bool isGameOver => currentState == GameState.GameOver;

    [Header("Cinematic Camera Transition")]
    public Camera mainCamera;
    public Transform menuCameraAnchor;
    public float cameraSwoopDuration = 0.85f;

    [Header("Player Chances (Lives)")]
    public int maxChances = 3;
    public int currentChances = 3;
    public int framesPassed = 0;

    [Header("UI Hierarchy Panels")]
    public RectTransform menuPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    public GameObject tutorialPanel;

    [Header("Mobile Touch Controls Root")]
    [Tooltip("Drag Mobile_Controls_Root from Canvas here. Stays hidden during menus!")]
    public GameObject mobileControlsRoot;

    [Tooltip("Keep checked to test mobile touch joystick/buttons in the Unity Editor.")]
    public bool simulateMobileInEditor = true;

    [Header("TextMeshPro UI Text Displays")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;

    [Header("Menu Slide Animation")]
    public float slideDuration = 0.45f;
    public float slideDistanceLeft = -1200f;

    private static bool skipMenuOnReload = false;
    private Vector2 menuStartPos;
    private JuicyFlightCamera cameraController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        currentChances = maxChances;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) cameraController = mainCamera.GetComponent<JuicyFlightCamera>();
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (skipMenuOnReload)
        {
            // 1. FAST RESTART: Skip menu, jump straight to gameplay
            skipMenuOnReload = false;
            currentState = GameState.Playing;

            if (menuPanel != null) menuPanel.gameObject.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
            if (cameraController != null) cameraController.enabled = true;

            // Turn ON mobile touch controls during flight
            SetMobileUIActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (AudioManager.Instance != null) AudioManager.Instance.ResumeMusic();
        }
        else
        {
            // 2. MAIN MENU: Keep mobile controls strictly OFF so buttons work cleanly!
            currentState = GameState.MainMenu;

            SetMobileUIActive(false);

            if (menuPanel != null)
            {
                menuStartPos = menuPanel.anchoredPosition;
                menuPanel.gameObject.SetActive(true);
            }

            if (hudPanel != null) hudPanel.SetActive(false);
            if (cameraController != null) cameraController.enabled = false;

            if (mainCamera != null && menuCameraAnchor != null)
            {
                mainCamera.transform.position = menuCameraAnchor.position;
                mainCamera.transform.rotation = menuCameraAnchor.rotation;
            }

            SetMenuCursor();
            if (AudioManager.Instance != null) AudioManager.Instance.ResumeMusic();
        }

        UpdateHUD();
    }

    // ==========================================
    // TUTORIAL PANEL FUNCTIONS
    // ==========================================

    public void OpenTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    // ==========================================
    // UI BUTTON FUNCTIONS
    // ==========================================

    public void StartGame()
    {
        if (currentState != GameState.MainMenu) return;
        StartCoroutine(CinematicPlaySequence());
    }

    private IEnumerator CinematicPlaySequence()
    {
        currentState = GameState.Transitioning;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(SlideMenuRoutine());

        if (mainCamera != null && cameraController != null && cameraController.target != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            Vector3 targetCenter = cameraController.target.position + (Vector3.up * cameraController.height);
            Vector3 targetPos = targetCenter - (Quaternion.Euler(0, cameraController.corridorBaseYaw, 0) * Vector3.forward * cameraController.distance);
            Quaternion targetRot = Quaternion.Euler(0, cameraController.corridorBaseYaw, 0);

            float elapsed = 0f;
            while (elapsed < cameraSwoopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraSwoopDuration);

                targetCenter = cameraController.target.position + (Vector3.up * cameraController.height);
                targetPos = targetCenter - (Quaternion.Euler(0, cameraController.corridorBaseYaw, 0) * Vector3.forward * cameraController.distance);

                mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            cameraController.enabled = true;
        }

        // Gameplay has officially begun: activate HUD & mobile controls!
        currentState = GameState.Playing;
        if (hudPanel != null) hudPanel.SetActive(true);
        SetMobileUIActive(true);
    }

    private IEnumerator SlideMenuRoutine()
    {
        if (menuPanel != null)
        {
            Vector2 targetPos = menuStartPos + new Vector2(slideDistanceLeft, 0f);
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
                menuPanel.anchoredPosition = Vector2.Lerp(menuStartPos, targetPos, t);
                yield return null;
            }

            menuPanel.gameObject.SetActive(false);
        }
    }

    public void RestartGameDirectly()
    {
        skipMenuOnReload = true;
        ReloadActiveScene();
    }

    public void ReturnToMainMenu()
    {
        skipMenuOnReload = false;
        ReloadActiveScene();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReloadActiveScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0) SceneManager.LoadScene(currentScene.buildIndex);
        else SceneManager.LoadScene(currentScene.name);
    }

    // ==========================================
    // GAMEPLAY EVENTS
    // ==========================================

    public void OnFrameSuccess()
    {
        if (currentState != GameState.Playing) return;
        framesPassed++;
        UpdateHUD();
    }

    public void TakeDamage(string reason)
    {
        if (currentState != GameState.Playing) return;
        currentChances--;
        UpdateHUD();

        if (currentChances <= 0) TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        currentState = GameState.GameOver;

        // Cut flight engine and let ragdoll drop
        JetpackFlightController player = FindAnyObjectByType<JetpackFlightController>();
        if (player != null)
        {
            player.enabled = false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.useGravity = true;
        }

        // Hide flight controls & HUD on game over
        SetMobileUIActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"FRAMES CLEARED: {framesPassed}";
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameOver();

        SetMenuCursor();
    }

    private void UpdateHUD()
    {
        if (livesText != null)
        {
            livesText.text = $"LIVES: {Mathf.Max(0, currentChances)} / {maxChances}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {framesPassed}";
        }
    }

    private void SetMenuCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetMobileUIActive(bool active)
    {
        if (mobileControlsRoot == null) return;

        if (active)
        {
#if UNITY_EDITOR
            mobileControlsRoot.SetActive(simulateMobileInEditor || Application.isMobilePlatform);
#elif UNITY_ANDROID || UNITY_IOS
            mobileControlsRoot.SetActive(true);
#else
            mobileControlsRoot.SetActive(false);
#endif
        }
        else
        {
            mobileControlsRoot.SetActive(false);
        }
    }
}