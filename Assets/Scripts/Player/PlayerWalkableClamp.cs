using UnityEngine;

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

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector2 pos = rb.position;
        RoomPathGrid grid = CurrentGrid();
        if (grid == null)
        {
            lastValid = pos;
            hasValid = true;
            return;
        }

        if (grid.IsWalkableWorld(pos))
        {
            lastValid = pos;
            hasValid = true;
            return;
        }

        if (!hasValid)
            return;

        rb.position = lastValid;
        rb.linearVelocity = Vector2.zero;
        if (dash != null && dash.IsDashing)
            Physics2D.SyncTransforms();
    }

    private RoomPathGrid CurrentGrid()
    {
        if (DungeonManager.Instance != null && DungeonManager.Instance.CurrentRoom != null)
            return RoomPathGrid.For(DungeonManager.Instance.CurrentRoom.transform);

        return RoomPathGrid.For(transform);
    }
}
