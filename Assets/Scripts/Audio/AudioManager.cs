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
    BossHurt,
    DoorUnlock,
    ShopLockpick,
    SpiderMelee1,
    SpiderMelee2,
    SpiderWebShot,
    SpiderWalk,
    SpiderPoisonFloor
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
    private const int SfxVoiceCount = 8;

    private AudioLibrary library;
    private AudioSource musicSource;
    private AudioSource loopSource;
    private AudioSource[] sfxVoices;
    private GameMusic currentMusic = GameMusic.None;
    private AudioClip currentMusicClip;
    private Coroutine musicFade;
    private int lastSpiderMelee = -1;
    private float loopClipGain = 1f;

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
            ApplyMusicVolume();
        }
    }

    public float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxPref, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxPref, clamped);
            ApplySfxVolume();
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

        musicSource = CreateVoice(loop: true);
        loopSource = CreateVoice(loop: true);
        sfxVoices = new AudioSource[SfxVoiceCount];
        for (int i = 0; i < SfxVoiceCount; i++)
            sfxVoices[i] = CreateVoice(loop: false);

        AudioListener.volume = MasterVolume;
        ApplyMusicVolume();
        ApplySfxVolume();

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

        GameSfx id = lootType == LootType.Health ? GameSfx.HealPickup
            : lootType == LootType.Key ? GameSfx.KeyPickup
            : GameSfx.ScrapPickup;
        Instance.PlaySfxInternal(id, fallback);
    }

    public static void PlayUpgrade(UpgradeSO upgrade, AudioClip fallback = null)
    {
        if (Instance == null) return;

        AudioClip clip = Instance.library != null ? Instance.library.GetUpgradeSfx(upgrade) : fallback;
        Instance.PlayClip(clip, 1f, 1f);
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
        Instance.PlayClip(clip, 1f, 1f);
    }

    public static void PlayBossHurt()
    {
        PlaySfx(GameSfx.BossHurt);
    }

    public static void PlaySpiderMelee()
    {
        if (Instance == null) return;

        int pick;
        if (Instance.lastSpiderMelee == 0)
            pick = 1;
        else if (Instance.lastSpiderMelee == 1)
            pick = 0;
        else
            pick = Random.Range(0, 2);

        Instance.lastSpiderMelee = pick;
        PlaySfx(pick == 0 ? GameSfx.SpiderMelee1 : GameSfx.SpiderMelee2);
    }

    public static void PlayLoop(GameSfx id)
    {
        if (Instance == null) return;
        Instance.PlayLoopInternal(id);
    }

    public static void StopLoop()
    {
        if (Instance == null) return;
        Instance.StopLoopInternal();
    }

    public static void PlayMusic(GameMusic track)
    {
        if (Instance == null) return;

        if (track == GameMusic.Menu || track == GameMusic.GameOver || track == GameMusic.Victory)
            Instance.StopLoopInternal();

        Instance.PlayMusicInternal(track);
    }

    public static void PlayMusicForRoom(RoomType roomType, RoomInstance room = null)
    {
        if (Instance == null) return;

        GameMusic track;
        if (roomType == RoomType.Shop)
            track = GameMusic.Shop;
        else if (roomType == RoomType.Boss)
            track = GameMusic.Boss;
        else
            track = GameMusic.Dungeon;

        if (track != GameMusic.Boss)
            Instance.StopLoopInternal();

        Instance.PlayMusicInternal(track, fallbackToDungeon: true);
    }

    private IEnumerator PlaySecondSwing(AudioClip fallback)
    {
        yield return new WaitForSecondsRealtime(SecondSwingDelay);

        AudioClip first = library != null ? library.GetSfx(GameSfx.QuickAttack) : fallback;
        AudioClip second = library != null ? library.GetSfx(GameSfx.QuickAttackSecond) : null;
        if (second == null || second == first)
            yield break;

        PlaySfxInternal(GameSfx.QuickAttackSecond, null);
    }

    private void PlaySfxInternal(GameSfx id, AudioClip fallback)
    {
        AudioClip clip = library != null ? library.GetSfx(id) : null;
        if (clip == null)
            clip = fallback;
        if (clip == null) return;

        float pitch = library != null ? library.GetSfxPitch(id) : 1f;
        float volume = library != null ? library.GetSfxVolume(id) : 1f;
        PlayClip(clip, volume, pitch);
    }

    private AudioSource CreateVoice(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = true;
        return source;
    }

    private AudioSource GetFreeVoice()
    {
        for (int i = 0; i < sfxVoices.Length; i++)
        {
            if (!sfxVoices[i].isPlaying)
                return sfxVoices[i];
        }

        return sfxVoices[0];
    }

    private void ApplyMusicVolume()
    {
        if (musicSource == null) return;
        float gain = library != null ? library.musicMixGain : 0.4f;
        musicSource.volume = Mathf.Clamp01(MusicVolume * gain);
    }

    private void ApplySfxVolume()
    {
        if (loopSource == null || loopSource.clip == null) return;
        loopSource.volume = Mathf.Clamp01(SfxVolume * loopClipGain);
    }

    private void PlayLoopInternal(GameSfx id)
    {
        if (loopSource == null) return;

        AudioClip clip = library != null ? library.GetSfx(id) : null;
        if (clip == null) return;

        if (loopSource.isPlaying && loopSource.clip == clip)
            return;

        float pitch = library != null ? library.GetSfxPitch(id) : 1f;
        float volume = library != null ? library.GetSfxVolume(id) : 1f;
        float mix = library != null ? library.sfxMixGain : 1f;
        loopClipGain = Mathf.Clamp01(volume * mix);

        loopSource.Stop();
        loopSource.clip = clip;
        loopSource.pitch = pitch > 0f ? pitch : 1f;
        loopSource.volume = Mathf.Clamp01(SfxVolume * loopClipGain);
        loopSource.Play();
    }

    private void StopLoopInternal()
    {
        if (loopSource == null) return;

        loopSource.Stop();
        loopSource.clip = null;
    }

    private void PlayClip(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || sfxVoices == null) return;

        AudioSource voice = GetFreeVoice();
        voice.Stop();
        voice.clip = clip;
        voice.pitch = pitch > 0f ? pitch : 1f;
        float mix = library != null ? library.sfxMixGain : 1f;
        voice.volume = Mathf.Clamp01(SfxVolume * volume * mix);
        voice.Play();
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
        float musicGain = library != null ? library.musicMixGain : 0.4f;
        float target = Mathf.Clamp01(MusicVolume * musicGain);
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
