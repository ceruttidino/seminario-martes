using UnityEngine;

/// <summary>
/// Clase base para todos los challenges. Permite que el dungeon trabaje con
/// cualquier challenge sin conocer su implementación concreta.
/// </summary>
public abstract class ChallengeRoomBase : MonoBehaviour
{
    protected RoomInstance roomInstance;

    /// <summary>Id único del challenge (ver constantes en ChallengeRunState).</summary>
    public abstract string ChallengeId { get; }

    /// <summary>Si es true, las puertas quedan trabadas aunque no haya combate activo.</summary>
    public virtual bool HoldsDoorsLocked => false;

    protected virtual void Awake()
    {
        roomInstance = GetComponent<RoomInstance>();
        if (roomInstance == null)
            roomInstance = GetComponentInParent<RoomInstance>();
    }

    /// <summary>Lo llama DungeonManager la primera vez que el jugador entra a la room.</summary>
    public abstract void Prepare();

    public abstract bool IsCompleted();

    protected void MarkRunCompleted()
    {
        ChallengeRunState.MarkCompleted(ChallengeId);
    }

    protected bool AlreadyCompletedThisRun()
    {
        return ChallengeRunState.WasCompleted(ChallengeId);
    }
}

