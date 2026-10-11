using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Portal en el piso + brazo del Cuervo. Un portal se abre donde estaba el jugador (aviso), el brazo sale
/// disparado (si el jugador sigue ahi, le pega) y queda expuesto un tiempo: si el jugador lo golpea, el
/// brazo se retrae y el cuervo recibe el daño. Si nadie lo golpea, se retrae solo.
/// </summary>
public class CrowArm : MonoBehaviour, IDamageable
{
    private const float StrikeDamageWindow = 0.3f;

    private SorcererCrowBoss owner;
    private Transform armVisual;
    private SpriteRenderer armRenderer;
    private Transform portalVisual;
    private Vector3 portalFullScale;
    private Vector3 armFullScale;
    private BoxCollider2D hitbox;
    private Vector2 strikeBoxCenterLocal;
    private Vector2 strikeBoxSize;

    private float tellTime;
    private int contactDamage;
    private float retractAt = float.PositiveInfinity;
    private float strikeDamageUntil;
    private bool exposed;
    private bool struck;
    private bool retracting;
    private bool playerHurt;

    public bool IsFinished { get; private set; }

    public static CrowArm Spawn(SorcererCrowBoss owner, Vector2 floorPosition, Transform parent, float tellTime, int contactDamage)
    {
        GameObject go = new GameObject("CrowArm");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            go.layer = enemyLayer;

        go.transform.position = floorPosition;
        if (parent != null)
            go.transform.SetParent(parent, true);

        CrowArm arm = go.AddComponent<CrowArm>();
        arm.Build(owner, tellTime, contactDamage);
        return arm;
    }

    /// <summary>Hora (Time.time) en la que el brazo debe retraerse si nadie lo golpeo antes.</summary>
    public void RetractAt(float time)
    {
        retractAt = time;
    }

    private void Build(SorcererCrowBoss newOwner, float newTellTime, int newContactDamage)
    {
        owner = newOwner;
        tellTime = Mathf.Max(0.05f, newTellTime);
        contactDamage = newContactDamage;

        // Portal (pegado al piso)
        GameObject portal = new GameObject("Portal");
        portal.transform.SetParent(transform, false);
        SpriteRenderer portalRenderer = portal.AddComponent<SpriteRenderer>();
        portalRenderer.sprite = CrowArt.Portal();
        portalRenderer.sortingOrder = 1;
        portalVisual = portal.transform;
        float portalScale = 2.6f / Mathf.Max(0.01f, portalRenderer.sprite.bounds.size.x);
        portalFullScale = new Vector3(portalScale, portalScale, 1f);
        portalVisual.localScale = Vector3.zero;

        // Brazo (el pivote del sprite esta abajo: crece hacia arriba)
        GameObject armObject = new GameObject("Arm");
        armObject.transform.SetParent(transform, false);
        armRenderer = armObject.AddComponent<SpriteRenderer>();
        armRenderer.sprite = CrowArt.Arm();
        armRenderer.sortingOrder = 11;
        armVisual = armObject.transform;
        float armScale = 2.4f / Mathf.Max(0.01f, armRenderer.sprite.bounds.size.y);
        armFullScale = new Vector3(armScale, armScale, 1f);
        armVisual.localScale = new Vector3(armFullScale.x, 0f, 1f);

        // Zona donde se lo puede golpear / donde pega al emerger
        float armHeight = 2.4f;
        strikeBoxSize = new Vector2(1.1f, armHeight);
        strikeBoxCenterLocal = new Vector2(0f, armHeight * 0.5f);

        hitbox = gameObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = strikeBoxSize;
        hitbox.offset = strikeBoxCenterLocal;
        hitbox.enabled = false;

        StartCoroutine(Lifecycle());
    }

    private IEnumerator Lifecycle()
    {
        // 1) Aviso: se abre el portal
        yield return Animate(tellTime, k => portalVisual.localScale = portalFullScale * k);

        // 2) El brazo sale disparado
        exposed = true;
        hitbox.enabled = true;
        strikeDamageUntil = Time.time + StrikeDamageWindow;
        yield return Animate(0.12f, k => armVisual.localScale = new Vector3(armFullScale.x, armFullScale.y * k, 1f));

        // 3) Expuesto hasta que lo golpeen o se cumpla el tiempo
        while (!struck && Time.time < retractAt)
        {
            TryHurtPlayer();
            yield return null;
        }

        // 4) Se retrae y se cierra el portal
        exposed = false;
        retracting = true;
        hitbox.enabled = false;
        yield return Animate(0.15f, k => armVisual.localScale = new Vector3(armFullScale.x, armFullScale.y * (1f - k), 1f));
        yield return Animate(0.25f, k => portalVisual.localScale = portalFullScale * (1f - k));

        IsFinished = true;
        Destroy(gameObject);
    }

    private static IEnumerator Animate(float duration, Action<float> apply)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            apply(Mathf.Clamp01(t / duration));
            yield return null;
        }

        apply(1f);
    }

    private void TryHurtPlayer()
    {
        if (playerHurt || contactDamage <= 0 || Time.time > strikeDamageUntil)
            return;

        Vector2 center = (Vector2)transform.position + strikeBoxCenterLocal;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, strikeBoxSize, 0f, LayerMask.GetMask("Player"));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            GameObject playerObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            if (BossPlayerUtils.DamagePlayer(playerObject, contactDamage))
            {
                playerHurt = true;
                return;
            }
        }
    }

    /// <summary>Lo llaman los ataques del player (rapido / pesado) al golpear el brazo.</summary>
    public void TakeDamage(float damage)
    {
        if (!exposed || struck || retracting)
            return;

        struck = true;

        if (armRenderer != null)
            armRenderer.color = new Color(1f, 0.55f, 0.55f, 1f);

        if (owner != null)
            owner.OnArmStruck(damage);
    }
}
