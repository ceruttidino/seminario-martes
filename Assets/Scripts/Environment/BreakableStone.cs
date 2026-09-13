using UnityEngine;

// Solo las explosiones la rompen, no los ataques del jugador.
public class BreakableStone : MonoBehaviour
{
    public void BreakFromExplosion()
    {
        Destroy(gameObject);
    }
}
