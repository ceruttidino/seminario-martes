using UnityEngine;

public class SnakeFleeState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private Vector2 fleeDirection;
    private float fleeTimer;

    public SnakeFleeState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter()
    {
        fleeTimer = snake.FleeDuration;

        Vector2 awayFromPlayer = player != null
            ? (Vector2)self.position - (Vector2)player.position
            : Vector2.down;

        if (awayFromPlayer.sqrMagnitude < 0.0001f)
            awayFromPlayer = Vector2.down;

        fleeDirection = awayFromPlayer.normalized;
    }

    public void Tick()
    {
        fleeTimer -= Time.deltaTime;

        if (fleeTimer <= 0f)
        {
            behaviour.SetState(new SnakeChaseState(player, movement, self, behaviour, snake));
            return;
        }

        movement.Move(fleeDirection, snake.FleeSpeed);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    public void OnWallHit(Vector2 normal)
    {
        fleeDirection = Vector2.Reflect(fleeDirection, normal).normalized;
    }
}
