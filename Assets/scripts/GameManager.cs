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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnFrameSuccess()
    {
        if (isGameOver) return;
        framesPassed++;
    }

    public void TakeDamage(string reason)
    {
        if (isGameOver) return;

        currentChances--;
        Debug.Log($"Damage: {reason} | Chances left: {currentChances}");

        if (currentChances < 0)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;

        PlayerFlightController player = FindAnyObjectByType<PlayerFlightController>();
        if (player != null)
        {
            player.forwardSpeed = 0f;
            player.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
    }

    void OnGUI()
    {
        GUIStyle heartStyle = new GUIStyle(GUI.skin.label);
        heartStyle.fontSize = 24;
        heartStyle.fontStyle = FontStyle.Bold;
        heartStyle.normal.textColor = Color.yellow;

        string hearts = "";
        for (int i = 0; i < Mathf.Max(0, currentChances); i++) hearts += " ♥ ";
        GUI.Label(new Rect(30, 25, 300, 40), $"CHANCES: {hearts} ({Mathf.Max(0, currentChances)})", heartStyle);
        GUI.Label(new Rect(30, 60, 300, 40), $"FRAMES CLEARED: {framesPassed}", heartStyle);

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
            GUI.Label(new Rect((Screen.width - 300) * 0.5f, (Screen.height - 60) * 0.5f, 300, 40), $"Frames Cleared: {framesPassed}", scoreStyle);

            if (GUI.Button(new Rect((Screen.width - 160) * 0.5f, (Screen.height + 40) * 0.5f, 160, 45), "RESTART"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}