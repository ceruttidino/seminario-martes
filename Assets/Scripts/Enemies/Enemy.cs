using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemyBehaviour behaviour;
    public EnemyHealth health;

    private void Awake()
    {
        behaviour = GetComponent<EnemyBehaviour>();
        health = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        behaviour.Tick();
    }
}
