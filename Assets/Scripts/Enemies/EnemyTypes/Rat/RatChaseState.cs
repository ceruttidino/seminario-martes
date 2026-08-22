using UnityEngine;

public class RatChaseState : IEnemyState
{
    private Transform player;
    private EnemyMovement movement;
    private Transform enemy;
    private EnemyBehaviour behaviour;
    private RegeneratingRat rat;

    private float slowDownDistance = 1.5f;

    public RatChaseState(Transform player, EnemyMovement movement, Transform enemy, EnemyBehaviour behaviour, RegeneratingRat rat)
    {
        this.player = player;
        this.movement = movement;
        this.enemy = enemy;
        this.behaviour = behaviour;
        this.rat = rat;
    }

    public void Enter(){ }
    
    public void Tick()
    {
        if (player == null)
        {
            Debug.LogError("PLAYER NULL");
            return;
        }

        Vector2 toPlayer = player.position - enemy.position;
        float distance = toPlayer.magnitude;
        float stopDistance = rat.AttackTriggerRange;

        if (distance <= stopDistance)
        {
            movement.Move(Vector2.zero);
            behaviour.SetState(new RatAttackState(player, movement, enemy, behaviour, rat));
            return;
        }

        Vector2 dir = toPlayer.normalized;

        float speedMultiplier = Mathf.Clamp01((distance - stopDistance) / (slowDownDistance - stopDistance));

        movement.Move(dir * speedMultiplier);
    }

    public void Exit() { }
}
