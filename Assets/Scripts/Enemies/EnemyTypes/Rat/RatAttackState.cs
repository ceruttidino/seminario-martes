using UnityEngine;

public class RatAttackState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly RegeneratingRat rat;

    private const float AttackWindup = 0.35f;

    private float elapsed;
    private bool attacked;

    public RatAttackState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, RegeneratingRat rat)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.rat = rat;
    }

    public void Enter()
    {
        elapsed = 0f;
        attacked = false;
        movement.Move(Vector2.zero);
        FacePlayer();
    }

    public void Tick()
    {
        elapsed += Time.deltaTime;
        movement.Move(Vector2.zero);

        if (!attacked)
        {
            if (player == null)
                return;

            FacePlayer();

            float distance = Vector2.Distance(self.position, player.position);
            if (distance > rat.AttackTriggerRange + 0.45f)
            {
                behaviour.SetState(new RatChaseState(player, movement, self, behaviour, rat));
                return;
            }

            if (elapsed < AttackWindup)
                return;

            attacked = true;
            elapsed = 0f;
            rat.BeginAttackFeedback(player.position);
            rat.TryStrike(player);
        }

        if (elapsed >= rat.AttackDuration)
            behaviour.SetState(new RatChaseState(player, movement, self, behaviour, rat));
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        movement.Face(player.position - self.position);
    }

    public void Exit()
    {
        rat.EndAttackFeedback();
    }
}
