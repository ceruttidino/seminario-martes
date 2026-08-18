using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueButton;

    [Header("Final Victory UI (opcional)")]
    [Tooltip("Si lo asignás, se activa solo cuando se vence al boss del último nivel (ej: texto 'Juego Completado').")]
    [SerializeField] private GameObject finalVictoryExtras;

    private void Awake()
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (finalVictoryExtras != null)
            finalVictoryExtras.SetActive(false);
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
        if (victoryScreen == null) return;

        bool isFinalFloor = DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor;

        // En el último nivel no tiene sentido ofrecer "continuar": se acaba el juego acá.
        if (continueButton != null)
            continueButton.gameObject.SetActive(!isFinalFloor);

        if (finalVictoryExtras != null)
            finalVictoryExtras.SetActive(isFinalFloor);

        victoryScreen.SetActive(true);
        victoryScreen.transform.SetAsLastSibling();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    // Permite seguir jugando tras vencer al jefe: oculta la pantalla de
    // victoria y le pide al DungeonManager que genere un nuevo piso.
    public void ContinueRun()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor)
            return;

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.StartNextFloor();
    }
}