using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UnityConsent;

/// <summary>
/// Telemetría de Raccgeon para la materia Métricas de Videojuegos.
/// Cada evento queda en la carpeta RaccgeonMetrics (raíz del proyecto en el editor, al lado del .exe en el build)
/// y, si Analytics está vinculado, también se manda a la nube.
/// </summary>
public static class GameMetrics
{
    static bool booted;
    static bool analyticsReady;
    static bool quitting;
    static bool sessionOpen;
    static bool runOpen;
    static bool restartPending;

    static string userId;
    static string sessionId;
    static string runId;
    static string logPath;
    static string cumulativePath;
    static string runsCsvPath;

    static float sessionStartUnscaled;
    static float runStartUnscaled;
    static float floorStartUnscaled;
    static float roomEnterUnscaled;
    static float pauseBeganUnscaled = -1f;
    static float pausedDuringRun;
    static float pauseAtFloorStart;
    static float pauseAtRoomEnter;

    static int roomsVisited;
    static int normalRoomsClearedThisFloor;
    static int enemyKillsThisRoom;
    static int enemiesSpawnedThisRoom;
    static int runIndex;
    static int bestFloorThisSession;
    static int previousRunBestFloor;
    static bool shopVisitedThisFloor;
    static bool challengeVisitedThisFloor;
    static bool firstRun = true;

    static string roomId = "";
    static string roomType = "";
    static bool roomHasCombat;
    static bool roomClearedSent;
    static bool bossFightOpen;
    static string bossId = "";
    static int bossPhase;

    static readonly Dictionary<int, float> enemySpawnAt = new Dictionary<int, float>();
    static readonly List<PendingEvent> pending = new List<PendingEvent>();

    struct PendingEvent
    {
        public string Name;
        public Dictionary<string, object> Fields;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (booted)
            return;

        booted = true;
        // Cada apertura del juego empieza con un jugador anónimo nuevo. El siguiente que elige
        // "jugar" desde el menú recibe otro id. Reiniciar tras morir conserva el mismo.
        userId = Guid.NewGuid().ToString("N");

        sessionId = Guid.NewGuid().ToString("N");
        sessionStartUnscaled = Time.unscaledTime;

        string dir = MetricsFolder();
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jsonl");
        cumulativePath = Path.Combine(dir, "todos.jsonl");
        runsCsvPath = Path.Combine(dir, "partidas.csv");

