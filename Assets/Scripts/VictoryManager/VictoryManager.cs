using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("Scenes")]
    [Tooltip("Nombre exacto de la escena del menú principal (tiene que estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Victory UI (pisos intermedios)")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueButton;

    [Header("Final Victory UI (último piso)")]
    [Tooltip("Panel que se muestra EN LUGAR del de victoria normal cuando se vence al boss del último nivel.")]
    [FormerlySerializedAs("finalVictoryExtras")]
    [SerializeField] private GameObject finalVictoryScreen;
    [Tooltip("Botón de menú principal dentro del panel final.")]
    [SerializeField] private Button finalMainMenuButton;

    [Header("Credits UI (opcional)")]
    [Tooltip("Pantalla de créditos ya armada en el Canvas.")]
    [SerializeField] private GameObject creditsScreen;
    [Tooltip("Botón que abre los créditos (ej: dentro del panel final).")]
    [SerializeField] private Button creditsButton;
    [Tooltip("Botón dentro de la pantalla de créditos para cerrarla y volver.")]
    [SerializeField] private Button creditsBackButton;
    [Tooltip("Botón (opcional) dentro de la pantalla de créditos para ir directo al menú principal.")]
    [SerializeField] private Button creditsMainMenuButton;

    // Panel que se ocultó al abrir los créditos, para volver a mostrarlo al cerrarlos.
    private GameObject screenHiddenByCredits;

    private void Awake()
    {
        IsOpen = false;

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (finalVictoryScreen != null)
            finalVictoryScreen.SetActive(false);

        if (creditsScreen != null)
            creditsScreen.SetActive(false);
    }

    private void Start()
    {
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueRun);

        if (finalMainMenuButton != null)
            finalMainMenuButton.onClick.AddListener(GoToMainMenu);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(CloseCredits);

        if (creditsMainMenuButton != null)
            creditsMainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void TriggerVictory()
    {
        if (IsOpen) return;

        bool isFinalFloor = DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor;

        // Si es el último piso y hay panel final, se usa ese; si no, el normal.
        GameObject screenToShow = (isFinalFloor && finalVictoryScreen != null)
            ? finalVictoryScreen
            : victoryScreen;

        if (screenToShow == null) return;

        IsOpen = true;

        if (victoryScreen != null)
            victoryScreen.SetActive(screenToShow == victoryScreen);

        if (finalVictoryScreen != null)
            finalVictoryScreen.SetActive(screenToShow == finalVictoryScreen);

        // Fallback: si no hay panel final asignado, el normal no debe ofrecer "continuar" en el último piso.
        if (continueButton != null)
            continueButton.gameObject.SetActive(!isFinalFloor);

        BringToFront(screenToShow);

        GamePause.SetPaused(true);
        AudioManager.PlayMusic(GameMusic.Victory);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenCredits()
    {
        if (creditsScreen == null) return;

        // Ocultar el panel de victoria que esté abierto y recordarlo.
        if (finalVictoryScreen != null && finalVictoryScreen.activeSelf)
            screenHiddenByCredits = finalVictoryScreen;
        else if (victoryScreen != null && victoryScreen.activeSelf)
            screenHiddenByCredits = victoryScreen;
        else
            screenHiddenByCredits = null;

        if (screenHiddenByCredits != null)
            screenHiddenByCredits.SetActive(false);

        creditsScreen.SetActive(true);
        BringToFront(creditsScreen);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseCredits()
    {
        HideCredits();

        // Volver al panel que estaba antes.
        if (screenHiddenByCredits != null)
        {
            screenHiddenByCredits.SetActive(true);
            BringToFront(screenHiddenByCredits);
            screenHiddenByCredits = null;
        }
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("VictoryManager: mainMenuSceneName está vacío.", this);
            return;
        }

        HideCredits();
        CloseVictoryHold();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartRun()
    {
        HideCredits();
        CloseVictoryHold();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ContinueRun()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.IsFinalFloor)
            return;

        HideCredits();
        screenHiddenByCredits = null;
        CloseVictoryHold();

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.StartNextFloor();
    }

    private void HideCredits()
    {
        if (creditsScreen != null)
            creditsScreen.SetActive(false);
    }

    private void CloseVictoryHold()
    {
        IsOpen = false;
        GamePause.SetPaused(false);
    }

    private static void BringToFront(GameObject screen)
    {
        if (screen == null) return;
        screen.transform.SetAsLastSibling();
        if (screen.transform.parent != null)
            screen.transform.parent.SetAsLastSibling();
    }
}