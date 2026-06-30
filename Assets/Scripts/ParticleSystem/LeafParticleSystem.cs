using System.Collections.Generic;
using UnityEngine;

public class LeafParticleSystem : MonoBehaviour
{
    [Header("Configuración de Hojas")]
    [SerializeField] private GameObject leafPrefab;
    [SerializeField] private int maxLeaves = 25;
    [SerializeField] private float spawnRate = 0.6f;
    [SerializeField] private float minSpeed = 0.8f;
    [SerializeField] private float maxSpeed = 2.2f;
    [SerializeField] private float minRotationSpeed = -30f;
    [SerializeField] private float maxRotationSpeed = 30f;

    [Header("Área de Spawn")]
    [SerializeField] private float spawnWidth = 25f;
    [SerializeField] private float spawnHeight = 15f;
    [SerializeField] private float minLifetime = 8f;
    [SerializeField] private float maxLifetime = 14f;

    private float spawnTimer = 0f;
    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    private void Start()
    {
        if (leafPrefab == null) return;

        for (int i = 0; i < maxLeaves; i++)
        {
            GameObject leaf = Instantiate(leafPrefab, transform);
            leaf.SetActive(false);
            pool.Enqueue(leaf);
        }
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnRate && pool.Count > 0)
        {
            SpawnLeaf();
            spawnTimer = 0f;
        }
    }

    private void SpawnLeaf()
    {
        if (pool.Count == 0) return;

        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, 0f);

        GameObject leaf = pool.Dequeue();
        leaf.transform.position = spawnPosition;
        leaf.transform.rotation = Quaternion.identity;
        leaf.SetActive(true);

        LeafMovement leafMovement = leaf.GetComponent<LeafMovement>();
        if (leafMovement == null)
            leafMovement = leaf.AddComponent<LeafMovement>();

        float fallSpeed = Random.Range(minSpeed, maxSpeed);
        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        float lifetime = Random.Range(minLifetime, maxLifetime);

        leafMovement.Initialize(fallSpeed, rotationSpeed, lifetime, () => ReturnToPool(leaf));
    }

    private void ReturnToPool(GameObject leaf)
    {
        leaf.SetActive(false);
        pool.Enqueue(leaf);
    }

    public void SetActive(bool active)
    {
        enabled = active;
    }
}
