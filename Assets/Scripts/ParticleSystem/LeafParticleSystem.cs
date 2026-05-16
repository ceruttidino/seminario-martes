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
    private int activeLeaves = 0;

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnRate && activeLeaves < maxLeaves)
        {
            SpawnLeaf();
            spawnTimer = 0f;
        }
    }

    private void SpawnLeaf()
    {
        if (leafPrefab == null) return;

        float randomX = Random.Range(-spawnWidth / 2, spawnWidth / 2);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, 0);

        GameObject leaf = Instantiate(leafPrefab, spawnPosition, Quaternion.identity);

        LeafMovement leafMovement = leaf.GetComponent<LeafMovement>();
        if (leafMovement == null)
            leafMovement = leaf.AddComponent<LeafMovement>();

        float fallSpeed = Random.Range(minSpeed, maxSpeed);
        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        float lifetime = Random.Range(minLifetime, maxLifetime);

        leafMovement.Initialize(fallSpeed, rotationSpeed, lifetime);

        activeLeaves++;
        Destroy(leaf, lifetime + 2f);

        // decrements the counter when the leaf is destroyed
        StartCoroutine(DecrementOnDestroy(lifetime + 2f));
    }

    private System.Collections.IEnumerator DecrementOnDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        activeLeaves--;
    }

    public void SetActive(bool active)
    {
        enabled = active;
    }
}
