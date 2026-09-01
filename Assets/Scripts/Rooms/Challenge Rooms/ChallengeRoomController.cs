using UnityEngine;

public class ChallengeRoomController : MonoBehaviour
{
    private RoomInstance roomInstance;

    private bool challengeStarted = false;
    private bool challengeCompleted = false;

    private void Awake()
    {
        roomInstance = GetComponentInParent<RoomInstance>();
    }

    public void StartChallenge()
    {
        if (challengeStarted || challengeCompleted)
            return;

        challengeStarted = true;

        if (roomInstance != null)
        {
            roomInstance.LockDoors();
        }
    }

    public void CompleteChallenge()
    {
        if (!challengeStarted || challengeCompleted)
            return;

        challengeCompleted = true;

        if (roomInstance != null)
        {
            roomInstance.UnlockDoorsAnimated();
        }
    }

    public bool IsCompleted()
    {
        return challengeCompleted;
    }
}
