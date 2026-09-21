using UnityEngine;

// Solo las explosiones la rompen, no los ataques del jugador.
public class BreakableStone : MonoBehaviour
{
    public void BreakFromExplosion()
    {
        GetComponentInParent<RoomInstance>()?.InvalidatePathGrid();
        Destroy(gameObject);
    }
}
