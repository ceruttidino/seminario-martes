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
        if (behaviour == null)
            return;

        if ((wallLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            if (collision.contactCount <= 0)
                return;

            Vector2 normal = Vector2.zero;
            for (int i = 0; i < collision.contactCount; i++)
                normal += collision.GetContact(i).normal;

            if (normal.sqrMagnitude < 0.0001f)
                return;

            behaviour.OnWallHit(normal.normalized);
        }
    }
}
