#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time Editor tool that wires autograder task components into the GRG
/// module autograder scenes (Modules 3b, 4, 5, and 6).
///
/// Run via: Tools > GRG > Setup Autograder Scenes
///
/// Module 2  — already wired (AutograderCheckpoint prefabs on all 12 gates).
/// Module 3a — DestinationCylinder placement already correct; 3D object models
///             must be placed manually per variant scene.
/// Module 3b — adds CoordinateThresholdTask: z >= 38 (drone reaches red room).
/// Module 4  — adds ProximityTask: within 3 m of SAR target at (15, 0, 18).
/// Module 5  — adds CoordinateThresholdTask: x >= 16 (drone exits maze).
/// Module 6  — duplicates Module6_LineFollowing as autograder scene, adds three
///             sequential CoordinateThresholdTask checkpoints along the F1 circuit,
///             and registers the new scene in EditorBuildSettings (index 158).
/// </summary>
public static class GRGAutograderSetup
{
    // Scene paths (relative to Assets/)
    private const string Mod3bScene    = "Assets/Scenes/UAV_Neo_Labs/Autograder/Module3b_ArUcoMaze/Mod3b_1_ArUcoMaze.unity";
    private const string Mod4Scene     = "Assets/Scenes/UAV_Neo_Labs/Autograder/Module4_SAR/Mod4_1_SAR.unity";
    private const string Mod5Scene     = "Assets/Scenes/UAV_Neo_Labs/Autograder/Module5_Maze/Mod5_1_Maze.unity";
    private const string Mod6Source    = "Assets/Scenes/UAV_Neo_Labs/Module6_LineFollowing.unity";
    private const string Mod6AgScene   = "Assets/Scenes/UAV_Neo_Labs/Autograder/Module6_LineFollow/Mod6_1_LineFollow.unity";

    /// <summary>
    /// Outcome of a single SetupModX() call. Reported per-module in the summary dialog.
    /// </summary>
    private enum SetupResult
    {
        Applied,    // newly added or created
        Skipped,    // already configured (idempotent skip, not an error)
        Failed      // an actual error occurred (logged via Debug.LogError)
    }

    [MenuItem("Tools/GRG/Setup Autograder Scenes")]
    public static void SetupAll()
    {
        // Warn if any scenes are dirty
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[GRGAutograderSetup] Cancelled — unsaved changes.");
            return;
        }

        SetupResult mod3b = SetupMod3b();
        SetupResult mod4  = SetupMod4();
        SetupResult mod5  = SetupMod5();
        SetupResult mod6  = SetupMod6();

        int applied = 0, skipped = 0, failed = 0;
        foreach (SetupResult r in new[] { mod3b, mod4, mod5, mod6 })
        {
            if (r == SetupResult.Applied)      applied++;
            else if (r == SetupResult.Skipped) skipped++;
            else                               failed++;
        }

        string summary = $"{applied} applied";
        if (skipped > 0) summary += $", {skipped} skipped";
        if (failed > 0)  summary += $", {failed} failed";

