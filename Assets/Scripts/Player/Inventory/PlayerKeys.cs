using System;
using UnityEngine;

public class PlayerKeys : MonoBehaviour
{
    private const int MaxKeys = 99;

    [SerializeField] private int currentKeys = 0;
    public int CurrentKeys => currentKeys;

    public event Action<int> OnKeysChanged;

    private void Start()
    {
        OnKeysChanged?.Invoke(currentKeys);
    }

    public void AddKeys(int amount, string source = "other")
    {
        if (amount <= 0) return;

        currentKeys = Mathf.Min(currentKeys + amount, MaxKeys);
        OnKeysChanged?.Invoke(currentKeys);
        GameMetrics.CurrencyEarned("keys", amount, currentKeys, source);
    }

    public bool UseKey(string sink = "lockpick")
    {
        if (currentKeys <= 0) return false;

        currentKeys--;
        OnKeysChanged?.Invoke(currentKeys);
        GameMetrics.CurrencySpent("keys", 1, currentKeys, sink);
        return true;
    }
}
