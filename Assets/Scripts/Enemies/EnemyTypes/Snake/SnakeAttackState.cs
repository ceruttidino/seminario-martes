using UnityEngine;

public class SnakeAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private float windupTimer;

    public SnakeAttackState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter()
    {
        windupTimer = snake.WindupDuration;
        movement.Move(Vector2.zero);
        snake.BeginWindupFeedback();
    }

    public void Tick()
    {
        windupTimer -= Time.deltaTime;

        if (windupTimer <= 0f)
        {
            snake.PerformAttack(player);
            behaviour.SetState(new SnakeFleeState(player, movement, self, behaviour, snake));
        }
    }

    public void Exit()
    {
        snake.EndWindupFeedback();
    }
}
