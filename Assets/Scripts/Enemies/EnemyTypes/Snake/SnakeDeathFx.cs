using UnityEngine;

/// <summary>
/// Animacion de muerte de la Serpiente Venenosa (hoja "SerpienteMuerte": se desvanece enroscada). La serpiente se
/// destruye apenas muere, asi que la animacion la reproduce este objeto aparte, que se limpia solo al terminar. No
/// cuenta como enemigo: las puertas de la room no esperan a que termine.
/// </summary>
public sealed class SnakeDeathFx : MonoBehaviour
{
    private Sprite[] frames;
    private float fps = 14f;
    private float time;
    private SpriteRenderer sr;

    public static void Spawn(Sprite[] frames, float fps, Vector3 position, Vector3 scale, SpriteRenderer source, Transform parent)
    {
        if (frames == null || frames.Length == 0)
            return;

        GameObject go = new GameObject("SnakeDeath");
        go.transform.position = position;
        go.transform.localScale = scale;
        if (parent != null)
            go.transform.SetParent(parent, true);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];

        // Mismo material y orden de dibujo que la serpiente (y color blanco: puede estar en pleno flash rojo / envenenada).
        if (source != null)
        {
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
        }

        SnakeDeathFx fx = go.AddComponent<SnakeDeathFx>();
        fx.frames = frames;
        fx.fps = Mathf.Max(1f, fps);
        fx.sr = renderer;
    }

    private void Update()
    {
        if (frames == null || sr == null)
        {
            Destroy(gameObject);
            return;
        }

        time += Time.deltaTime * fps;
        int index = Mathf.FloorToInt(time);
        if (index >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        sr.sprite = frames[index];
    }
}
