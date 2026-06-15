using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject settingsCanvas;
    public void StartGame()
    {
        SceneManager.LoadScene("Gym");
    }

    public void SettingsMenu()
    {
        menuCanvas.SetActive(false);
        settingsCanvas.SetActive(true);
    }

    public void GoToMainMenu() 
    {
        menuCanvas.SetActive(true);
        settingsCanvas.SetActive(false);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
