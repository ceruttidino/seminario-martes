using System.Collections;
using UnityEngine;

// NUEVO (archivo completo)
// Centraliza el "juice" del combate: hit stop, camera shake, camera kick,
// partículas de impacto y sonido de impacto.
// Poner UNO por escena (por ejemplo en un GameObject "CombatFeedback").
[DefaultExecutionOrder(1000)] // corre después de los scripts de cámara
public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }
    public static bool IsHitStopped { get; private set; }

    [Header("Hit Stop")]
    [SerializeField] private bool hitStopEnabled = true;
    [Tooltip("Tope de congelamiento para que varios golpes juntos no traben el juego")]
    [SerializeField] private float maxHitStop = 0.15f;
    [Tooltip("timeScale durante el hit stop. NO usar 0: GamePause considera timeScale 0 como 'pausa' y se perdería el input del jugador.")]
    [SerializeField] private float hitStopTimeScale = 0.02f;

    [Header("Camera Shake")]
    [Tooltip("Ideal: la Main Camera como HIJA de un 'CameraRig'. Tu script de rooms mueve el rig, esto sacude la cámara.")]
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float maxShakeOffset = 0.3f;
    [Tooltip("Rotación de la cámara. Dejar en 0 si usás Pixel Perfect Camera")]
    [SerializeField] private float maxShakeAngle = 0f;
    [SerializeField] private float shakeFrequency = 25f;
    [Tooltip("Cuánto trauma se pierde por segundo")]
    [SerializeField] private float traumaDecay = 2.5f;
    [Tooltip("Qué tan rápido vuelve la cámara después del kick direccional")]
    [SerializeField] private float kickReturnSpeed = 20f;

    [Header("Hit VFX (partículas de impacto)")]
    [SerializeField] private GameObject hitSparkPrefab;
    [SerializeField] private float hitSparkLifetime = 0.5f;
    [SerializeField] private float hitSparkAngleOffset = 0f;

    [Header("Hit SFX")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private Vector2 hitPitchRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private float minTimeBetweenHitSounds = 0.03f;

    private float trauma;
    private Vector2 kickOffset;
    private float noiseSeed;
    private Vector3 lastPosOffset;
    private float lastAngle;

    private float hitStopEndRealtime;
    private float timeScaleBeforeHitStop = 1f;
    private float lastHitSoundTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (shakeTarget == null && Camera.main != null)
            shakeTarget = Camera.main.transform;

        noiseSeed = Random.Range(0f, 100f);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        // Si se cambia de escena en pleno hit stop, no dejar el juego congelado
        if (IsHitStopped)
        {
            if (Mathf.Approximately(Time.timeScale, hitStopTimeScale))
                Time.timeScale = timeScaleBeforeHitStop;
            IsHitStopped = false;
        }
    }

    // ---------------- HIT STOP ----------------

    /// <summary>Congela el juego unos milisegundos. Si ya hay uno activo, lo extiende (no se apilan).</summary>
    public void HitStop(float duration)
    {
        if (!hitStopEnabled || duration <= 0f) return;
        if (!IsHitStopped && GamePause.IsGameplayFrozen) return;

        duration = Mathf.Min(duration, maxHitStop);
        hitStopEndRealtime = Mathf.Max(hitStopEndRealtime, Time.unscaledTime + duration);

        if (!IsHitStopped)
            StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        IsHitStopped = true;
        timeScaleBeforeHitStop = Time.timeScale;
        Time.timeScale = hitStopTimeScale;

        while (Time.unscaledTime < hitStopEndRealtime)
            yield return null;

        // Solo restauramos si nadie más cambió el timeScale mientras tanto
        // (ej: si abrieron la pausa justo en el hit stop, GamePause.SetPaused se encarga)
        if (Mathf.Approximately(Time.timeScale, hitStopTimeScale))
            Time.timeScale = timeScaleBeforeHitStop;

        IsHitStopped = false;
    }

    // ---------------- CAMERA ----------------

    /// <summary>Shake aleatorio. 0.2 = golpe chico, 0.5 = golpe fuerte, 1 = explosión.</summary>
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    /// <summary>Empujoncito de cámara en la dirección del golpe. Es lo que hace que el golpe "pese".</summary>
    public void Kick(Vector2 direction, float amount)
    {
        if (amount <= 0f || direction == Vector2.zero) return;
        kickOffset += direction.normalized * amount;
        kickOffset = Vector2.ClampMagnitude(kickOffset, maxShakeOffset * 2f);
    }

    private void LateUpdate()
    {
        if (shakeTarget == null) return;

        // Tiempo sin escalar: la cámara se sigue moviendo durante el hit stop
        float dt = Time.unscaledDeltaTime;
        Vector3 newOffset = Vector3.zero;
        float newAngle = 0f;

        if (trauma > 0f)
        {
            float shake = trauma * trauma; // curva cuadrática: golpes chicos casi no mueven, grandes sí
            float t = Time.unscaledTime * shakeFrequency;

            newOffset.x = (Mathf.PerlinNoise(noiseSeed, t) * 2f - 1f) * maxShakeOffset * shake;
            newOffset.y = (Mathf.PerlinNoise(noiseSeed + 10f, t) * 2f - 1f) * maxShakeOffset * shake;
            newAngle = (Mathf.PerlinNoise(noiseSeed + 20f, t) * 2f - 1f) * maxShakeAngle * shake;

            trauma = Mathf.Max(0f, trauma - traumaDecay * dt);
        }

        if (kickOffset.sqrMagnitude > 0.000001f)
        {
            newOffset += (Vector3)kickOffset;
            kickOffset = Vector2.Lerp(kickOffset, Vector2.zero, 1f - Mathf.Exp(-kickReturnSpeed * dt));
        }
        else
        {
            kickOffset = Vector2.zero;
        }

        if (newOffset == lastPosOffset && Mathf.Approximately(newAngle, lastAngle)) return;

        // Se aplica como delta para no pisar el movimiento que haga tu script de cámara
        shakeTarget.localPosition += newOffset - lastPosOffset;
        shakeTarget.localRotation *= Quaternion.Euler(0f, 0f, newAngle - lastAngle);

        lastPosOffset = newOffset;
        lastAngle = newAngle;
    }

    // ---------------- VFX / SFX ----------------

    public void SpawnHitVfx(Vector2 position, Vector2 direction)
    {
        if (hitSparkPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + hitSparkAngleOffset;
        GameObject vfx = Instantiate(hitSparkPrefab, position, Quaternion.Euler(0f, 0f, angle));

        if (hitSparkLifetime > 0f)
            Destroy(vfx, hitSparkLifetime);
    }

    public void PlayHitSound()
    {
        if (hitAudioSource == null || hitClips == null || hitClips.Length == 0) return;
        if (Time.unscaledTime - lastHitSoundTime < minTimeBetweenHitSounds) return;

        lastHitSoundTime = Time.unscaledTime;
        hitAudioSource.pitch = Random.Range(hitPitchRange.x, hitPitchRange.y);
        hitAudioSource.PlayOneShot(hitClips[Random.Range(0, hitClips.Length)]);
    }
}