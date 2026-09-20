using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    private bool isPaused = false;

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameOverManager.IsOpen || VictoryManager.IsOpen) return;

        if (TabMenuUI.IsOpen)
        {
            TabMenuUI.CloseCurrent();
            return;
        }

        if (!isPaused && GamePause.IsGameplayFrozen) return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (GameOverManager.IsOpen || VictoryManager.IsOpen) return;

        PauseMainMenu();
        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();
        GamePause.SetPaused(true);
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (GameOverManager.IsOpen || VictoryManager.IsOpen) return;

        pausePanel.SetActive(false);
        PauseMainMenu();
        GamePause.SetPaused(false);
        isPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitToMenu()
    {
        GamePause.SetPaused(false);
        SceneManager.LoadScene("Main Menu");
    }

    public void PauseSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void PauseMainMenu()
    {
        pauseMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }
}
