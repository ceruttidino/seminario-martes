using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(false);
    }

    private void Start()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void TriggerVictory()
    {
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            victoryScreen.transform.SetAsLastSibling();
            Debug.Log("Pantalla de Victoria activada");
        }
    }

    public void GoToMainMenu()
    {
        Debug.Log("Volviendo al menú principal...");

        SceneManager.LoadScene("Scenes/Main Menu");
    }
}