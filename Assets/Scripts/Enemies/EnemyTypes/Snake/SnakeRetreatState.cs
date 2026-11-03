using UnityEngine;

/// <summary>
/// El jugador se acerco a menos de 3 tiles: la serpiente se aleja hasta quedar a una distancia comoda de disparo
/// (GDD). Si la acorralan (no puede seguir alejandose) deja de huir y dispara igual.
/// </summary>
public class SnakeRetreatState : IEnemyState
{
    private const float ArriveDistance = 0.4f;
    private const float RepickInterval = 0.8f;

    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private Vector2 target;
    private bool hasTarget;
    private float repickAt;
    private int stuckCount;
    private PoisonousSnake.StuckWatch stuckWatch;

    public SnakeRetreatState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter()
    {
        stuckCount = 0;
        stuckWatch.Reset(self.position);
        PickTarget();
    }

    public void Tick()
    {
        if (player == null)
        {
            movement.Move(Vector2.zero);
            return;
        }

        Vector2 position = self.position;
        Vector2 playerPosition = player.position;
        float distance = Vector2.Distance(position, playerPosition);

        // Ya esta a la distancia apropiada: vuelve a lo suyo (seguir atacando si ya habia empezado, si no acechar).
        if (distance >= snake.ComfortDistance)
        {
            if (snake.Engaged)
                behaviour.SetState(new SnakeAttackState(player, movement, self, behaviour, snake, false));
            else
                behaviour.SetState(new SnakeRoamState(player, movement, self, behaviour, snake));
            return;
        }

        // Acorralada: no puede alejarse mas (dos ventanas seguidas sin avanzar). Dispara en vez de quedarse inmovil
        // (y no vuelve a huir por un momento).
        if (stuckWatch.Tick(position, Time.deltaTime))
        {
            stuckCount++;

            if (stuckCount >= 2)
            {
                behaviour.SetState(new SnakeAttackState(player, movement, self, behaviour, snake, true));
                return;
            }

            PickTarget();
        }

        bool arrived = hasTarget && Vector2.Distance(position, target) <= ArriveDistance;
        if (!hasTarget || arrived || Time.time >= repickAt)
            PickTarget();

        if (hasTarget)
            movement.MoveTowards(target, snake.RetreatSpeed);
        else
            movement.Move(Vector2.zero);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }

    /// <summary>Lo avisa EnemyBehaviour cuando la serpiente choca con una pared: busca otro rumbo.</summary>
    public void OnWallHit(Vector2 normal)
    {
        PickTarget();
    }

    private void PickTarget()
    {
        repickAt = Time.time + RepickInterval;

        Vector2 playerPosition = player != null ? (Vector2)player.position : (Vector2)self.position;
        hasTarget = snake.TryPickPoint(playerPosition, PoisonousSnake.PickMode.Retreat, out target);
    }
}
