#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// NUEVO (archivo completo). TIENE QUE IR EN UNA CARPETA LLAMADA "Editor" (ej: Assets/Scripts/Editor/).
//
// Crea los RoomInformation automáticamente a partir de los prefabs de rooms:
// seleccionás los prefabs en la ventana Project > clic derecho > Dungeon > Crear Room Information.
// Cada asset se guarda en Assets/Data/Rooms/<Tipo>/ con el mismo nombre del prefab.
// El tipo se adivina por los componentes y el nombre del prefab: revisalo igual.
public static class RoomInformationCreator
{
    private const string RootFolder = "Assets/Data/Rooms";

    [MenuItem("Assets/Dungeon/Crear Room Information desde prefabs", false, 2000)]
    private static void CreateFromSelection()
    {
        int created = 0;
        int skipped = 0;

        foreach (Object obj in Selection.objects)
        {
            GameObject prefab = obj as GameObject;
            if (prefab == null) continue;

            RoomInstance room = prefab.GetComponent<RoomInstance>();
            if (room == null)
            {
                Debug.LogWarning($"[RoomCreator] '{prefab.name}' no tiene RoomInstance en el root: se saltea.", prefab);
                skipped++;
                continue;
            }

            RoomType type = GuessType(prefab);
            string folder = $"{RootFolder}/{type}";
            EnsureFolder(folder);

            string path = $"{folder}/{prefab.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<RoomInformation>(path) != null)
            {
                Debug.Log($"[RoomCreator] Ya existe '{path}': se saltea.");
                skipped++;
                continue;
            }

            RoomInformation info = ScriptableObject.CreateInstance<RoomInformation>();
            info.roomID = prefab.name;
            info.type = type;
            info.prefab = room;

            AssetDatabase.CreateAsset(info, path);
            created++;

            if (type == RoomType.Challenge)
                Debug.LogWarning($"[RoomCreator] '{prefab.name}' es Challenge: acordate de completar su Challenge Id.", info);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomCreator] Listo: {created} creados, {skipped} salteados. Están en {RootFolder}.");
    }

    // El menú solo se habilita si hay al menos un prefab con RoomInstance seleccionado
    [MenuItem("Assets/Dungeon/Crear Room Information desde prefabs", true)]
    private static bool CanCreateFromSelection()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is GameObject go && go.GetComponent<RoomInstance>() != null)
                return true;
        }

        return false;
    }

    private static RoomType GuessType(GameObject prefab)
    {
        // Primero por componentes (lo más confiable)
        if (prefab.GetComponentInChildren<BossBase>(true) != null)
            return RoomType.Boss;

        if (prefab.GetComponent<ChallengeRoomController>() != null || prefab.GetComponentInChildren<ChallengeRoomBase>(true) != null)
            return RoomType.Challenge;

        // Después por nombre
        string n = prefab.name.ToLowerInvariant();
        if (n.Contains("shop") || n.Contains("tienda")) return RoomType.Shop;
        if (n.Contains("start") || n.Contains("inicio")) return RoomType.Start;
        if (n.Contains("boss") || n.Contains("jefe")) return RoomType.Boss;
        if (n.Contains("challenge") || n.Contains("desafio")) return RoomType.Challenge;

        return RoomType.Normal;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
