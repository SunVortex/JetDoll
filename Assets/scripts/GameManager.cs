using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Chances (Lives)")]
    public int maxChances = 3;
    public int currentChances = 3;

    [Header("Game State")]
    public bool isGameOver = false;
    public bool isPlaying = false;
    public int framesPassed = 0;

    [Header("Manual UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject quitConfirmPopup;
    public GameObject gameOverPanel;

    [Header("Optional Text Displays")]
    public TextMeshProUGUI hudChancesText;
    public TextMeshProUGUI hudScoreText;
    public TextMeshProUGUI gameOverScoreText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentChances = maxChances;
    }

    private void Start()
    {
        // Start paused in Menu
        Time.timeScale = 0f;
        isPlaying = false;
        isGameOver = false;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateHUD();
    }

    public void OnFrameSuccess()
    {
        if (isGameOver || !isPlaying) return;
        framesPassed++;
        UpdateHUD();
    }

    public void TakeDamage(string reason)
    {
        if (isGameOver || !isPlaying) return;

        currentChances--;
        UpdateHUD();

        if (currentChances <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        isPlaying = false;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"Frames Cleared: {framesPassed}";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateHUD()
    {
        if (hudChancesText != null)
        {
            string hearts = "";
            for (int i = 0; i < Mathf.Max(0, currentChances); i++) hearts += " ♥ ";
            hudChancesText.text = $"CHANCES: {hearts} ({Mathf.Max(0, currentChances)})";
        }

        if (hudScoreText != null)
        {
            hudScoreText.text = $"FRAMES CLEARED: {framesPassed}";
        }
    }

    // ==========================================
    // UI BUTTON FUNCTIONS
    // ==========================================

    public void PlayGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);

        isPlaying = true;
        isGameOver = false;
        Time.timeScale = 1f;

        // Directly launch the player flight controller
        PlayerFlightController player = FindAnyObjectByType<PlayerFlightController>();
        if (player != null)
        {
            player.StartFlight();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 0f;
        isPlaying = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenQuitConfirm()
    {
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(true);
    }

    public void CancelQuit()
    {
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
    }

    public void ConfirmQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}