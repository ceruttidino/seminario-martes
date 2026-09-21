using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyBehaviour))]
public class PoisonousSnake : MonoBehaviour
{
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float attackRange = 0.7f;

    [Header("Attack")]
    [SerializeField] private float windupDuration = 0.5f;
    [SerializeField] private float biteStunDuration = 0.45f;
    [SerializeField] private float attackDamage = 1f;
    public float AttackDamage => attackDamage;
    [SerializeField] private DamageFlash damageFlash;

    [Header("Flee")]
    [SerializeField] private float fleeSpeed = 4f;

    [Header("Company")]
    [SerializeField] private GameObject companionPrefab;

    private bool stunned;
    private float stunTimer;
    private EnemyMovement movement;

    public float ChaseSpeed => chaseSpeed;
    public float AttackRange => attackRange;
    public float WindupDuration => 0.5f;
    public float FleeSpeed => fleeSpeed;
    public bool IsStunned => stunned;
    public GameObject CompanionPrefab => companionPrefab;

    private void Awake()
    {
        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
        movement = GetComponent<EnemyMovement>();
        windupDuration = 0.5f;
    }

    private void Update()
    {
        if (!stunned)
            return;

        stunTimer -= Time.deltaTime;
        if (movement != null)
            movement.Move(Vector2.zero);

        if (stunTimer <= 0f)
            stunned = false;
    }

    public void BeginWindupFeedback()
    {
        damageFlash?.StartLoopFlash();
    }

    public void EndWindupFeedback()
    {
        damageFlash?.StopLoopFlash();
    }

    public void Stun(float duration)
    {
        stunned = true;
        stunTimer = duration;
        if (movement != null)
            movement.Move(Vector2.zero);
    }

    public bool PerformAttack(Transform player)
    {
        if (player == null)
            return false;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > attackRange + 0.2f)
            return false;

        PlayerPoisonStatus poisonStatus = player.GetComponent<PlayerPoisonStatus>();
        if (poisonStatus == null)
            poisonStatus = player.gameObject.AddComponent<PlayerPoisonStatus>();

        PlayerMovement playerMove = player.GetComponent<PlayerMovement>();
        playerMove?.Stun(biteStunDuration);
        Stun(biteStunDuration);

        if (poisonStatus.IsPoisoned)
        {
            player.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
        }
        else
        {
            poisonStatus.ApplyPoison();
        }

        return true;
    }

    public static void EnsureNonSnakeCompany(RoomInstance room)
    {
        if (room == null)
            return;

        EnemyBehaviour[] behaviours = room.GetComponentsInChildren<EnemyBehaviour>(true);
        bool hasSnake = false;
        bool hasNonSnake = false;
        PoisonousSnake snake = null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;

            if (behaviours[i].Type == EnemyType.Snake)
            {
                hasSnake = true;
                if (snake == null)
                    snake = behaviours[i].GetComponent<PoisonousSnake>();
            }
            else
            {
                hasNonSnake = true;
            }
        }

        if (!hasSnake || hasNonSnake || snake == null || snake.companionPrefab == null)
            return;

        Vector3 spawnPos = snake.transform.position + Vector3.right * 1.1f;
        RoomPathGrid grid = RoomPathGrid.For(room.transform);
        if (grid != null && !grid.IsWalkableWorld(spawnPos))
            spawnPos = snake.transform.position + Vector3.left * 1.1f;

        GameObject companion = Instantiate(snake.companionPrefab, spawnPos, Quaternion.identity, room.transform);
        room.RegisterSpawnedCombatEnemy(companion);
    }
}
