using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        if (gameOverScreen == null)
        {
            Debug.LogError("GameOverManager: gameOverScreen no está asignado en el Inspector.");
            return;
        }

        gameOverScreen.SetActive(false);

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.OnPlayerDeath += ShowGameOverScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverScreen == null) return;

        gameOverScreen.SetActive(true);
        gameOverScreen.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
