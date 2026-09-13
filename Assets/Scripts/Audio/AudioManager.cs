using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSfx
{
    HealPickup,
    ScrapPickup,
    KeyPickup,
    BoxingGlovePickup,
    SneakersPickup,
    UpgradePickup,
    QuickAttack,
    QuickAttackSecond,
    AreaAttack,
    Dash,
    PlayerHit,
    RatHurt,
    AntHurt,
    SnailHurt,
    MoleHurt,
    HedgehogHurt,
    TurtleHurt,
    SnakeHurt,
    OwlHurt,
    BossHurt
}

public enum GameMusic
{
    None,
    Menu,
    Dungeon,
    Shop,
    UpgradeRoom,
    Boss,
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
    private const float SecondSwingDelay = 0.12f;

    private AudioLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private GameMusic currentMusic = GameMusic.None;
    private AudioClip currentMusicClip;
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

    public static void PlayLoot(LootType lootType, UpgradeSO upgrade = null, AudioClip fallback = null)
    {
        if (Instance == null) return;

        if (lootType == LootType.Upgrade)
        {
            PlayUpgrade(upgrade, fallback);
            return;
        }

        AudioClip clip = Instance.library != null ? Instance.library.GetLootSfx(lootType) : null;
        Instance.PlayClip(clip != null ? clip : fallback);
    }

    public static void PlayUpgrade(UpgradeSO upgrade, AudioClip fallback = null)
    {
        if (Instance == null) return;

        AudioClip clip = Instance.library != null ? Instance.library.GetUpgradeSfx(upgrade) : null;
        Instance.PlayClip(clip != null ? clip : fallback);
    }

    public static void PlayQuickAttack(AudioClip fallback = null)
    {
        if (Instance == null) return;

        Instance.PlaySfxInternal(GameSfx.QuickAttack, fallback);
        Instance.StartCoroutine(Instance.PlaySecondSwing(fallback));
    }

    public static void PlayEnemyHurt(EnemyType enemyType)
    {
        if (Instance == null) return;

        AudioClip clip = Instance.library != null ? Instance.library.GetEnemyHurtSfx(enemyType) : null;
        Instance.PlayClip(clip);
    }

    public static void PlayBossHurt()
    {
        PlaySfx(GameSfx.BossHurt);
    }

    public static void PlayMusic(GameMusic track)
    {
        if (Instance == null) return;
        Instance.PlayMusicInternal(track);
    }

    public static void PlayMusicForRoom(RoomType roomType, RoomInstance room = null)
    {
        if (Instance == null) return;

        GameMusic track;
        if (room != null && room.GetComponentInChildren<ConnectionRoomBuffSpawner>(true) != null)
            track = GameMusic.UpgradeRoom;
        else if (roomType == RoomType.Shop)
            track = GameMusic.Shop;
        else if (roomType == RoomType.Boss)
            track = GameMusic.Boss;
        else
            track = GameMusic.Dungeon;

        Instance.PlayMusicInternal(track, fallbackToDungeon: true);
    }

    private IEnumerator PlaySecondSwing(AudioClip fallback)
    {
        yield return new WaitForSecondsRealtime(SecondSwingDelay);

        AudioClip second = library != null ? library.GetSfx(GameSfx.QuickAttackSecond) : null;
        PlayClip(second != null ? second : (library != null ? library.GetSfx(GameSfx.QuickAttack) : fallback));
    }

    private void PlaySfxInternal(GameSfx id, AudioClip fallback)
    {
        AudioClip clip = library != null ? library.GetSfx(id) : null;
        PlayClip(clip != null ? clip : fallback);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    private void PlayMusicInternal(GameMusic track, bool fallbackToDungeon = false)
    {
        if (track == GameMusic.None)
        {
            StopMusic();
            return;
        }

        int floor = DungeonManager.Instance != null ? DungeonManager.Instance.CurrentFloor : 1;
        AudioClip clip = library != null ? library.GetMusic(track, floor) : null;

        if (clip == null && fallbackToDungeon && track != GameMusic.Dungeon)
        {
            track = GameMusic.Dungeon;
            clip = library != null ? library.GetMusic(track, floor) : null;
        }

        if (clip == null) return;
        if (currentMusic == track && currentMusicClip == clip && musicSource.isPlaying)
            return;

        currentMusic = track;
        currentMusicClip = clip;

        if (musicFade != null)
            StopCoroutine(musicFade);

        musicFade = StartCoroutine(FadeToMusic(clip));
    }

    private void StopMusic()
    {
        currentMusic = GameMusic.None;
        currentMusicClip = null;

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
