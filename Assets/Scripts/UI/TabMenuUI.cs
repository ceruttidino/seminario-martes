using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabMenuUI : MonoBehaviour
{
    public static TabMenuUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    [System.Serializable]
    public class ToastRow
    {
        public GameObject root;
        public CanvasGroup canvasGroup;
        public Image icon;
        public TMP_Text label;
        public TMP_Text count;
        [HideInInspector] public string itemKey;
        [HideInInspector] public int amount;
        [HideInInspector] public Coroutine routine;
    }

    [Header("Siempre visible")]
    [SerializeField] private Button tabButton;

    [Header("Solo con TAB")]
    [SerializeField] private GameObject dimOverlay;
    [SerializeField] private GameObject[] inventoryPanels;

    [Header("Inventario de buffs")]
    [SerializeField] private Image[] buffSlotImages;
    [SerializeField] private TMP_Text[] buffSlotNames;
    [SerializeField] private TMP_Text[] buffSlotCounts;

    [Header("Avisos de pickup")]
    [SerializeField] private GameObject toastContainer;
    [SerializeField] private ToastRow[] toastRows;
    [SerializeField] private float pickupToastDuration = 3f;

    private bool open;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (tabButton != null)
        {
            tabButton.onClick.AddListener(Toggle);
            Image buttonImage = tabButton.GetComponent<Image>();
            Sprite tabIcon = FindTabSprite();
            if (buttonImage != null && tabIcon != null)
                buttonImage.sprite = tabIcon;
        }

        SetInventoryVisible(false);
        HideAllToasts();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (GameOverManager.IsOpen || VictoryManager.IsOpen) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (open && CombatBlocksTab())
        {
            Close();
            return;
        }

        if (keyboard.tabKey.wasPressedThisFrame)
            Toggle();

        if (open && keyboard.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Toggle()
    {
        if (open)
            Close();
        else
            Open();
    }

    public static void CloseCurrent()
    {
        if (Instance != null)
            Instance.Close();
    }

    public void Open()
    {
        if (open) return;
        if (GameOverManager.IsOpen || VictoryManager.IsOpen) return;
        if (GamePause.IsPaused) return;
        if (CombatBlocksTab()) return;

        HideAllToasts();
        open = true;
        RefreshBuffSlots();
        SetInventoryVisible(true);
    }

    public void Close()
    {
        if (!open) return;

        open = false;
        SetInventoryVisible(false);
    }

    public static bool CombatBlocksTab()
    {
        DungeonManager dungeon = DungeonManager.Instance;
        RoomInstance room = dungeon != null ? dungeon.CurrentRoom : null;
        if (room == null)
            return false;

        if (room.HasLivingEnemies())
            return true;

        ChallengeRoomController challenge = room.GetComponent<ChallengeRoomController>();
        return challenge != null && challenge.HoldsDoorsLocked;
    }

    public void ShowPickup(Sprite icon, string label, int amount = 1)
    {
        if (open) return;
        if (toastRows == null || toastRows.Length == 0) return;

        string key = EnglishItemName(label);
        if (amount < 1) amount = 1;

        ToastRow existing = FindActiveRow(key);
        if (existing != null)
        {
            existing.amount += amount;
            ApplyRow(existing, icon, key, existing.amount);
            RestartRow(existing);
            return;
        }

        ToastRow free = FindFreeRow();
        if (free == null)
            free = toastRows[0];

        ApplyRow(free, icon, key, amount);
        RestartRow(free);
        RefreshToastContainer();
    }

    private ToastRow FindActiveRow(string key)
    {
        for (int i = 0; i < toastRows.Length; i++)
        {
            ToastRow row = toastRows[i];
            if (row != null && row.root != null && row.root.activeSelf && row.itemKey == key)
                return row;
        }

        return null;
    }

    private ToastRow FindFreeRow()
    {
        for (int i = 0; i < toastRows.Length; i++)
        {
            ToastRow row = toastRows[i];
            if (row != null && row.root != null && !row.root.activeSelf)
                return row;
        }

        return null;
    }

    private void ApplyRow(ToastRow row, Sprite icon, string key, int amount)
    {
        row.itemKey = key;
        row.amount = amount;

        if (row.icon != null)
        {
            row.icon.sprite = icon;
            row.icon.enabled = icon != null;
        }

        if (row.label != null)
            row.label.text = key;

        if (row.count != null)
        {
            bool showCount = amount > 1;
            row.count.text = showCount ? $"x{amount}" : string.Empty;
            row.count.enabled = showCount;
        }
    }

    private void RestartRow(ToastRow row)
    {
        if (row.routine != null)
            StopCoroutine(row.routine);

        row.routine = StartCoroutine(ToastRoutine(row));
    }

    private IEnumerator ToastRoutine(ToastRow row)
    {
        if (row.root != null)
            row.root.SetActive(true);

        RefreshToastContainer();

        float fadeIn = 0.25f;
        float fadeOut = 0.7f;
        float hold = Mathf.Max(0.2f, pickupToastDuration - fadeIn - fadeOut);

        yield return FadeRow(row, 0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return FadeRow(row, 1f, 0f, fadeOut);

        HideRow(row);
        RefreshToastContainer();
    }

    private IEnumerator FadeRow(ToastRow row, float from, float to, float duration)
    {
        if (row.canvasGroup == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        row.canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            row.canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        row.canvasGroup.alpha = to;
    }

    private void HideAllToasts()
    {
        if (toastRows == null) return;

        for (int i = 0; i < toastRows.Length; i++)
            HideRow(toastRows[i]);

        RefreshToastContainer();
    }

    private void HideRow(ToastRow row)
    {
        if (row == null) return;

        if (row.routine != null)
        {
            StopCoroutine(row.routine);
            row.routine = null;
        }

        row.itemKey = null;
        row.amount = 0;

        if (row.canvasGroup != null)
            row.canvasGroup.alpha = 0f;

        if (row.root != null)
            row.root.SetActive(false);
    }

    private void RefreshToastContainer()
    {
        if (toastContainer == null || toastRows == null) return;

        bool any = false;
        for (int i = 0; i < toastRows.Length; i++)
        {
            if (toastRows[i] != null && toastRows[i].root != null && toastRows[i].root.activeSelf)
            {
                any = true;
                break;
            }
        }

        toastContainer.SetActive(any);
    }

    private void SetInventoryVisible(bool visible)
    {
        if (dimOverlay != null)
            dimOverlay.SetActive(visible);

        if (inventoryPanels == null) return;

        for (int i = 0; i < inventoryPanels.Length; i++)
        {
            if (inventoryPanels[i] != null)
                inventoryPanels[i].SetActive(visible);
        }
    }

    private void RefreshBuffSlots()
    {
        if (buffSlotImages == null) return;

        PlayerUpgradeManager manager = FindFirstObjectByType<PlayerUpgradeManager>();
        List<UpgradeSO> collected = manager != null ? manager.GetCollectedUpgrades() : null;

        var counts = new Dictionary<UpgradeSO, int>();
        var order = new List<UpgradeSO>();

        if (collected != null)
        {
            for (int i = 0; i < collected.Count; i++)
            {
                UpgradeSO upgrade = collected[i];
                if (upgrade == null) continue;

                if (counts.ContainsKey(upgrade))
                    counts[upgrade]++;
                else
                {
                    counts[upgrade] = 1;
                    order.Add(upgrade);
                }
            }
        }

        for (int i = 0; i < buffSlotImages.Length; i++)
        {
            Image slot = buffSlotImages[i];
            if (slot == null) continue;

            bool hasItem = i < order.Count;
            GameObject row = slot.transform.parent != null ? slot.transform.parent.gameObject : slot.gameObject;
            row.SetActive(hasItem);

            if (!hasItem) continue;

            UpgradeSO upgrade = order[i];
            slot.enabled = true;
            slot.sprite = upgrade.icon;
            slot.color = Color.white;

            if (buffSlotNames != null && i < buffSlotNames.Length && buffSlotNames[i] != null)
            {
                buffSlotNames[i].text = EnglishItemName(upgrade.upgradeName);
                buffSlotNames[i].enabled = true;
            }

            if (buffSlotCounts != null && i < buffSlotCounts.Length && buffSlotCounts[i] != null)
            {
                int times = counts[upgrade];
                buffSlotCounts[i].text = times > 1 ? $"x{times}" : string.Empty;
                buffSlotCounts[i].enabled = times > 1;
            }
        }
    }

    private static Sprite FindTabSprite()
    {
        Sprite resource = Resources.Load<Sprite>("UI/TabButton");
        if (resource != null)
            return resource;

        Sprite[] sliced = Resources.LoadAll<Sprite>("UI/TabButton");
        if (sliced != null && sliced.Length > 0)
            return sliced[0];

        Sprite[] all = Resources.FindObjectsOfTypeAll<Sprite>();
        Sprite fallback = null;
        for (int i = 0; i < all.Length; i++)
        {
            Sprite sprite = all[i];
            if (sprite == null) continue;
            string name = sprite.name;
            if (name.Equals("tab 1", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("tab_1", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("TabButton", System.StringComparison.OrdinalIgnoreCase))
                return sprite;

            if (fallback == null && name.StartsWith("tab", System.StringComparison.OrdinalIgnoreCase))
                fallback = sprite;
        }

        return fallback;
    }

    public static string EnglishItemName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Item";

        switch (name.Trim())
        {
            case "Ganzua":
            case "Ganzúa":
            case "Ganzuas":
            case "Ganzúas":
            case "Key":
                return "Lockpick";
            case "MisteryPotion":
                return "Mystery Potion";
            case "HealthLoot":
                return "Heart";
            default:
                return name.Trim();
        }
    }
}
