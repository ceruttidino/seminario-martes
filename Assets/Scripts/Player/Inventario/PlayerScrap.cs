using UnityEngine;

public class PlayerScrap : MonoBehaviour
{
    [SerializeField] private int currentScrap = 0;

    public void AddScrap(int amount)
    {
        currentScrap += amount;
        Debug.Log($"Scrap actual: {currentScrap}");
    }

    public int GetScrap() => currentScrap;
}