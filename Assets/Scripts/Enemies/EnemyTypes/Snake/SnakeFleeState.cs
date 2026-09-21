using UnityEngine;

public class SnakeFleeState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private Vector2 wanderPoint;
    private bool hasWanderPoint;
    private float nextRepickTime;
    private float stuckTimer;
    private Vector2 lastPos;

    public SnakeFleeState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter()
    {
        lastPos = self.position;
        PickNewSpot(force: true);
    }

    public void Tick()
    {
        if (snake.IsStunned)
        {
            movement.Move(Vector2.zero);
            return;
        }

        PlayerPoisonStatus poison = player != null ? player.GetComponent<PlayerPoisonStatus>() : null;
        if (poison == null || !poison.IsPoisoned)
        {
            behaviour.SetState(new SnakeChaseState(player, movement, self, behaviour, snake));
            return;
        }

        Vector2 pos = self.position;
        if (Vector2.Distance(pos, lastPos) < 0.04f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;
        lastPos = pos;

        bool arrived = hasWanderPoint && Vector2.Distance(pos, wanderPoint) <= 0.45f;
        if (!hasWanderPoint || arrived || Time.time >= nextRepickTime || stuckTimer > 0.45f)
            PickNewSpot(force: arrived || stuckTimer > 0.45f);

        if (hasWanderPoint)
            movement.MoveTowards(wanderPoint, snake.FleeSpeed);
        else
            movement.Move(Vector2.zero);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    public void OnWallHit(Vector2 normal)
    {
        PickNewSpot(force: true);
    }

    private void PickNewSpot(bool force)
    {
        if (!force && Time.time < nextRepickTime && hasWanderPoint)
            return;

        stuckTimer = 0f;
        nextRepickTime = Time.time + Random.Range(1.1f, 2.1f);

        Vector2 avoid = player != null ? (Vector2)player.position : (Vector2)self.position;
        RoomPathGrid grid = RoomPathGrid.For(self);
        if (grid != null && grid.TryGetWanderPoint(self.position, avoid, 1.6f, out Vector2 point))
        {
            wanderPoint = point;
            hasWanderPoint = true;
            return;
        }

        hasWanderPoint = false;
    }
}
