using UnityEngine;

/// <summary>
/// Cuervito del ataque "chillido". Vuela en linea recta por el eje X intentando pegarle al jugador.
/// - Negro: NO se puede golpear.
/// - Blanco (version chica del jefe): se puede golpear; al recibir el golpe desaparece y daña al cuervo.
/// </summary>
public class CrowRaven : MonoBehaviour, IDamageable
{
    private SorcererCrowBoss owner;
    private bool isWhite;
    private bool hit;
    private float direction;
    private float speed;
    private int damage;
    private float centerX;
    private float travelHalfWidth;
    private float baseY;
    private float bobOffset;

    public bool IsWhite => isWhite;

    public static CrowRaven Spawn(SorcererCrowBoss owner, bool white, Vector2 position, float direction, float speed, int damage,
        float centerX, float travelHalfWidth, Transform parent)
    {
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

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = CrowArt.Raven(white);
        renderer.flipX = direction < 0f;
        renderer.sortingOrder = 12;
        go.transform.localScale = Vector3.one * (white ? 1.3f : 1.2f);

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, 0.55f);

        CrowRaven raven = go.AddComponent<CrowRaven>();
        raven.owner = owner;
        raven.isWhite = white;
        raven.direction = direction >= 0f ? 1f : -1f;
        raven.speed = speed;
        raven.damage = damage;
        raven.centerX = centerX;
        raven.travelHalfWidth = travelHalfWidth;
        raven.baseY = position.y;
        raven.bobOffset = Random.value * 6f;
        return raven;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        pos.x += direction * speed * Time.deltaTime;
        pos.y = baseY + Mathf.Sin((Time.time + bobOffset) * 9f) * 0.06f;
        transform.position = pos;

        if (Mathf.Abs(pos.x - centerX) > travelHalfWidth)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || !other.CompareTag("Player"))
            return;

        GameObject playerObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        BossPlayerUtils.DamagePlayer(playerObject, damage);
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
