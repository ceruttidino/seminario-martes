using UnityEngine;

public class TurtleWanderState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly SpikyTurtle turtle;

    private const float MinWanderInterval = 1f;
    private const float MaxWanderInterval = 2.5f;

    private Vector2 wanderDirection;
    private float wanderTimer;

    private bool isWindingUp;
    private float windupTimer;

    public TurtleWanderState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, SpikyTurtle turtle)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.turtle = turtle;
    }

    public void Enter()
    {
        isWindingUp = false;
        PickNewDirection();
    }

    public void Tick()
    {
        if (isWindingUp)
        {
            TickWindup();
            return;
        }

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            PickNewDirection();
        }

        movement.Move(wanderDirection);

        float distanceToPlayer = Vector2.Distance(self.position, player.position);
        if (distanceToPlayer <= turtle.DetectionRange)
        {
            StartWindup();
        }
    }

    public void Exit()
    {
        // Defensivo: si algun dia se sale del wander por otro motivo mientras
        // esta en pleno windup, evita que el flash quede pegado.
        if (isWindingUp)
            turtle.EndWindupFeedback();
    }

    public void OnWallHit(Vector2 normal)
    {
        wanderDirection = Vector2.Reflect(wanderDirection, normal).normalized;
    }

    private void PickNewDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer = Random.Range(MinWanderInterval, MaxWanderInterval);
    }

    private void StartWindup()
    {
        isWindingUp = true;
        windupTimer = turtle.ChargeWindup;
        movement.Move(Vector2.zero);
        turtle.BeginWindupFeedback();
    }

    private void TickWindup()
    {
        movement.Move(Vector2.zero);
        windupTimer -= Time.deltaTime;

        if (windupTimer <= 0f)
        {
            turtle.EndWindupFeedback();

            Vector2 direction = (player.position - self.position);
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;

            behaviour.SetState(new TurtleChargeState(player, movement, self, behaviour, turtle, direction.normalized));
        }
    }
}
