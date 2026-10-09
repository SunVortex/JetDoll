using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Chances (Lives)")]
    public int maxChances = 3;
    public int currentChances = 3;

    [Header("Game State")]
    public bool isGameOver = false;
    public int framesPassed = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentChances = maxChances;
    }

    public void OnFrameSuccess()
    {
        if (isGameOver) return;
        framesPassed++;
        Debug.Log($"Frame Cleared! Total: {framesPassed}");
    }

    public void TakeDamage(string reason)
    {
        if (isGameOver) return;

        currentChances--;
        Debug.Log($"Damage Taken: {reason} | Remaining Chances: {currentChances}");

        if (currentChances <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;

        // Cut flight engine and let the ragdoll tumble limp
        JetpackFlightController player = FindAnyObjectByType<JetpackFlightController>();
        if (player != null)
        {
            player.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
            }
        }

        // Disable camera script to free up mouse for UI clicks
        JuicyFlightCamera camScript = FindAnyObjectByType<JuicyFlightCamera>();
        if (camScript != null)
        {
            camScript.enabled = false;
        }

        // Unlock mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        // 1. HUD: LIVES & SCORE
        GUIStyle heartStyle = new GUIStyle(GUI.skin.label);
        heartStyle.fontSize = 22;
        heartStyle.fontStyle = FontStyle.Bold;
        heartStyle.normal.textColor = Color.yellow;

        string hearts = "";
        for (int i = 0; i < Mathf.Max(0, currentChances); i++) hearts += " ♥ ";
        GUI.Label(new Rect(30, 25, 350, 35), $"CHANCES: {hearts} ({Mathf.Max(0, currentChances)})", heartStyle);
        GUI.Label(new Rect(30, 55, 350, 35), $"FRAMES CLEARED: {framesPassed}", heartStyle);

        // 2. GAME OVER SCREEN
        if (isGameOver)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            GUI.Box(new Rect((Screen.width - 320) * 0.5f, (Screen.height - 220) * 0.5f, 320, 220), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 32;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.red;

            GUI.Label(new Rect((Screen.width - 300) * 0.5f, (Screen.height - 180) * 0.5f, 300, 50), "GAME OVER", titleStyle);

            GUIStyle scoreStyle = new GUIStyle(GUI.skin.label);
            scoreStyle.fontSize = 18;
            scoreStyle.alignment = TextAnchor.MiddleCenter;
            scoreStyle.normal.textColor = Color.white;
            GUI.Label(new Rect((Screen.width - 300) * 0.5f, (Screen.height - 70) * 0.5f, 300, 40), $"Frames Cleared: {framesPassed}", scoreStyle);

            // RESTART BUTTON
            if (GUI.Button(new Rect((Screen.width - 160) * 0.5f, (Screen.height + 30) * 0.5f, 160, 45), "RESTART"))
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene.buildIndex >= 0)
                {
                    SceneManager.LoadScene(currentScene.buildIndex);
                }
                else
                {
                    SceneManager.LoadScene(currentScene.name);
                }
            }
        }
    }
}