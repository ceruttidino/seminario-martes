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
    private Color originalColor;

    [Header("Loot (prefab + LootItem por cada tipo)")]
    [SerializeField] private DropEntry heartDrop;
    [SerializeField] private DropEntry keyDrop;
    [SerializeField] private DropEntry scrapDrop;
    [SerializeField] private Transform spawnPoint;

    [Header("Loot Config")]
    [Range(0f, 100f)]
    [SerializeField] private float chanceToFindLoot = 75f;
    [SerializeField] private int maxItems = 2;

    [Header("Enemigo Topo")]
    [SerializeField] private GameObject molePrefab;
    [Range(0f, 100f)]
    [SerializeField] private float moleSpawnChance = 10f;

    private bool isDug = false;
    private bool isPlayerNear = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (spawnPoint == null) spawnPoint = transform;

        gameObject.layer = LayerMask.NameToLayer("Interactable");

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
    }

    public void ShowHighlight(bool show)
    {
        if (isDug) return;
        isPlayerNear = show;
        if (spriteRenderer != null)
            spriteRenderer.color = show ? highlightColor : originalColor;
    }

    public void Interact()
    {
        if (isDug || !isPlayerNear) return;
        isDug = true;

        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // pequeña chance de que aparezca un Topo en lugar de loot
        if (molePrefab != null && Random.Range(0f, 100f) <= moleSpawnChance)
        {
            Transform roomParent = GetComponentInParent<RoomInstance>()?.transform;
            GameObject mole = Instantiate(molePrefab, spawnPoint.position, Quaternion.identity);
            if (roomParent != null)
                mole.transform.SetParent(roomParent, true);
            Destroy(gameObject);
            return;
        }

        if (Random.Range(0f, 100f) <= chanceToFindLoot)
            SpawnLoot();

        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        // armar lista de drops disponibles (ignorar los no asignados en el inspector)
        var options = new List<DropEntry>();
        if (heartDrop.prefab != null) options.Add(heartDrop);
        if (keyDrop.prefab != null) options.Add(keyDrop);
        if (scrapDrop.prefab != null) options.Add(scrapDrop);

        if (options.Count == 0) return;

        // elegir UN tipo y spawnear 1-maxItems del mismo (sin mezclar)
        DropEntry chosen = options[Random.Range(0, options.Count)];
        int amount = Random.Range(1, maxItems + 1);

        Transform roomParent = GetComponentInParent<RoomInstance>()?.transform;

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1.2f);
            Vector3 pos = spawnPoint.position + new Vector3(offset.x, offset.y, 0f);
            GameObject spawned = Instantiate(chosen.prefab, pos, Quaternion.identity);

            // pasar el LootItem al pickup para que sepa qué efecto aplicar
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
