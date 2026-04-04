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

    private void Update()
    {
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
            {
                currentInteractable.ShowHighlight(false);
            }

            currentInteractable = foundInteractable;

            if (currentInteractable != null)
            {
                currentInteractable.ShowHighlight(true); // resalta el objeto
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint == null ? transform.position : interactionPoint.position, interactionRadius);
    }
}
