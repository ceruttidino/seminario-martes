using UnityEngine;

public class TurtleChargeState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly SpikyTurtle turtle;

    private Vector2 direction;
    private float chargeTimer;

    public TurtleChargeState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, SpikyTurtle turtle, Vector2 initialDirection)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.turtle = turtle;
        direction = initialDirection.normalized;
    }

    public void Enter()
    {
        chargeTimer = turtle.ChargeDuration;
    }

    public void Tick()
    {
        // La embestida NO debe esquivar obstaculos: es un ataque que choca y
        // rebota a proposito contra paredes/piedras (ver OnWallHit mas abajo).
        movement.Move(direction, turtle.ChargeSpeed, avoidObstacles: false);

        chargeTimer -= Time.deltaTime;
        if (chargeTimer <= 0f)
        {
            EndCharge();
        }
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    public void OnWallHit(Vector2 normal)
    {
        direction = Vector2.Reflect(direction, normal).normalized;
    }

    private void EndCharge()
    {
        behaviour.SetState(new TurtleUpsideDownState(player, movement, self, behaviour, turtle));
    }
}
