using System.Collections;
using UnityEngine;

public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Base")]
    [SerializeField] protected float pauseBetweenAttacks = 2f;

    protected bool isDead = false;
    
    protected virtual void Awake()
    {
        SetSummonVisible(false);
    }

    protected virtual void Start()
    {
        StartCoroutine(BeginAfterSummon());
    }

    private IEnumerator BeginAfterSummon()
    {
        EnemySummon.PlaySmoke(transform.position);
        yield return new WaitForSeconds(EnemySummon.Delay);
        SetSummonVisible(true);
        StartCoroutine(BossRoutine());
    }

    private void SetSummonVisible(bool visible)
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = visible;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = visible;
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
