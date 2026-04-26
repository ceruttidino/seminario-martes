using TMPro;
using UnityEngine;

public class PlayerResourcesUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerScrap playerScrap;
    [SerializeField] private PlayerKeys playerKeys;

    [Header("UI")]
    [SerializeField] private TMP_Text scrapText;
    [SerializeField] private TMP_Text keysText;

    private void Awake()
    {
        if (playerScrap == null) 
        {
            playerScrap = FindFirstObjectByType<PlayerScrap>();
        }
        if (playerKeys == null)
        {
            playerKeys = FindFirstObjectByType<PlayerKeys>();
        }
    }

    private void OnEnable()
    {
        if (playerScrap != null)
            playerScrap.OnScrapChanged += UpdateScrapUI;

        if (playerKeys != null)
            playerKeys.OnKeysChanged += UpdateKeysUI;
    }

    private void Start()
    {
        if (playerScrap != null)
            UpdateScrapUI(playerScrap.CurrentScrap);

        if (playerKeys != null)
            UpdateKeysUI(playerKeys.CurrentKeys);
    }

    private void OnDisable()
    {
        if (playerScrap != null)
            playerScrap.OnScrapChanged -= UpdateScrapUI;

        if (playerKeys != null)
            playerKeys.OnKeysChanged -= UpdateKeysUI;
    }

    private void UpdateScrapUI(int amount)
    {
        scrapText.text = amount.ToString();
    }

    private void UpdateKeysUI(int amount)
    {
        keysText.text = amount.ToString();
    }

}
