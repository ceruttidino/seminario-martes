using UnityEngine;

public class AntDetection : MonoBehaviour
{
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask playerPlayer;

    public Transform DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerPlayer);
        return hit ? hit.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
