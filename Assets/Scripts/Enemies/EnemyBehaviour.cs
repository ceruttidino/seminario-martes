using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private IEnemyState currentState;

    [SerializeField] private EnemyType enemyType;

    [SerializeField] private Transform player;
    private EnemyAttack attack;

    private EnemyMovement movement;
    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
    }

    private void Update()
    {
        currentState?.Tick();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        switch (enemyType)
        {
            case EnemyType.Rat:
                SetState(new ChaseState(player, movement, transform, attack));
                break;

            case EnemyType.Snail:
                SetState(new SnailMoveState(player, movement, transform));
                break;

            case EnemyType.Ant:
                SetState(new AntMoveState(player, movement, transform, this));
                break;
        }
    }

    public void SetState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Tick()
    {
        currentState?.Tick();
    }

    public void OnWallHit(Vector2 normal)
    {
        if (currentState is SnailMoveState snail)
        {
            snail.OnWallHit(normal);
        }
        if (currentState is AntMoveState ant)
        {
            ant.OnWallHit(normal);
        }
    }
}
