using UnityEngine;

// CAMBIADO (archivo completo): antes instanciaba un prefab de baba (SlimeTile) cada X segundos.
// Ahora alimenta una línea de baba continua (SlimeTrail) que sigue al caracol.
// Toda la configuración (grosor, color, duración, daño) se edita acá, en el Inspector del caracol.
public class SlimeTrailSpawner : MonoBehaviour
{
    [Tooltip("De dónde sale la baba respecto al pivot del caracol. Si el pivot está en el centro, bajalo un poco para que salga del 'piso' (ej: 0, -0.15).")]
    [SerializeField] private Vector2 trailOffset = Vector2.zero; // NUEVO

    [SerializeField] private SlimeTrailSettings settings = new SlimeTrailSettings(); // NUEVO

    private SlimeTrail trail; // NUEVO

    private Vector2 TrailOrigin => (Vector2)transform.position + trailOffset; // NUEVO

    // NUEVO
    private void Start()
    {
        EnsureTrail();
    }

    // CAMBIADO: antes había un timer que instanciaba sprites. Ahora se le pasa la posición a la línea cada frame.
    private void Update()
    {
        EnsureTrail();
        trail.Feed(TrailOrigin);
    }

    // NUEVO: la baba vive aparte (como hija de la room, igual que los SlimeTile viejos),
    // así no se mueve con el caracol y sigue ahí si el caracol muere.
    private void EnsureTrail()
    {
        if (trail != null) return;
        trail = SlimeTrail.Create(settings, transform.parent, gameObject);
    }

    // NUEVO: si el caracol muere, la baba que dejó se termina de secar sola y después se borra
    private void OnDestroy()
    {
        if (trail != null)
            trail.Release();
    }
}