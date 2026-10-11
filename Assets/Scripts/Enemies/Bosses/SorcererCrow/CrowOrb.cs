using UnityEngine;

/// <summary>
/// Orbe espectral del Cuervo. Viaja hacia donde estaba el jugador al dispararse y le hace daño al tocarlo.
/// La ULTIMA orbe de la tanda es "desviable": si el jugador usa el ATAQUE PESADO (E) cuando la orbe esta
/// encima, vuelve disparada hacia el cuervo y lo daña + lo aturde.
/// </summary>
public class CrowOrb : MonoBehaviour
{
    private SorcererCrowBoss owner;
    private SpriteRenderer sr;
    private Vector2 velocity;
    private int damage;
    private bool deflectable;
    private bool reflected;
    private float deflectRadius;
    private float reflectSpeed;
    private float lifeLeft = 7f;
    private float pulseOffset;
    private float baseScale = 1f;

    private static int wallLayer = -2;

    public bool IsReflected => reflected;

    public static CrowOrb Spawn(SorcererCrowBoss owner, Vector2 position, Vector2 direction, float speed, int damage,
        bool deflectable, float deflectRadius, float reflectSpeed, float scale, Transform parent)
    {
        GameObject go = new GameObject(deflectable ? "CrowOrb_Deflectable" : "CrowOrb");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = deflectable ? CrowArt.OrbDeflectable() : CrowArt.Orb();
        renderer.sortingOrder = 12;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        CrowOrb orb = go.AddComponent<CrowOrb>();
        orb.owner = owner;
        orb.sr = renderer;
        orb.velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector2.down * speed;
        orb.damage = damage;
        orb.deflectable = deflectable;
        orb.deflectRadius = deflectRadius;
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
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (reflected && owner != null)
        {
            // La orbe devuelta persigue al cuervo para que siempre llegue.
            Vector2 toCrow = owner.HitPoint - (Vector2)transform.position;
            if (toCrow.sqrMagnitude > 0.01f)
                velocity = toCrow.normalized * reflectSpeed;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);

        // Latido: la orbe desviable late mas fuerte para que se note.
        float pulse = 1f + Mathf.Sin((Time.time + pulseOffset) * (deflectable ? 14f : 8f)) * (deflectable ? 0.12f : 0.06f);
        transform.localScale = Vector3.one * (baseScale * pulse);
    }

    private void OnHeavyAttack(Vector2 center, float radius)
    {
        if (!deflectable || reflected || owner == null)
            return;

        float reach = Mathf.Min(radius, deflectRadius);
        if (Vector2.Distance(center, transform.position) > reach)
            return;

        reflected = true;
        lifeLeft = 6f;
        if (sr != null)
            sr.color = new Color(1f, 1f, 0.75f, 1f);

        owner.OnOrbDeflected();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reflected)
        {
            if (owner != null && other.GetComponentInParent<SorcererCrowBoss>() == owner)
            {
                owner.OnOrbReflectedHit();
                Destroy(gameObject);
            }

            return;
        }

        if (other.CompareTag("Player"))
        {
            GameObject playerObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            BossPlayerUtils.DamagePlayer(playerObject, damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && other.gameObject.layer == WallLayer())
            Destroy(gameObject);
    }

    private static int WallLayer()
    {
        if (wallLayer == -2)
            wallLayer = LayerMask.NameToLayer("Wall");

        return wallLayer;
    }
}
