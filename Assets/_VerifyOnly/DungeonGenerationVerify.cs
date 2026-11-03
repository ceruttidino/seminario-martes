#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verifica el generador dinámico: puertas bidireccionales, sin puertas huérfanas,
/// y que armar un piso nuevo (como al salir del boss) no se cuelga.
/// </summary>
public static class DungeonGenerationVerify
{
    public static void Run()
    {
        int failures = 0;
        RoomInstance dummy = CreateDummyRoom();

        try
        {
            failures += CheckProgressionLengths();
            failures += SimulateWalks(dummy, floors: 3, walksPerFloor: 40);
            failures += SimulateNextFloor(dummy);
        }
        finally
        {
            if (dummy != null)
                Object.DestroyImmediate(dummy.gameObject);
        }

        if (failures > 0)
        {
            Debug.LogError($"[DungeonGenerationVerify] FALLÓ con {failures} error(es).");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[DungeonGenerationVerify] OK: generación dinámica, puertas espejo y cambio de piso.");
        EditorApplication.Exit(0);
    }

    private static int CheckProgressionLengths()
    {
        GameObject host = new GameObject("ProgressionHost");
        DungeonProgression progression = host.AddComponent<DungeonProgression>();
        int failures = 0;

        try
        {
            progression.PrepareFloor(1);
            int floor1 = CountUntilBoss(progression);
            progression.PrepareFloor(2);
            int floor2 = CountUntilBoss(progression);
            progression.PrepareFloor(3);
            int floor3 = CountUntilBoss(progression);

            Debug.Log($"[DungeonGenerationVerify] Rooms normales hasta Boss: piso1={floor1}, piso2={floor2}, piso3={floor3}");

            if (floor1 < 10)
            {
                Debug.LogError($"[DungeonGenerationVerify] El piso 1 quedó corto ({floor1} normales).");
                failures++;
            }

            if (floor2 <= floor1 || floor3 <= floor2)
            {
                Debug.LogError("[DungeonGenerationVerify] Los pisos no crecen con el progreso.");
                failures++;
            }
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        return failures;
    }

    private static int CountUntilBoss(DungeonProgression progression)
    {
        int normals = 0;
        int guard = 0;
        while (!progression.HasSpawnedBoss() && guard++ < 80)
        {
            RoomType next = progression.GetNextRoomType();
            if (next == RoomType.Normal)
                normals++;
            progression.RegisterGeneratedRoom(next);
        }

        return normals;
    }

    private static int SimulateWalks(RoomInstance dummy, int floors, int walksPerFloor)
    {
        int failures = 0;

        for (int floor = 1; floor <= floors; floor++)
        {
            RoomInformation start = MakeInfo("Start", RoomType.Start, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            RoomInformation shop = MakeInfo("Shop", RoomType.Shop, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            RoomInformation boss = MakeInfo("Boss", RoomType.Boss, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right);
            List<RoomInformation> normals = new List<RoomInformation>
            {
                MakeInfo("N_LR", RoomType.Normal, dummy, DoorDirection.Left, DoorDirection.Right),
                MakeInfo("N_UD", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down),
                MakeInfo("N_UR", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Right),
                MakeInfo("N_ALL", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down, DoorDirection.Left, DoorDirection.Right),
                MakeInfo("N_UL", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Left),
                MakeInfo("N_DR", RoomType.Normal, dummy, DoorDirection.Down, DoorDirection.Right)
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

    private static int SimulateNextFloor(RoomInstance dummy)
    {
        RoomInformation start = MakeInfo("Start", RoomType.Start, dummy, DoorDirection.Up, DoorDirection.Down);
        RoomInformation boss = MakeInfo("Boss", RoomType.Boss, dummy, DoorDirection.Up, DoorDirection.Down);
        List<RoomInformation> normals = new List<RoomInformation>
        {
            MakeInfo("N1", RoomType.Normal, dummy, DoorDirection.Up, DoorDirection.Down)
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

    private static RoomInformation MakeInfo(string id, RoomType type, RoomInstance dummy, params DoorDirection[] doors)
    {
        RoomInformation info = ScriptableObject.CreateInstance<RoomInformation>();
        info.roomID = id;
        info.type = type;
        info.prefab = dummy;
        info.availableDoors = new List<DoorDirection>(doors);
        return info;
    }
}
#endif
