using System;
using UnityEngine;

public class PlayerScrap : MonoBehaviour
{
    private const int MaxScrap = 99;

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

        currentScrap = Mathf.Min(currentScrap + amount, MaxScrap);
        OnScrapChanged?.Invoke(currentScrap);
    }

    public bool TrySpendScrap(int amount)
    {
        if (amount <= 0) return false;

        if (currentScrap >= amount)
        {
            currentScrap -= amount;
            OnScrapChanged?.Invoke(currentScrap);
            return true;
        }

        return false;
    }
}
