using UnityEngine;

public class ChaseState : IEnemyState
{
    private Transform player;
    private EnemyMovement movement;
    private Transform enemy;

    public ChaseState(Transform player, EnemyMovement movement, Transform enemy)
    {
        this.player = player;
        this.movement = movement;
        this.enemy = enemy;
    }

    public void Enter(){ }
    
    public void Tick()
    {
        if (player == null)
        {
            Debug.LogError("PLAYER NULL");
            return;
        }

        Vector2 dir = (player.position - enemy.position).normalized;
        movement.Move(dir);
    }

    public void Exit() { }
}
