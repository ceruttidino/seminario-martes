using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

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

        // Si estás en Controles o Settings, ESC vuelve al menú de pausa en vez de reanudar
        if (isPaused && (controlsPanel.activeSelf || settingsPanel.activeSelf))
        {
            PauseMainMenu();
            return;
        }

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
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void PauseControls()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void PauseMainMenu()
    {
        pauseMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }
}