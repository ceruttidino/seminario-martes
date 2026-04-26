using System;
using UnityEngine;

public class PlayerScrap : MonoBehaviour
{
    [SerializeField] private int currentScrap = 0;

    public int CurrentScrap => currentScrap;

    public event Action<int> OnScrapChanged;

    private void Start()
    {
        OnScrapChanged?.Invoke(currentScrap);
    }


    public void AddScrap(int amount)
    {
        if (amount <= 0) return;

        currentScrap += amount;
        OnScrapChanged?.Invoke(currentScrap);

        Debug.Log($"Scrap actual: {currentScrap}");
    }

    public bool TrySpendScrap(int amount)
    {
        if (amount <= 0) return false;

        if (currentScrap >= amount)
        {
            currentScrap -= amount;
            OnScrapChanged?.Invoke(currentScrap);

            Debug.Log($"Scrap restante: {currentScrap}");
            return true;
        }

        Debug.Log("No hay suficiente scrap");
        return false;
    }
}

