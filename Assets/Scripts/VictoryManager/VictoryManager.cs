using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueButton;

    [Header("Final Victory UI (opcional)")]
    [Tooltip("Si lo asignás, se activa solo cuando se vence al boss del último nivel (ej: texto 'Juego Completado').")]
    [SerializeField] private GameObject finalVictoryExtras;

    private void Awake()
    {
        IsOpen = false;

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (finalVictoryExtras != null)
            finalVictoryExtras.SetActive(false);
    }

    private void Start()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(RestartRun);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueRun);
    }

    public void TriggerVictory()
    {
        if (victoryScreen == null) return;
        if (IsOpen) return;

        IsOpen = true;

        bool isFinalFloor = DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor;

        if (continueButton != null)
            continueButton.gameObject.SetActive(!isFinalFloor);

        if (finalVictoryExtras != null)
            finalVictoryExtras.SetActive(isFinalFloor);

        victoryScreen.SetActive(true);
        victoryScreen.transform.SetAsLastSibling();

        GamePause.SetPaused(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartRun()
    {
        CloseVictoryHold();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ContinueRun()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor)
            return;

        CloseVictoryHold();

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.StartNextFloor();
    }

    private void CloseVictoryHold()
    {
        IsOpen = false;
        GamePause.SetPaused(false);
    }
}
