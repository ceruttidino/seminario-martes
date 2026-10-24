using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class RoomNavMeshBaker
{
    private const string RoomPrefabsFolder = "Assets/Prefabs/Room Prefabs";
    private const string NavMeshFolder = "Assets/NavMesh/Rooms";
    private const string MenuBakeAll = "Raccgeon/Bakear NavMesh de todas las rooms";
    private const string MenuBakeMissing = "Raccgeon/Bakear NavMesh faltantes";

    [InitializeOnLoadMethod]
    private static void HookPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            BakeRooms(false);
    }

    [MenuItem(MenuBakeAll)]
    public static void BakeAllRooms()
    {
        BakeRooms(true);
    }

    [MenuItem(MenuBakeMissing)]
    public static void BakeMissingRooms()
    {
        BakeRooms(false);
    }

    [MenuItem("Raccgeon/Bakear Enemy_Room_3 y Spike_Room_3")]
    public static void BakeEnemy3AndSpike3()
    {
        string[] paths =
        {
            "Assets/Prefabs/Room Prefabs/Enemy_Room_3.prefab",
            "Assets/Prefabs/Room Prefabs/Difficulty-2/Enemy_Room_3.prefab",
            "Assets/Prefabs/Room Prefabs/Difficulty-3/Enemy_Room_3.prefab",
            "Assets/Prefabs/Room Prefabs/Spike_Room_3.prefab",
            "Assets/Prefabs/Room Prefabs/Difficulty-2/Spike_Room_3.prefab",
            "Assets/Prefabs/Room Prefabs/Difficulty-3/Spike_Room_3.prefab"
        };

        int baked = 0;
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                EditorUtility.DisplayProgressBar("NavMesh", paths[i], (i + 1f) / paths.Length);
                if (BakePrefab(paths[i]))
                    baked++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[NavMesh] Enemy_Room_3 / Spike_Room_3: {baked}/{paths.Length} bakeadas.");
    }

    public static void BakeRooms(bool force)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { RoomPrefabsFolder });
        List<string> paths = new List<string>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<RoomInstance>() != null)
                paths.Add(path);
        }

        if (paths.Count == 0)
        {
            Debug.LogWarning("[NavMesh] No encontré prefabs de RoomInstance.");
            return;
        }

        EnsureFolder(NavMeshFolder);

        int baked = 0;
        int skipped = 0;
        int failed = 0;

        try
        {
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                string name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.DisplayProgressBar(
                    "Bakeando NavMesh de rooms",
                    $"{name} ({i + 1}/{paths.Count})",
                    (i + 1f) / paths.Count);

                if (!force && IsUpToDate(path))
                {
                    skipped++;
                    continue;
                }

                if (BakePrefab(path))
                    baked++;
                else
                    failed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[NavMesh] Bake de rooms terminado. Actualizadas: {baked} · ya estaban: {skipped} · fallaron: {failed}.");
    }

    public static bool BakePrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            RoomBakedNavMesh baked = root.GetComponent<RoomBakedNavMesh>();
            if (baked == null)
                baked = root.AddComponent<RoomBakedNavMesh>();

            if (!BakeComponent(baked, prefabPath))
                return false;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static bool BakeComponent(RoomBakedNavMesh baked, string prefabPath = null)
    {
        if (baked == null)
            return false;

        RoomInstance room = baked.GetComponent<RoomInstance>();
        if (room == null)
            return false;

        if (string.IsNullOrEmpty(prefabPath))
            prefabPath = FindPrefabPath(baked);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("[NavMesh] No pude resolver el prefab de la room.");
            return false;
        }

        string assetPath = ToAssetPath(prefabPath);
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace("\\", "/"));

        RoomBakeSettings settings = baked.ToSettings();
        NavMeshData existing = baked.Data != null
            ? baked.Data
            : AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);

        Bounds interior;
        NavMeshData data;

        if (existing != null && AssetDatabase.GetAssetPath(existing) == assetPath
            && RoomNavMeshBuild.Update(existing, room, settings, out interior))
        {
            data = existing;
            EditorUtility.SetDirty(existing);
        }
        else
        {
            data = RoomNavMeshBuild.Build(room, settings, out interior);
            if (data == null)
            {
                Debug.LogError($"[NavMesh] Falló el bake de '{prefabPath}'.");
                return false;
            }

            data.name = Path.GetFileNameWithoutExtension(assetPath);
            if (existing != null && AssetDatabase.GetAssetPath(existing) == assetPath)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(data, assetPath);
        }

        baked.Assign(AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath), interior);
        EditorUtility.SetDirty(baked);
        Debug.Log($"[NavMesh] '{Path.GetFileNameWithoutExtension(prefabPath)}' · piso {interior.size.x:0.00}x{interior.size.y:0.00}");
        return true;
    }

    public static string FindPrefabPath(Object obj)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && obj is Component component && stage.IsPartOfPrefabContents(component.gameObject))
            return stage.assetPath;

        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            return path;

        if (obj is Component c)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(c))
                return AssetDatabase.GetAssetPath(c.gameObject);

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject);
            if (source != null)
                return AssetDatabase.GetAssetPath(source);
        }

        return null;
    }

    public static string ToAssetPath(string prefabPath)
    {
        string relative = prefabPath.Replace("\\", "/");
        const string prefix = RoomPrefabsFolder + "/";
        if (relative.StartsWith(prefix))
            relative = relative.Substring(prefix.Length);

        relative = relative.Replace("/", "_");
        if (relative.EndsWith(".prefab"))
            relative = relative.Substring(0, relative.Length - ".prefab".Length);

        return NavMeshFolder + "/" + relative + ".asset";
    }

    private static bool IsUpToDate(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        RoomBakedNavMesh baked = prefab != null ? prefab.GetComponent<RoomBakedNavMesh>() : null;
        if (baked == null || !baked.HasData)
            return false;

        string dataPath = AssetDatabase.GetAssetPath(baked.Data);
        return !string.IsNullOrEmpty(dataPath) && File.Exists(ToAbsolute(dataPath));
    }

    private static string ToAbsolute(string assetPath)
    {
        string project = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(project, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string[] parts = assetFolder.Replace("\\", "/").Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

[CustomEditor(typeof(RoomBakedNavMesh))]
public class RoomBakedNavMeshEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();
    private Mesh previewMesh;
    private NavMeshData previewData;

    private void OnDisable()
    {
        if (previewMesh != null)
            DestroyImmediate(previewMesh);
        previewMesh = null;
        previewData = null;
    }

    public override void OnInspectorGUI()
    {
        RoomBakedNavMesh baked = (RoomBakedNavMesh)target;

        EditorGUILayout.HelpBox(
            "1) Abrí el prefab de la room.\n" +
            "2) En la Scene, arrastrá el recuadro VERDE hasta el borde interno de las paredes (sin taparlas).\n" +
            "3) Los recuadros ROJOS son las piedras: tienen que coincidir con su collider.\n" +
            "4) Tocá Bakear NavMesh de esta room.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Recalcular piso desde paredes"))
        {
            Undo.RecordObject(baked, "Recalcular piso NavMesh");
            baked.RecalculateInteriorFromWalls();
            EditorUtility.SetDirty(baked);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Bakear NavMesh de esta room"))
            BakeSelected(baked);
        EditorGUILayout.EndHorizontal();

        if (baked.HasData)
            EditorGUILayout.LabelField("Mesh", baked.Data.name);
        else
            EditorGUILayout.HelpBox("Esta room todavía no tiene NavMesh bakeado.", MessageType.Warning);
    }

    private void OnSceneGUI()
    {
        RoomBakedNavMesh baked = (RoomBakedNavMesh)target;
        Bounds interior = baked.InteriorBounds;

        boundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        boundsHandle.center = interior.center;
        boundsHandle.size = new Vector3(interior.size.x, interior.size.y, 0f);
        boundsHandle.SetColor(new Color(0.2f, 1f, 0.35f, 1f));
        boundsHandle.handleColor = new Color(0.2f, 1f, 0.35f, 1f);
        boundsHandle.wireframeColor = new Color(0.2f, 1f, 0.35f, 1f);

        EditorGUI.BeginChangeCheck();
        boundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(baked, "Ajustar piso NavMesh");
            baked.SetManualInterior(boundsHandle.center, boundsHandle.size);
            EditorUtility.SetDirty(baked);
        }

        Handles.color = new Color(0.15f, 1f, 0.35f, 0.12f);
        Handles.DrawSolidRectangleWithOutline(
            GetRect(interior),
            new Color(0.15f, 1f, 0.35f, 0.08f),
            new Color(0.15f, 1f, 0.35f, 0.95f));

        RoomInstance room = baked.GetComponent<RoomInstance>();
        if (room != null)
        {
            List<Bounds> holes = new List<Bounds>(32);
            RoomNavMeshBuild.CollectPreviewHoles(room, baked.ToSettings(), holes);
            for (int i = 0; i < holes.Count; i++)
            {
                Handles.DrawSolidRectangleWithOutline(
                    GetRect(holes[i]),
                    new Color(1f, 0.15f, 0.1f, 0.12f),
                    new Color(1f, 0.25f, 0.15f, 0.95f));
            }
        }

        DrawBakedMesh(baked);

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 260f, 70f));
        GUILayout.Label("NavMesh: verde = piso, rojo = recorte", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Bakear esta room"))
            BakeSelected(baked);
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void BakeSelected(RoomBakedNavMesh baked)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool inStage = stage != null && stage.IsPartOfPrefabContents(baked.gameObject);

        if (inStage)
        {
            if (RoomNavMeshBaker.BakeComponent(baked, stage.assetPath))
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
                RebuildPreview(baked);
                SceneView.RepaintAll();
            }
            return;
        }

        string path = RoomNavMeshBaker.FindPrefabPath(baked);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog(
                "NavMesh",
                "Abrí el prefab de la room (doble click) y bakeá desde ahí.",
                "OK");
            return;
        }

        if (RoomNavMeshBaker.BakePrefab(path))
        {
            RebuildPreview(baked);
            SceneView.RepaintAll();
        }
    }

    private void DrawBakedMesh(RoomBakedNavMesh baked)
    {
        if (!baked.HasData)
            return;

        if (previewData != baked.Data)
            RebuildPreview(baked);
        if (previewMesh == null || previewMesh.vertexCount < 3)
            return;

        Handles.color = new Color(0.15f, 0.85f, 1f, 0.25f);
        Vector3[] vertices = previewMesh.vertices;
        int[] indices = previewMesh.triangles;
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            Handles.DrawLine(vertices[indices[i]], vertices[indices[i + 1]]);
            Handles.DrawLine(vertices[indices[i + 1]], vertices[indices[i + 2]]);
            Handles.DrawLine(vertices[indices[i + 2]], vertices[indices[i]]);
        }
    }

    private void RebuildPreview(RoomBakedNavMesh baked)
    {
        previewData = baked.Data;
        if (previewMesh == null)
            previewMesh = new Mesh { name = "NavMeshEditorPreview" };
        else
            previewMesh.Clear();

        if (previewData == null)
            return;

        NavMeshDataInstance instance = NavMesh.AddNavMeshData(previewData, Vector3.zero, RoomNavMeshBuild.BakeRotation);
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        instance.Remove();

        if (triangulation.vertices == null || triangulation.indices == null || triangulation.indices.Length < 3)
            return;

        List<Vector3> vertices = new List<Vector3>(triangulation.vertices.Length);
        for (int i = 0; i < triangulation.vertices.Length; i++)
        {
            Vector3 v = triangulation.vertices[i];
            v.z = -0.05f;
            vertices.Add(v);
        }

        previewMesh.SetVertices(vertices);
        previewMesh.SetTriangles(triangulation.indices, 0);
        previewMesh.RecalculateBounds();
    }

    private static Vector3[] GetRect(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        return new[]
        {
            new Vector3(min.x, min.y, 0f),
            new Vector3(max.x, min.y, 0f),
            new Vector3(max.x, max.y, 0f),
            new Vector3(min.x, max.y, 0f)
        };
    }
}
