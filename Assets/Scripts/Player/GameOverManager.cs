using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Serializable]
    private struct EnemyPortrait
    {
        public EnemyType type;
        public Sprite sprite;
    }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [Tooltip("Nombre EXACTO de la escena del menú principal (tiene que estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Killer Portrait")]
    [Tooltip("Image del canvas donde se muestra al enemigo que te mató.")]
    [SerializeField] private Image killerImage;
    [Tooltip("Opcional. Sprite fijo por tipo de enemigo. Si un tipo no está acá, se usa el frame que tenía el enemigo al pegarte.")]
    [SerializeField] private EnemyPortrait[] portraitOverrides;
    [Tooltip("Opcional. Se muestra si no se sabe quién te mató. Si está vacío, la Image se oculta.")]
    [SerializeField] private Sprite unknownKillerSprite;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        IsOpen = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    private void Start()
    {
        if (gameOverScreen == null)
        {
            Debug.LogError("GameOverManager: gameOverScreen no está asignado en el Inspector.");
            return;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnPlayerDeath += ShowGameOverScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDeath -= ShowGameOverScreen;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverScreen == null) return;
        if (IsOpen) return;

        IsOpen = true;
        ShowKiller(playerHealth != null ? playerHealth.LastDamageSource : null);

        gameOverScreen.SetActive(true);
        BringToFront(gameOverScreen);

        GamePause.SetPaused(true);
        AudioManager.PlayMusic(GameMusic.GameOver);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowKiller(DamageSourceInfo killer)
    {
        if (killerImage == null) return;

        Sprite sprite = ResolvePortrait(killer);
        if (sprite == null)
        {
            if (killer != null)
                Debug.LogWarning($"GameOverManager: '{killer.Name}' no tiene sprite ni override de retrato.");

            killerImage.gameObject.SetActive(false);
            return;
        }

        killerImage.gameObject.SetActive(true);
        killerImage.sprite = sprite;
        killerImage.preserveAspect = true;
        // Color blanco forzado: el sprite viene de la foto, pero la Image no hereda tints/alpha del enemigo
        // (flash de daño, búho invisible, etc.).
        killerImage.color = Color.white;
    }

    private Sprite ResolvePortrait(DamageSourceInfo killer)
    {
        if (killer == null)
            return unknownKillerSprite;

        // Un DamagePortrait con sprite fijo en el prefab manda sobre todo lo demás.
        if (killer.IsExplicitPortrait && killer.Sprite != null)
            return killer.Sprite;

        if (killer.HasEnemyType && portraitOverrides != null)
        {
            for (int i = 0; i < portraitOverrides.Length; i++)
            {
                if (portraitOverrides[i].type == killer.EnemyType && portraitOverrides[i].sprite != null)
                    return portraitOverrides[i].sprite;
            }
        }

        return killer.Sprite != null ? killer.Sprite : unknownKillerSprite;
    }

    public void RestartLevel()
    {
        IsOpen = false;
        GamePause.SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GameOverManager: mainMenuSceneName está vacío.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError($"GameOverManager: la escena '{mainMenuSceneName}' no existe o no está en Build Settings.");
            return;
        }

        // Mismo orden que RestartLevel: despausar ANTES de cargar, si no el menú arranca con timeScale = 0.
        IsOpen = false;
        GamePause.SetPaused(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void BringToFront(GameObject screen)
    {
        if (screen == null) return;
        screen.transform.SetAsLastSibling();
        if (screen.transform.parent != null)
            screen.transform.parent.SetAsLastSibling();
    }
}