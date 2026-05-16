using System.Collections.Generic;
using UnityEngine;

public class SlimeTrailSpawner : MonoBehaviour
{
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private float spawnInterval = 0.25f;
    [SerializeField] private int maxSlimeInstances = 15;

    private float timer;
    private readonly List<GameObject> activeSlimes = new List<GameObject>();

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f) return;

        timer = spawnInterval;

        activeSlimes.RemoveAll(s => s == null);

        if (activeSlimes.Count >= maxSlimeInstances) return;

        GameObject slime = Instantiate(slimePrefab, transform.position, Quaternion.identity);
        slime.transform.SetParent(transform.parent);
        activeSlimes.Add(slime);
    }
}
