using UnityEngine;

/// <summary>
/// Orbe espectral del Cuervo. Viaja hacia donde estaba el jugador al dispararse y le hace daño al tocarlo.
/// La ULTIMA orbe de la tanda es "desviable": si el jugador esta usando el ATAQUE PESADO (E) en el momento en que la
/// orbe choca con el (GDD: "en el frame exacto en que la pelota y el player colisionan") vuelve disparada hacia el
/// cuervo y lo daña + lo aturde.
///
/// Todas las colisiones se resuelven a mano en cada frame (en pasos cortos, sin depender de los callbacks de trigger ni
/// de la capa del collider): jugador, paredes/props solidos, cuervo (orbe devuelta) y limites de la arena. Asi la orbe
/// no "atraviesa" nada ni se pierde un golpe entre frames.
/// </summary>
public class CrowOrb : MonoBehaviour
{
    // Radio del collider de la orbe (local, antes de la escala). Es el mismo que se usa para las colisiones manuales.
    private const float ColliderRadius = 0.4f;

    // Paso maximo por sub-iteracion: la orbe nunca avanza mas que esto sin revisar colisiones.
    private const float MaxStep = 0.2f;

    // Al apretar el ataque pesado, si la orbe esta a esta distancia extra del contacto cuenta como colision.
    private const float PressTolerance = 0.3f;

    // La orbe recien disparada ignora paredes un instante (nace pegada a la boca del cuervo).
    private const float WallGrace = 0.15f;

    private SorcererCrowBoss owner;
    private SpriteRenderer sr;
    private Vector2 velocity;
    private int damage;
    private bool deflectable;
    private bool reflected;
    private bool resolved;
    private float reflectSpeed;
    private float lifeLeft = 7f;
    private float age;
    private float pulseOffset;
    private float baseScale = 1f;

    private Collider2D playerCollider;
    private AreaAttack playerHeavyAttack;
    private ContactFilter2D solidFilter;
    private bool solidFilterReady;
    private readonly Collider2D[] solidBuffer = new Collider2D[16];

    public bool IsReflected => reflected;

    public static CrowOrb Spawn(SorcererCrowBoss owner, Vector2 position, Vector2 direction, float speed, int damage,
        bool deflectable, float reflectSpeed, float scale, Transform parent)
    {
        GameObject go = new GameObject(deflectable ? "CrowOrb_Deflectable" : "CrowOrb");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = deflectable ? CrowArt.OrbDeflectable() : CrowArt.Orb();
        renderer.sortingOrder = 12;

        // Rigidbody + collider trigger: solo para ver la forma real de la orbe (Gizmos) y para que otros sistemas
        // de fisica la "vean". La logica de colision NO depende de ellos (ver ResolveCollisions).
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = ColliderRadius;

        CrowOrb orb = go.AddComponent<CrowOrb>();
        orb.owner = owner;
        orb.sr = renderer;
        orb.velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector2.down * speed;
        orb.damage = damage;
        orb.deflectable = deflectable;
        orb.reflectSpeed = reflectSpeed;
        orb.pulseOffset = Random.value * 6f;
        orb.baseScale = scale;
        return orb;
    }

    private void OnEnable()
    {
        AreaAttack.Performed += OnHeavyAttack;
    }

    private void OnDisable()
    {
        AreaAttack.Performed -= OnHeavyAttack;
    }

    private void Update()
    {
        if (resolved)
            return;

        float dt = Time.deltaTime;
        age += dt;
        lifeLeft -= dt;
        if (lifeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (reflected)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            // La orbe devuelta persigue al cuervo para que siempre llegue.
            Vector2 toCrow = owner.HitPoint - (Vector2)transform.position;
            if (toCrow.sqrMagnitude > 0.0001f)
                velocity = toCrow.normalized * reflectSpeed;
        }

        // Movimiento en pasos cortos: la orbe no se saltea al jugador, al cuervo ni a una pared aunque vaya rapido.
        Vector2 frameMove = velocity * dt;
        int steps = Mathf.Max(1, Mathf.CeilToInt(frameMove.magnitude / MaxStep));
        Vector3 stepMove = frameMove / steps;

        for (int i = 0; i < steps && !resolved; i++)
        {
            transform.position += stepMove;
            ResolveCollisions();
        }

        if (resolved)
            return;

        // Latido: la orbe desviable late mas fuerte para que se note.
        float pulse = 1f + Mathf.Sin((Time.time + pulseOffset) * (deflectable ? 14f : 8f)) * (deflectable ? 0.12f : 0.06f);
        transform.localScale = Vector3.one * (baseScale * pulse);
    }

