using System;
using UnityEngine;

public class PlayerKeys : MonoBehaviour
{
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

        currentKeys += amount;
        OnKeysChanged?.Invoke(currentKeys);

        Debug.Log($"Llaves: {currentKeys}");
    }

    public bool UseKey()
    {
        if (currentKeys > 0)
        {
            currentKeys--;
            OnKeysChanged?.Invoke(currentKeys);

            Debug.Log($"Llaves restantes: {currentKeys}");
            return true;
        }

        Debug.Log("No hay llaves");
        return false;
    }

}