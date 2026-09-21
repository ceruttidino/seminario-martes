using UnityEngine;

public class PlayerFeetMarker : MonoBehaviour
{
    public static void EnsureOn(GameObject player)
    {
        if (player == null)
            return;

        if (player.GetComponentInChildren<PlayerFeetMarker>() != null)
            return;

        CapsuleCollider2D body = player.GetComponent<CapsuleCollider2D>();
        var feet = new GameObject("PlayerFeet");
        feet.transform.SetParent(player.transform, false);
        feet.layer = player.layer;
        feet.tag = "Player";

        float y = body != null ? body.offset.y - body.size.y * 0.38f : -0.38f;
        feet.transform.localPosition = new Vector3(0f, y, 0f);

        BoxCollider2D box = feet.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.28f, 0.14f);
        feet.AddComponent<PlayerFeetMarker>();
    }
}
