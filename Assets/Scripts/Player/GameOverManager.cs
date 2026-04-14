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
        Debug.Log("GameOverManager iniciado");

        if (gameOverScreen == null)
        {
            Debug.LogError("gameOverScreen NO está asignado");
            return;
        }

        gameOverScreen.SetActive(false);

        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
            ph.OnPlayerDeath += ShowGameOverScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);
    }

    private void ShowGameOverScreen()
    {
        Debug.Log("ShowGameOverScreen llamado - Intentando mostrar panel...");

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);

            gameOverScreen.transform.SetAsLastSibling();

            Debug.Log("✅ Panel activado y enviado al frente");
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}