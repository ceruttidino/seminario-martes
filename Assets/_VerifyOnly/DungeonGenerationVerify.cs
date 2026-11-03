#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// CAMBIADO (archivo completo): actualizado al sistema nuevo de rooms.
// Verifica el generador: puertas bidireccionales, sin puertas huérfanas, que armar un piso nuevo
// no se cuelga, el largo de los niveles y que todos los Floor Config del proyecto estén bien armados.
//
// Se corre desde el menú Tools > Dungeon > Verificar generación (el resultado sale en la consola).
// También se puede correr por línea de comandos con -executeMethod DungeonGenerationVerify.Run.
public static class DungeonGenerationVerify
{
    // NUEVO: antes solo se podía correr por línea de comandos
    [MenuItem("Tools/Dungeon/Verificar generación")]
    private static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        int failures = 0;
        RoomInstance dummy = CreateDummyRoom();
        List<RoomInformation> createdInfos = new List<RoomInformation>();

        try
        {
            failures += CheckProgressionLengths();
            failures += SimulateWalks(dummy, createdInfos, floors: 3, walksPerFloor: 40);
            failures += SimulateNextFloor(dummy, createdInfos);
            failures += CheckProjectFloorConfigs(); // NUEVO
        }
        finally
        {
            if (dummy != null)
                Object.DestroyImmediate(dummy.gameObject);

            // NUEVO: los RoomInformation de prueba se borran (antes quedaban en memoria)
            foreach (RoomInformation info in createdInfos)
            {
                if (info != null)
                    Object.DestroyImmediate(info);
            }
        }

        if (failures > 0)
            Debug.LogError($"[DungeonGenerationVerify] FALLÓ con {failures} error(es).");
        else
            Debug.Log("[DungeonGenerationVerify] OK: generación dinámica, puertas espejo, cambio de piso y Floor Configs.");

