using UnityEngine;

/// <summary>
/// Reproduce la animacion de muerte de un enemigo (hojas "...Derrota" / "...Muerte"). El enemigo se destruye apenas
/// muere (EnemyHealth.Die), asi que la animacion la reproduce este objeto aparte, que se limpia solo al terminar.
/// No tiene EnemyHealth: no cuenta como enemigo y las puertas de la room no esperan a que termine.
///
/// Lo crea EnemyDeathAnimation (un componente por enemigo). Copia capa y orden de dibujo del sprite original y, si hay,
/// una sombra que se va apagando junto con la animacion.
/// </summary>
public sealed class EnemyDeathFx : MonoBehaviour
{
    /// <summary>Datos de la sombra del enemigo (se copia tal cual estaba en reposo y se apaga durante la animacion).</summary>
    public struct ShadowInfo
    {
        public Sprite sprite;
        public Material material;
        public Color color;
        public Vector3 localPosition;
        public Vector3 localScale;
        public int sortingLayerID;
        public int sortingOrder;
    }

    private Sprite[] frames;
    private float fps = 14f;
    private int startFrame;
    private float lastFrameFade;

    private float age;
    private float animTime;
    private SpriteRenderer sr;

    private SpriteRenderer shadowRenderer;
    private Color shadowColor;

    /// <summary>Segundos que dura la animacion completa (cuadros + desvanecido final).</summary>
    public float Duration => animTime + lastFrameFade;

    /// <summary>Crea la animacion de muerte en la posicion del enemigo.</summary>
    /// <param name="frames">Cuadros de la hoja.</param>
    /// <param name="fps">Cuadros por segundo.</param>
    /// <param name="source">Sprite del enemigo: de ahi salen posicion, rotacion, capa y orden de dibujo.</param>
    /// <param name="material">Material con el que dibujar: el de REPOSO (durante el golpe el enemigo tiene el de flash blanco).</param>
    /// <param name="scale">Escala en el mundo.</param>
    /// <param name="parent">Room que contiene al enemigo (para que se limpie con ella).</param>
    /// <param name="startFrame">Primer cuadro a mostrar.</param>
    /// <param name="lastFrameFade">Segundos que el ultimo cuadro se queda y se desvanece (0 = desaparece al terminar).</param>
    /// <param name="sortingBoost">Se suma al orden de dibujo del enemigo (para quedar por encima de otros efectos).</param>
    /// <param name="flipX">Espejar horizontalmente (mirando para el mismo lado que el enemigo).</param>
    /// <param name="shadow">Sombra del enemigo (opcional).</param>
    public static EnemyDeathFx Spawn(Sprite[] frames, float fps, SpriteRenderer source, Material material, Vector3 scale,
        Transform parent, int startFrame = 0, float lastFrameFade = 0f, int sortingBoost = 0, bool flipX = false,
        ShadowInfo? shadow = null)
    {
        if (frames == null || frames.Length == 0 || source == null)
            return null;

        startFrame = Mathf.Clamp(startFrame, 0, frames.Length - 1);

        GameObject go = new GameObject("EnemyDeath");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = scale;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[startFrame];
        renderer.flipX = flipX;
        renderer.color = Color.white;
        if (material != null)
            renderer.sharedMaterial = material;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + sortingBoost;

        EnemyDeathFx fx = go.AddComponent<EnemyDeathFx>();
        fx.frames = frames;
        fx.fps = Mathf.Max(1f, fps);
        fx.startFrame = startFrame;
        fx.lastFrameFade = Mathf.Max(0f, lastFrameFade);
        fx.animTime = (frames.Length - startFrame) / fx.fps;
        fx.sr = renderer;

        if (shadow.HasValue && shadow.Value.sprite != null)
            fx.AddShadow(shadow.Value);

        return fx;
    }

    private void AddShadow(ShadowInfo info)
    {
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = info.localPosition;
        shadow.transform.localScale = info.localScale;

        shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = info.sprite;
        shadowRenderer.sortingLayerID = info.sortingLayerID;
        shadowRenderer.sortingOrder = info.sortingOrder;
        if (info.material != null)
            shadowRenderer.sharedMaterial = info.material;

        shadowColor = info.color;
        shadowRenderer.color = info.color;
    }

    private void Update()
    {
        if (frames == null || sr == null)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        int index = startFrame + Mathf.FloorToInt(age * fps);

        if (index >= frames.Length)
        {
            float over = age - animTime;
            if (lastFrameFade <= 0f || over >= lastFrameFade)
            {
                Destroy(gameObject);
                return;
            }

            ShowFrame(frames.Length - 1);
            Color color = sr.color;
            color.a = 1f - over / lastFrameFade;
            sr.color = color;
            FadeShadow(1f);
            return;
        }

        ShowFrame(index);
        FadeShadow(animTime > 0f ? Mathf.Clamp01(age / animTime) : 1f);
    }

    private void ShowFrame(int index)
    {
        Sprite frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        if (frame != null)
            sr.sprite = frame;
    }

    private void FadeShadow(float progress)
    {
        if (shadowRenderer == null)
            return;

        Color color = shadowColor;
        color.a = shadowColor.a * (1f - progress);
        shadowRenderer.color = color;
    }
}
