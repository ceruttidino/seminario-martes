using UnityEngine;

/// <summary>
/// Evita que el player quede sobre el vacio / fuera del piso (por ejemplo si un enemigo lo empuja).
/// Solo corrige el VACIO. Los obstaculos (bolsas, props, paredes) los resuelve la fisica normal:
/// si tambien se tomaban como "no caminables", el mapache podia quedar trabado pegado a una bolsa recien
/// abierta (la grilla de pathfinding se reconstruye al romperla y su celda pasaba a estar bloqueada).
/// </summary>
public class PlayerWalkableClamp : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 lastValid;
    private bool hasValid;
    private PlayerDash dash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
        lastValid = rb != null ? rb.position : (Vector2)transform.position;
        hasValid = true;
    }

    /// <summary>Lo llama DungeonManager al mover al player a la entrada de una room.</summary>
    public void ResetTo(Vector2 position)
    {
        lastValid = position;
        hasValid = true;
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector2 pos = rb.position;
        RoomPathGrid grid = CurrentGrid();

        if (grid == null || !grid.IsOverVoid(pos))
        {
            lastValid = pos;
            hasValid = true;
            return;
        }

        // Fail-safe: si la ultima posicion "buena" tambien es vacio (ej. cambio de room), no hay a donde volver:
        // se acepta la posicion actual en vez de dejar al player clavado.
        if (!hasValid || grid.IsOverVoid(lastValid))
        {
            ResetTo(pos);
            return;
        }

        rb.position = lastValid;
        rb.linearVelocity = Vector2.zero;
        if (dash != null && dash.IsDashing)
            Physics2D.SyncTransforms();
    }

    private RoomPathGrid CurrentGrid()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
            return RoomPathGrid.ForQuick(DungeonManager.Instance.CurrentRoom.transform);

        return RoomPathGrid.ForQuick(transform);
    }
}
