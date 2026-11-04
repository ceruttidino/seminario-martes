using UnityEngine;

/// <summary>
/// Animacion por sprites de la Serpiente Venenosa. Reemplaza al Animator viejo: las hojas nuevas (idle, caminar de
/// lado / de espaldas, ataque de frente / de lado y muerte) se reproducen desde aca, sin controller ni transiciones.
///
/// - Camina / descansa segun EnemyMovement.Velocity. El Rigidbody ya no empuja al enemigo: el paso lo recorta el NavMesh.
/// - Para atacar, la IA llama BeginAttack(direccion) y despues EndAttack().
/// - El flip horizontal lo sigue manejando EnemyMovement (los sprites miran a la izquierda, flipX = mira a la derecha).
/// - Mientras el enemigo esta aturdido por un golpe (HitReaction) la animacion queda congelada, como en el resto.
/// </summary>
[DisallowMultipleComponent]
public class SnakeAnimator : MonoBehaviour
{
    private enum Pose { Idle, WalkSide, WalkBack, AttackFront, AttackSide }

    [Header("Referencias (se buscan solas si estan vacias)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HitReaction hitReaction;
    [SerializeField] private EnemyMovement movement;

    [Header("Frames")]
    [Tooltip("Enroscada, de frente. Tambien se usa cuando avanza hacia abajo.")]
    [SerializeField] private Sprite[] idleFrames;
    [Tooltip("Reptando de costado (mira a la izquierda).")]
    [SerializeField] private Sprite[] walkSideFrames;
    [Tooltip("Reptando de espaldas (se aleja de la camara).")]
    [SerializeField] private Sprite[] walkBackFrames;
    [Tooltip("Ataque mirando a la camara.")]
    [SerializeField] private Sprite[] attackFrontFrames;
    [Tooltip("Ataque de costado (mira a la izquierda). Tambien se usa cuando el objetivo esta arriba.")]
    [SerializeField] private Sprite[] attackSideFrames;
    [Tooltip("Muerte (la reproduce SnakeDeathFx porque la serpiente se destruye al morir).")]
    [SerializeField] private Sprite[] deathFrames;
    [Tooltip("Cuadros del proyectil de veneno (la reproduce SnakeVenomProjectile).")]
    [SerializeField] private Sprite[] projectileFrames;

    [Header("Velocidad de reproduccion (frames por segundo)")]
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float walkSideFps = 10f;
    [SerializeField] private float walkBackFps = 12f;
    [SerializeField] private float attackFps = 12f;
    [SerializeField] private float deathFps = 14f;
    [SerializeField] private float projectileFps = 14f;

    [Header("Ataque")]
    [Tooltip("Frame (desde 0) del ataque de frente en el que sale el veneno.")]
    [SerializeField] private int attackFrontFireFrame = 5;
    [Tooltip("Frame (desde 0) del ataque de costado en el que sale el veneno.")]
    [SerializeField] private int attackSideFireFrame = 3;

    [Header("Movimiento")]
    [Tooltip("Velocidad minima para considerar que esta caminando.")]
    [SerializeField] private float moveThreshold = 0.08f;

    private Pose pose = Pose.Idle;
    private float poseTime;
    private bool attacking;
    private Pose attackPose = Pose.AttackSide;
    private bool facingVertical = true;

    public Sprite[] DeathFrames => deathFrames;
    public float DeathFps => deathFps;
    public Sprite[] ProjectileFrames => projectileFrames;
    public float ProjectileFps => projectileFps;
    public bool IsAttacking => attacking;
    public bool HasAttackFrames => (attackFrontFrames != null && attackFrontFrames.Length > 0)
                                    || (attackSideFrames != null && attackSideFrames.Length > 0);

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (hitReaction == null) hitReaction = GetComponent<HitReaction>();
        if (movement == null) movement = GetComponent<EnemyMovement>();

        ApplyFrame();
    }

    private void Update()
    {
        if (spriteRenderer == null)
            return;

        // Congelada mientras el golpe la tiene aturdida (igual que el resto de los enemigos).
        if (hitReaction != null && hitReaction.IsStunned)
            return;

        Pose wanted = ChoosePose();
        if (wanted != pose)
        {
            pose = wanted;
            poseTime = 0f;
        }

        poseTime += Time.deltaTime;
        ApplyFrame();
    }

    // ------------------------------------------------------------------ ataque

    /// <summary>
    /// Arranca la animacion de ataque hacia 'aimDirection'. Devuelve en cuantos segundos tiene que salir el veneno
    /// (cuando la boca esta abierta) y deja en 'totalDuration' lo que dura la animacion completa.
    /// </summary>
    public float BeginAttack(Vector2 aimDirection, out float totalDuration)
    {
        // De frente solo si el objetivo esta claramente abajo; de costado si esta al costado o arriba (no hay ataque de espaldas).
        bool front = aimDirection.y < 0f && Mathf.Abs(aimDirection.y) > Mathf.Abs(aimDirection.x) * 0.9f;
        attackPose = front ? Pose.AttackFront : Pose.AttackSide;

        attacking = true;
        pose = attackPose;
        poseTime = 0f;

        Sprite[] frames = FramesFor(attackPose);
        float fps = Mathf.Max(1f, attackFps);
        totalDuration = frames != null && frames.Length > 0 ? frames.Length / fps : 0.6f;

        int fireFrame = front ? attackFrontFireFrame : attackSideFireFrame;
        if (frames != null && frames.Length > 0)
            fireFrame = Mathf.Clamp(fireFrame, 0, frames.Length - 1);

        ApplyFrame();
        return Mathf.Min(totalDuration, fireFrame / fps);
    }

    public void EndAttack()
    {
        if (!attacking)
            return;

        attacking = false;
        pose = Pose.Idle;
        poseTime = 0f;
        ApplyFrame();
    }

    /// <summary>true si la serpiente esta ahora de costado o de frente respecto de 'direction' (sirve para ubicar la boca).</summary>
    public static bool IsHorizontal(Vector2 direction)
    {
        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) * 0.8f;
    }