    // ================================================================= colisiones

    private float WorldRadius()
    {
        return ColliderRadius * Mathf.Max(0.01f, baseScale);
    }

    private void ResolveCollisions()
    {
        Vector2 center = transform.position;
        float radius = WorldRadius();

        if (reflected)
        {
            // Orbe devuelta: al tocar el cuerpo del cuervo le hace daño y lo aturde. Atraviesa todo lo demas.
            if (owner != null && owner.OverlapsBody(center, radius))
            {
                resolved = true;
                owner.OnOrbReflectedHit();
                Destroy(gameObject);
            }

            return;
        }

        // 1) Jugador: si en este momento esta usando el ataque pesado y la orbe es desviable, la devuelve; si no, le pega.
        if (TouchesPlayer(center, radius))
        {
            if (deflectable && HeavyAttackActive())
                Deflect();
            else
                HitPlayer();

            return;
        }

        // 2) Paredes y props solidos: la orbe se rompe contra ellos.
        if (age > WallGrace && HitsSolid(center, radius * 0.85f))
        {
            Pop();
            return;
        }

        // 3) Si por algun motivo salio de la arena, se descarta.
        if (owner != null && owner.IsOutsideArena(center, 2.5f))
            Pop();
    }

    private void EnsurePlayer()
    {
        if (playerCollider != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

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

        if (playerHeavyAttack == null)
            playerHeavyAttack = playerObject.GetComponentInChildren<AreaAttack>();
    }

    private bool TouchesPlayer(Vector2 center, float radius)
    {
        EnsurePlayer();
        if (playerCollider == null || !playerCollider.enabled)
            return false;

        Vector2 closest = playerCollider.ClosestPoint(center);
        return (closest - center).sqrMagnitude <= radius * radius;
    }

    private bool HeavyAttackActive()
    {
        EnsurePlayer();
        return playerHeavyAttack != null && playerHeavyAttack.IsAttacking;
    }

    private bool HitsSolid(Vector2 center, float radius)
    {
        if (!solidFilterReady)
        {
            solidFilter = new ContactFilter2D();
            solidFilter.useTriggers = false;
            solidFilter.useLayerMask = false;
            solidFilterReady = true;
        }

        int count = Physics2D.OverlapCircle(center, radius, solidFilter, solidBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = solidBuffer[i];
            if (other == null || other.isTrigger)
                continue;

            if (IsBlocker(other))
                return true;
        }

        return false;
    }

    /// <summary>Paredes, props y demas geometria fija. El cuervo, el jugador, enemigos y otras orbes NO frenan la orbe.</summary>
    private bool IsBlocker(Collider2D other)
    {
        if (other.GetComponentInParent<SorcererCrowBoss>() != null)
            return false;

        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && body.bodyType != RigidbodyType2D.Static)
            return false;

        return true;
    }

    // ================================================================= resultados

    /// <summary>El jugador apreto el ataque pesado: si la orbe ya esta tocando (o casi) al jugador, se devuelve en ese mismo instante.</summary>
    private void OnHeavyAttack(Vector2 attackCenter, float attackRadius)
    {
        if (resolved || reflected || !deflectable)
            return;

        if (TouchesPlayer(transform.position, WorldRadius() + PressTolerance))
            Deflect();
    }

    private void Deflect()
    {
        if (resolved || reflected)
            return;

        reflected = true;
        lifeLeft = 6f;
        if (sr != null)
            sr.color = new Color(1f, 1f, 0.75f, 1f);

        if (owner != null)
            owner.OnOrbDeflected();
    }

    private void HitPlayer()
    {
        resolved = true;

        EnsurePlayer();
        if (playerCollider != null)
        {
            GameObject playerObject = playerCollider.attachedRigidbody != null
                ? playerCollider.attachedRigidbody.gameObject
                : playerCollider.gameObject;
            BossPlayerUtils.DamagePlayer(playerObject, damage);
        }

        Destroy(gameObject);
    }

    private void Pop()
    {
        resolved = true;
        Destroy(gameObject);
    }
}
