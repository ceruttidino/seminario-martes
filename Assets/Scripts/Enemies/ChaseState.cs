using UnityEngine;

public class ChaseState : IEnemyState
{
    private Transform player;
    private EnemyMovement movement;
    private Transform enemy;
    private EnemyAttack attack;

    private float stopDistance = 0.8f;
    private float slowDownDistance = 1.5f;

    public ChaseState(Transform player, EnemyMovement movement, Transform enemy, EnemyAttack attack)
    {
        this.player = player;
        this.movement = movement;
        this.enemy = enemy;
        this.attack = attack;
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

        if (distance <= stopDistance)
        {
            movement.Move(Vector2.zero);
            attack.TryAttack();
            return;
        }

        Vector2 dir = toPlayer.normalized;

        float speedMultiplier = Mathf.Clamp01((distance - stopDistance) / (slowDownDistance - stopDistance));

        movement.Move(dir * speedMultiplier);
    }

    public void Exit() { }
}
