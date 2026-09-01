using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        IsOpen = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    private void Start()
    {
        if (gameOverScreen == null)
        {
            Debug.LogError("GameOverManager: gameOverScreen no está asignado en el Inspector.");
            return;
        }

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.OnPlayerDeath += ShowGameOverScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverScreen == null) return;
        if (IsOpen) return;

        IsOpen = true;
        gameOverScreen.SetActive(true);
        gameOverScreen.transform.SetAsLastSibling();

        GamePause.SetPaused(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        IsOpen = false;
        GamePause.SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
