using UnityEngine;

/// <summary>
/// La serpiente escupe veneno hacia el jugador. Cada ataque: apunta (animacion de ataque) y suelta la gota en el
/// momento en que abre la boca. Despues de cada disparo hace una pausa y, si el jugador sigue a tiro, dispara otra vez;
/// si lo perdio de vista se reubica hasta tener linea de disparo. Sigue asi hasta que un disparo le acierta al jugador
/// (ahi vuelve a acechar) o hasta que el jugador se le acerca demasiado (se aleja).
/// </summary>
public class SnakeAttackState : IEnemyState
{
    private enum Phase { Windup, Recover, Reposition }

    private const float ArriveDistance = 0.4f;
    private const float RangeCheckInterval = 0.15f;
    // Despues de un escupitajo que se estrello: cuanto tiene que moverse (y cuanto tiempo se le da) antes de reintentar.
    private const float MinRepositionTravel = 1.2f;
    private const float MaxMustMoveSeconds = 4f;

    private readonly Transform player;
    private readonly EnemyMovement movement;
    private readonly Transform self;
    private readonly EnemyBehaviour behaviour;
    private readonly PoisonousSnake snake;
    private readonly bool cornered;

    private Phase phase;
    private float phaseTimer;
    private float fireDelay;
    private bool fired;

    private Vector2 target;
    private bool hasTarget;
    private float retargetAt;
    private float nextRangeCheck;
    private bool mustMove;
    private Vector2 repositionOrigin;
    private PoisonousSnake.StuckWatch stuckWatch;

    /// <param name="cornered">true si viene de quedar acorralada: dispara aunque el jugador este muy cerca.</param>
    public SnakeAttackState(Transform player, EnemyMovement movement, Transform self, EnemyBehaviour behaviour,
        PoisonousSnake snake, bool cornered)
    {
        this.player = player;
        this.movement = movement;
        this.self = self;
        this.behaviour = behaviour;
        this.snake = snake;
        this.cornered = cornered;
    }

    public void Enter()
    {
        snake.Engage();
        if (cornered)
            snake.SuppressRetreat(1.8f);

        BeginWindup();
    }

    public void Tick()
    {
        if (player == null)
        {
            movement.Move(Vector2.zero);
            return;
        }

        // Le acerto al jugador, o el jugador ya esta envenenado: deja de escupir.
        if (!snake.Engaged || snake.IsPlayerPoisoned(player))
        {
            if (snake.IsPlayerPoisoned(player))
                behaviour.SetState(new SnakeRetreatState(player, movement, self, behaviour, snake));
            else
                behaviour.SetState(new SnakeRoamState(player, movement, self, behaviour, snake));
            return;
        }

        Vector2 position = self.position;
        Vector2 playerPosition = player.position;
        Vector2 toPlayer = playerPosition - position;
        float distance = toPlayer.magnitude;

        // El jugador se acerco demasiado: cancela el ataque y se aleja.
        if (distance <= snake.TooCloseRange && !snake.RetreatSuppressed)
        {
            behaviour.SetState(new SnakeRetreatState(player, movement, self, behaviour, snake));
            return;
        }

        switch (phase)
        {
            case Phase.Windup:
                TickWindup(toPlayer, playerPosition);
                break;
            case Phase.Recover:
                TickRecover(distance, position, playerPosition);
                break;
            default:
                TickReposition(distance, position, playerPosition);
                break;
        }
    }

    // ------------------------------------------------------------------ fases

    private void BeginWindup()
    {
        phase = Phase.Windup;
        phaseTimer = 0f;
        fired = false;
        movement.Move(Vector2.zero);

        Vector2 toPlayer = player != null ? (Vector2)player.position - (Vector2)self.position : Vector2.down;
        movement.Face(toPlayer);

        SnakeAnimator animator = snake.Animator;
        if (animator != null && animator.HasAttackFrames)
            fireDelay = animator.BeginAttack(toPlayer, out _);
        else
            fireDelay = 0.4f;
    }