        EditorUtility.DisplayDialog(
            "GRG Autograder Setup",
            $"Done. {summary}.\n\n" +
            $"{Sym(mod3b)} Module 3b: CoordinateThresholdTask (z >= 38)\n" +
            $"{Sym(mod4)} Module 4:  ProximityTask (3 m radius at SAR target)\n" +
            $"{Sym(mod5)} Module 5:  CoordinateThresholdTask (x >= 16)\n" +
            $"{Sym(mod6)} Module 6:  3-checkpoint line-follow autograder scene\n\n" +
            "Legend:  ✓ applied   – already configured   ✗ failed (see Console)\n\n" +
            "Module 3a still needs 3D object models placed manually " +
            "(Pineapple, Vase, Hourglass, Cactus, Book).",
            "OK");
    }

    private static string Sym(SetupResult r) =>
        r == SetupResult.Applied ? "✓" :
        r == SetupResult.Skipped ? "–" :
                                   "✗";

    // ── Module 3b — ArUco maze, complete when drone reaches red room (z >= 38) ──

    private static SetupResult SetupMod3b()
    {
        Scene scene = EditorSceneManager.OpenScene(Mod3bScene, OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError($"[GRGAutograderSetup] Could not open {Mod3bScene}"); return SetupResult.Failed; }

        const string taskName = "AutograderTask_ZoneReach";
        if (GameObject.Find(taskName) != null)
        {
            Debug.Log($"[GRGAutograderSetup] {Mod3bScene}: task already exists, skipping.");
            EditorSceneManager.CloseScene(scene, true);
            return SetupResult.Skipped;
        }

        // Place an invisible marker just inside the red room (z=37.5 is the room floor label)
        var go = new GameObject(taskName);
        go.transform.position = new Vector3(0f, 1.5f, 40f);  // centre of red room, 1.5 m altitude

        var task = go.AddComponent<CoordinateThresholdTask>();
        // CoordinateThresholdTask fields: axis=Z, threshold=38, greaterThanOrEqual=true, points=10
        // Set via SerializedObject so [SerializeField] private fields are written correctly
        var so = new SerializedObject(task);
        so.FindProperty("axis").enumValueIndex      = (int)CoordinateThresholdTask.Axis.Z;
        so.FindProperty("threshold").floatValue      = 38f;
        so.FindProperty("greaterThanOrEqual").boolValue = true;
        so.FindProperty("points").floatValue          = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GRGAutograderSetup] {Mod3bScene}: CoordinateThresholdTask added at {go.transform.position}.");
        return SetupResult.Applied;
    }

    // ── Module 4 — SAR, complete when drone is within 3 m of landing pad ──

    private static SetupResult SetupMod4()
    {
        Scene scene = EditorSceneManager.OpenScene(Mod4Scene, OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError($"[GRGAutograderSetup] Could not open {Mod4Scene}"); return SetupResult.Failed; }

        const string taskName = "AutograderTask_Proximity";
        if (GameObject.Find(taskName) != null)
        {
            Debug.Log($"[GRGAutograderSetup] {Mod4Scene}: task already exists, skipping.");
            EditorSceneManager.CloseScene(scene, true);
            return SetupResult.Skipped;
        }

        // SAR target is at (15, 0, 18) — place task at the landing pad centre
        var go = new GameObject(taskName);
        go.transform.position = new Vector3(15f, 0.1f, 18f);

        var task = go.AddComponent<ProximityTask>();
        var so = new SerializedObject(task);
        so.FindProperty("completionRadius").floatValue = 3f;
        so.FindProperty("points").floatValue           = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GRGAutograderSetup] {Mod4Scene}: ProximityTask (3 m radius) added at {go.transform.position}.");
        return SetupResult.Applied;
    }

    // ── Module 5 — Maze, complete when drone exits (x >= 16) ──

    private static SetupResult SetupMod5()
    {
        Scene scene = EditorSceneManager.OpenScene(Mod5Scene, OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError($"[GRGAutograderSetup] Could not open {Mod5Scene}"); return SetupResult.Failed; }

        const string taskName = "AutograderTask_ExitReach";
        if (GameObject.Find(taskName) != null)
        {
            Debug.Log($"[GRGAutograderSetup] {Mod5Scene}: task already exists, skipping.");
            EditorSceneManager.CloseScene(scene, true);
            return SetupResult.Skipped;
        }

        // ExitMarker is at (16, 0.1, 20) — task fires when drone crosses x=16
        var go = new GameObject(taskName);
        go.transform.position = new Vector3(16f, 1.5f, 20f);  // match ExitMarker x/z

        var task = go.AddComponent<CoordinateThresholdTask>();
        var so = new SerializedObject(task);
        so.FindProperty("axis").enumValueIndex         = (int)CoordinateThresholdTask.Axis.X;
        so.FindProperty("threshold").floatValue         = 16f;
        so.FindProperty("greaterThanOrEqual").boolValue  = true;
        so.FindProperty("points").floatValue             = 10f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GRGAutograderSetup] {Mod5Scene}: CoordinateThresholdTask added at {go.transform.position}.");
        return SetupResult.Applied;
    }

    // ── Module 6 — Line Following F1 circuit, 3 sequential checkpoints ──
    //
    //   Task 1 (3 pts):  z >= 45  — drone passed T1 corner (N→E)
    //   Task 2 (3 pts):  x >= 55  — drone reached east side past T2
    //   Task 3 (4 pts):  z <= -25 — drone reached back straight (past T5)
    //   Total: 10 pts, time limit 120 s
    //
    //   The autograder scene is a copy of Module6_LineFollowing.unity with an
    //   AutograderManager parent holding the three task GameObjects as children.

    private static SetupResult SetupMod6()
    {
        // Track whether anything in this run actually changed something on disk.
        bool didAnything = false;

        // ── 1. Duplicate source scene ──────────────────────────────────────────
        if (!System.IO.File.Exists(Mod6Source.Replace("Assets/", Application.dataPath + "/")))
        {
            Debug.LogError($"[GRGAutograderSetup] Source scene not found: {Mod6Source}");
            return SetupResult.Failed;
        }

        if (System.IO.File.Exists(Mod6AgScene.Replace("Assets/", Application.dataPath + "/")))
        {
            Debug.Log($"[GRGAutograderSetup] {Mod6AgScene}: scene already exists, checking for tasks.");
        }
        else
        {
            // Ensure the target directory exists. AssetDatabase.CopyAsset does not
            // create missing folders, so we walk the path and create each segment.
            string targetDir = System.IO.Path.GetDirectoryName(Mod6AgScene).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(targetDir))
            {
                string[] segments = targetDir.Split('/');
                string accum = segments[0]; // "Assets"
                for (int i = 1; i < segments.Length; i++)
                {
                    string next = accum + "/" + segments[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(accum, segments[i]);
                    accum = next;
                }
            }

            bool copied = AssetDatabase.CopyAsset(Mod6Source, Mod6AgScene);
            if (!copied)
            {
                Debug.LogError($"[GRGAutograderSetup] Failed to copy {Mod6Source} → {Mod6AgScene}");
                return SetupResult.Failed;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[GRGAutograderSetup] Duplicated source scene to {Mod6AgScene}.");
            didAnything = true;
        }

        // ── 2. Open the autograder scene and add tasks ─────────────────────────
        Scene scene = EditorSceneManager.OpenScene(Mod6AgScene, OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError($"[GRGAutograderSetup] Could not open {Mod6AgScene}"); return SetupResult.Failed; }

        const string managerName = "AutograderManager_Mod6";
        if (GameObject.Find(managerName) != null)
        {
            Debug.Log($"[GRGAutograderSetup] {Mod6AgScene}: AutograderManager already exists, skipping.");
        }
        else
        {
            // Ensure LineColorRandomizer manager exists (duplicated from source scene)
            if (GameObject.Find("LineManager") == null)
            {
                var lm = new GameObject("LineManager");
                lm.AddComponent<LineColorRandomizer>();
            }

            // Parent GameObject — AutograderManager component finds child tasks via GetComponentsInChildrenOrdered
            var manager = new GameObject(managerName);
            manager.AddComponent<AutograderManager>();

            // Task 1: drone passes first corner (z >= 45)
            AddCoordTask(manager, "Task1_PassFirstCorner",
                new Vector3(0f, 1.5f, 47f),
                CoordinateThresholdTask.Axis.Z, 45f, true, 3f);

            // Task 2: drone reaches east side (x >= 55)
            AddCoordTask(manager, "Task2_ReachEastSide",
                new Vector3(57f, 1.5f, 55f),
                CoordinateThresholdTask.Axis.X, 55f, true, 3f);

            // Task 3: drone reaches back straight (z <= -25)
            AddCoordTask(manager, "Task3_ReachBackStraight",
                new Vector3(80f, 1.5f, -27f),
                CoordinateThresholdTask.Axis.Z, -25f, false, 4f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GRGAutograderSetup] {Mod6AgScene}: AutograderManager + 3 tasks + LineColorRandomizer added.");
            didAnything = true;
        }

        // ── 3. Register scene in EditorBuildSettings ───────────────────────────
        string guid = AssetDatabase.AssetPathToGUID(Mod6AgScene);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[GRGAutograderSetup] Could not get GUID for {Mod6AgScene}");
            return SetupResult.Failed;
        }

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyAdded = false;
        int assignedIndex = -1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].guid.ToString() == guid)
            {
                alreadyAdded = true;
                assignedIndex = i;
                break;
            }
        }

        if (!alreadyAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(Mod6AgScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            assignedIndex = scenes.Count - 1;
            Debug.Log($"[GRGAutograderSetup] {Mod6AgScene} added to Build Settings at index {assignedIndex}.");
            didAnything = true;
        }
        else
        {
            Debug.Log($"[GRGAutograderSetup] {Mod6AgScene} already in Build Settings at index {assignedIndex}.");
        }

        if (assignedIndex != 158)
        {
            Debug.LogWarning($"[GRGAutograderSetup] Expected build index 158 but got {assignedIndex}. " +
                             $"Update LevelCollection.cs: AutograderBuildIndex = {assignedIndex} for Module 6.");
        }

        return didAnything ? SetupResult.Applied : SetupResult.Skipped;
    }

    private static void AddCoordTask(GameObject parent, string name, Vector3 pos,
        CoordinateThresholdTask.Axis axis, float threshold, bool greaterOrEqual, float points)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;

        var task = go.AddComponent<CoordinateThresholdTask>();
        var so = new SerializedObject(task);
        so.FindProperty("axis").enumValueIndex          = (int)axis;
        so.FindProperty("threshold").floatValue          = threshold;
        so.FindProperty("greaterThanOrEqual").boolValue   = greaterOrEqual;
        so.FindProperty("points").floatValue              = points;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
