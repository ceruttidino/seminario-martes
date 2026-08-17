using UnityEngine;

public class SnakeChaseState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    public SnakeChaseState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
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
        if (player == null) return;

        Vector2 toPlayer = player.position - self.position;
        float distance = toPlayer.magnitude;

        if (distance <= snake.AttackRange)
        {
            behaviour.SetState(new SnakeAttackState(player, movement, self, behaviour, snake));
            return;
        }

        movement.Move(toPlayer.normalized, snake.ChaseSpeed);
    }

    public void Exit() { }
}
