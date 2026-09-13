using UnityEngine;

public class ChallengeContainerHunter : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float speedMultiplier = 0.6f;

    private Supercontainer container;
    private EnemyMovement movement;
    private float damage = 1f;
    private float nextAttackTime;
    private bool ready;

    public void Setup(Supercontainer target, float slowMultiplier)
    {
        container = target;
        speedMultiplier = slowMultiplier;
        movement = GetComponent<EnemyMovement>();
        damage = ResolveDamage();
        DisablePlayerAttacks();
        IgnorePlayerCollision();
        ready = true;
    }

    private void Update()
    {
        if (!ready || container == null || container.IsDestroyed)
        {
            movement?.Move(Vector2.zero);
            return;
        }

        Vector2 toContainer = container.transform.position - transform.position;
        float distance = toContainer.magnitude;

        if (distance > attackRange)
        {
            float speed = movement != null ? movement.Speed * speedMultiplier : 1.5f;
            movement?.Move(toContainer.normalized, speed);
            return;
        }

        movement?.Move(Vector2.zero);
        movement?.Face(toContainer);

        if (Time.time >= nextAttackTime)
        {
            container.TakeHitFromEnemy(damage);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private float ResolveDamage()
    {
        RegeneratingRat rat = GetComponent<RegeneratingRat>();
        if (rat != null)
            return rat.AttackDamage;

        PoisonousSnake snake = GetComponent<PoisonousSnake>();
        if (snake != null)
            return snake.AttackDamage;

        ExplosiveHedgehog hedgehog = GetComponent<ExplosiveHedgehog>();
        if (hedgehog != null)
            return hedgehog.ExplosionDamage;

        SpikyTurtle turtle = GetComponent<SpikyTurtle>();
        if (turtle != null)
            return turtle.ChargeDamage;

        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null)
            return attack.Damage;

        return 1f;
    }

    private void DisablePlayerAttacks()
    {
        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null)
            attack.enabled = false;

        SlimeTrailSpawner slime = GetComponent<SlimeTrailSpawner>();
        if (slime != null)
            slime.enabled = false;

        ExplosiveHedgehog hedgehog = GetComponent<ExplosiveHedgehog>();
        if (hedgehog != null)
            hedgehog.enabled = false;

        SpikyTurtle turtle = GetComponent<SpikyTurtle>();
        if (turtle != null)
            turtle.enabled = false;

        Mole mole = GetComponent<Mole>();
        if (mole != null)
            mole.enabled = false;

        RatRegeneration regeneration = GetComponent<RatRegeneration>();
        if (regeneration != null)
            regeneration.enabled = false;
    }

    private void IgnorePlayerCollision()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();
        Collider2D[] selfCols = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D self in selfCols)
        {
            if (self == null) continue;
            foreach (Collider2D other in playerCols)
            {
                if (other == null) continue;
                Physics2D.IgnoreCollision(self, other, true);
            }
        }
    }
}
