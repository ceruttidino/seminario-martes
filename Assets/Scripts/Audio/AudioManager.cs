using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSfx
{
    Dash,
    PlayerHit,
    QuickAttack,
    AreaAttack,
    LootPickup,
    UpgradePickup
}

public enum GameMusic
{
    None,
    Menu,
    Dungeon,
    Shop,
    Boss,
    Challenge,
    Victory,
    GameOver
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MasterPref = "audio.masterVolume";
    private const string MusicPref = "audio.musicVolume";
    private const string SfxPref = "audio.sfxVolume";
    private const float MusicFadeTime = 0.35f;

    private AudioLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private GameMusic currentMusic = GameMusic.None;
    private Coroutine musicFade;

    public float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterPref, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterPref, clamped);
            AudioListener.volume = clamped;
        }
    }

    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicPref, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicPref, clamped);
            if (musicSource != null)
                musicSource.volume = clamped;
        }
    }

    public float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxPref, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxPref, clamped);
            if (sfxSource != null)
                sfxSource.volume = clamped;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        library = Resources.Load<AudioLibrary>("AudioLibrary");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.ignoreListenerPause = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.ignoreListenerPause = true;

        AudioListener.volume = MasterVolume;
        musicSource.volume = MusicVolume;
        sfxSource.volume = SfxVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplySceneMusic(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneMusic(scene);
    }

    public static void PlaySfx(GameSfx id, AudioClip fallback = null)
    {
        if (Instance == null) return;
        Instance.PlaySfxInternal(id, fallback);
    }

    public static void PlayMusic(GameMusic track)
    {
        if (Instance == null) return;
        Instance.PlayMusicInternal(track);
    }

    public static void PlayMusicForRoom(RoomType roomType)
    {
        if (Instance == null) return;

        GameMusic track = roomType switch
        {
            RoomType.Shop => GameMusic.Shop,
            RoomType.Boss => GameMusic.Boss,
            RoomType.Challenge => GameMusic.Challenge,
            _ => GameMusic.Dungeon
        };

        Instance.PlayMusicInternal(track, fallbackToDungeon: true);
    }

    private void PlaySfxInternal(GameSfx id, AudioClip fallback)
    {
        AudioClip clip = library != null ? library.GetSfx(id) : null;
        if (clip == null)
            clip = fallback;
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    private void PlayMusicInternal(GameMusic track, bool fallbackToDungeon = false)
    {
        if (track == GameMusic.None)
        {
            StopMusic();
            return;
        }

        AudioClip clip = library != null ? library.GetMusic(track) : null;
        if (clip == null && fallbackToDungeon && track != GameMusic.Dungeon)
        {
            track = GameMusic.Dungeon;
            clip = library != null ? library.GetMusic(track) : null;
        }

        if (clip == null) return;
        if (currentMusic == track && musicSource.clip == clip && musicSource.isPlaying)
            return;

        currentMusic = track;

        if (musicFade != null)
            StopCoroutine(musicFade);

        musicFade = StartCoroutine(FadeToMusic(clip));
    }

    private void StopMusic()
    {
        currentMusic = GameMusic.None;

        if (musicFade != null)
            StopCoroutine(musicFade);

        musicSource.Stop();
        musicSource.clip = null;
    }

    private IEnumerator FadeToMusic(AudioClip nextClip)
    {
        float startVolume = musicSource.volume;
        bool hasCurrent = musicSource.isPlaying && musicSource.clip != null;

        if (hasCurrent)
        {
            float t = 0f;
            while (t < MusicFadeTime)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / MusicFadeTime);
                yield return null;
            }
        }

        musicSource.clip = nextClip;
        musicSource.volume = 0f;
        musicSource.Play();

        float fadeIn = 0f;
        float target = MusicVolume;
        while (fadeIn < MusicFadeTime)
        {
            fadeIn += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, target, fadeIn / MusicFadeTime);
            yield return null;
        }

        musicSource.volume = target;
        musicFade = null;
    }

    private void ApplySceneMusic(Scene scene)
    {
        if (scene.name == "Gym")
            PlayMusicInternal(GameMusic.Dungeon, fallbackToDungeon: true);
        else
            PlayMusicInternal(GameMusic.Menu);
    }
}