    private void TickWindup(Vector2 toPlayer, Vector2 playerPosition)
    {
        movement.Move(Vector2.zero);

        // Sigue al jugador con la mirada mientras apunta (el disparo sale hacia donde esta en el momento de soltarlo).
        movement.Face(toPlayer);

        phaseTimer += Time.deltaTime;
        if (fired || phaseTimer < fireDelay)
            return;

        fired = true;
        Vector2 aim = playerPosition - snake.MuzzlePosition(toPlayer);
        if (aim.sqrMagnitude < 0.0001f)
            aim = toPlayer;

        snake.Fire(aim);

        phase = Phase.Recover;
        phaseTimer = 0f;
    }

    private void TickRecover(float distance, Vector2 position, Vector2 playerPosition)
    {
        movement.Move(Vector2.zero);

        phaseTimer += Time.deltaTime;

        // La animacion de ataque queda en su ultimo cuadro hasta aca; despues vuelve a la pose de descanso.
        if (phaseTimer >= 0.45f && snake.Animator != null && snake.Animator.IsAttacking)
            snake.Animator.EndAttack();

        if (phaseTimer < snake.ShotCooldown)
            return;

        // Si el escupitajo anterior se estrello contra algo (o ya no hay linea de disparo) cambia de lugar en vez de
        // seguir disparando contra lo mismo desde el mismo punto.
        bool blocked = snake.ConsumeBlockedShot();
        if (!blocked && CanFire(distance, position, playerPosition))
            BeginWindup();
        else
            BeginReposition(blocked);
    }

    /// <param name="mustMove">true si el ultimo escupitajo se estrello: no vuelve a disparar hasta moverse un poco.</param>
    private void BeginReposition(bool mustMove)
    {
        phase = Phase.Reposition;
        phaseTimer = 0f;
        hasTarget = false;
        nextRangeCheck = 0f;
        stuckWatch.Reset(self.position);

        this.mustMove = mustMove;
        repositionOrigin = self.position;

        if (snake.Animator != null)
            snake.Animator.EndAttack();
    }

    private void TickReposition(float distance, Vector2 position, Vector2 playerPosition)
    {
        phaseTimer += Time.deltaTime;

        // Si no puede moverse (rincon, pasillo) deja de exigirlo despues de un rato.
        if (mustMove && (phaseTimer >= MaxMustMoveSeconds || Vector2.Distance(position, repositionOrigin) >= MinRepositionTravel))
            mustMove = false;

        // En cuanto vuelve a tener linea de disparo (y ya se movio si hacia falta), ataca.
        if (!mustMove && Time.time >= nextRangeCheck)
        {
            nextRangeCheck = Time.time + RangeCheckInterval;
            if (CanFire(distance, position, playerPosition))
            {
                BeginWindup();
                return;
            }
        }

        bool stuck = stuckWatch.Tick(position, Time.deltaTime);

        bool arrived = hasTarget && Vector2.Distance(position, target) <= ArriveDistance;
        if (!hasTarget || arrived || Time.time >= retargetAt || stuck)
        {
            hasTarget = snake.TryPickPoint(playerPosition, PoisonousSnake.PickMode.Reposition, out target);
            retargetAt = Time.time + Random.Range(1.2f, 2f);

            if (!hasTarget)
            {
                movement.Move(Vector2.zero);
                return;
            }
        }

        movement.MoveTowards(target, snake.RepositionSpeed);
    }

    private bool CanFire(float distance, Vector2 position, Vector2 playerPosition)
    {
        if (distance > snake.FireRange)
            return false;

        Vector2 toPlayer = playerPosition - position;
        return snake.HasLineOfFire(snake.MuzzlePosition(toPlayer), playerPosition);
    }

    public void Exit()
    {
        if (snake.Animator != null)
            snake.Animator.EndAttack();
    }
}
