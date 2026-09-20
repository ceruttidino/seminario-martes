using UnityEngine;

public class HedgehogArmingState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform enemyTransform;
    private readonly EnemyBehaviour behaviour;
    private readonly ExplosiveHedgehog hedgehog;

    private float countdown;

    public HedgehogArmingState(
        Transform player,
        EnemyMovement movement,
        Transform enemy,
        EnemyBehaviour behaviour,
        ExplosiveHedgehog hedgehog)
    {
        this.player = player;
        this.movement = movement;
        this.enemyTransform = enemy;
        this.behaviour = behaviour;
        this.hedgehog = hedgehog;
    }

    public void Enter()
    {
        countdown = hedgehog.CountdownDuration;
        hedgehog.BeginArmingFeedback();
        ApproachPlayerSlowly();
    }

    public void Tick()
    {
        if (hedgehog == null) return;

        if (player != null)
        {
            float distance = Vector2.Distance(enemyTransform.position, player.position);
            if (distance >= hedgehog.CancelRange)
            {
                behaviour.SetState(new HedgehogChaseState(player, movement, enemyTransform, behaviour, hedgehog));
                return;
            }
        }

        countdown -= Time.deltaTime;
        float elapsed = hedgehog.CountdownDuration - countdown;
        hedgehog.UpdateArmingFeedback(hedgehog.CountdownDuration > 0f ? elapsed / hedgehog.CountdownDuration : 1f);

        if (!hedgehog.IsBeingKnockedBack)
            ApproachPlayerSlowly();

        if (countdown <= 0f)
            hedgehog.Explode();
    }

    public void Exit()
    {
        hedgehog?.EndArmingFeedback();
    }

    private void ApproachPlayerSlowly()
    {
        if (player == null || enemyTransform == null)
        {
            movement.Move(Vector2.zero);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)enemyTransform.position;
        if (toPlayer.sqrMagnitude < 0.01f)
        {
            movement.Move(Vector2.zero);
            return;
        }

        movement.Move(toPlayer.normalized, hedgehog.ArmingMoveSpeed);
    }
}
