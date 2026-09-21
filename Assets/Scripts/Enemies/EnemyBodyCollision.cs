using UnityEngine;

public class EnemyBodyCollision : MonoBehaviour
{
    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        bool isBoss = GetComponent<BossBase>() != null;

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.isTrigger)
                continue;

            // Enemies must collide with each other.
            if (enemyLayer >= 0)
                col.excludeLayers &= ~(1 << enemyLayer);

            // Regular enemies never physically shove the player. The boss stays solid.
            if (playerLayer >= 0)
            {
                if (isBoss)
                    col.excludeLayers &= ~(1 << playerLayer);
                else
                    col.excludeLayers |= 1 << playerLayer;
            }
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (isBoss)
        {
            rb.mass = 80f;
            return;
        }

        if (rb.mass > 20f)
            rb.mass = 8f;
    }
}
