using UnityEngine;

public class SnakeAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private float windupTimer;
    private bool struck;

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
        if (snake.IsStunned)
        {
            movement.Move(Vector2.zero);
            return;
        }

        if (struck)
        {
            PlayerPoisonStatus poison = player != null ? player.GetComponent<PlayerPoisonStatus>() : null;
            if (poison != null && poison.IsPoisoned)
                behaviour.SetState(new SnakeFleeState(player, movement, self, behaviour, snake));
            else
                behaviour.SetState(new SnakeChaseState(player, movement, self, behaviour, snake));
            return;
        }

        windupTimer -= Time.deltaTime;
        movement.Move(Vector2.zero);

        if (windupTimer <= 0f)
        {
            snake.PerformAttack(player);
            struck = true;
        }
    }

    public void Exit()
    {
        snake.EndWindupFeedback();
    }
}
