using UnityEngine;

// NUEVO (archivo completo)
// Un "grupo" de enemigos dentro de una room: qué enemigo, cuántos y dónde aparecen.
// RoomInstance tiene una lista de estos, así una room puede mezclar varios tipos.
[System.Serializable]
public class EnemySpawnGroup
{
    [HideInInspector] public string name; // Unity lo usa como título del elemento en la lista (se completa solo)

    [Tooltip("Prefab del enemigo de este grupo")]
    public GameObject enemyPrefab;

    [Tooltip("Cantidad mínima. Si querés una cantidad fija, poné el mismo número en mínimo y máximo.")]
    [Min(0)] public int minCount = 1;

    [Tooltip("Cantidad máxima. En cada partida sale un número al azar entre mínimo y máximo.")]
    [Min(0)] public int maxCount = 1;

    [Tooltip("Dónde puede aparecer este grupo. Si hay más enemigos que puntos, se reparten alrededor de los puntos.")]
    public Transform[] spawnPoints;

    [Tooltip("Mezcla los puntos: si hay más puntos que enemigos, cada partida usa puntos distintos.")]
    public bool randomizePoints = true;

    public int RollCount()
    {
        int max = Mathf.Max(minCount, maxCount);
        return Random.Range(minCount, max + 1);
    }

    /// <summary>Mantiene los valores coherentes y arma el título que se ve en el Inspector.</summary>
    public void Validate()
    {
        if (maxCount < minCount) maxCount = minCount;

        string enemyName = enemyPrefab != null ? enemyPrefab.name : "(sin enemigo)";
        string count = minCount == maxCount ? $"x{minCount}" : $"x{minCount}-{maxCount}";
        int points = 0;
        if (spawnPoints != null)
            foreach (Transform p in spawnPoints)
                if (p != null) points++;

        name = $"{enemyName} {count} ({points} puntos)";
    }
}