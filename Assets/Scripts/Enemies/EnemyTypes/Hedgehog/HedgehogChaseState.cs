using UnityEngine;

public class HedgehogChaseState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform enemy;
    private readonly EnemyBehaviour behaviour;
    private readonly ExplosiveHedgehog hedgehog;

    public HedgehogChaseState(
        Transform player,
        EnemyMovement movement,
        Transform enemy,
        EnemyBehaviour behaviour,
        ExplosiveHedgehog hedgehog)
    {
        this.player = player;
        this.movement = movement;
        this.enemy = enemy;
        this.behaviour = behaviour;
        this.hedgehog = hedgehog;
    }

    public void Enter() { }

    public void Tick()
    {
        if (player == null || hedgehog == null) return;

        float distance = Vector2.Distance(player.position, enemy.position);
        if (distance <= hedgehog.ArmingRange)
        {
            behaviour.SetState(new HedgehogArmingState(player, movement, enemy, behaviour, hedgehog));
            return;
        }

        if (hedgehog.IsBeingKnockedBack)
            return;

        movement.MoveTowards(player.position, hedgehog.ChaseSpeed);
    }

    public void Exit() { }
}
