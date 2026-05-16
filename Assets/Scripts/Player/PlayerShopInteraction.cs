using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShopInteraction : MonoBehaviour
{
    private ShopItem currentItem;

    private void Update()
    {
        if (currentItem != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentItem.TryBuy(GetComponent<PlayerScrap>(), gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ShopItem item = collision.GetComponent<ShopItem>();
        if (item != null)
            currentItem = item;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ShopItem>() == currentItem)
            currentItem = null;
    }
}
