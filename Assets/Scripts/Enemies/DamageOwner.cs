using UnityEngine;

/// <summary>
/// Se pega a un hazard spawneado (proyectil, tile, charco) para que el daño se le atribuya a quien lo creó.
/// La foto del dueño se toma AL SPAWNEAR: si el dueño muere y el charco sigue ahí, igual sabemos quién fue.
/// Uso: DamageOwner.Assign(hazardGameObject, gameObject);
/// Encadena solo: si el dueño es a su vez un hazard con DamageOwner (glob -> charco), hereda al dueño original.
/// </summary>
[DisallowMultipleComponent]
public class DamageOwner : MonoBehaviour
{
    public DamageSourceInfo Info { get; private set; }

    public static void Assign(GameObject hazard, GameObject owner)
    {
        if (hazard == null || owner == null) return;

        DamageOwner tag = hazard.GetComponent<DamageOwner>();
        if (tag == null)
            tag = hazard.AddComponent<DamageOwner>();

        tag.Info = DamageSourceInfo.From(owner);
    }
}