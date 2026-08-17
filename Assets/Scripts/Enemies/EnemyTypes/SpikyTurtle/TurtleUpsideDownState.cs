using UnityEngine;

public class TurtleUpsideDownState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly SpikyTurtle turtle;
    private readonly EnemyHealth health;

    private float timer;
    private bool hitReceived;

    public TurtleUpsideDownState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, SpikyTurtle turtle)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.turtle = turtle;
        health = self.GetComponent<EnemyHealth>();
    }

    public void Enter()
    {
        timer = turtle.UpsideDownDuration;
        hitReceived = false;

        turtle.SetUpsideDown(true);
        movement.Move(Vector2.zero);

        if (health != null)
            health.OnDamaged += HandleHit;
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        if (hitReceived || timer <= 0f)
        {
            behaviour.SetState(new TurtleWanderState(player, movement, self, behaviour, turtle));
        }
    }

    public void Exit()
    {
        turtle.SetUpsideDown(false);

        if (health != null)
            health.OnDamaged -= HandleHit;
    }

    private void HandleHit()
    {
        hitReceived = true;
    }
}
