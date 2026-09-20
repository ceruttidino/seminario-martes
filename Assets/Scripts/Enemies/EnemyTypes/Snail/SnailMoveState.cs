using UnityEngine;

public class SnailMoveState : IEnemyState
{
    private const float TrailActiveDuration = 4f;
    private const float TrailPausedDuration = 2.5f;
    private const float TrailCycleDuration = TrailActiveDuration + TrailPausedDuration;

    private IMovement movement;
    private Transform enemyTransform;
    // El caracol no hace daño por contacto: solo el rastro de baba.
    private EnemyAttack attack;
    private SlimeTrailSpawner slimeTrailSpawner;

    private Vector2 direction;
    private float changeDirTimer;

    private float trailCycleTimer;
    private bool isTrailActive;

    public SnailMoveState(Transform player, IMovement movement, Transform transform, EnemyAttack attack)
    {
        this.movement = movement;
        this.enemyTransform = transform;
        this.attack = attack;
        this.slimeTrailSpawner = transform.GetComponent<SlimeTrailSpawner>();
    }

    public void Enter()
    {
        PickNewDirection();

        trailCycleTimer = 0f;
        SetTrailActive(true);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
        SetTrailActive(true);
    }

    public void Tick()
    {
        changeDirTimer -= Time.deltaTime;

        if (changeDirTimer <= 0)
        {
            PickNewDirection();
        }

        if (movement is EnemyMovement enemyMovement)
            enemyMovement.Move(direction, enemyMovement.Speed, false);
        else
            movement.Move(direction);

        UpdateTrailCycle();
    }

    private void UpdateTrailCycle()
    {
        trailCycleTimer += Time.deltaTime;

        if (trailCycleTimer >= TrailCycleDuration)
        {
            trailCycleTimer -= TrailCycleDuration;
        }

        SetTrailActive(trailCycleTimer < TrailActiveDuration);
    }

    private void SetTrailActive(bool active)
    {
        if (isTrailActive == active) return;

        isTrailActive = active;

        if (slimeTrailSpawner != null)
        {
            slimeTrailSpawner.enabled = active;
        }
    }

    private void PickNewDirection()
    {
        direction = Random.insideUnitCircle.normalized;
        changeDirTimer = Random.Range(2f, 4f);
    }

    public void OnWallHit(Vector2 normal)
    {
        direction = normal;

        if (movement is EnemyMovement enemyMovement)
            enemyMovement.Move(normal * 2f, enemyMovement.Speed, false);
        else
            movement.Move(normal * 2f);

        direction += Random.insideUnitCircle * 0.05f;
        direction.Normalize();
    }
}
