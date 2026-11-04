using System.Collections.Generic;
using UnityEngine;

public class DiggingSpot : MonoBehaviour, IInteractable
{
    [System.Serializable]
    private struct DropEntry
    {
        public GameObject prefab;
        public LootItem lootItem;
    }

    [Header("Visuales")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("Donde flota el cartel de la tecla F respecto del pozo (unidades de mundo).")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.85f, 0f);
    private Color originalColor;
    private InteractPrompt prompt;

    [Header("Loot (prefab + LootItem por cada tipo)")]
    [SerializeField] private DropEntry heartDrop;
    [SerializeField] private DropEntry keyDrop;
    [SerializeField] private DropEntry scrapDrop;
    [SerializeField] private Transform spawnPoint;

    [Header("Loot Config")]
    [Range(0f, 100f)]
    [SerializeField] private float chanceToFindLoot = 45f;
    [SerializeField] private int maxItems = 1;

    [Header("Enemigo Topo")]
    [SerializeField] private GameObject molePrefab;
    [Range(0f, 100f)]
    [SerializeField] private float moleSpawnChance = 15f;

    private bool isDug = false;
    private bool isPlayerNear = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (spawnPoint == null) spawnPoint = transform;

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
            gameObject.layer = interactableLayer;

        // El pozo nunca bloquea el cuerpo. Si el prefab trae un collider solido, el jugador se queda pegado encima.
        Collider2D[] cols = GetComponents<Collider2D>();
        if (cols.Length == 0)
        {
            BoxCollider2D created = gameObject.AddComponent<BoxCollider2D>();
            created.isTrigger = true;
        }
        else
        {
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].isTrigger = true;
            }
        }

        // Cartel "F" flotante (misma fuente que el resto del juego). Aparece cuando el jugador esta cerca.
        // Si el cartel falla, cavar con F sigue funcionando.
        try
        {
            prompt = InteractPrompt.Create(transform, promptOffset);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    public void ShowHighlight(bool show)
    {
        if (isDug)
        {
            SetPromptVisible(false);
            return;
        }

        isPlayerNear = show;
        if (spriteRenderer != null)
            spriteRenderer.color = show ? highlightColor : originalColor;

        SetPromptVisible(show);
    }

    public void Interact()
    {
        if (isDug || !isPlayerNear) return;

        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null && room.HasLivingEnemies())
        {
            GameObject stuckPlayer = GameObject.FindGameObjectWithTag("Player");
            stuckPlayer?.GetComponent<PlayerMovement>()?.ReleaseFromSolids();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerMovement movement = player != null ? player.GetComponent<PlayerMovement>() : null;
        if (movement == null || !movement.TryPlayDig(CompleteDig))
            return;

        isDug = true;
        SetPromptVisible(false);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // Se apaga ya: durante la animacion el jugador esta quieto encima del pozo y, al soltarse,
        // un collider solido lo dejaba empotrado.
        ReleaseCollision();
    }

    private void SetPromptVisible(bool visible)
    {
        if (prompt != null)
            prompt.SetVisible(visible);
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
    }

    private void CompleteDig()
    {
        Vector3 resultPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        RoomInstance room = GetComponentInParent<RoomInstance>();

        DisableInteraction();

        if (ShouldSpawnMole())
        {
            GameMetrics.DigCompleted("mole");
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            GameObject mole = Instantiate(molePrefab, resultPosition, Quaternion.identity);
            // El topo no tiene Rigidbody (collider estatico) y nace donde esta parado el jugador.
            FreePlayerFromMole(mole);

            if (room != null)
            {
                mole.transform.SetParent(room.transform, true);
                room.RegisterSpawnedCombatEnemy(mole);
            }

            EnemyHealth moleHealth = mole.GetComponent<EnemyHealth>();
            if (moleHealth != null)
                moleHealth.OnDeath += ShowHoleAfterMole;
            else
                ShowHole();

            return;
        }

        if (Random.Range(0f, 100f) <= chanceToFindLoot)
            SpawnLoot();

        ShowHole();
    }

    private void ReleaseCollision()
    {
        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;

            cols[i].isTrigger = true;
            cols[i].enabled = false;
        }
    }

    /// <summary>
    /// Separa al jugador del topo recien salido. El topo queda quieto en el pozo con un circulo solido;
    /// si el cuerpo del jugador nace adentro, la fisica no lo puede sacar y queda trabado.
    /// </summary>
    private static void FreePlayerFromMole(GameObject mole)
    {
        if (mole == null)
            return;

        Collider2D[] moleCols = mole.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < moleCols.Length; i++)
        {
            if (moleCols[i] != null)
                moleCols[i].enabled = false;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Rigidbody2D rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        bool cleared = true;

        if (rb != null)
        {
            const float clearance = 1.15f;
            Vector2 origin = mole.transform.position;
            Vector2 delta = rb.position - origin;
            if (delta.sqrMagnitude < clearance * clearance)
            {
                Vector2 preferred = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.down;
                Vector2[] dirs =
                {
                    preferred,
                    Vector2.down,
                    Vector2.up,
                    Vector2.left,
                    Vector2.right
                };

                int wallMask = 1 << 10;
                Vector2 chosen = origin + preferred * clearance;
                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector2 candidate = origin + dirs[i] * clearance;
                    if (Physics2D.OverlapCircle(candidate, 0.28f, wallMask) != null)
                        continue;

                    chosen = candidate;
                    break;
                }

                rb.position = chosen;
                rb.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                cleared = (rb.position - origin).sqrMagnitude >= (clearance * clearance) - 0.01f;
            }
        }

        for (int i = 0; i < moleCols.Length; i++)
        {
            if (moleCols[i] != null)
                moleCols[i].enabled = true;
        }

        if (cleared || player == null)
            return;

        Collider2D body = player.GetComponent<Collider2D>();
        if (body == null)
            return;

        for (int i = 0; i < moleCols.Length; i++)
        {
            if (moleCols[i] != null && !moleCols[i].isTrigger)
                Physics2D.IgnoreCollision(body, moleCols[i]);
        }
    }

    private void DisableInteraction()
    {
        isPlayerNear = false;
        SetPromptVisible(false);
        ReleaseCollision();

        DiggingPileBlink[] blinks = GetComponentsInChildren<DiggingPileBlink>(true);
        for (int i = 0; i < blinks.Length; i++)
        {
            if (blinks[i] != null)
                blinks[i].gameObject.SetActive(false);
        }
    }

    private void ShowHoleAfterMole()
    {
        ShowHole();
    }

    private void ShowHole()
    {
        Sprite hole = LoadHoleSprite();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            if (hole != null)
                spriteRenderer.sprite = hole;
            spriteRenderer.sortingOrder = 0;
        }
    }

    private static Sprite cachedHole;
    private static bool holeLoadAttempted;

    private static Sprite LoadHoleSprite()
    {
        if (holeLoadAttempted)
            return cachedHole;

        holeLoadAttempted = true;
        cachedHole = Resources.Load<Sprite>("Effects/DigHole");
        if (cachedHole != null)
            return cachedHole;

        Texture2D texture = Resources.Load<Texture2D>("Effects/DigHole");
        if (texture == null)
            return null;

        cachedHole = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            64f,
            0,
            SpriteMeshType.FullRect);
        return cachedHole;
    }

    private bool ShouldSpawnMole()
    {
        if (molePrefab == null) return false;
        if (Random.Range(0f, 100f) > moleSpawnChance) return false;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.CurrentHealth <= 1)
            return false;

        return true;
    }

    private void SpawnLoot()
    {
        var options = new List<DropEntry>();
        if (heartDrop.prefab != null) options.Add(heartDrop);
        if (keyDrop.prefab != null) options.Add(keyDrop);
        if (scrapDrop.prefab != null) options.Add(scrapDrop);

        if (options.Count == 0) return;

        DropEntry chosen = options[Random.Range(0, options.Count)];
        int amount = Random.Range(1, maxItems + 1);

        Transform roomParent = GetComponentInParent<RoomInstance>()?.transform;

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1.2f);
            Vector3 pos = spawnPoint.position + new Vector3(offset.x, offset.y, 0f);
            GameObject spawned = Instantiate(chosen.prefab, pos, Quaternion.identity);

            LootPickup pickup = spawned.GetComponent<LootPickup>();
            if (pickup != null && chosen.lootItem != null)
                pickup.SetLootItem(chosen.lootItem);

            if (roomParent != null)
                spawned.transform.SetParent(roomParent, true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, 1.2f);
    }
}
