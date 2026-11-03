using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactionRadius = 1.5f;
    public LayerMask interactableLayer;
    public Transform interactionPoint;

    private IInteractable currentInteractable;

    [Tooltip("OBSOLETO: el cartel de la tecla F ahora lo dibuja cada objeto interactuable (InteractPrompt, con la fuente del juego). Este texto viejo queda apagado.")]
    [SerializeField] private GameObject interactableAdvice;

    private void Awake()
    {
        // El texto "F" que colgaba del jugador usaba la fuente por defecto de TextMeshPro y no la del resto del juego.
        // Ahora el cartel aparece sobre el pozo / puerta / item, asi que el viejo se apaga para no duplicar la letra.
        if (interactableAdvice != null)
            interactableAdvice.SetActive(false);
    }

    private void Update()
    {
        if (GamePause.IsGameplayFrozen) return;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.IsActionLocked) return;

        CheckNearbyInteractables();

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void CheckNearbyInteractables()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(interactionPoint == null ? transform.position : interactionPoint.position, interactionRadius, interactableLayer);

        IInteractable foundInteractable = null;

        if (colliders.Length > 0)
        {

            foundInteractable = colliders[0].GetComponent<IInteractable>();
        }

        if (currentInteractable != foundInteractable)
        {
            if (currentInteractable != null)
                currentInteractable.ShowHighlight(false);

            currentInteractable = foundInteractable;

            if (currentInteractable != null)
                currentInteractable.ShowHighlight(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint == null ? transform.position : interactionPoint.position, interactionRadius);
    }
}
