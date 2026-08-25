using System.Collections;
using UnityEngine;

public class LootPopMover : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float arcHeight = 0.5F;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool IsPopping { get; private set; }

    public void Launch(Vector3 targetPosition, Collider2D colliderToIgnore)
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null && colliderToIgnore != null)
            Physics2D.IgnoreCollision(myCollider, colliderToIgnore, true);

        IsPopping = true;
        StartCoroutine(PopRoutine(transform.position, targetPosition));
    }

    private IEnumerator PopRoutine(Vector3 start, Vector3 end)
    {
        float t = 0f;
        while (t < duration)
        {
            t+= Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = easeCurve.Evaluate(normalized);

            Vector3 flatPos = Vector3.Lerp(start, end, eased);
            float height = arcHeight * Mathf.Sin(normalized * Mathf.PI);
            transform.position = flatPos + new Vector3(0f, height, 0f);

            yield return null;
        }

        transform.position = end;
        IsPopping = false;
    }
}
