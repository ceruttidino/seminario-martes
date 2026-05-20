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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError($"EnemyBehaviour: no se encontró un objeto con tag 'Player' para {gameObject.name}.");
            return;
        }

        player = playerObj.transform;

        switch (enemyType)
        {
            case EnemyType.Rat:
                SetState(new RatChaseState(player, movement, transform, attack));
                break;

            case EnemyType.Snail:
                SetState(new SnailMoveState(player, movement, transform));
                break;

            case EnemyType.Ant:
                SetState(new AntMoveState(player, movement, transform, this, attack));
                break;

            case EnemyType.Mole:
                // Mole maneja su propia lógica con el componente Mole.cs
                break;

            default:
                Debug.LogWarning($"EnemyBehaviour: no hay estado implementado para el tipo '{enemyType}' en {gameObject.name}.");
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
