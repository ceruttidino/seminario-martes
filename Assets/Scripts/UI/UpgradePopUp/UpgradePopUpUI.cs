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
    [SerializeField] private float slideDistance = 200f; // cuánto sube al aparecer
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

        // Guarda la posición visible y calcula la oculta (más abajo)
        shownPos = panel.anchoredPosition;
        hiddenPos = shownPos - new Vector2(0f, slideDistance);

        panel.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;
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
            iconImage.enabled = upgrade.icon != null; // evita el cuadro blanco si no hay ícono
        }
        if (nameText != null) nameText.text = upgrade.upgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.description;

        yield return Animate(hiddenPos, shownPos, 0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Animate(shownPos, hiddenPos, 1f, 0f, fadeOutTime);
    }

    private IEnumerator Animate(Vector2 fromPos, Vector2 toPos, float fromA, float toA, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled: funciona aunque pauses con timeScale = 0
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            panel.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            canvasGroup.alpha = Mathf.Lerp(fromA, toA, k);
            yield return null;
        }
        panel.anchoredPosition = toPos;
        canvasGroup.alpha = toA;
    }
}