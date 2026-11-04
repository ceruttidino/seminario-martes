using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Volumen")]
    [Range(0f, 1f)] public float sfxMixGain = 1f;
    [Range(0f, 1f)] public float musicMixGain = 0.4f;

    [Header("Música — GDD")]
    [Tooltip("Main Menu")]
    public AudioClip menuMusic;
    [Tooltip("Exploration and combat")]
    public AudioClip dungeonMusic;
    [Tooltip("Item shop")]
    public AudioClip shopMusic;
    [Tooltip("Character upgrade room (connection room)")]
    public AudioClip upgradeRoomMusic;
    [Tooltip("Un track por boss, índice = piso - 1. Se usa si el tema del sapo o del cuervo todavía está vacío.")]
    public AudioClip[] bossMusicByFloor;
    [Tooltip("Música de la boss fight del sapo")]
    public AudioClip toadBossMusic;
    [Tooltip("Música de la boss fight del cuervo")]
    public AudioClip crowBossMusic;

    [Header("Música extra")]
    public AudioClip victoryMusic;
    public AudioClip gameOverMusic;

    [Header("Items")]
    [Tooltip("Raccoon eating fruit")]
    public AudioClip healPickup;
    [Tooltip("Grabbing screws / metal rattle")]
    public AudioClip scrapPickup;
    [Tooltip("Key jiggle / lockpick")]
    public AudioClip keyPickup;

    [Header("Upgrades")]
    [Tooltip("Punching bag — Boxing Glove")]
    public AudioClip boxingGlovePickup;
    public UpgradeSO boxingGloveUpgrade;
    [Tooltip("Squeaking shoes — Sneakers")]
    public AudioClip sneakersPickup;
    public UpgradeSO sneakersUpgrade;
    public AudioClip defaultUpgradePickup;

    [Header("Player")]
    public AudioClip quickAttack;
    [Range(0.1f, 3f)] public float quickAttackPitch = 2f;
    [Range(0f, 1f)] public float quickAttackVolume = 0.095f;
    public AudioClip quickAttackSecond;
    [Range(0.1f, 3f)] public float quickAttackSecondPitch = 2f;
    [Range(0f, 1f)] public float quickAttackSecondVolume = 0.095f;
    public AudioClip areaAttack;
    [Range(0f, 1f)] public float areaAttackVolume = 0.095f;
    public AudioClip dash;
    [Range(0f, 1f)] public float dashVolume = 1f;
    public AudioClip playerHit;
    [Range(0f, 1f)] public float playerHitVolume = 0.095f;

    [Header("Enemies (hurt)")]
    public AudioClip ratHurt;
    public AudioClip antHurt;
    public AudioClip snailHurt;
    public AudioClip moleHurt;
    public AudioClip hedgehogHurt;
    public AudioClip turtleHurt;
    public AudioClip snakeHurt;
    public AudioClip owlHurt;

    [Header("Bosses")]
    public AudioClip bossHurt;

    [Header("Doors")]
    public AudioClip doorUnlock;
    public AudioClip shopLockpick;

    [Header("Boss — Mutant Spider")]
    public AudioClip spiderMelee1;
    public AudioClip spiderMelee2;
    public AudioClip spiderWebShot;
    public AudioClip spiderWalk;
    [Range(0f, 1f)] public float spiderWalkVolume = 0.35f;
    public AudioClip spiderPoisonFloor;

    [Header("Contenedor y basura")]
    [Tooltip("Contenedor golpeado")]
    public AudioClip containerHit;
    [Tooltip("Contenedor abierto")]
    public AudioClip containerOpen;
    [Tooltip("Bolsa de basura golpeada")]
    public AudioClip trashBagHit;
    [Tooltip("Bolsa de basura abierta")]
    public AudioClip trashBagOpen;

    [Header("Enemies (sonido predeterminado)")]
    public AudioClip ratDefault;
    public AudioClip snailDefault;
    public AudioClip moleDefault;
    public AudioClip antDefault;
    public AudioClip snakeDefault;
    public AudioClip hedgehogDefault;
    public AudioClip turtleDefault;

    [Header("Muerte")]
    [Tooltip("Muerte general de todos los enemigos")]
    public AudioClip enemyDeath;

    public AudioClip GetMusic(GameMusic track, int floor = 1)
    {
        switch (track)
        {
            case GameMusic.Menu: return menuMusic;
            case GameMusic.Dungeon: return dungeonMusic;
            case GameMusic.Shop: return shopMusic;
            case GameMusic.UpgradeRoom: return upgradeRoomMusic;
            case GameMusic.Boss: return GetBossMusic(floor);
            case GameMusic.Victory: return victoryMusic;
            case GameMusic.GameOver: return gameOverMusic;
            default: return null;
        }
    }

    public AudioClip GetBossMusic(int floor)
    {
        if (bossMusicByFloor == null || bossMusicByFloor.Length == 0)
            return null;

        int index = Mathf.Clamp(floor - 1, 0, bossMusicByFloor.Length - 1);
        if (bossMusicByFloor[index] != null)
            return bossMusicByFloor[index];

        for (int i = 0; i < bossMusicByFloor.Length; i++)
        {
            if (bossMusicByFloor[i] != null)
                return bossMusicByFloor[i];
        }

        return null;
    }

    public AudioClip GetSfx(GameSfx id)
    {
        switch (id)
        {
            case GameSfx.HealPickup: return healPickup;
            case GameSfx.ScrapPickup: return scrapPickup;
            case GameSfx.KeyPickup: return keyPickup;
            case GameSfx.BoxingGlovePickup: return boxingGlovePickup;
            case GameSfx.SneakersPickup: return sneakersPickup;
            case GameSfx.UpgradePickup: return defaultUpgradePickup;
            case GameSfx.QuickAttack: return quickAttack;
            case GameSfx.QuickAttackSecond: return quickAttackSecond;
            case GameSfx.AreaAttack: return areaAttack;
            case GameSfx.Dash: return dash;
            case GameSfx.PlayerHit: return playerHit;
            case GameSfx.RatHurt: return ratHurt;
            case GameSfx.AntHurt: return antHurt;
            case GameSfx.SnailHurt: return snailHurt;
            case GameSfx.MoleHurt: return moleHurt;
            case GameSfx.HedgehogHurt: return hedgehogHurt;
            case GameSfx.TurtleHurt: return turtleHurt;
            case GameSfx.SnakeHurt: return snakeHurt;
            case GameSfx.OwlHurt: return owlHurt;
            case GameSfx.BossHurt: return bossHurt;
            case GameSfx.DoorUnlock: return doorUnlock;
            case GameSfx.ShopLockpick: return shopLockpick;
            case GameSfx.SpiderMelee1: return spiderMelee1;
            case GameSfx.SpiderMelee2: return spiderMelee2;
            case GameSfx.SpiderWebShot: return spiderWebShot;
            case GameSfx.SpiderWalk: return spiderWalk;
            case GameSfx.SpiderPoisonFloor: return spiderPoisonFloor;
            case GameSfx.ContainerHit: return containerHit;
            case GameSfx.ContainerOpen: return containerOpen;
            case GameSfx.TrashBagHit: return trashBagHit;
            case GameSfx.TrashBagOpen: return trashBagOpen;
            case GameSfx.RatDefault: return ratDefault;
            case GameSfx.SnailDefault: return snailDefault;
            case GameSfx.MoleDefault: return moleDefault;
            case GameSfx.AntDefault: return antDefault;
            case GameSfx.SnakeDefault: return snakeDefault;
            case GameSfx.HedgehogDefault: return hedgehogDefault;
            case GameSfx.TurtleDefault: return turtleDefault;
            case GameSfx.EnemyDeath: return enemyDeath;
            default: return null;
        }
    }

    public float GetSfxPitch(GameSfx id)
    {
        switch (id)
        {
            case GameSfx.QuickAttack: return quickAttackPitch;
            case GameSfx.QuickAttackSecond: return quickAttackSecondPitch;
            default: return 1f;
        }
    }

    public float GetSfxVolume(GameSfx id)
    {
        switch (id)
        {
            case GameSfx.QuickAttack: return quickAttackVolume;
            case GameSfx.QuickAttackSecond: return quickAttackSecondVolume;
            case GameSfx.AreaAttack: return areaAttackVolume;
            case GameSfx.Dash: return dashVolume;
            case GameSfx.PlayerHit: return playerHitVolume;
            case GameSfx.SpiderWalk: return spiderWalkVolume;
            default: return 1f;
        }
    }

    public AudioClip GetLootSfx(LootType lootType)
    {
        switch (lootType)
        {
            case LootType.Health: return healPickup;
            case LootType.Scrap: return scrapPickup;
            case LootType.Key: return keyPickup;
            default: return null;
        }
    }

    public AudioClip GetUpgradeSfx(UpgradeSO upgrade)
    {
        if (upgrade == null) return defaultUpgradePickup;

        if (IsUpgrade(upgrade, boxingGloveUpgrade, "boxing"))
            return boxingGlovePickup != null ? boxingGlovePickup : defaultUpgradePickup;

        if (IsUpgrade(upgrade, sneakersUpgrade, "sneaker", "shoe"))
            return sneakersPickup != null ? sneakersPickup : defaultUpgradePickup;

        return defaultUpgradePickup;
    }

    public AudioClip GetEnemyHurtSfx(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Rat: return ratHurt;
            case EnemyType.Ant: return antHurt;
            case EnemyType.Snail: return snailHurt;
            case EnemyType.Mole: return moleHurt;
            case EnemyType.Hedgehog: return hedgehogHurt;
            case EnemyType.Turtle: return turtleHurt;
            case EnemyType.Snake: return snakeHurt;
            case EnemyType.Owl: return owlHurt;
            default: return null;
        }
    }

    public AudioClip GetEnemyDefaultSfx(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Rat: return ratDefault;
            case EnemyType.Snail: return snailDefault;
            case EnemyType.Mole: return moleDefault;
            case EnemyType.Ant: return antDefault;
            case EnemyType.Snake: return snakeDefault;
            case EnemyType.Hedgehog: return hedgehogDefault;
            case EnemyType.Turtle: return turtleDefault;
            default: return null;
        }
    }

    private static bool IsUpgrade(UpgradeSO upgrade, UpgradeSO reference, params string[] nameParts)
    {
        if (reference != null && upgrade == reference)
            return true;

        string name = upgrade.upgradeName;
        if (string.IsNullOrEmpty(name))
            name = upgrade.name;

        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < nameParts.Length; i++)
        {
            if (name.IndexOf(nameParts[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
