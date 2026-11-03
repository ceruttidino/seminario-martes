using UnityEngine;

/// <summary>
/// Apariencia del jefe Slime (el antiguo Sapo Asqueroso). Es SOLO visual: no toca vida, dano ni ataques.
///
/// Va en el hijo "Visual" del jefe. Se encarga de:
///   - elegir el sprite del cuerpo segun lo que esta haciendo (reposo con respiracion y parpadeo, boca abierta al
///     preparar la lengua / el moco, en el aire mientras salta),
///   - pupilas que siguen al jugador con la mirada,
///   - efectos de baba: chorreones, salpicadura al aterrizar, escupitajo y el derretido al morir.
///
/// DisgustedToadBoss le avisa que hace con OnAnimBool / OnAnimTrigger (los mismos nombres que ya usaba para el Animator).
/// </summary>
[DisallowMultipleComponent]
public class SlimeBossVisual : MonoBehaviour
{
    private const float PupilTravel = 0.075f;
    private const float BreathHz = 2.2f;

    private static readonly Color BaseColor = SlimeArt.Base;
    private static readonly Color LightColor = SlimeArt.Light;
    private static readonly Color BrightColor = SlimeArt.Bright;

    private SpriteRenderer body;
    private Transform bossRoot;
    private Transform mouth;
    private Transform effectsParent;
    private Transform player;
    private SpriteRenderer pupilLeft;
    private SpriteRenderer pupilRight;

    private bool inAir;
    private bool mouthWiggle;
    private bool tongueOut;
    private float slimeTellUntil;
    private float openUntil;
    private float blinkUntil;
    private float nextBlink;
    private float nextDrip;
    private bool dead;

    private SlimeArt.BodyPose currentPose = (SlimeArt.BodyPose)(-1);

    /// <param name="bodyRenderer">SpriteRenderer del cuerpo (el del hijo Visual).</param>
    /// <param name="mouthPoint">Transform en la boca (desde donde sale la lengua / el moco). Se acomoda solo.</param>
    /// <param name="roomParent">Padre de los efectos (la room), para que desaparezcan con ella.</param>
    public void Initialize(SpriteRenderer bodyRenderer, Transform mouthPoint, Transform roomParent)
    {
        body = bodyRenderer;
        mouth = mouthPoint;
        effectsParent = roomParent;
        bossRoot = transform.parent != null ? transform.parent : transform;

        if (body == null)
            return;

        body.sprite = SlimeArt.Body(SlimeArt.BodyPose.IdleA);
        body.color = Color.white;
        currentPose = SlimeArt.BodyPose.IdleA;

        // La boca queda en la cara del slime (el prefab la traia en el costado del sapo).
        if (mouth != null && mouth.parent == transform)
            mouth.localPosition = SlimeArt.MouthCenter(SlimeArt.BodyPose.IdleA);

        pupilLeft = CreatePupil("PupilLeft");
        pupilRight = CreatePupil("PupilRight");

        nextBlink = Time.time + Random.Range(1.5f, 3.5f);
        nextDrip = Time.time + Random.Range(0.4f, 1.2f);
    }

