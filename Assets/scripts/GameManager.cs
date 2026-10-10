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
        Time.timeScale = 1f;
        isPlaying = false;
        isGameOver = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

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
    // UI BUTTON METHODS
    // ==========================================

    public void PlayGame()
    {
        Debug.Log("PLAY BUTTON CLICKED!");

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);

        isPlaying = true;
        isGameOver = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void ReturnToMainMenu()
    {
        isPlaying = false;
        isGameOver = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenQuitConfirm()
    {
        Debug.Log("QUIT BUTTON CLICKED!");
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