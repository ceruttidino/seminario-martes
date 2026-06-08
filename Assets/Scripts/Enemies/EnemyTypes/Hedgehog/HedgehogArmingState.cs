using UnityEngine;

public class HedgehogArmingState : IEnemyState
{
    private readonly Transform player;
    private readonly EnemyMovement movement;
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
        this.hedgehog = hedgehog;
        enemyHealth = enemy.GetComponent<EnemyHealth>();
    }

    public void Enter()
    {
        countdown = hedgehog.CountdownDuration;
        knockbackEndTime = 0f;
        movement.Move(Vector2.zero);

        if (enemyHealth != null)
            enemyHealth.OnDamaged += HandleDamaged;
    }

    public void Tick()
    {
        if (hedgehog == null) return;

        countdown -= Time.deltaTime;

        if (Time.time >= knockbackEndTime)
            movement.Move(Vector2.zero);

        if (countdown <= 0f)
        {
            hedgehog.Explode();
            return;
        }
    }

    public void Exit()
    {
        if (enemyHealth != null)
            enemyHealth.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged()
    {
        if (player == null || hedgehog == null) return;

        hedgehog.ApplyHitKnockback(player.position);
        knockbackEndTime = Time.time + hedgehog.HitKnockbackDuration;
    }
}
