/*
 * BuildF1Track.cs  —  BWSI Module 6 F1-style track builder
 *
 * Menu: BWSI ▶ Build F1 Track (Module 6)
 *
 * Replaces the simple rectangular track in Module6_LineFollowing with an
 * 8-corner F1-inspired layout:
 *
 *   - 6 right turns + 2 left turns  (net = 4 clockwise = closed loop ✓)
 *   - Long 110 m main straight (N, x = 0)
 *   - 60 m back straight (W, z = -34)
 *   - Two chicane-like left turns (T3 S→E, T6 W→S)
 *   - Track bounds: x ≈ 0–116, z ≈ -70–66
 *
 * Corner geometry uses the WallCurve.obj mesh (smooth 90° arc, 8 m radius).
 * Straight pieces use the RedLine prefab (10 m × 0.4 m flat strip).
 *
 * HOW TO USE:
 *   1. Open the Module6_LineFollowing scene in the Unity Editor.
 *   2. Run  BWSI ▶ Build F1 Track (Module 6)  from the menu bar.
 *   3. The scene is saved automatically.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildF1Track
{
    // ── Asset paths ──────────────────────────────────────────────────────────
    const string WALL_CURVE_PATH  = "Assets/Models/WallCurve.obj";
    const string RED_FLAT_PATH    = "Assets/Materials/FlatTexture/RedFlat.mat";
    const string RED_LINE_PATH    = "Assets/Prefabs/Obstacles/Lines/RedLine.prefab";

    // ── Track Y height (all pieces sit at this world-Y) ──────────────────────
    const float Y = 0.02f;

    [MenuItem("BWSI/Build F1 Track (Module 6)")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.Contains("Module6"))
        {
            Debug.LogError($"[F1Track] Wrong scene active: '{scene.name}'. Open Module6_LineFollowing first.");
            return;
        }

        // ── Load assets ──────────────────────────────────────────────────────
        var wallMesh  = AssetDatabase.LoadAssetAtPath<Mesh>(WALL_CURVE_PATH);
        var redMat    = AssetDatabase.LoadAssetAtPath<Material>(RED_FLAT_PATH);
        var linePrefab= AssetDatabase.LoadAssetAtPath<GameObject>(RED_LINE_PATH);

        if (wallMesh == null)  { Debug.LogError("[F1Track] WallCurve mesh not found at " + WALL_CURVE_PATH);  return; }
        if (redMat   == null)  { Debug.LogError("[F1Track] RedFlat material not found at " + RED_FLAT_PATH);  return; }
        if (linePrefab == null){ Debug.LogError("[F1Track] RedLine prefab not found at " + RED_LINE_PATH);    return; }

        // ── Delete old track objects ──────────────────────────────────────────
        var toDelete = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            string n = go.name;
            if (n.StartsWith("RedLine") || n.StartsWith("RedLineCurve") ||
                n.StartsWith("Corner")  || n.StartsWith("CURVE_INSPECT") ||
                n.StartsWith("F1Track") || n.StartsWith("Track_"))
                toDelete.Add(go);
        }
        foreach (var go in toDelete) Object.DestroyImmediate(go);

        int corners   = 0;
        int straights = 0;

        // ────────────────────────────────────────────────────────────────────
        // CORNERS
        // Each entry: (world_x, world_y, world_z, parent_rotY, isLeftTurn)
        //
        // Right turns — child scale ( 8, 0.01, 8)
        //   Y=180° N→E   Y=270° E→S   Y=0° S→W   Y=90° W→N
        //
        // Left  turns — child scale (-8, 0.01, 8)   (mirrors the arc)
        //   Y=0°  S→E   Y=90° W→S   Y=180° N→W   Y=270° E→N
        // ────────────────────────────────────────────────────────────────────
        var cornerDefs = new (float x, float z, float rotY, bool left, string label)[]
        {
            (  0f,  50f, 180f, false, "T1_NtoE_R"),   // T1  right  N→E
            ( 66f,  58f, 270f, false, "T2_EtoS_R"),   // T2  right  E→S
            ( 74f,  20f,   0f,  true, "T3_StoE_L"),   // T3  LEFT   S→E
            (100f,  12f, 270f, false, "T4_EtoS_R"),   // T4  right  E→S
            (108f, -26f,   0f, false, "T5_StoW_R"),   // T5  right  S→W
            ( 40f, -34f,  90f,  true, "T6_WtoS_L"),   // T6  LEFT   W→S
            ( 32f, -62f,   0f, false, "T7_StoW_R"),   // T7  right  S→W
            (  8f, -70f,  90f, false, "T8_WtoN_R"),   // T8  right  W→N
        };

        foreach (var c in cornerDefs)
        {
            var parent = new GameObject($"Corner_{c.label}");
            parent.transform.position    = new Vector3(c.x, Y, c.z);
            parent.transform.eulerAngles = new Vector3(0f, c.rotY, 0f);

            var child = new GameObject("Mesh");
            child.transform.SetParent(parent.transform, false);
            float sx = c.left ? -8f : 8f;
            child.transform.localScale = new Vector3(sx, 0.01f, 8f);

            child.AddComponent<MeshFilter>().sharedMesh = wallMesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = redMat;

            corners++;
        }

        // ────────────────────────────────────────────────────────────────────
        // STRAIGHT PIECES  (RedLine prefab: 10 m long along its local Z axis)
        //   rot=  0° → piece runs N-S (along world Z)
        //   rot= 90° → piece runs E-W (along world X)
        //
        // Positions are piece centres, spaced 10 m apart along each straight.
        // ────────────────────────────────────────────────────────────────────
        var straightDefs = new (float x, float z, float rotY)[]
        {
            // ── N main straight  (x=0, N-S pieces) ───────────────── 110 m ──
            (  0f, -57f,  0f),
            (  0f, -47f,  0f),
            (  0f, -37f,  0f),
            (  0f, -27f,  0f),
            (  0f, -17f,  0f),
            (  0f,  -7f,  0f),
            (  0f,   3f,  0f),
            (  0f,  13f,  0f),
            (  0f,  23f,  0f),
            (  0f,  33f,  0f),
            (  0f,  43f,  0f),

            // ── E1  (z=58, E-W pieces)  T1 exit → T2 entry ─────────  60 m ──
            ( 13f,  58f, 90f),
            ( 23f,  58f, 90f),
            ( 33f,  58f, 90f),
            ( 43f,  58f, 90f),
            ( 53f,  58f, 90f),
            ( 63f,  58f, 90f),

            // ── S1  (x=74, N-S pieces)  T2 exit → T3 entry ─────────  22 m ──
            ( 74f,  45f,  0f),
            ( 74f,  35f,  0f),

            // ── E2  (z=12, E-W pieces)  T3 exit → T4 entry ─────────  20 m ──
            ( 87f,  12f, 90f),
            ( 97f,  12f, 90f),

            // ── S2  (x=108, N-S pieces) T4 exit → T5 entry ─────────  20 m ──
            (108f,  -1f,  0f),
            (108f, -11f,  0f),

            // ── W1  (z=-34, E-W pieces) T5 exit → T6 entry ─────────  60 m ──
            ( 95f, -34f, 90f),
            ( 85f, -34f, 90f),
            ( 75f, -34f, 90f),
            ( 65f, -34f, 90f),
            ( 55f, -34f, 90f),
            ( 45f, -34f, 90f),

            // ── S3  (x=32, N-S pieces)  T6 exit → T7 entry ─────────  10 m ──
            ( 32f, -47f,  0f),

            // ── W2  (z=-70, E-W pieces) T7 exit → T8 entry ─────────  20 m ──
            ( 19f, -70f, 90f),
            ( 13f, -70f, 90f),
        };

        foreach (var s in straightDefs)
        {
            var go = Object.Instantiate(linePrefab);
            go.name = $"RedLine_S{straights++}";
            go.transform.position    = new Vector3(s.x, Y, s.z);
            go.transform.eulerAngles = new Vector3(0f, s.rotY, 0f);
        }

        // ── LineColorRandomizer manager ───────────────────────────────────────
        // Destroy any existing manager so we don't duplicate it on rebuild
        var existing = GameObject.Find("LineManager");
        if (existing != null) Object.DestroyImmediate(existing);

        var lineManager = new GameObject("LineManager");
        lineManager.AddComponent<LineColorRandomizer>();

        // ── Save ─────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[F1Track] Built: {corners} corners + {straights} straight pieces. Scene saved.");
        EditorUtility.DisplayDialog("F1 Track Built",
            $"Module 6 F1 track complete!\n\n" +
            $"  {corners} smooth arc corners\n" +
            $"  {straights} straight line pieces\n\n" +
            $"Track bounds: x ≈ 0–116 m, z ≈ -70–66 m\n" +
            $"Perimeter: ~380 m\n\n" +
            $"Scene saved.", "OK");
    }
}
