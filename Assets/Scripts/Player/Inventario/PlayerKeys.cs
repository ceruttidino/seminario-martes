using UnityEngine;

public class PlayerKeys : MonoBehaviour
{
    [SerializeField] private int currentKeys = 0;

    public void AddKeys(int amount)
    {
        currentKeys += amount;
        Debug.Log($"Llaves: {currentKeys}");
    }

    public bool UseKey()
    {
        if (currentKeys > 0)
        {
            currentKeys--;
            Debug.Log($"Llaves restantes: {currentKeys}");
            return true;
        }

        Debug.Log("No hay llaves");
        return false;
    }

    public int GetKeys() => currentKeys;
}