    // ------------------------------------------------------------------ pose

    private Pose ChoosePose()
    {
        if (attacking)
            return attackPose;

        Vector2 velocity = movement != null ? movement.Velocity : (body != null ? body.linearVelocity : Vector2.zero);
        if (velocity.sqrMagnitude < moveThreshold * moveThreshold)
            return Pose.Idle;

        // Los sprites miran a la izquierda: al ir hacia la derecha se espejan. (EnemyMovement solo espeja si tiene un
        // Animator; la serpiente ya no usa uno, asi que lo hace esta clase.)
        if (Mathf.Abs(velocity.x) > moveThreshold && Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y) * 0.5f)
            spriteRenderer.flipX = velocity.x > 0f;

        // Histeresis: solo cambia de eje cuando uno domina claramente (evita parpadeos en las diagonales).
        float ax = Mathf.Abs(velocity.x);
        float ay = Mathf.Abs(velocity.y);
        if (ay > ax * 1.2f)
            facingVertical = true;
        else if (ax > ay * 1.2f)
            facingVertical = false;

        if (!facingVertical)
            return Pose.WalkSide;

        // Hacia arriba: de espaldas. Hacia abajo: enroscada de frente (no hay hoja de caminar de frente).
        return velocity.y > 0f ? Pose.WalkBack : Pose.Idle;
    }

    private Sprite[] FramesFor(Pose p)
    {
        switch (p)
        {
            case Pose.WalkSide: return walkSideFrames;
            case Pose.WalkBack: return walkBackFrames;
            case Pose.AttackFront: return attackFrontFrames;
            case Pose.AttackSide: return attackSideFrames;
            default: return idleFrames;
        }
    }

    private float FpsFor(Pose p)
    {
        switch (p)
        {
            case Pose.WalkSide: return walkSideFps;
            case Pose.WalkBack: return walkBackFps;
            case Pose.AttackFront:
            case Pose.AttackSide: return attackFps;
            default: return idleFps;
        }
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null)
            return;

        Sprite[] frames = FramesFor(pose);
        if (frames == null || frames.Length == 0)
            return;

        int index = Mathf.FloorToInt(poseTime * Mathf.Max(1f, FpsFor(pose)));
        bool isAttackPose = pose == Pose.AttackFront || pose == Pose.AttackSide;

        // El ataque se queda en el ultimo cuadro hasta que la IA lo corta; el resto se repite en bucle.
        index = isAttackPose ? Mathf.Min(index, frames.Length - 1) : index % frames.Length;

        Sprite frame = frames[index];
        if (frame != null && spriteRenderer.sprite != frame)
            spriteRenderer.sprite = frame;
    }
}
