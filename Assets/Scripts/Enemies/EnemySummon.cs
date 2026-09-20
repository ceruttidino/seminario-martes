using System.Collections;
using UnityEngine;

public static class EnemySummon
{
    public const float Delay = 1.25f;

    public static void Hold(GameObject enemy)
    {
        if (enemy == null) return;

        foreach (Behaviour behaviour in enemy.GetComponents<Behaviour>())
        {
            if (behaviour == null) continue;
            if (behaviour is EnemyHealth) continue;
            behaviour.enabled = false;
        }

        foreach (SpriteRenderer renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = false;

        foreach (Collider2D col in enemy.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public static void Release(GameObject enemy)
    {
        if (enemy == null) return;

        foreach (Behaviour behaviour in enemy.GetComponents<Behaviour>())
        {
            if (behaviour == null) continue;
            behaviour.enabled = true;
        }

        foreach (SpriteRenderer renderer in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = true;

        foreach (Collider2D col in enemy.GetComponentsInChildren<Collider2D>(true))
            col.enabled = true;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = true;
    }

    public static IEnumerator SpawnAfterSmoke(GameObject prefab, Vector3 position, Transform parent, System.Action<GameObject> onSpawned)
    {
        PlaySmoke(position);
        yield return new WaitForSeconds(Delay);
        if (prefab == null)
            yield break;

        GameObject enemy = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        onSpawned?.Invoke(enemy);
    }

    public static IEnumerator RevealAfterSmoke(GameObject enemy, Vector3 smokePosition)
    {
        Hold(enemy);
        PlaySmoke(smokePosition);
        yield return new WaitForSeconds(Delay);
        Release(enemy);
    }

    public static IEnumerator PlaySmokeAt(Transform target)
    {
        if (target == null)
            yield break;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        bool[] rendererEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererEnabled[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }

        Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
        bool[] colliderEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderEnabled[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        PlaySmoke(target.position);
        yield return new WaitForSeconds(Delay);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = rendererEnabled[i];
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = colliderEnabled[i];
        }
    }

    public static void PlaySmoke(Vector3 position)
    {
        GameObject go = new GameObject("SummonSmoke");
        go.transform.position = position;

        ParticleSystem particles = CreateStoppedParticles(go);
        ParticleSystem.MainModule main = particles.main;
        main.duration = Delay;
        main.loop = false;
        main.startLifetime = 0.95f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.92f, 0.04f, 0.04f, 0.92f);
        main.gravityModifier = -0.28f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 520;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 320f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.32f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.85f, 1f, 0.2f));

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.08f, 0.05f), 0f),
                new GradientColorKey(new Color(0.7f, 0.02f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 8;
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        particles.Play(true);
        Object.Destroy(go, Delay + 0.8f);
    }

    public static void PlayBloodHit(Vector3 position)
    {
        GameObject go = new GameObject("BloodHit");
        go.transform.position = position;

        ParticleSystem particles = CreateStoppedParticles(go);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.28f;
        main.startSpeed = 1.6f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.7f, 0.05f, 0.05f, 1f);
        main.gravityModifier = 0.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 18;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        particles.Play(true);
        Object.Destroy(go, 0.7f);
    }

    private static ParticleSystem CreateStoppedParticles(GameObject go)
    {
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        return particles;
    }

    public static void SpawnAfterDelay(GameObject prefab, Vector3 position, Transform parent, System.Action<GameObject> onSpawned)
    {
        if (prefab == null)
            return;

        GameObject host = new GameObject("SummonDelay");
        host.AddComponent<SummonDelayHost>().Begin(prefab, position, parent, onSpawned);
    }

    private sealed class SummonDelayHost : MonoBehaviour
    {
        public void Begin(GameObject prefab, Vector3 position, Transform parent, System.Action<GameObject> onSpawned)
        {
            StartCoroutine(Run(prefab, position, parent, onSpawned));
        }

        private IEnumerator Run(GameObject prefab, Vector3 position, Transform parent, System.Action<GameObject> onSpawned)
        {
            PlaySmoke(position);
            yield return new WaitForSeconds(Delay);

            if (prefab != null)
            {
                GameObject enemy = Object.Instantiate(prefab, position, Quaternion.identity, parent);
                onSpawned?.Invoke(enemy);
            }

            Destroy(gameObject);
        }
    }
}
