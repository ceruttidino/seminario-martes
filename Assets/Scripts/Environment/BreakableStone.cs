using UnityEngine;

// Solo rompible por explosiones (ej. Hedgehog), no por ataques del jugador.
public class BreakableStone : MonoBehaviour
{
    public void BreakFromExplosion()
    {
        Destroy(gameObject);
    }
}