        // CAMBIADO: antes cerraba Unity SIEMPRE. Ahora solo cuando se corre por línea de comandos.
        if (Application.isBatchMode)
            EditorApplication.Exit(failures > 0 ? 1 : 0);
    }

    // CAMBIADO: DungeonProgression ya no es un componente y el largo viene de cada Floor Config.
    // Ahora se verifica que con y sin Shop se generen las rooms normales esperadas antes del Boss.
    private static int CheckProgressionLengths()
    {
        int failures = 0;
        DungeonProgression progression = new DungeonProgression();

        progression.Configure(7, 5, hasShop: true);
        int withShop = CountUntilBoss(progression, out bool shopAppeared);

        progression.Configure(7, 5, hasShop: false);
        int withoutShop = CountUntilBoss(progression, out bool shopAppearedWithoutShop);

        Debug.Log($"[DungeonGenerationVerify] Normales hasta Boss: con shop={withShop}, sin shop={withoutShop}");

        if (withShop != 12 || !shopAppeared)
        {
            Debug.LogError($"[DungeonGenerationVerify] Con shop se esperaban 12 normales y una Shop (salieron {withShop}, shop={shopAppeared}).");
            failures++;
        }

        if (withoutShop != 12 || shopAppearedWithoutShop)
        {
            Debug.LogError($"[DungeonGenerationVerify] Sin shop se esperaban 12 normales y ninguna Shop (salieron {withoutShop}, shop={shopAppearedWithoutShop}).");
            failures++;
        }

        return failures;
    }

    private static int CountUntilBoss(DungeonProgression progression, out bool shopAppeared)
    {
        int normals = 0;
        int guard = 0;
        shopAppeared = false;

        while (!progression.HasSpawnedBoss() && guard++ < 80)
        {
            RoomType next = progression.GetNextRoomType();
            if (next == RoomType.Normal)
                normals++;
            if (next == RoomType.Shop)
                shopAppeared = true;
            progression.RegisterGeneratedRoom(next);
        }

        return normals;
    }

    private static int SimulateWalks(RoomInstance dummy, List<RoomInformation> created, int floors, int walksPerFloor)
    {
        int failures = 0;

        for (int floor = 1; floor <= floors; floor++)
        {
            RoomInformation start = MakeInfo(created, "Start", RoomType.Start, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            RoomInformation shop = MakeInfo(created, "Shop", RoomType.Shop, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            RoomInformation boss = MakeInfo(created, "Boss", RoomType.Boss, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            List<RoomInformation> normals = new List<RoomInformation>
            {
                MakeInfo(created, "N_LR", RoomType.Normal, dummy, DoorDirection.Left, DoorDirection.Right),
                MakeInfo(created, "N_UD", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down),
                MakeInfo(created, "N_UR", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Right),
                MakeInfo(created, "N_ALL", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right),
                MakeInfo(created, "N_UL", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Left),
                MakeInfo(created, "N_DR", RoomType.Normal, dummy, DoorDirection.Down, DoorDirection.Right)
            };

            DungeonLayout layout = new DungeonLayout();
            layout.Build(start, normals, shop, boss, new List<RoomInformation>());

            RoomNode current = layout.StartNode;
            layout.CreateNextRoom(current, DoorDirection.Up, RoomType.Normal);

            for (int step = 0; step < walksPerFloor; step++)
            {
                List<DoorDirection> options = new List<DoorDirection>();
                foreach (DoorDirection dir in System.Enum.GetValues(typeof(DoorDirection)))
                {
                    if (current.information != null && current.information.HasDoor(dir))
                        options.Add(dir);
                }

                if (options.Count == 0)
                    break;

                DoorDirection chosen = options[step % options.Count];
                RoomNode next = layout.CreateNextRoom(current, chosen, RoomType.Normal);
                if (next == null)
                    continue;

                DoorDirection back = chosen.Opposite();
                if (next.GetNeighbor(back) != current)
                {
                    Debug.LogError($"[DungeonGenerationVerify] La room {next.name} no tiene la puerta de vuelta {back}.");
                    failures++;
                    break;
                }

                if (next.information == null || !next.information.HasDoor(back))
                {
                    Debug.LogError($"[DungeonGenerationVerify] Prefab {next.name} no tiene la puerta de entrada {back}.");
                    failures++;
                    break;
                }

                current = next;
            }

            failures += ValidateGraph(layout, floor);
        }

        return failures;
    }

    private static int SimulateNextFloor(RoomInstance dummy, List<RoomInformation> created)
    {
        RoomInformation start = MakeInfo(created, "Start", RoomType.Start, dummy, DoorDirection.Up, DoorDirection.Down);
        RoomInformation boss = MakeInfo(created, "Boss", RoomType.Boss, dummy, DoorDirection.Up, DoorDirection.Down);
        List<RoomInformation> normals = new List<RoomInformation>
        {
            MakeInfo(created, "N1", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down)
        };

        DungeonLayout first = new DungeonLayout();
        first.Build(start, normals, null, boss, null);
        first.CreateNextRoom(first.StartNode, DoorDirection.Up, RoomType.Normal);

        DungeonLayout second = new DungeonLayout();
        second.Build(start, normals, null, boss, null);
        RoomNode next = second.CreateNextRoom(second.StartNode, DoorDirection.Up, RoomType.Normal);
        if (next == null || second.StartNode == null)
        {
            Debug.LogError("[DungeonGenerationVerify] El cambio de piso no pudo armar el Start.");
            return 1;
        }

        Debug.Log("[DungeonGenerationVerify] Cambio de piso OK (Start + primera room).");
        return 0;
    }

    // NUEVO: revisa todos los Floor Config del proyecto con la misma validación que usa el juego
    private static int CheckProjectFloorConfigs()
    {
        int failures = 0;
        string[] guids = AssetDatabase.FindAssets("t:DungeonFloorConfig");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[DungeonGenerationVerify] No hay ningún Floor Config en el proyecto todavía.");
            return 0;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DungeonFloorConfig config = AssetDatabase.LoadAssetAtPath<DungeonFloorConfig>(path);
            if (config == null) continue;

            if (config.Validate(out string problems))
            {
                Debug.Log($"[DungeonGenerationVerify] Floor Config OK: {path}");
            }
            else
            {
                Debug.LogError($"[DungeonGenerationVerify] {path} tiene problemas:\n{problems}", config);
                failures++;
            }
        }

        return failures;
    }

    private static int ValidateGraph(DungeonLayout layout, int floor)
    {
        int failures = 0;
        foreach (RoomNode node in layout.GetAllRooms())
        {
            foreach (KeyValuePair<DoorDirection, RoomNode> pair in node.neighboors)
            {
                RoomNode other = pair.Value;
                if (other == null)
                    continue;

                DoorDirection back = pair.Key.Opposite();
                if (other.GetNeighbor(back) != node)
                {
                    Debug.LogError($"[DungeonGenerationVerify] Piso {floor}: {node.name} -> {pair.Key} no es espejo.");
                    failures++;
                }

                if (node.information != null && !node.information.HasDoor(pair.Key))
                {
                    Debug.LogError($"[DungeonGenerationVerify] Piso {floor}: {node.name} tiene vecino {pair.Key} sin esa puerta en el prefab.");
                    failures++;
                }
            }
        }

        Debug.Log($"[DungeonGenerationVerify] Piso {floor}: {layout.GetAllRooms().Count} rooms, grafo coherente={failures == 0}");
        return failures;
    }

    private static RoomInstance CreateDummyRoom()
    {
        GameObject go = new GameObject("VerifyDummyRoom");
        return go.AddComponent<RoomInstance>();
    }

    // CAMBIADO: availableDoors ya no existe; las puertas de prueba se cargan con SetManualDoors
    private static RoomInformation MakeInfo(List<RoomInformation> created, string id, RoomType type, RoomInstance dummy, params DoorDirection[] doors)
    {
        RoomInformation info = ScriptableObject.CreateInstance<RoomInformation>();
        info.name = id;
        info.roomID = id;
        info.type = type;
        info.prefab = dummy;
        info.SetManualDoors(doors);
        created.Add(info);
        return info;
    }
}
#endif