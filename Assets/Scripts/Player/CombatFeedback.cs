using UnityEngine; // CAMBIADO: ya no hace falta System.Collections (no hay corrutina)

// NUEVO (archivo completo)
// Centraliza el "juice" del combate: camera shake, camera kick,
// partículas de impacto y sonido de impacto.
// CAMBIADO: el HIT STOP ahora lo hace HitStopManager (se crea solo, no depende de esta escena).
// HitStop() e IsHitStopped siguen existiendo para no romper a los scripts que ya los usan.
// Poner UNO por escena (por ejemplo en un GameObject "CombatFeedback").
[DefaultExecutionOrder(1000)] // corre después de los scripts de cámara
public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }
    public static bool IsHitStopped => HitStopManager.IsActive; // CAMBIADO: ahora lo informa HitStopManager

    // ELIMINADO: los campos de Hit Stop (hitStopEnabled, maxHitStop, hitStopTimeScale). Ahora están en HitStopManager.

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

    // ELIMINADO: hitStopEndRealtime y timeScaleBeforeHitStop
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
        // CAMBIADO: el timeScale ya no se toca acá (lo maneja HitStopManager)
        if (Instance == this)
            Instance = null;
    }

    // ---------------- HIT STOP ----------------

    /// <summary>Congela el juego unos milisegundos. Si ya hay uno activo, lo extiende (no se apilan).</summary>
    public void HitStop(float duration)
    {
        HitStopManager.Play(duration); // CAMBIADO: delega en HitStopManager (ELIMINADO: HitStopRoutine)
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