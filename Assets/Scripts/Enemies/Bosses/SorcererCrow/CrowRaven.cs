using UnityEngine;

/// <summary>
/// Cuervito del ataque "chillido". Vuela en linea recta intentando pegarle al jugador:
/// - En estado Normal solo vuela por el eje X (desde los costados de la sala).
/// - En estado Poseido tambien puede volar por el eje Y (desde arriba o desde abajo).
/// - Negro: NO se puede golpear.
/// - Blanco (version chica del jefe): se puede golpear; al recibir el golpe desaparece y daña al cuervo.
///
/// El daño al jugador se resuelve a mano en cada frame (OverlapBox rotado con la forma real del cuervito), sin depender
/// de los callbacks de trigger. El collider (trigger) queda para que los ataques del player detecten a los blancos.
/// </summary>
public class CrowRaven : MonoBehaviour, IDamageable
{
    private static readonly Vector2 BodyBoxSize = new Vector2(0.95f, 0.55f);

    private SorcererCrowBoss owner;
    private bool isWhite;
    private bool hit;
    private bool hurtPlayer;
    private Vector2 direction;
    private float speed;
    private int damage;
    private Vector2 startPosition;
    private float traveled;
    private float travelLimit;
    private float bobOffset;
    private float scale;
    private ContactFilter2D playerFilter;
    private bool filterReady;
    private readonly Collider2D[] playerBuffer = new Collider2D[6];

    public bool IsWhite => isWhite;

    /// <param name="direction">Direccion de vuelo (se normaliza): horizontal = eje X, vertical = eje Y.</param>
    /// <param name="travelLimit">Distancia total que recorre antes de desaparecer.</param>
    public static CrowRaven Spawn(SorcererCrowBoss owner, bool white, Vector2 position, Vector2 direction, float speed, int damage,
        float travelLimit, Transform parent)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.left;
        bool vertical = Mathf.Abs(dir.y) > Mathf.Abs(dir.x);

        GameObject go = new GameObject(white ? "CrowRaven_White" : "CrowRaven_Black");
        go.transform.position = position;
        if (parent != null)
            go.transform.SetParent(parent, true);

        // Solo los blancos se pueden golpear: estan en la capa Enemy (la que escanean los ataques del player).
        if (white)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                go.layer = enemyLayer;
        }

        float scale = white ? 1.3f : 1.2f;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CrowArt.Raven(white);
        renderer.sortingOrder = 12;

        // El sprite mira hacia la derecha: horizontal se espeja, vertical se rota (asi el collider acompaña al cuerpo).
        if (vertical)
        {
            go.transform.rotation = Quaternion.Euler(0f, 0f, dir.y > 0f ? 90f : -90f);
        }
        else
        {
            renderer.flipX = dir.x < 0f;
        }

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = BodyBoxSize;

        CrowRaven raven = go.AddComponent<CrowRaven>();
        raven.owner = owner;
        raven.isWhite = white;
        raven.direction = dir;
        raven.speed = speed;
        raven.damage = damage;
        raven.startPosition = position;
        raven.travelLimit = travelLimit;
        raven.bobOffset = Random.value * 6f;
        raven.scale = scale;
        return raven;
    }

    private void Update()
    {
        if (hit)
            return;

        traveled += speed * Time.deltaTime;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float bob = Mathf.Sin((Time.time + bobOffset) * 9f) * 0.06f;
        transform.position = startPosition + direction * traveled + perpendicular * bob;

        if (traveled > travelLimit)
        {
            Destroy(gameObject);
            return;
        }

        TryHurtPlayer();
    }

    private void TryHurtPlayer()
    {
        if (hurtPlayer)
            return;

        if (!filterReady)
        {
            playerFilter = new ContactFilter2D();
            playerFilter.useLayerMask = true;
            playerFilter.layerMask = LayerMask.GetMask("Player");
            playerFilter.useTriggers = true;
            filterReady = true;
        }

        Vector2 size = new Vector2(BodyBoxSize.x * scale, BodyBoxSize.y * scale);
        float angle = transform.eulerAngles.z;
        int count = Physics2D.OverlapBox(transform.position, size, angle, playerFilter, playerBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = playerBuffer[i];
            if (other == null)
                continue;

            GameObject playerObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (playerObject.GetComponentInParent<PlayerHealth>() == null)
                continue;

            if (BossPlayerUtils.DamagePlayer(playerObject, damage))
                hurtPlayer = true;

            return;
        }
    }

    /// <summary>Lo llaman los ataques del player al golpearlo (solo cuenta si es blanco).</summary>
    public void TakeDamage(float damageAmount)
    {
        if (!isWhite || hit)
            return;

        hit = true;

        if (owner != null)
            owner.OnWhiteRavenStruck(damageAmount);

        Destroy(gameObject);
    }
}
