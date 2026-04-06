using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour, IMovement
{
    [SerializeField] private float speed = 3f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }
}
