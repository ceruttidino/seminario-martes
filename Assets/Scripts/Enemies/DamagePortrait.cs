using UnityEngine;

/// <summary>
/// Opcional, en el ROOT de un enemigo/jefe. Define qué se muestra en el Game Over cuando este enemigo te mata.
/// - portrait asignado: se usa siempre ese sprite (le gana a los overrides por EnemyType del GameOverManager).
/// - si no: se usa el frame actual de spriteSource (útil cuando el root tiene varios SpriteRenderer, ej: Shadow + Visual).
/// Los jefes lo agregan solos en Awake con DamagePortrait.Ensure(...).
/// </summary>
[DisallowMultipleComponent]
public class DamagePortrait : MonoBehaviour
{
    [Tooltip("Sprite fijo para el Game Over. Si está vacío se usa el frame actual de Sprite Source.")]
    [SerializeField] private Sprite portrait;
    [Tooltip("SpriteRenderer del que se toma el frame actual si no hay Portrait fijo.")]
    [SerializeField] private SpriteRenderer spriteSource;

    public Sprite CurrentSprite(out bool isExplicit)
    {
        isExplicit = portrait != null;
        if (portrait != null) return portrait;
        return spriteSource != null ? spriteSource.sprite : null;
    }

    /// <summary>Lo agrega si falta. No pisa lo que hayas configurado a mano en el Inspector.</summary>
    public static DamagePortrait Ensure(GameObject root, SpriteRenderer source)
    {
        if (root == null) return null;

        DamagePortrait dp = root.GetComponent<DamagePortrait>();
        if (dp == null)
            dp = root.AddComponent<DamagePortrait>();

        if (dp.spriteSource == null)
            dp.spriteSource = source;

        return dp;
    }
}