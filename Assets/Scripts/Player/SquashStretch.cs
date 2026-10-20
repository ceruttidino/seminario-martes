using UnityEngine;

// NUEVO (archivo completo)
// Squash & stretch con resorte. Ponerlo en el objeto VISUAL (el que tiene el SpriteRenderer),
// idealmente un hijo "Visual", para no escalar los colliders.
// Respeta el flip por escala negativa (localScale.x = -1).
public class SquashStretch : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Más alto = vuelve más rápido a la escala normal")]
    [SerializeField] private float returnSpeed = 18f;

    private Vector3 baseScale;
    private Vector2 currentFactor = Vector2.one;
    private bool active;

    private void Awake()
    {
        if (target == null) target = transform;

        baseScale = new Vector3(
            Mathf.Abs(target.localScale.x),
            Mathf.Abs(target.localScale.y),
            target.localScale.z);
    }

    /// <summary>Ej: (1.3, 0.75) = aplastado y ancho. (0.8, 1.25) = estirado.</summary>
    public void Punch(Vector2 factor)
    {
        currentFactor = factor;
        active = true;
    }

    private void LateUpdate()
    {
        if (!active) return;

        // Tiempo escalado: durante el hit stop la deformación queda congelada (se lee mejor el impacto)
        currentFactor = Vector2.Lerp(currentFactor, Vector2.one, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));

        bool settled = (currentFactor - Vector2.one).sqrMagnitude < 0.00001f;
        if (settled)
        {
            currentFactor = Vector2.one;
            active = false;
        }

        float signX = Mathf.Sign(target.localScale.x);
        float signY = Mathf.Sign(target.localScale.y);

        target.localScale = new Vector3(
            baseScale.x * currentFactor.x * signX,
            baseScale.y * currentFactor.y * signY,
            baseScale.z);
    }
}