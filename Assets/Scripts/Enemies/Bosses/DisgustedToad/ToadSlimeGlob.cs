using System.Collections;
using UnityEngine;

public class ToadSlimeGlob : MonoBehaviour
{
    [Tooltip("Hijo con el sprite del moco. Sube en arco; el root viaja por el piso.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float spinSpeed = 540f;

    public void Launch(Vector3 from, Vector3 to, float flightTime, SlimePuddle puddlePrefab, float puddleRadius, Transform puddleParent)
    {
        StartCoroutine(Fly(from, to, flightTime, puddlePrefab, puddleRadius, puddleParent));
    }

    private IEnumerator Fly(Vector3 from, Vector3 to, float flightTime, SlimePuddle puddlePrefab, float puddleRadius, Transform puddleParent)
    {
        float t = 0f;
        flightTime = Mathf.Max(0.01f, flightTime);

        while (t < flightTime)
        {
            float k = t / flightTime;
            transform.position = Vector3.Lerp(from, to, k);
            if (visual != null)
            {
                visual.localPosition = Vector3.up * (4f * arcHeight * k * (1f - k));
                visual.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            }
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        SlimePuddle.Spawn(puddlePrefab, to, puddleRadius, puddleParent);
        Destroy(gameObject);
    }
}
