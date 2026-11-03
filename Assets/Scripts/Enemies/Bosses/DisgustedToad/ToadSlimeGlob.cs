using System.Collections;
using UnityEngine;

public class ToadSlimeGlob : MonoBehaviour
{
    [Tooltip("Hijo con el sprite del moco. Sube en arco; el root viaja por el piso.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float spinSpeed = 540f;

    private const float DripInterval = 0.09f;

    private void Awake()
    {
        // Aspecto de baba del jefe Slime. Solo visual: el vuelo y el charco que deja no cambian.
        SpriteRenderer renderer = visual != null ? visual.GetComponentInChildren<SpriteRenderer>() : null;
        if (renderer != null)
        {
            renderer.sprite = SlimeArt.Glob();
            renderer.color = Color.white;
        }
    }

    public void Launch(Vector3 from, Vector3 to, float flightTime, SlimePuddle puddlePrefab, float puddleRadius, Transform puddleParent)
    {
        StartCoroutine(Fly(from, to, flightTime, puddlePrefab, puddleRadius, puddleParent));
    }

    private IEnumerator Fly(Vector3 from, Vector3 to, float flightTime, SlimePuddle puddlePrefab, float puddleRadius, Transform puddleParent)
    {
        float t = 0f;
        float nextDrip = 0f;
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

            // Va chorreando baba por el camino.
            if (visual != null && t >= nextDrip)
            {
                nextDrip = t + DripInterval;
                GooFx.Drip(visual.position, puddleParent, SlimeArt.Base, Random.Range(0.07f, 0.12f), 0.35f);
            }

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        SlimePuddle.Spawn(puddlePrefab, to, puddleRadius, puddleParent);

        // Salpicadura al caer (efecto visual).
        GooFx.Droplets(to, puddleParent, 12, 2f, 4.6f, 0.5f, 9f, SlimeArt.Light, SlimeArt.Base, 0.09f, 0.2f);
        GooFx.Ring(to, puddleParent, new Color(SlimeArt.Bright.r / 255f, SlimeArt.Bright.g / 255f, SlimeArt.Bright.b / 255f, 0.8f), 0.8f, Mathf.Max(1.2f, puddleRadius * 1.6f), 0.4f, 0.55f);

        Destroy(gameObject);
    }
}
