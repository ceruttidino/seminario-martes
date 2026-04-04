using UnityEngine;
using System.Collections.Generic;

public class DiggingSpot : MonoBehaviour, IInteractable
{
    [Header("Visuales y Detección")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.yellow;
    private Color originalColor;

    [Header("Configuración de Loot")]
    [Tooltip("Probabilidad general de encontrar ALGO en la tierra (0 = jamás, 100 = siempre revisa la tabla)")]
    [SerializeField] [Range(0f, 100f)] private float chanceToFindLoot = 100f; // CHANCE PARA TESTEAR

    [Tooltip("El LootTable contiene internamente TODAS las listas (Bolsa Común, Contenedor Verde, etc.)")]
    [SerializeField] private LootTableSO lootTable;
    [SerializeField] private Transform spawnPoint;

    [Header("Elige con qué lista interactuar")]
    [Tooltip("Si pones CommonBag usa esa lista del LootTable. Si pones GreenContainer usa la otra.")]
    [SerializeField] private TrashType trashTypeParaExcavar = TrashType.CommonBag;

    [Header("Enemigo Especial")]
    [SerializeField] private GameObject specialEnemyPrefab;
    [SerializeField] [Range(0f, 100f)] private float enemySpawnChance = 15f;

    private bool isDug = false;
    private bool isPlayerNear = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (spawnPoint == null) spawnPoint = transform;

        
        gameObject.layer = LayerMask.NameToLayer("Interactable");

        
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }
    }

    public void ShowHighlight(bool show)
    {
        if (isDug) return;
        
        isPlayerNear = show;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = show ? highlightColor : originalColor;
        }
    }

    public void Interact()
    {
        if (isDug || !isPlayerNear) return;

        Debug.Log("Excavando en la zona...");
        isDug = true;

        
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // spawnea enemigo
        float randomValue = Random.Range(0f, 100f);
        if (specialEnemyPrefab != null && randomValue <= enemySpawnChance)
        {
            Debug.Log("¡Apareció un enemigo al excavar!");
            Instantiate(specialEnemyPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            // tira loot segun porcentaje
            if (Random.value * 100f <= chanceToFindLoot)
            {
                if (lootTable != null)
                {
                    SpawnLoot();
                }
                else
                {
                    Debug.Log("No se encontró nada (No hay tabla de loot asignada).");
                }
            }
            else
            {
                Debug.Log("No se encontró nada (suerte = mala).");
            }
        }

        // destruye el objeto
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        LootItem[] itemsToSpawn = lootTable.GetRandomLoot(trashTypeParaExcavar); 

        if (itemsToSpawn == null || itemsToSpawn.Length == 0)
        {
            Debug.Log("No se encontró nada en este tile.");
            return;
        }

        bool spawnedSomething = false;

        foreach (var item in itemsToSpawn)
        {
            if (item != null && item.prefab != null)
            {
                // los objetos no caen en el centro exacto
                Vector2 randomOffset = Random.insideUnitCircle * 0.5f; 
                Instantiate(item.prefab, (Vector2)spawnPoint.position + randomOffset, Quaternion.identity);
                spawnedSomething = true;
            }
        }

        if (!spawnedSomething)
        {
            Debug.Log("No se encontró nada (Items sin prefab).");
        }
    }
}
