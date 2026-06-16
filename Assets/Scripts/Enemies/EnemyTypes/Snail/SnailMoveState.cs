using UnityEngine;

public class SnailMoveState : IEnemyState
{
    private IMovement movement;
    private Transform enemyTransform;
    private EnemyAttack attack;

    private Vector2 direction;
    private float changeDirTimer;

    public SnailMoveState(Transform player, IMovement movement, Transform transform, EnemyAttack attack)
    {
        this.movement = movement;
        this.enemyTransform = transform;
        this.attack = attack;
    }

    public void Enter()
    {
        PickNewDirection();
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    public void Tick()
    {
        changeDirTimer -= Time.deltaTime;

        if (changeDirTimer <= 0)
        {
            PickNewDirection();
        }

        movement.Move(direction);

        attack?.TryAttack();
    }

    private void PickNewDirection()
    {
        direction = Random.insideUnitCircle.normalized;
        changeDirTimer = Random.Range(2f, 4f);
    }

    public void OnWallHit(Vector2 normal)
    {
        direction = normal;

        movement.Move(normal * 2f);

        direction += Random.insideUnitCircle * 0.05f;
        direction.Normalize();
    }
}
