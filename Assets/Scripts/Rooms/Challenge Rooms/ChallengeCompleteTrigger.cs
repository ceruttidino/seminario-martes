using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChallengeCompleteTrigger : MonoBehaviour
{
    [SerializeField]
    private ChallengeRoomController challengeRoom;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (challengeRoom == null)
        {
            challengeRoom =
                GetComponentInParent<ChallengeRoomController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (challengeRoom == null)
            return;

        challengeRoom.CompleteChallenge();

        gameObject.SetActive(false);
    }
}