        Application.quitting += OnQuit;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // El spec pide recolección automática, sin formulario. El id es anónimo.
        try
        {
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Granted
            });
        }
        catch (Exception exception)
        {
            Debug.LogWarning("GameMetrics: no se pudo dar consentimiento de Analytics. " + exception.Message);
        }

        _ = InitAnalytics();
        Track("session_started", null);
        sessionOpen = true;
    }

    static async System.Threading.Tasks.Task InitAnalytics()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (quitting || UnityServices.State != ServicesInitializationState.Initialized)
                return;

            // InitializeAsync puede terminar sin registrar Analytics si el proyecto no está vinculado.
            _ = AnalyticsService.Instance;
            analyticsReady = true;
            FlushPending();
        }
        catch (Exception exception)
        {
            analyticsReady = false;
            if (!quitting)
                Debug.LogWarning("GameMetrics: Unity Analytics no está inicializado. Los eventos quedan en " + logPath + ". " + exception.Message);
        }
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Gym")
            return;

        string reason = restartPending ? "restart" : "new_game";
        restartPending = false;
        if (reason == "restart")
            Track("run_restarted", null);

        BeginRun(reason);
    }

    static void OnQuit()
    {
        // Al salir de Play, Unity ya destruyó Analytics. El JSONL igual recibe session_ended.
        quitting = true;
        analyticsReady = false;

        if (runOpen)
            EndRun("quit");

        if (sessionOpen)
        {
            Track("session_ended", ("duration_s", PlaySeconds(sessionStartUnscaled)), ("runs_in_session", runIndex));
            sessionOpen = false;
        }
    }

    public static void MarkRestart()
    {
        restartPending = true;
    }

    public static void QuitRun(string reason)
    {
        if (!runOpen)
            return;

        EndRun(string.IsNullOrEmpty(reason) ? "quit" : reason);
    }

    public static void NotifyPaused(bool paused, string source)
    {
        if (paused)
        {
            if (pauseBeganUnscaled < 0f)
                pauseBeganUnscaled = Time.unscaledTime;

            if (!string.IsNullOrEmpty(source))
                Track("game_paused", ("source", source));
        }
        else if (pauseBeganUnscaled >= 0f)
        {
            pausedDuringRun += Time.unscaledTime - pauseBeganUnscaled;
            pauseBeganUnscaled = -1f;
        }
    }

    public static void RoomEntered(RoomNode node, bool hasCombat)
    {
        if (node == null || node.information == null)
            return;

        roomId = string.IsNullOrEmpty(node.information.roomID) ? node.uniqueNodeID : node.information.roomID;
        roomType = node.information.IsBuffRoom ? "Buff" : node.information.type.ToString();
        roomHasCombat = hasCombat;
        roomClearedSent = false;
        roomEnterUnscaled = Time.unscaledTime;
        pauseAtRoomEnter = PausedSoFar();
        enemyKillsThisRoom = 0;
        enemiesSpawnedThisRoom = 0;
        roomsVisited++;

        if (node.information.type == RoomType.Shop)
            shopVisitedThisFloor = true;
        if (node.information.type == RoomType.Challenge)
            challengeVisitedThisFloor = true;

        Track("room_entered",
            ("has_combat", hasCombat),
            ("rooms_visited", roomsVisited),
            ("first_run", firstRun),
            ("seconds_from_run_start", RunSeconds()));

        if (node.information.type == RoomType.Boss && !bossFightOpen)
        {
            bossFightOpen = true;
            bossId = BossIdFromRoom(roomId);
            bossPhase = 1;
            Track("boss_fight_started", ("boss_id", bossId), ("phase", "normal"));
        }
    }

    public static void RoomCleared()
    {
        if (!roomHasCombat || roomClearedSent)
            return;

        roomClearedSent = true;

        float combat = Mathf.Max(0f, Time.unscaledTime - roomEnterUnscaled - Mathf.Max(0f, PausedSoFar() - pauseAtRoomEnter));
        if (roomType == "Normal")
            normalRoomsClearedThisFloor++;

        Track("room_cleared",
            ("combat_s", combat),
            ("enemies_spawned", enemiesSpawnedThisRoom),
            ("enemies_killed", enemyKillsThisRoom),
            ("first_run", firstRun),
            ("rooms_cleared_before_boss", normalRoomsClearedThisFloor),
            ("shop_visited", shopVisitedThisFloor),
            ("challenge_visited", challengeVisitedThisFloor));
    }

    public static void RewardSpawned(string lootType)
    {
        Track("room_reward_spawned", ("loot_type", lootType));
    }

    public static void PlayerDamaged(float amount, string source, int hpAfter, int hpMax)
    {
        Track("player_damaged",
            ("amount", Mathf.RoundToInt(amount)),
            ("source", source ?? "unknown"),
            ("hp_after", hpAfter),
            ("hp_max", hpMax));
    }

    public static void PlayerDied(string source, string enemyType, int hpMax)
    {
        int floor = Floor();
        if (floor > bestFloorThisSession)
            bestFloorThisSession = floor;

        Track("player_died",
            ("killer_source", source ?? "unknown"),
            ("killer_enemy_type", enemyType ?? ""),
            ("rooms_visited", roomsVisited),
            ("hp_max", hpMax),
            ("first_run", firstRun));

        if (bossFightOpen)
        {
            Track("boss_fight_ended", ("boss_id", bossId), ("phase", bossPhase <= 1 ? "normal" : "possessed"), ("result", "lose"), ("duration_s", RunSeconds()));
            bossFightOpen = false;
        }

        EndRun("death");
    }

    public static void FloorCleared(bool isFinal)
    {
        int floor = Floor();
        if (floor > bestFloorThisSession)
            bestFloorThisSession = floor;

        Track("floor_cleared",
            ("is_final", isFinal),
            ("duration_s", FloorSeconds()),
            ("rooms_visited", roomsVisited),
            ("died_on_floor", false));

        Track("floor_timing",
            ("duration_s", FloorSeconds()),
            ("rooms_visited", roomsVisited),
            ("died_on_floor", false));

        if (isFinal)
        {
            Track("run_won", ("floor", floor), ("duration_s", RunSeconds()), ("runs_in_session", runIndex));
            EndRun("win");
        }
    }

    public static void NotifyFloorStarted()
    {
        int floor = Floor();
        if (floor > bestFloorThisSession)
            bestFloorThisSession = floor;

        pauseAtFloorStart = PausedSoFar();
        floorStartUnscaled = Time.unscaledTime;
        normalRoomsClearedThisFloor = 0;
        shopVisitedThisFloor = false;
        challengeVisitedThisFloor = false;
        ClosePauseClock();
    }

    public static void AbilityUsed(string abilityId)
    {
        Track("ability_used",
            ("ability_id", abilityId),
            ("time_from_run_start_s", RunSeconds()));
    }

    public static void TabOpened()
    {
        Track("tab_opened", ("tab_reason", "manual"));
    }

    public static void CreditsOpened()
    {
        Track("credits_opened", null);
    }

    public static void CurrencyEarned(string currency, int amount, int balance, string source)
    {
        Track("currency_earned",
            ("currency", currency),
            ("amount", amount),
            ("balance_after", balance),
            ("source", source ?? "other"));
    }

    public static void CurrencySpent(string currency, int amount, int balance, string sink)
    {
        Track("currency_spent",
            ("currency", currency),
            ("amount", amount),
            ("balance_after", balance),
            ("sink", sink ?? "other"));
    }

    public static void ShopPurchase(string itemId, string itemType, int price, int scrapBefore)
    {
        Track("shop_purchase",
            ("item_id", itemId ?? ""),
            ("item_type", itemType ?? ""),
            ("price", price),
            ("scrap_before", scrapBefore),
            ("success", true));
    }

    public static void ShopPurchaseFailed(string itemId, string itemType, int price, int scrapBefore, string reason)
    {
        Track("shop_purchase_failed",
            ("item_id", itemId ?? ""),
            ("item_type", itemType ?? ""),
            ("price", price),
            ("scrap_before", scrapBefore),
            ("success", false),
            ("fail_reason", reason ?? "cant_afford"));
    }

    public static void UpgradeOffered(string upgradeId, string source)
    {
        Track("upgrade_offered", ("upgrade_id", upgradeId ?? ""), ("source", source ?? "unknown"));
    }

    public static void UpgradeCollected(string upgradeId, string source, int count)
    {
        Track("upgrade_collected",
            ("upgrade_id", upgradeId ?? ""),
            ("source", source ?? "unknown"),
            ("upgrade_count", count));
    }

    public static void DigSpotSpawned()
    {
        Track("dig_spot_spawned", null);
    }

    public static void DigCompleted(string outcome)
    {
        Track("dig_completed", ("outcome", outcome ?? "empty"), ("mole_spawned", outcome == "mole"));
    }

    public static void TrashOpened(string trashType)
    {
        Track("trash_opened", ("trash_type", trashType ?? "bag"));
    }

    public static void LootPicked(string lootType, string source)
    {
        Track("loot_picked", ("loot_type", lootType ?? "none"), ("source", source ?? "loot"));
    }

    public static void ChallengeStarted(string challengeId)
    {
        Track("challenge_started", ("challenge_id", challengeId ?? ""));
    }

    public static void ChallengeFinished(string challengeId, string result, int waveReached)
    {
        Track("challenge_finished",
            ("challenge_id", challengeId ?? ""),
            ("result", result ?? ""),
            ("wave_reached", waveReached));
    }

    public static void LockpickUsed(string target)
    {
        Track("lockpick_used", ("target", target ?? "shop_door"), ("had_key", true));
    }

    public static void LockpickFailed(string target)
    {
        Track("lockpick_failed", ("target", target ?? "shop_door"), ("had_key", false));
    }

    public static void EnemySpawned(EnemyHealth health)
    {
        if (health == null)
            return;

        enemySpawnAt[health.GetInstanceID()] = Time.unscaledTime;
        enemiesSpawnedThisRoom++;
        Track("enemy_spawned", ("enemy_type", EnemyLabel(health)), ("enemy_id", health.GetInstanceID()));
    }

    public static void EnemyKilled(EnemyHealth health, bool phaseChange)
    {
        if (health == null)
            return;

        float spawned = 0f;
        enemySpawnAt.TryGetValue(health.GetInstanceID(), out spawned);
        float ttk = spawned > 0f ? Time.unscaledTime - spawned : 0f;
        string label = EnemyLabel(health);

        if (!phaseChange)
        {
            enemyKillsThisRoom++;
            Track("enemy_killed", ("enemy_type", label), ("enemy_id", health.GetInstanceID()), ("ttk_s", ttk));
        }

        if (health.GetComponent<BossBase>() == null && health.GetComponentInParent<BossBase>() == null)
            return;

        bossId = BossIdFromName(label);
        if (phaseChange)
        {
            Track("boss_fight_ended", ("boss_id", bossId), ("phase", "normal"), ("result", "phase_clear"), ("duration_s", ttk));
            bossPhase = 2;
            Track("boss_fight_started", ("boss_id", bossId), ("phase", "possessed"));
            return;
        }

        if (bossFightOpen)
        {
            Track("boss_fight_ended",
                ("boss_id", bossId),
                ("phase", bossPhase <= 1 ? "normal" : "possessed"),
                ("result", "win"),
                ("duration_s", ttk));
            bossFightOpen = false;
        }
    }

    static void BeginRun(string reason)
    {
        if (runOpen)
            EndRun("replaced");

        if (reason != "restart" && runIndex > 0)
            userId = Guid.NewGuid().ToString("N");

        runOpen = true;
        runIndex++;
        runId = Guid.NewGuid().ToString("N");
        runStartUnscaled = Time.unscaledTime;
        pausedDuringRun = 0f;
        pauseBeganUnscaled = -1f;
        roomsVisited = 0;
        bestFloorThisSession = 1;
        bossFightOpen = false;
        NotifyFloorStarted();

        Track("run_started", ("reason", reason), ("run_index", runIndex), ("first_run", firstRun));
    }

    static void EndRun(string reason)
    {
        if (!runOpen)
            return;

        int floor = Mathf.Max(1, bestFloorThisSession);
        float duration = RunSeconds();
        if (reason == "death" || reason == "quit" || reason == "pause_exit")
        {
            Track("run_failed",
                ("reason", reason == "death" ? "death" : "quit"),
                ("session_elapsed_s", PlaySeconds(sessionStartUnscaled)));
        }

        Track("run_ended",
            ("reason", reason),
            ("duration_s", duration),
            ("best_floor", floor),
            ("previous_best_floor", previousRunBestFloor),
            ("rooms_visited", roomsVisited));

        if (reason == "death")
        {
            Track("floor_timing",
                ("duration_s", FloorSeconds()),
                ("rooms_visited", roomsVisited),
                ("died_on_floor", true));
        }

        previousRunBestFloor = floor;
        firstRun = false;
        runOpen = false;
        bossFightOpen = false;
        AppendRunRow(reason, floor, duration);
    }

    static string MetricsFolder()
    {
        // Application.dataPath es Assets en el editor y Nombre_Data en el build.
        // El padre es la carpeta del proyecto o la carpeta del .exe: se copia entera después del evento.
        string root = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(root, "RaccgeonMetrics");
    }

    static void AppendRunRow(string reason, int floor, float duration)
    {
        if (string.IsNullOrEmpty(runsCsvPath))
            return;

        bool header = !File.Exists(runsCsvPath);
        var sb = new StringBuilder();
        if (header)
            sb.Append("fecha,user_id,session_id,run_id,motivo,duracion_s,mejor_piso,salas,build\n");

        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append(',').Append(userId);
        sb.Append(',').Append(sessionId);
        sb.Append(',').Append(runId);
        sb.Append(',').Append(reason);
        sb.Append(',').Append(duration.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append(',').Append(floor.ToString(CultureInfo.InvariantCulture));
        sb.Append(',').Append(roomsVisited.ToString(CultureInfo.InvariantCulture));
        sb.Append(',').Append(Application.version ?? "");
        sb.Append('\n');
        File.AppendAllText(runsCsvPath, sb.ToString());
    }

    static void Track(string eventName, params (string key, object value)[] fields)
    {
        try
        {
            var data = Envelope();
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(fields[i].key))
                        data[fields[i].key] = fields[i].value ?? "";
                }
            }

            WriteLine(eventName, data);
            SendAnalytics(eventName, data);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("GameMetrics: no se pudo registrar " + eventName + ". " + exception.Message);
        }
    }

    static Dictionary<string, object> Envelope()
    {
        int hp = 0;
        int hpMax = 0;
        int scrap = 0;
        int keys = 0;
        int upgrades = 0;

        PlayerHealth health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null)
        {
            hp = health.CurrentHealth;
            hpMax = health.MaxHealth;
            PlayerScrap scrapComp = health.GetComponent<PlayerScrap>();
            PlayerKeys keysComp = health.GetComponent<PlayerKeys>();
            PlayerUpgradeManager upgradesComp = health.GetComponent<PlayerUpgradeManager>();
            if (scrapComp != null) scrap = scrapComp.CurrentScrap;
            if (keysComp != null) keys = keysComp.CurrentKeys;
            if (upgradesComp != null && upgradesComp.GetCollectedUpgrades() != null)
                upgrades = upgradesComp.GetCollectedUpgrades().Count;
        }

        return new Dictionary<string, object>
        {
            { "user_id", userId ?? "" },
            { "session_id", sessionId ?? "" },
            { "run_id", runId ?? "" },
            { "unix_ms", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
            { "build_version", Application.version ?? "" },
            { "floor", Floor() },
            { "room_id", roomId ?? "" },
            { "room_type", roomType ?? "" },
            { "player_hp", hp },
            { "player_max_hp", hpMax },
            { "scrap", scrap },
            { "keys", keys },
            { "upgrade_count", upgrades }
        };
    }

    static int Floor()
    {
        return DungeonManager.Instance != null ? DungeonManager.Instance.CurrentFloor : 0;
    }

    static float RunSeconds()
    {
        if (!runOpen)
            return 0f;
        return Mathf.Max(0f, Time.unscaledTime - runStartUnscaled - PausedSoFar());
    }

    static float FloorSeconds()
    {
        return Mathf.Max(0f, Time.unscaledTime - floorStartUnscaled - Mathf.Max(0f, PausedSoFar() - pauseAtFloorStart));
    }

    static float PlaySeconds(float from)
    {
        return Mathf.Max(0f, Time.unscaledTime - from);
    }

    static float PausedSoFar()
    {
        float extra = pauseBeganUnscaled >= 0f ? Time.unscaledTime - pauseBeganUnscaled : 0f;
        return pausedDuringRun + extra;
    }

    static void ClosePauseClock()
    {
        if (pauseBeganUnscaled >= 0f)
        {
            pausedDuringRun += Time.unscaledTime - pauseBeganUnscaled;
            pauseBeganUnscaled = Time.unscaledTime;
        }
    }

    static void WriteLine(string eventName, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(logPath))
            return;

        var sb = new StringBuilder(256);
        sb.Append("{\"event\":\"").Append(Esc(eventName)).Append('"');
        foreach (var pair in data)
        {
            sb.Append(",\"").Append(Esc(pair.Key)).Append("\":");
            sb.Append(JsonValue(pair.Value));
        }
        sb.Append("}\n");
        string line = sb.ToString();
        File.AppendAllText(logPath, line);
        if (!string.IsNullOrEmpty(cumulativePath))
            File.AppendAllText(cumulativePath, line);
    }

    static void SendAnalytics(string eventName, Dictionary<string, object> data)
    {
        if (!analyticsReady)
        {
            pending.Add(new PendingEvent { Name = eventName, Fields = data });
            if (pending.Count > 400)
                pending.RemoveAt(0);
            return;
        }

        Record(eventName, data);
    }

    static void FlushPending()
    {
        for (int i = 0; i < pending.Count; i++)
            Record(pending[i].Name, pending[i].Fields);
        pending.Clear();
    }

    static void Record(string eventName, Dictionary<string, object> data)
    {
        try
        {
            var custom = new CustomEvent(Sanitize(eventName));
            foreach (var pair in data)
            {
                string key = Sanitize(pair.Key);
                if (string.IsNullOrEmpty(key))
                    continue;

                switch (pair.Value)
                {
                    case bool b:
                        custom.Add(key, b);
                        break;
                    case int i:
                        custom.Add(key, i);
                        break;
                    case long l:
                        custom.Add(key, l);
                        break;
                    case float f:
                        custom.Add(key, f);
                        break;
                    case double d:
                        custom.Add(key, (float)d);
                        break;
                    default:
                        custom.Add(key, pair.Value != null ? pair.Value.ToString() : "");
                        break;
                }
            }

            AnalyticsService.Instance.RecordEvent(custom);
        }
        catch (Exception exception)
        {
            analyticsReady = false;
            if (quitting || IsUnavailable(exception))
                return;

            Debug.LogWarning("GameMetrics: Analytics rechazó " + eventName + ". " + exception.Message);
        }
    }

    static bool IsUnavailable(Exception exception)
    {
        string message = exception != null ? exception.Message : "";
        return message.IndexOf("has not been initialized", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string EnemyLabel(EnemyHealth health)
    {
        EnemyBehaviour behaviour = health.GetComponent<EnemyBehaviour>();
        if (behaviour == null)
            behaviour = health.GetComponentInParent<EnemyBehaviour>();
        if (behaviour != null)
            return behaviour.Type.ToString();

        BossBase boss = health.GetComponent<BossBase>();
        if (boss == null)
            boss = health.GetComponentInParent<BossBase>();
        if (boss != null)
            return boss.GetType().Name;

        return health.gameObject.name;
    }

    static string BossIdFromRoom(string id)
    {
        string named = BossIdFromName(id);
        if (named == "mutant_spider" || named == "disgusted_toad" || named == "sorcerer_crow")
            return named;

        int floor = Floor();
        if (floor <= 1)
            return "mutant_spider";
        if (floor == 2)
            return "disgusted_toad";
        return "sorcerer_crow";
    }

    static string BossIdFromName(string name)
    {
        string n = (name ?? "").ToLowerInvariant();
        if (n.Contains("spider") || n.Contains("arana") || n.Contains("araña"))
            return "mutant_spider";
        if (n.Contains("toad") || n.Contains("sapo"))
            return "disgusted_toad";
        if (n.Contains("crow") || n.Contains("cuervo"))
            return "sorcerer_crow";
        return string.IsNullOrEmpty(name) ? "boss" : Sanitize(name);
    }

    static string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "x";

        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                sb.Append(c);
            else
                sb.Append('_');
        }

        if (sb.Length == 0 || char.IsDigit(sb[0]))
            sb.Insert(0, 'e');

        return sb.ToString();
    }

    static string JsonValue(object value)
    {
        switch (value)
        {
            case bool b:
                return b ? "true" : "false";
            case int i:
                return i.ToString(CultureInfo.InvariantCulture);
            case long l:
                return l.ToString(CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("0.###", CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("0.###", CultureInfo.InvariantCulture);
            default:
                return "\"" + Esc(value != null ? value.ToString() : "") + "\"";
        }
    }

    static string Esc(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }
}
