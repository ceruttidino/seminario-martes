using UnityEngine;

public class EnemyCollisionHandler : MonoBehaviour
{
    private EnemyBehaviour behaviour;

    [SerializeField] private LayerMask wallLayer;

    private void Awake()
    {
        behaviour = GetComponent<EnemyBehaviour>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((wallLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            ContactPoint2D contact = collision.contacts[0];
            Vector2 normal = contact.normal;

            behaviour.OnWallHit(normal);
        }
    }
}
