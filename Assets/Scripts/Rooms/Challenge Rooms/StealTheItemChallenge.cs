using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Challenge "Steal the Item": el jugador tiene que cruzar una room grande llena de
/// trampas y guardias que patrullan, y llegar al item sin ser detectado ni golpeado.
///
/// - Si llega al item: obtiene 5 scrap.
/// - Si lo golpea un guardia o una trampa: pierde un corazón entero, lo expulsan de
///   la room y no puede volver a entrar (el nodo queda sellado).
///
/// Va en el mismo GameObject que el RoomInstance de la room de challenge.
/// </summary>
public class StealTheItemChallenge : ChallengeRoomBase
{
    [Header("Objetivo")]
    [SerializeField] private StealableItem item;
    [SerializeField] private List<StealChallengeGuard> guards = new List<StealChallengeGuard>();
    [SerializeField] private List<StealChallengeTrap> traps = new List<StealChallengeTrap>();

    [Header("Recompensa")]
    [Tooltip("LootItem del scrap (el mismo que usás en las LootTableSO).")]
    [SerializeField] private LootItem scrapLoot;
    [Tooltip("Prefab con LootPickup que se usa para dropear el scrap.")]
    [SerializeField] private GameObject scrapPickupPrefab;
    [SerializeField] private int scrapReward = 5;
    [SerializeField] private float rewardSpreadRadius = 0.85f;

    [Header("Castigo")]
    [Tooltip("Daño que recibe el jugador al ser golpeado. Poné el valor que equivalga a un corazón entero en tu PlayerHealth.")]
    [SerializeField] private float fullHeartDamage = 2f;
    [Tooltip("Segundos entre el golpe y la expulsión (para que se vea el hit).")]
    [SerializeField] private float expelDelay = 0.6f;

    private bool challengeStarted;
    private bool challengeFinished;
    private bool resolving;

    public override string ChallengeId => ChallengeRunState.StealTheItem;

    // El jugador puede irse cuando quiera: si sale sin robar, simplemente abandona.
    public override bool HoldsDoorsLocked => false;

    public bool IsRunning => challengeStarted && !challengeFinished;

    protected override void Awake()
    {
        base.Awake();
        AutoFillReferences();
    }

    private void AutoFillReferences()
    {
        if (item == null)
            item = GetComponentInChildren<StealableItem>(true);

        if (guards.Count == 0)
            guards.AddRange(GetComponentsInChildren<StealChallengeGuard>(true));

        if (traps.Count == 0)
            traps.AddRange(GetComponentsInChildren<StealChallengeTrap>(true));
    }

    public override void Prepare()
    {
        if (challengeFinished)
            return;

        // Ya se jugó (ganado o perdido) en esta run: la room queda inerte.
        if (AlreadyCompletedThisRun())
        {
            challengeFinished = true;
            ShutDownRoom();
            item?.MarkStolen();
            roomInstance?.UnlockDoorsInstant();
            return;
        }

        challengeStarted = true;

        item?.Prepare(this);

        foreach (StealChallengeGuard guard in guards)
        {
            if (guard != null)
                guard.Prepare(this);
        }

        foreach (StealChallengeTrap trap in traps)
        {
            if (trap != null)
                trap.Prepare(this);
        }

        roomInstance?.UnlockDoorsInstant();
    }

    public override bool IsCompleted()
    {
        return challengeFinished;
    }

    /// <summary>Lo llama StealableItem cuando el jugador toca el item.</summary>
    public void NotifyItemStolen()
    {
        if (!IsRunning || resolving)
            return;

        resolving = true;

        challengeFinished = true;
        MarkRunCompleted();
        ShutDownRoom();

        item?.MarkStolen();
        DropScrap();

        roomInstance?.UnlockDoorsAnimated();
    }

    /// <summary>Lo llaman los guardias y las trampas cuando conectan un golpe.</summary>
    public void NotifyPlayerHit(GameObject player)
    {
        if (!IsRunning || resolving)
            return;

        resolving = true;
        StartCoroutine(FailRoutine(player));
    }

    /// <summary>Alerta a todos los guardias (lo usan las trampas tipo alarma).</summary>
    public void AlertAllGuards(Vector3 origin)
    {
        if (!IsRunning)
            return;

        foreach (StealChallengeGuard guard in guards)
        {
            if (guard != null)
                guard.ForceAlert(origin);
        }
    }

    private IEnumerator FailRoutine(GameObject player)
    {
        challengeFinished = true;
        MarkRunCompleted();
        ShutDownRoom();

        ApplyHeartPenalty(player);

        yield return new WaitForSeconds(expelDelay);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.ExpelFromCurrentRoom();
    }

    private void ApplyHeartPenalty(GameObject player)
    {
        PlayerHealth health = player != null ? player.GetComponentInParent<PlayerHealth>() : null;

        if (health == null)
            health = FindFirstObjectByType<PlayerHealth>();

        if (health == null)
            return;

        // >>> ADAPTAR: cambiá esta línea por la firma real de tu PlayerHealth.
        health.TakeDamage(fullHeartDamage);
    }

    private void DropScrap()
    {
        if (scrapPickupPrefab == null || scrapReward <= 0)
            return;

        Transform parent = roomInstance != null ? roomInstance.transform : transform;
        Vector3 origin = item != null ? item.transform.position : transform.position;

        for (int i = 0; i < scrapReward; i++)
        {
            float angle = (360f / scrapReward) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * rewardSpreadRadius;

            GameObject drop = Instantiate(scrapPickupPrefab, origin + offset, Quaternion.identity, parent);

            LootPickup loot = drop.GetComponent<LootPickup>();
            if (loot != null && scrapLoot != null)
                loot.SetLootItem(scrapLoot);
        }
    }

    private void ShutDownRoom()
    {
        foreach (StealChallengeGuard guard in guards)
        {
            if (guard != null)
                guard.Deactivate();
        }

        foreach (StealChallengeTrap trap in traps)
        {
            if (trap != null)
                trap.Deactivate();
        }
    }
}

