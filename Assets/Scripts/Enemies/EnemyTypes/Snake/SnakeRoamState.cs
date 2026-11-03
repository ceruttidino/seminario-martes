using UnityEngine;

/// <summary>
/// La serpiente deambula por la room "acechando" al jugador: se mueve entre puntos a distancia media de el, haciendo
/// pausas, pero NO lo persigue. Cuando lo ve (a 10 tiles o menos y con linea de vision libre) se detiene, lo mira un
/// instante y pasa a atacar. Si el jugador se le acerca a menos de 3 tiles se aleja.
/// </summary>
public class SnakeRoamState : IEnemyState
{
    private const float ArriveDistance = 0.35f;
    private const float SightCheckInterval = 0.12f;

    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;

    private Vector2 target;
    private bool hasTarget;
    private float pauseUntil;
    private float retargetAt;
    private float seeTimer;
    private float nextSightCheck;
    private bool hasSight;
    private PoisonousSnake.StuckWatch stuckWatch;

    public SnakeRoamState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour, PoisonousSnake snake)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
    }

    public void Enter()
    {
        hasTarget = false;
        seeTimer = 0f;
        hasSight = false;
        nextSightCheck = 0f;
        stuckWatch.Reset(self.position);
        pauseUntil = Time.time + Random.Range(0.2f, 0.7f);
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
        Vector2 toPlayer = playerPosition - position;
        float distance = toPlayer.magnitude;

        // 1) Muy cerca del jugador: se aleja (GDD: 3 tiles).
        if (distance <= snake.TooCloseRange && !snake.RetreatSuppressed)
        {
            behaviour.SetState(new SnakeRetreatState(player, movement, self, behaviour, snake));
            return;
        }

        // 2) Ya empezo a atacar y todavia no acerto: sigue atacando.
        if (snake.Engaged)
        {
            behaviour.SetState(new SnakeAttackState(player, movement, self, behaviour, snake, false));
            return;
        }

        // 3) Deteccion: linea de vision libre dentro del alcance de disparo.
        if (Time.time >= nextSightCheck)
        {
            nextSightCheck = Time.time + SightCheckInterval;
            hasSight = distance <= snake.FireRange && snake.HasLineOfFire(snake.MuzzlePosition(toPlayer), playerPosition);
        }

        if (hasSight && !snake.IsCalm)
        {
            // Lo vio: se queda quieta mirandolo un instante (aviso para el jugador) y despues ataca.
            movement.Move(Vector2.zero);
            movement.Face(toPlayer);

            seeTimer += Time.deltaTime;
            if (seeTimer >= snake.DetectionDelay)
                behaviour.SetState(new SnakeAttackState(player, movement, self, behaviour, snake, false));

            return;
        }

        seeTimer = Mathf.Max(0f, seeTimer - Time.deltaTime * 2f);

        Wander(position, playerPosition);
    }

    private void Wander(Vector2 position, Vector2 playerPosition)
    {
        if (Time.time < pauseUntil)
        {
            movement.Move(Vector2.zero);
            stuckWatch.Reset(position);
            return;
        }

        bool arrived = hasTarget && Vector2.Distance(position, target) <= ArriveDistance;
        bool stuck = stuckWatch.Tick(position, Time.deltaTime);

        if (arrived)
        {
            // Llego: descansa un rato y despues elige otro punto.
            hasTarget = false;
            pauseUntil = Time.time + Random.Range(0.6f, 1.6f);
            movement.Move(Vector2.zero);
            return;
        }

        if (!hasTarget || Time.time >= retargetAt || stuck)
        {
            hasTarget = snake.TryPickPoint(playerPosition, PoisonousSnake.PickMode.Roam, out target);
            retargetAt = Time.time + Random.Range(3f, 5f);

            if (!hasTarget)
            {
                pauseUntil = Time.time + 0.5f;
                movement.Move(Vector2.zero);
                return;
            }
        }

        movement.MoveTowards(target, snake.RoamSpeed);
    }

    public void Exit()
    {
        movement.Move(Vector2.zero);
    }
}
