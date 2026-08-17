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
            Vector2 normal = Vector2.zero;
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                normal += collision.contacts[i].normal;
            }
            normal = (normal / collision.contacts.Length).normalized;

            behaviour.OnWallHit(normal);
        }
    }
}
