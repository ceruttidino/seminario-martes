using System.Collections;
using UnityEngine;

public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Base")]
    [SerializeField] protected float pauseBetweenAttacks = 2f;

    protected bool isDead = false;
    
    protected virtual void Start()
    {
        StartCoroutine(BossRoutine());
    }

    protected abstract IEnumerator BossRoutine();

    protected IEnumerator PauseBetweenAttacks()
    {
        yield return new WaitForSeconds(pauseBetweenAttacks);
    }

    protected virtual void StopBoss()
    {
        isDead = true;
        StopAllCoroutines();
    }
}
