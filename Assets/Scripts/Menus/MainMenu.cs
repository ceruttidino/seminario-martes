using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Gym");
    }

    public void SettingsMenu()
    {
        SceneManager.LoadScene("Settings Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
