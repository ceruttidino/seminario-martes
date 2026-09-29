using System.Collections;
using UnityEngine;

public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Base")]
    [SerializeField] protected float pauseBetweenAttacks = 2f;

    protected bool isDead = false;

    protected BossExitDoor bossExitDoor;

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

    public virtual void EnsureExitDoor(DoorDirection wall)
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();

        if (bossExitDoor == null && room != null)
            bossExitDoor = room.GetComponentInChildren<BossExitDoor>(true);

        if (bossExitDoor == null)
        {
            GameObject go = new GameObject("BossExitDoor");
            go.transform.SetParent(room != null ? room.transform : transform.parent, false);
            bossExitDoor = go.AddComponent<BossExitDoor>();
        }

        Vector3 roomCenter = room != null ? room.transform.position : transform.position;
        Vector3 spawnPos = room != null ? room.GetDoorWorldPosition(wall) : roomCenter;

        bossExitDoor.Place(spawnPos, wall, roomCenter);
    }

    protected void OpenRoomAfterDefeat()
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
            room.UnlockDoorsAnimated();

        if (bossExitDoor == null && room != null)
            EnsureExitDoor(room.GetBossExitDirection());

        if (bossExitDoor != null)
            bossExitDoor.OpenAfterBoss();
    }
}
