using UnityEngine;

/// <summary>
/// Gota de veneno que escupe la Serpiente Venenosa. Viaja en linea recta hasta chocar:
///   - contra el jugador: lo envenena (si ya estaba envenenado le hace el dano del mordisco),
///   - contra una pared / roca / basura: se deshace en una salpicadura,
///   - si no choca con nada: desaparece al agotar su vida util.
/// Los demas enemigos no la frenan. Se crea con SnakeVenomProjectile.Spawn(...) (no hace falta un prefab).
///
/// Igual que las orbes del Cuervo, las colisiones se resuelven por codigo en pasos cortos (no depende de los eventos de
/// trigger de la fisica): asi nunca atraviesa al jugador ni una pared aunque vaya rapido.
/// </summary>
public sealed class SnakeVenomProjectile : MonoBehaviour
{
    private static readonly Color VenomLight = new Color(0.68f, 0.38f, 0.86f, 1f);
    private static readonly Color VenomDark = new Color(0.36f, 0.16f, 0.52f, 1f);

    private const int SortingOrder = 11;
    public const float HitRadius = 0.2f;
    private const float MaxStep = 0.18f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float fps = 14f;
    private float animTime;

    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifeLeft;
    private bool consumed;
    private PoisonousSnake owner;
    private Transform effectsParent;
    private int obstacleMask;

    private Collider2D playerCollider;
    private ContactFilter2D solidFilter;
    private readonly Collider2D[] solidBuffer = new Collider2D[16];

    public static SnakeVenomProjectile Spawn(Vector2 position, Vector2 direction, Sprite[] frames, float fps,
        float speed, float lifetime, float damage, PoisonousSnake owner, Transform parent)
    {
        GameObject go = new GameObject("SnakeVenom");
        go.transform.position = position;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SnakeVenomProjectile projectile = go.AddComponent<SnakeVenomProjectile>();
        projectile.Setup(direction, frames, fps, speed, lifetime, damage, owner, parent);
        return projectile;
    }

    private void Setup(Vector2 shootDirection, Sprite[] animFrames, float animFps, float shotSpeed, float lifetime,
        float shotDamage, PoisonousSnake snake, Transform parent)
    {
        direction = shootDirection.sqrMagnitude > 0.0001f ? shootDirection.normalized : Vector2.down;
        frames = animFrames;
        fps = Mathf.Max(1f, animFps);
        speed = shotSpeed;
        lifeLeft = Mathf.Max(0.2f, lifetime);
        damage = shotDamage;
        owner = snake;
        effectsParent = parent;
        obstacleMask = LayerMask.GetMask("Default", "Trash", "Wall");

        solidFilter = new ContactFilter2D();
        solidFilter.useTriggers = false;
        solidFilter.useLayerMask = false;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = SortingOrder;
        spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : CrowArt.Orb();
        if (frames == null || frames.Length == 0)
            spriteRenderer.color = VenomLight; // sin frames: se usa la esfera del cuervo teñida de violeta

        // El sprite mira hacia la derecha: se gira hacia donde viaja y, si va hacia la izquierda, se da vuelta en Y
        // para que el brillo del veneno quede siempre arriba.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        spriteRenderer.flipY = direction.x < 0f;

        // Si el dueño muere, la pantalla de "causa de muerte" igual sabe quien disparo.
        if (snake != null)
            DamageOwner.Assign(gameObject, snake.gameObject);
    }

    private void Update()
    {
        if (consumed)
            return;

        float dt = Time.deltaTime;
        lifeLeft -= dt;
        if (lifeLeft <= 0f)
        {
            Splash(false);
            return;
        }

        if (frames != null && frames.Length > 1)
        {
            animTime += dt * fps;
            spriteRenderer.sprite = frames[Mathf.FloorToInt(animTime) % frames.Length];
        }

        // Movimiento en pasos cortos para no saltearse al jugador ni una pared.
        Vector2 frameMove = direction * (speed * dt);
        int steps = Mathf.Max(1, Mathf.CeilToInt(frameMove.magnitude / MaxStep));
        Vector3 stepMove = frameMove / steps;

        for (int i = 0; i < steps && !consumed; i++)
        {
            transform.position += stepMove;
            ResolveCollisions();
        }
    }

    // ------------------------------------------------------------------ colisiones

    private void ResolveCollisions()
    {
        Vector2 center = transform.position;

        PlayerHealth player = TouchedPlayer(center, HitRadius);
        if (player != null)
        {
            HitPlayer(player);
            return;
        }

        if (HitsSolid(center, HitRadius * 0.85f))
        {
            // Se estrello contra una pared / roca antes de llegar: la serpiente tiene que cambiar de lugar.
            if (owner != null)
                owner.NotifyVenomBlocked();

            Splash(false);
        }
    }

    private PlayerHealth TouchedPlayer(Vector2 center, float radius)
    {
        if (playerCollider == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return null;

            Collider2D[] colliders = playerObject.GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && !colliders[i].isTrigger)
                {
                    playerCollider = colliders[i];
                    break;
                }
            }

            if (playerCollider == null && colliders.Length > 0)
                playerCollider = colliders[0];
        }

        if (playerCollider == null || !playerCollider.enabled)
            return null;

        Vector2 closest = playerCollider.ClosestPoint(center);
        if ((closest - center).sqrMagnitude > radius * radius)
            return null;

        return playerCollider.GetComponentInParent<PlayerHealth>();
    }

    private bool HitsSolid(Vector2 center, float radius)
    {
        int count = Physics2D.OverlapCircle(center, radius, solidFilter, solidBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = solidBuffer[i];
            if (other != null && !other.isTrigger && IsBlocker(other))
                return true;
        }

        return false;
    }

    /// <summary>Paredes, rocas, basura y demas geometria fija. Jugador, enemigos y restos de rata NO frenan el veneno.</summary>
    private bool IsBlocker(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            return false;
        if (other.GetComponentInParent<EnemyHealth>() != null || other.GetComponentInParent<RatBody>() != null)
            return false;
        if (other.gameObject.name.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        // Cuerpos que se mueven (jugador / enemigos / otros proyectiles) no cuentan; lo fijo o lo que esta en las capas
        // de obstaculos (basura con Rigidbody, paredes, rocas) si.
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || body.bodyType == RigidbodyType2D.Static)
            return true;

        return (obstacleMask & (1 << other.gameObject.layer)) != 0;
    }

    // ------------------------------------------------------------------ resultados

    private void HitPlayer(PlayerHealth player)
    {
        consumed = true;

        PoisonousSnake.ApplyVenomHit(player.gameObject, gameObject, damage);

        if (owner != null)
            owner.NotifyVenomHit();

        CameraShake.Play(0.12f, 0.07f);
        PlaySplash(true);
        Destroy(gameObject);
    }

    private void Splash(bool onPlayer)
    {
        if (consumed)
            return;

        consumed = true;
        PlaySplash(onPlayer);
        Destroy(gameObject);
    }

    private void PlaySplash(bool onPlayer)
    {
        Vector2 point = transform.position;

        GooFx.Burst(point, effectsParent, onPlayer ? 9 : 6, 1.2f, 3.2f, 0.4f, VenomLight, VenomDark, 0.07f, 0.15f, SortingOrder + 1);
        GooFx.Ring(point, effectsParent, new Color(VenomLight.r, VenomLight.g, VenomLight.b, 0.9f),
            0.2f, onPlayer ? 1.1f : 0.8f, 0.3f, 0.7f, SortingOrder - 1);
    }
}
