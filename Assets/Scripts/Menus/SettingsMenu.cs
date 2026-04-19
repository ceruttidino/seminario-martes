using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    public void QuitToMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
