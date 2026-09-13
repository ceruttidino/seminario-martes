using UnityEngine;

public class Supercontainer : MonoBehaviour
{
    [SerializeField] private float maxHealth = 24f;

    private float currentHealth;
    private bool destroyed;
    private SpriteRenderer spriteRenderer;
    private Transform barFill;

    public bool IsDestroyed => destroyed;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    public event System.Action Destroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        BuildHealthBar();
    }

    public void TakeHitFromEnemy(float damage)
    {
        if (destroyed) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshHealthBar();

        if (currentHealth <= 0f)
            Break();
    }

    public void MarkOpened()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.35f, 0.85f, 0.4f, 1f);

        HideHealthBar();
    }

    private void Break()
    {
        destroyed = true;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        HideHealthBar();
        Destroyed?.Invoke();
    }

    private void BuildHealthBar()
    {
        Sprite square = spriteRenderer != null ? spriteRenderer.sprite : null;
        if (square == null) return;

        GameObject barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        barRoot.transform.localScale = new Vector3(1.35f, 0.16f, 1f);

        SpriteRenderer bg = barRoot.AddComponent<SpriteRenderer>();
        bg.sprite = square;
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        bg.sortingOrder = 8;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot.transform, false);
        barFill = fillGo.transform;

        SpriteRenderer fill = fillGo.AddComponent<SpriteRenderer>();
        fill.sprite = square;
        fill.color = Color.green;
        fill.sortingOrder = 9;

        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (barFill == null) return;

        float percent = HealthNormalized;
        barFill.localScale = new Vector3(percent, 0.7f, 1f);
        barFill.localPosition = new Vector3((percent - 1f) * 0.5f, 0f, 0f);

        SpriteRenderer fill = barFill.GetComponent<SpriteRenderer>();
        if (fill != null)
            fill.color = Color.Lerp(new Color(0.85f, 0.15f, 0.1f), new Color(0.25f, 0.85f, 0.25f), percent);
    }

    private void HideHealthBar()
    {
        if (barFill != null && barFill.parent != null)
            barFill.parent.gameObject.SetActive(false);
    }
}
