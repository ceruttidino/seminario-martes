using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour, IMovement
{
    [SerializeField] private float speed = 3f;
    private Rigidbody2D rb;

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector2 lastFacingDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer  == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Move(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;

        UpdateAnimator(direction);
    }

    private void UpdateAnimator(Vector2 direction)
    {
        if (animator == null) return;

        float currentSpeed = direction.magnitude;

        float animationPlaybackSpeed = currentSpeed > 0.01f ? 1f : 0f;
        animator.SetFloat("AnimSpeed", animationPlaybackSpeed);

        if (direction != Vector2.zero)
        {
            lastFacingDirection = direction.normalized;

            // Flip the sprite based on horizontal direction
            if (spriteRenderer != null)
            {
                if (direction.x > 0) spriteRenderer.flipX = true;
                else if (direction.x < 0) spriteRenderer.flipX = false;
            }
        }

        animator.SetFloat("X", lastFacingDirection.x);
        animator.SetFloat("Y", lastFacingDirection.y);
    }
}
