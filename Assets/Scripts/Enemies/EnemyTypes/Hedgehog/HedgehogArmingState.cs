using UnityEngine;

public class HedgehogArmingState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform enemyTransform;
    private readonly ExplosiveHedgehog hedgehog;

    private EnemyHealth enemyHealth;
    private float countdown;
    private float knockbackEndTime;

    public HedgehogArmingState(
        Transform player,
        EnemyMovement movement,
        Transform enemy,
        EnemyBehaviour behaviour,
        ExplosiveHedgehog hedgehog)
    {
        this.player = player;
        this.movement = movement;
        this.enemyTransform = enemy;
        this.hedgehog = hedgehog;
        enemyHealth = enemy.GetComponent<EnemyHealth>();
    }

    public void Enter()
    {
        countdown = hedgehog.CountdownDuration;
        knockbackEndTime = 0f;
        ApproachPlayerSlowly();
        hedgehog.BeginArmingFeedback();

        if (enemyHealth != null)
            enemyHealth.OnDamaged += HandleDamaged;
    }

    public void Tick()
    {
        if (hedgehog == null) return;

        countdown -= Time.deltaTime;

        if (Time.time >= knockbackEndTime)
            ApproachPlayerSlowly();

        if (countdown <= 0f)
        {
            hedgehog.Explode();
            return;
        }
    }

    public void Exit()
    {
        hedgehog?.EndArmingFeedback();

        if (enemyHealth != null)
            enemyHealth.OnDamaged -= HandleDamaged;
    }

    // Sigue avanzando hacia el jugador, pero mucho mas lento que en la persecucion
    // normal, para que la explosion no sea trivial de esquivar quedandose quieto.
    private void ApproachPlayerSlowly()
    {
        if (player == null || enemyTransform == null)
        {
            movement.Move(Vector2.zero);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)enemyTransform.position;

        if (toPlayer.sqrMagnitude < 0.01f)
        {
            movement.Move(Vector2.zero);
            return;
        }

        movement.Move(toPlayer.normalized, hedgehog.ArmingMoveSpeed);
    }

    private void HandleDamaged()
    {
        if (player == null || hedgehog == null) return;

        hedgehog.ApplyHitKnockback(player.position);
        knockbackEndTime = Time.time + hedgehog.HitKnockbackDuration;
    }
}
