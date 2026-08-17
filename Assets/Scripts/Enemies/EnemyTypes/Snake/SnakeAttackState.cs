using UnityEngine;

public class SnakeAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;
    private readonly Animator animator;

    private float windupTimer;

    public SnakeAttackState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
        animator = self.GetComponent<Animator>();
    }

    public void Enter()
    {
        windupTimer = snake.WindupDuration;
        movement.Move(Vector2.zero);

        if (animator != null)
            animator.SetBool("IsWindingUp", true);
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
        if (animator != null)
            animator.SetBool("IsWindingUp", false);
    }
}
