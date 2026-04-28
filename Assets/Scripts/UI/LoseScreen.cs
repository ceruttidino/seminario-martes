using UnityEngine;
using UnityEngine.SceneManagement;

public class LoseScreen : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("Gym");
    }
}
