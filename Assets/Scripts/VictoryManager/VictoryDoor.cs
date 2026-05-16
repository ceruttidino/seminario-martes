using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VictoryDoor : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool destroyOnTouch = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            VictoryManager victoryManager = FindFirstObjectByType<VictoryManager>();

            if (victoryManager != null)
            {
                victoryManager.TriggerVictory();
            }
            else
            {
                Debug.LogError("No se encontró VictoryManager en la escena");
            }

            if (destroyOnTouch)
                Destroy(gameObject, 0.2f);
        }
    }
}
