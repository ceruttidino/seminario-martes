using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToadTongue : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Boca del sapo. La lengua sale de acá.")]
    [SerializeField] private Transform origin;
    [SerializeField] private Transform tip;

    [Header("Velocidades")]
    [SerializeField] private float extendSpeed = 26f;
    [SerializeField] private float retractSpeed = 20f;
    [SerializeField] private float pullSpeed = 13f;
    [Tooltip("Tiempo estirada si no agarró al jugador: ventana para pegarle.")]
    [SerializeField] private float lingerTime = 0.8f;

    [Header("Forma")]
    [Tooltip("Cuánto se pasa de la posición del jugador. Garantiza que siempre llegue.")]
    [SerializeField] private float overshoot = 1f;
    [SerializeField] private float hitboxThickness = 0.3f;
    [SerializeField] private float grabRadius = 0.6f;
    [Tooltip("Distancia a la boca donde suelta al jugador. Mayor al radio del cuerpo del sapo.")]
    [SerializeField] private float releaseDistance = 1.3f;

    [Header("Daño")]
    [Tooltip("Daño al terminar de arrastrarlo. 0 = solo lo arrastra.")]
    [SerializeField] private int pullDamage = 1;

    private LineRenderer line;
    private BoxCollider2D hitbox;
    private Vector2 currentTip;
    private GameObject heldPlayer;
    private Rigidbody2D heldRb;

    public bool IsOut { get; private set; }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        hitbox.isTrigger = true;
        if (origin == null) origin = transform.parent;
        Hide();
    }

    private void OnDisable() => ReleasePlayer();

    public void Hide()
    {
        IsOut = false;
        if (line != null) line.enabled = false;
        if (hitbox != null) hitbox.enabled = false;
        if (tip != null) tip.gameObject.SetActive(false);
    }

    public void ForceRelease()
    {
        ReleasePlayer();
        Hide();
    }

    // Corre dentro de la corrutina del boss, así el StopAllCoroutines del boss también la corta.
    public IEnumerator Lash(Transform target)
    {
        if (target == null || origin == null) yield break;

        Vector2 start = origin.position;
        Vector2 toTarget = (Vector2)target.position - start;
        Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.down;
        float maxLength = toTarget.magnitude + overshoot;

        Show();
        float length = 0f;
        bool grabbed = false;
        SetShape(dir, 0f);

        // Extender
        while (length < maxLength)
        {
            Vector2 previousTip = currentTip;
            length = Mathf.Min(maxLength, length + extendSpeed * Time.deltaTime);
            SetShape(dir, length);

            // chequea el tramo recorrido este frame, así con fps bajos no lo atraviesa
            if (target != null && DistancePointToSegment(target.position, previousTip, currentTip) <= grabRadius)
            {
                grabbed = true;
                break;
            }
            yield return null;
        }

        if (grabbed && target != null)
        {
            GrabPlayer(target.gameObject);
            while (length > releaseDistance)
            {
                length = Mathf.Max(releaseDistance, length - pullSpeed * Time.deltaTime);
                SetShape(dir, length);
                MoveHeldPlayer(currentTip);
                yield return null;
            }

            GameObject pulled = heldPlayer;
            ReleasePlayer();
            BossPlayerUtils.DamagePlayer(pulled, pullDamage);
        }
        else
        {
            float t = 0f;
            while (t < lingerTime)
            {
                SetShape(dir, length);
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Retraer
        while (length > 0f)
        {
            length = Mathf.Max(0f, length - retractSpeed * Time.deltaTime);
            SetShape(dir, length);
            yield return null;
        }
        Hide();
    }

    private void Show()
    {
        IsOut = true;
        line.enabled = true;
        hitbox.enabled = true;
        if (tip != null) tip.gameObject.SetActive(true);
    }

    private void SetShape(Vector2 dir, float length)
    {
        Vector2 o = origin.position;
        currentTip = o + dir * length;

        line.SetPosition(0, o);
        line.SetPosition(1, currentTip);

        transform.position = o;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        float sx = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        hitbox.size = new Vector2(Mathf.Max(0.01f, length) / sx, hitboxThickness / sy);
        hitbox.offset = new Vector2(length * 0.5f / sx, 0f);

        if (tip != null)
        {
            tip.position = currentTip;
            tip.rotation = transform.rotation;
        }
    }

    private void GrabPlayer(GameObject playerObject)
    {
        heldPlayer = playerObject;
        heldRb = playerObject.GetComponent<Rigidbody2D>();
        BossPlayerUtils.SetPlayerControl(playerObject, false);
    }

    private void MoveHeldPlayer(Vector2 position)
    {
        if (heldPlayer == null) return;
        heldPlayer.transform.position = position;
        if (heldRb != null)
        {
            heldRb.linearVelocity = Vector2.zero;
            heldRb.position = position;
        }
    }

    private void ReleasePlayer()
    {
        if (heldPlayer != null) BossPlayerUtils.SetPlayerControl(heldPlayer, true);
        heldPlayer = null;
        heldRb = null;
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.00001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
        return Vector2.Distance(p, a + ab * t);
    }
}