    private SpriteRenderer CreatePupil(string objectName)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = SlimeArt.Pupil();
        renderer.sortingOrder = body.sortingOrder + 1;
        renderer.enabled = false;
        return renderer;
    }

    // ------------------------------------------------------------------ avisos del jefe

    public void OnAnimBool(string parameter, bool value)
    {
        switch (parameter)
        {
            case "InAir": inAir = value; break;
            case "MouthWiggle": mouthWiggle = value; break;
            case "TongueOut": tongueOut = value; break;
        }
    }

    public void OnAnimTrigger(string parameter)
    {
        switch (parameter)
        {
            case "SlimeTell":
                // Boca abierta mientras se sacude para escupir (el escupitajo corta esto con "Spit").
                slimeTellUntil = Time.time + 1.6f;
                break;

            case "Spit":
                slimeTellUntil = 0f;
                openUntil = Time.time + 0.45f;
                SpitBurst();
                break;

            case "Land":
                LandSplash();
                break;
        }
    }

    /// <summary>La baba se derrite hasta ser un charco. Se llama justo antes de que el jefe se destruya.</summary>
    public void PlayDeath()
    {
        if (dead || body == null)
            return;

        dead = true;
        Vector3 feet = transform.position;
        Vector3 scale = transform.lossyScale;

        GooFx.Melt(body.sprite, feet, scale, body.flipX, Color.white, body.sortingOrder, effectsParent, 1.5f, 1.5f);
        GooFx.Droplets(feet + Vector3.up * 0.7f, effectsParent, 24, 1.8f, 6f, 0.85f, 9f, LightColor, BaseColor, 0.1f, 0.26f);
        GooFx.Ring(feet, effectsParent, new Color(BrightColor.r, BrightColor.g, BrightColor.b, 0.95f), 1.2f, 6.5f, 0.75f, 0.5f);
        GooFx.Splat(feet, effectsParent, new Color(BaseColor.r, BaseColor.g, BaseColor.b, 0.75f), 3.6f, 4.5f, 0.5f);

        body.enabled = false;
        if (pupilLeft != null) pupilLeft.enabled = false;
        if (pupilRight != null) pupilRight.enabled = false;
    }

    // ------------------------------------------------------------------ cada frame

    private void LateUpdate()
    {
        if (body == null || dead)
            return;

        UpdatePose();
        UpdatePupils();
        UpdateDrips();
    }

    private void UpdatePose()
    {
        bool open = mouthWiggle || tongueOut || Time.time < slimeTellUntil || Time.time < openUntil;

        if (!inAir && !open && Time.time >= nextBlink)
        {
            blinkUntil = Time.time + 0.14f;
            nextBlink = Time.time + Random.Range(2.2f, 4.6f);
        }

        SlimeArt.BodyPose wanted;
        if (inAir)
            wanted = SlimeArt.BodyPose.Air;
        else if (open)
            wanted = SlimeArt.BodyPose.Open;
        else if (Time.time < blinkUntil)
            wanted = SlimeArt.BodyPose.Blink;
        else
            wanted = Mathf.FloorToInt(Time.time * BreathHz) % 2 == 0 ? SlimeArt.BodyPose.IdleA : SlimeArt.BodyPose.IdleB;

        if (wanted != currentPose)
        {
            currentPose = wanted;
            body.sprite = SlimeArt.Body(wanted);
        }
    }

    private void UpdatePupils()
    {
        if (pupilLeft == null || pupilRight == null)
            return;

        // Sin cuerpo visible (invocacion) o con los ojos cerrados no hay pupilas.
        bool show = body.enabled && currentPose != SlimeArt.BodyPose.Blink;
        pupilLeft.enabled = show;
        pupilRight.enabled = show;
        if (!show)
            return;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        Vector2 gaze = Vector2.zero;
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)bossRoot.position;
            float distance = toPlayer.magnitude;
            if (distance > 0.01f)
                gaze = toPlayer / distance * PupilTravel * Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(distance / 4f));
        }

        PlacePupil(pupilLeft, SlimeArt.EyeCenter(currentPose, false) + gaze);
        PlacePupil(pupilRight, SlimeArt.EyeCenter(currentPose, true) + gaze);
    }

    private void PlacePupil(SpriteRenderer pupil, Vector2 localPosition)
    {
        pupil.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        pupil.sortingOrder = body.sortingOrder + 1;

        // Que tambien parpadee en rojo cuando le pegan (DamageFlash tiñe el cuerpo).
        pupil.color = body.color;
        pupil.sharedMaterial = body.sharedMaterial;
    }

    private void UpdateDrips()
    {
        if (Time.time < nextDrip)
            return;

        nextDrip = Time.time + Random.Range(0.8f, 1.9f);
        if (inAir || !body.enabled)
            return;

        Vector2 feet = transform.position;
        Vector2 origin = feet + new Vector2(Random.Range(-0.85f, 0.85f), Random.Range(0.25f, 0.5f));
        GooFx.Drip(origin, effectsParent, Color.Lerp(BaseColor, LightColor, Random.value), Random.Range(0.09f, 0.15f), 0.38f, body.sortingOrder + 2);
    }

    // ------------------------------------------------------------------ efectos puntuales

    private void LandSplash()
    {
        Vector2 feet = transform.position;

        GooFx.Droplets(feet + Vector2.up * 0.1f, effectsParent, 14, 2.2f, 5.2f, 0.55f, 9f, LightColor, BaseColor, 0.09f, 0.22f, body.sortingOrder + 2);
        GooFx.Ring(feet, effectsParent, new Color(BrightColor.r, BrightColor.g, BrightColor.b, 0.85f), 1f, 4.4f, 0.45f, 0.5f);
        GooFx.Splat(feet, effectsParent, new Color(BaseColor.r, BaseColor.g, BaseColor.b, 0.55f), 2.6f, 2.2f, 0.5f);
    }

    private void SpitBurst()
    {
        Vector2 point = mouth != null ? (Vector2)mouth.position : (Vector2)transform.position + Vector2.up * 0.5f;
        GooFx.Burst(point, effectsParent, 9, 1.4f, 4f, 0.35f, LightColor, BaseColor, 0.08f, 0.18f, body.sortingOrder + 2);
    }
}
