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

    public void AddKeys(int amount)
    {
        if (amount <= 0) return;

        currentKeys = Mathf.Min(currentKeys + amount, MaxKeys);
        OnKeysChanged?.Invoke(currentKeys);
    }

    public bool UseKey()
    {
        if (currentKeys <= 0) return false;

        currentKeys--;
        OnKeysChanged?.Invoke(currentKeys);
        return true;
    }
}
