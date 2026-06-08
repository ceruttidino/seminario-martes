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

        Vector2 toPlayer = player.position - enemy.position;
        float distance = toPlayer.magnitude;

        if (distance <= hedgehog.ArmingRange)
        {
            behaviour.SetState(new HedgehogArmingState(player, movement, enemy, behaviour, hedgehog));
            return;
        }

        movement.Move(toPlayer.normalized);
    }

    public void Exit() { }
}
