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
            Debug.Log("¡Jugador tocó la puerta de victoria!");

            VictoryManager victoryManager = FindObjectOfType<VictoryManager>();

            if (victoryManager != null)
            {
                victoryManager.TriggerVictory();
                Debug.Log("VictoryManager activado correctamente");
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