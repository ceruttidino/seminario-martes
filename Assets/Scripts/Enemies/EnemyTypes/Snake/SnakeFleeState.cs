using UnityEngine;

public class SnakeFleeState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    public SnakeFleeState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter() { }

    public void Tick()
    {
        if (snake.IsStunned)
        {
            movement.Move(Vector2.zero);
            return;
        }

        PlayerPoisonStatus poison = player != null ? player.GetComponent<PlayerPoisonStatus>() : null;
        if (poison == null || !poison.IsPoisoned)
        {
            behaviour.SetState(new SnakeChaseState(player, movement, self, behaviour, snake));
            return;
        }

        Vector2 away = player != null
            ? (Vector2)self.position - (Vector2)player.position
            : Vector2.down;

        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.down;

        Vector2 fleePoint = (Vector2)self.position + away.normalized * 2.4f;
        movement.MoveTowards(fleePoint, snake.FleeSpeed);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    public void OnWallHit(Vector2 normal)
    {
        Vector2 bounce = Vector2.Reflect(((Vector2)self.position - (Vector2)player.position).normalized, normal);
        movement.MoveTowards((Vector2)self.position + bounce * 2f, snake.FleeSpeed);
    }
}
