using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private IEnemyState currentState;

    [SerializeField] private Transform player;

    private EnemyMovement movement;
    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        SetState(new ChaseState(player, movement, transform));
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
}
