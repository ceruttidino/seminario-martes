using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EnemyBehaviour behaviour;
    public EnemyHealth health;

    private void Awake()
    {
        behaviour = GetComponent<EnemyBehaviour>();
        health = GetComponent<EnemyHealth>();

        if (GetComponent<EnemyBodyCollision>() == null)
            gameObject.AddComponent<EnemyBodyCollision>();
        if (GetComponent<EnemySeparation>() == null)
            gameObject.AddComponent<EnemySeparation>();
    }
}
