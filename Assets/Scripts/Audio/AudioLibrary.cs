using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Música (opcional hasta tener tracks)")]
    public AudioClip menuMusic;
    public AudioClip dungeonMusic;
    public AudioClip shopMusic;
    public AudioClip bossMusic;
    public AudioClip challengeMusic;
    public AudioClip victoryMusic;
    public AudioClip gameOverMusic;

    [Header("SFX")]
    public AudioClip dash;
    public AudioClip playerHit;
    public AudioClip quickAttack;
    public AudioClip areaAttack;
    public AudioClip lootPickup;
    public AudioClip upgradePickup;

    public AudioClip GetMusic(GameMusic track)
    {
        switch (track)
        {
            case GameMusic.Menu: return menuMusic;
            case GameMusic.Dungeon: return dungeonMusic;
            case GameMusic.Shop: return shopMusic;
            case GameMusic.Boss: return bossMusic;
            case GameMusic.Challenge: return challengeMusic;
            case GameMusic.Victory: return victoryMusic;
            case GameMusic.GameOver: return gameOverMusic;
            default: return null;
        }
    }

    public AudioClip GetSfx(GameSfx id)
    {
        switch (id)
        {
            case GameSfx.Dash: return dash;
            case GameSfx.PlayerHit: return playerHit;
            case GameSfx.QuickAttack: return quickAttack;
            case GameSfx.AreaAttack: return areaAttack;
            case GameSfx.LootPickup: return lootPickup;
            case GameSfx.UpgradePickup: return upgradePickup;
            default: return null;
        }
    }
}
