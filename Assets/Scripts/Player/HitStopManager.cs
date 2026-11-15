using UnityEngine;
using UnityEngine.InputSystem;

// NUEVO (archivo completo)
// HIT STOP: cuando un golpe del jugador conecta, el juego se frena unos milisegundos.
//
// NO hay que ponerlo en ninguna escena: se crea solo al arrancar el juego
// (aparece en la Hierarchy, dentro de "DontDestroyOnLoad", como "[HitStopManager]").
//
// Corre DESPUÉS de todos los demás scripts: si otro script pisa Time.timeScale
// durante el freeze, lo vuelve a poner y te avisa en la Console.
[DefaultExecutionOrder(10000)]
public class HitStopManager : MonoBehaviour
{
    // ---------------- AJUSTES (se cambian acá, en el código) ----------------

    /// <summary>Muestra en la Console qué pasa con cada golpe. Ponelo en false cuando ya funcione.</summary>
    public static bool DebugLogs = true;

    // Velocidad del juego durante el freeze. NO usar 0: GamePause toma timeScale 0 como pausa.
    private const float StopTimeScale = 0.02f;

    // Tope: aunque se junten muchos golpes seguidos, nunca congela más que esto.
    private const float MaxDuration = 0.25f;

    // Solo en el Editor: apretá esta tecla para un freeze largo de prueba.
    private const Key TestKey = Key.T;
    private const float TestDuration = 0.5f;

    // ------------------------------------------------------------------------

    private static HitStopManager instance;
    private static bool quitting;

    private bool active;
    private float endRealtime;
    private float timeScaleBefore = 1f;
    private bool warnedOverride;

    public static bool IsActive => instance != null && instance.active;

    // Por si tenés desactivado el "Domain Reload" en Enter Play Mode Options: limpia lo estático en cada Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
    }

    // Se crea solo al arrancar (así la tecla de prueba funciona desde el primer segundo).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate();
    }

    /// <summary>Congela el juego 'duration' segundos (tiempo real). Si ya hay un freeze, lo extiende.</summary>
    public static void Play(float duration)
    {
        HitStopManager manager = GetOrCreate();
        if (manager != null)
            manager.Begin(duration, MaxDuration);
    }

    private static HitStopManager GetOrCreate()
    {
        if (instance != null) return instance;
        if (quitting) return null;

        GameObject go = new GameObject("[HitStopManager]");
        DontDestroyOnLoad(go);
        return go.AddComponent<HitStopManager>(); // el Awake setea 'instance'
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void Begin(float duration, float maxDuration)
    {
        if (duration <= 0f) return;

        if (GamePause.IsPaused || (!active && Time.timeScale <= 0f))
        {
            if (DebugLogs) Debug.Log("[HitStop] Ignorado: el juego está en pausa.");
            return;
        }

        duration = Mathf.Min(duration, maxDuration);
        endRealtime = Mathf.Max(endRealtime, Time.unscaledTime + duration);

        if (!active)
        {
            active = true;
            warnedOverride = false;
            // Si el timeScale quedó trabado en el valor del freeze por algún motivo, al terminar volvemos a 1
            timeScaleBefore = Mathf.Approximately(Time.timeScale, StopTimeScale) ? 1f : Time.timeScale;
        }

        Time.timeScale = StopTimeScale;

        if (DebugLogs) Debug.Log($"[HitStop] FREEZE de {duration:0.000} s");
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current[TestKey].wasPressedThisFrame)
        {
            Debug.Log("[HitStop] Prueba con la tecla " + TestKey);
            Begin(TestDuration, TestDuration);
        }
#endif

        if (!active) return;

        // La pausa manda: al despausar, GamePause vuelve a poner timeScale en 1
        if (GamePause.IsPaused)
        {
            active = false;
            return;
        }

        if (Time.unscaledTime >= endRealtime)
        {
            Finish();
            return;
        }

        // Si otro script cambió el timeScale en este frame, lo volvemos a poner
        if (!Mathf.Approximately(Time.timeScale, StopTimeScale))
        {
            if (DebugLogs && !warnedOverride)
            {
                warnedOverride = true;
                Debug.LogWarning($"[HitStop] Otro script cambió Time.timeScale a {Time.timeScale} durante el freeze. " +
                                 "Lo corrijo, pero conviene encontrarlo: en Visual Studio, Ctrl+Shift+F y buscá 'timeScale'.");
            }

            Time.timeScale = StopTimeScale;
        }
    }

    private void Finish()
    {
        active = false;

        if (!GamePause.IsPaused)
            Time.timeScale = timeScaleBefore;
    }

    private void OnDisable()
    {
        // Nunca dejar el juego congelado si este objeto se apaga
        if (active) Finish();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }
}