using UnityEngine;

public class RatAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly RegeneratingRat rat;

    private float elapsed;
    private bool hasStruck;

    public RatAttackState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, RegeneratingRat rat)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.rat = rat;
    }

    public void Enter()
    {
        elapsed = 0f;
        hasStruck = false;
        movement.Move(Vector2.zero);
        rat.BeginAttackFeedback();
    }

    public void Tick()
    {
        elapsed += Time.deltaTime;

        if (!hasStruck && elapsed >= rat.HitTiming)
        {
            hasStruck = true;
            rat.TryStrike(player);
        }

        if (elapsed >= rat.AttackDuration)
        {
            behaviour.SetState(new RatChaseState(player, movement, self, behaviour, rat));
        }
    }

    public void Exit()
    {
        rat.EndAttackFeedback();
    }
}
