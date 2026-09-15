using UnityEngine;

public class Supercontainer : MonoBehaviour
{
    [SerializeField] private float maxHealth = 24f;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    private float currentHealth;
    private bool destroyed;
    private bool defenseStarted;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Transform barFill;
    private GameObject healthBarRoot;

    public bool IsDestroyed => destroyed;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    public event System.Action Destroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        BuildHealthBar();
        ApplyClosedVisual();
        HideHealthBar();
    }

    public void PrepareForChallenge()
    {
        destroyed = false;
        defenseStarted = false;
        currentHealth = maxHealth;
        ApplyClosedVisual();
        SetPresent(true);
        HideHealthBar();
        RefreshHealthBar();
    }

    public void BeginDefense()
    {
        if (destroyed) return;

        defenseStarted = true;
        ShowHealthBar();
        RefreshHealthBar();
    }

    public void NotifyPlayerHit()
    {
        if (destroyed || defenseStarted)
            return;

        ChallengeRoomController controller = GetComponentInParent<ChallengeRoomController>();
        if (controller == null)
            return;

        controller.StartChallenge();
    }

    public void TakeHitFromEnemy(float damage)
    {
        if (destroyed || !defenseStarted) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshHealthBar();

        if (currentHealth <= 0f)
            Break();
    }

    public void MarkOpened()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            if (openSprite != null)
                spriteRenderer.sprite = openSprite;
        }

        HideHealthBar();
    }

    public void MarkFailed()
    {
        SetPresent(false);
        HideHealthBar();
    }

    private void Break()
    {
        destroyed = true;
        MarkFailed();
        Destroyed?.Invoke();
    }

    private void ApplyClosedVisual()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = Color.white;
        if (closedSprite != null)
            spriteRenderer.sprite = closedSprite;
    }

    private void SetPresent(bool present)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = present;

        if (bodyCollider != null)
            bodyCollider.enabled = present;
    }

    private void BuildHealthBar()
    {
        Sprite square = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f),
            4f);

        healthBarRoot = new GameObject("HealthBar");
        healthBarRoot.transform.SetParent(transform, false);
        healthBarRoot.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        healthBarRoot.transform.localScale = new Vector3(1.6f, 0.14f, 1f);

        SpriteRenderer bg = healthBarRoot.AddComponent<SpriteRenderer>();
        bg.sprite = square;
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        bg.sortingOrder = 8;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(healthBarRoot.transform, false);
        barFill = fillGo.transform;

        SpriteRenderer fill = fillGo.AddComponent<SpriteRenderer>();
        fill.sprite = square;
        fill.color = Color.green;
        fill.sortingOrder = 9;

        healthBarRoot.SetActive(false);
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

    private void ShowHealthBar()
    {
        if (healthBarRoot != null)
            healthBarRoot.SetActive(true);
    }

    private void HideHealthBar()
    {
        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);
    }
}
