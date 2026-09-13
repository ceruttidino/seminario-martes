using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePopupUI : MonoBehaviour
{
    public static UpgradePopupUI Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Animación")]
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float fadeInTime = 0.25f;
    [SerializeField] private float holdTime = 2.5f;
    [SerializeField] private float fadeOutTime = 0.4f;

    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private readonly Queue<UpgradeSO> queue = new Queue<UpgradeSO>();
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        shownPos = panel.anchoredPosition;
        hiddenPos = shownPos - new Vector2(0f, slideDistance);

        panel.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Toast only: never steal clicks from Pause / Victory / Game Over.
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(UpgradeSO upgrade)
    {
        if (upgrade == null) return;
        queue.Enqueue(upgrade);
        if (!isShowing) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isShowing = true;
        while (queue.Count > 0)
            yield return StartCoroutine(ShowRoutine(queue.Dequeue()));
        isShowing = false;
    }

    private IEnumerator ShowRoutine(UpgradeSO upgrade)
    {
        if (iconImage != null)
        {
            iconImage.sprite = upgrade.icon;
            iconImage.enabled = upgrade.icon != null;
        }
        if (nameText != null) nameText.text = upgrade.upgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.description;

        yield return Animate(hiddenPos, shownPos, 0f, 1f, fadeInTime);

        float held = 0f;
        while (held < holdTime)
        {
            if (ShouldHideForMenus())
            {
                canvasGroup.alpha = 0f;
                yield return null;
                continue;
            }

            if (canvasGroup.alpha < 1f)
                canvasGroup.alpha = 1f;

            held += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Animate(shownPos, hiddenPos, 1f, 0f, fadeOutTime);
    }

    private IEnumerator Animate(Vector2 fromPos, Vector2 toPos, float fromA, float toA, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (ShouldHideForMenus())
            {
                canvasGroup.alpha = 0f;
                yield return null;
                continue;
            }

            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            panel.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            canvasGroup.alpha = Mathf.Lerp(fromA, toA, k);
            yield return null;
        }
        panel.anchoredPosition = toPos;
        canvasGroup.alpha = ShouldHideForMenus() ? 0f : toA;
    }

    private static bool ShouldHideForMenus()
    {
        return GamePause.IsGameplayFrozen
            || GameOverManager.IsOpen
            || VictoryManager.IsOpen;
    }
}
