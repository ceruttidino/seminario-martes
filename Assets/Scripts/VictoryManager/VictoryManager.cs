using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(false);
    }

    private void Start()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueRun);
    }

    public void TriggerVictory()
    {
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            victoryScreen.transform.SetAsLastSibling();
        }
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    // Permite seguir jugando tras vencer al jefe: oculta la pantalla de
    // victoria y le pide al DungeonManager que genere un nuevo piso.
    public void ContinueRun()
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.StartNextFloor();
    }
}