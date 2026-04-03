using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private float pickupDelay = 0.6f;        // tiempo antes de que se pueda recoger
    [SerializeField] private bool autoPickup = false;         // cambia a FALSE para loot de cofres

    private bool canBePickedUp = false;
    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;
        GetComponent<Collider2D>().isTrigger = true;   // aseguro que sea Trigger
    }

    private void Update()
    {
        if (!canBePickedUp && Time.time > spawnTime + pickupDelay)
        {
            canBePickedUp = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canBePickedUp) return;

        if (other.CompareTag("Player"))
        {
            // acá va la lógica de recoger según el tipo de loot

            Destroy(gameObject);

            Debug.Log($"Jugador recogió: {gameObject.name}");

        }
    }

    // para q no se agarre inmediatamente al spawnear
    public void DisableAutoPickup()
    {
        autoPickup = false;
    }
}