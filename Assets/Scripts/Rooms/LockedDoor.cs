using UnityEngine;
using UnityEngine.InputSystem;

public class LockedDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = true;

    private bool playerInRange = false;
    private GameObject currentPlayer;

    private void Update()
    {
        if (!playerInRange || !isLocked) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            TryOpen();
    }

    private void TryOpen()
    {
        if (currentPlayer == null) return;

        PlayerKeys keys = currentPlayer.GetComponent<PlayerKeys>();
        if (keys != null && keys.UseKey())
            OpenDoor();
    }

    private void OpenDoor()
    {
        isLocked = false;
        GetComponent<Collider2D>().enabled = false;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        currentPlayer = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        currentPlayer = null;
    }
}
