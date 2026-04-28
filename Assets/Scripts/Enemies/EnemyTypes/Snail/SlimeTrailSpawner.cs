using UnityEngine;

public class SlimeTrailSpawner : MonoBehaviour
{
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private float spawnInterval = 0.25f;

    private float timer;

    private void Update()
    {
        timer -= Time.deltaTime;

        if(timer <= 0)
        {
            GameObject slime = Instantiate(slimePrefab, transform.position, Quaternion.identity);
            slime.transform.SetParent(transform.parent);
            timer = spawnInterval;
        }
    }
}
