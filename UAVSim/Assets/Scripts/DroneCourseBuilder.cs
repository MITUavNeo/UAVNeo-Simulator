#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool that populates the active scene with a professional drone racing course.
/// Run from the menu: UAVNeo > 3. Build Drone Racing Course
///
/// OPTIMIZED for laptops: reduced geometry counts, no transparent materials,
/// shared material instances, sphere → cube replacements where possible.
/// </summary>
public class DroneCourseBuilder : MonoBehaviour
{
    // ═══════ Shared material cache ═══════
    static Material nYellow;
    static Material matPillar;
    static Material matFixture;
    static Material matPost;
    static Material matARWhite;
    static Material matARBlack;

    [MenuItem("UAVNeo/3. Build Drone Racing Course")]
    public static void BuildCourse()
    {
        // ── Clean up any previous course ──
        GameObject old = GameObject.Find("DroneCourse");
        if (old != null) DestroyImmediate(old);

        // ── Remove existing scene ground objects to avoid Z-fighting ──
        CleanExistingGround();

        // ── Root container ──
        GameObject root = new GameObject("DroneCourse");
        Undo.RegisterCreatedObjectUndo(root, "Build Drone Course");

        // ═══════════════════════════════════════════════════
        //  MATERIALS
        // ═══════════════════════════════════════════════════
        Material matFloor     = CreateMat("FloorDark",     new Color(0.12f, 0.12f, 0.14f));
        Material matFloorGrid = CreateMat("FloorGrid",     new Color(0.18f, 0.18f, 0.22f), 1);
        Material matConcreteD = CreateMat("ConcreteDark",  new Color(0.22f, 0.22f, 0.25f));
        Material matConcreteL = CreateMat("ConcreteLight", new Color(0.45f, 0.46f, 0.48f));
        Material matMetal     = CreateMat("Metal",         new Color(0.55f, 0.57f, 0.60f));
        Material matBlack     = CreateMat("Black",         new Color(0.08f, 0.08f, 0.10f));
        Material matRed       = CreateMat("Red",           new Color(0.80f, 0.12f, 0.12f));
        Material matBlue      = CreateMat("Blue",          new Color(0.12f, 0.30f, 0.85f));
        Material matOrange    = CreateMat("Orange",        new Color(0.95f, 0.50f, 0.05f));
        Material matYellow    = CreateMat("Yellow",        new Color(0.95f, 0.82f, 0.08f));

        Material nCyan    = CreateEmissiveMat("NeonCyan",    new Color(0.0f, 0.85f, 1.0f),   3f);
        Material nMagenta = CreateEmissiveMat("NeonMagenta", new Color(1.0f, 0.10f, 0.65f),  3f);
        Material nGreen   = CreateEmissiveMat("NeonGreen",   new Color(0.15f, 1.0f, 0.25f),  3f);
        Material nOrange  = CreateEmissiveMat("NeonOrange",  new Color(1.0f, 0.45f, 0.0f),   3f);
        Material nWhite   = CreateEmissiveMat("NeonWhite",   new Color(0.90f, 0.92f, 1.0f),  2f);
        Material nRed     = CreateEmissiveMat("NeonRed",     new Color(1.0f, 0.15f, 0.10f),  2.5f);
        nYellow           = CreateEmissiveMat("NeonYellow",  new Color(1.0f, 0.85f, 0.10f),  2.5f);

        matPillar  = CreateMat("PillarMat",  new Color(0.22f, 0.22f, 0.25f));
        matFixture = CreateMat("FixtureMat", new Color(0.2f, 0.2f, 0.2f));
        matPost    = CreateMat("PostMat",    new Color(0.3f, 0.3f, 0.32f));
        matARWhite = CreateMat("ARWhite",    Color.white);
        matARBlack = CreateMat("ARBlack",    new Color(0.05f, 0.05f, 0.05f));

        // ═══════════════════════════════════════════════════
        //  1. ARENA FLOOR (optimized: fewer grid lines)
        // ═══════════════════════════════════════════════════
        GameObject floorParent = Empty("ArenaFloor", root.transform);

        Box("FloorSlab", floorParent.transform, V(0, -0.15f, 0), Q0, V(300, 0.3f, 300), matFloor);

        // Grid lines — CLIPPED around the line-following area
        float lfXMin = 25f, lfXMax = 95f, lfZMin = -110f, lfZMax = -12f;
        float gridExtent = 120f;

        for (int i = -3; i <= 3; i++)
        {
            float coord = i * 40f;

            if (coord > lfXMin && coord < lfXMax)
            {
                float lenA = lfZMin - (-gridExtent);
                if (lenA > 1f)
                    Box($"GridX_{i}_A", floorParent.transform,
                        V(coord, 0.005f, (-gridExtent + lfZMin) / 2f), Q0,
                        V(0.08f, 0.005f, lenA), matFloorGrid);
                float lenB = gridExtent - lfZMax;
                if (lenB > 1f)
                    Box($"GridX_{i}_B", floorParent.transform,
                        V(coord, 0.005f, (lfZMax + gridExtent) / 2f), Q0,
                        V(0.08f, 0.005f, lenB), matFloorGrid);
            }
            else
            {
                Box($"GridX_{i}", floorParent.transform,
                    V(coord, 0.005f, 0), Q0, V(0.08f, 0.005f, 240), matFloorGrid);
            }

            if (coord > lfZMin && coord < lfZMax)
            {
                float lenA = lfXMin - (-gridExtent);
                if (lenA > 1f)
                    Box($"GridZ_{i}_A", floorParent.transform,
                        V((-gridExtent + lfXMin) / 2f, 0.005f, coord), Q0,
                        V(lenA, 0.005f, 0.08f), matFloorGrid);
                float lenB = gridExtent - lfXMax;
                if (lenB > 1f)
                    Box($"GridZ_{i}_B", floorParent.transform,
                        V((lfXMax + gridExtent) / 2f, 0.005f, coord), Q0,
                        V(lenB, 0.005f, 0.08f), matFloorGrid);
            }
            else
            {
                Box($"GridZ_{i}", floorParent.transform,
                    V(0, 0.005f, coord), Q0, V(240, 0.005f, 0.08f), matFloorGrid);
            }
        }

        float circleRadius = 15f;
        int circleSegs = 12;
        for (int i = 0; i < circleSegs; i++)
        {
            float a1 = (float)i / circleSegs * Mathf.PI * 2;
            float a2 = (float)(i + 1) / circleSegs * Mathf.PI * 2;
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * circleRadius, 0.005f, Mathf.Sin(a1) * circleRadius);
            Vector3 p2 = new Vector3(Mathf.Cos(a2) * circleRadius, 0.005f, Mathf.Sin(a2) * circleRadius);
            Vector3 mid = (p1 + p2) / 2f;
            float segLen = (p2 - p1).magnitude;
            Quaternion rot = Quaternion.LookRotation(p2 - p1, Vector3.up);
            Box($"Circle_{i}", floorParent.transform, mid, rot, V(0.1f, 0.005f, segLen), nCyan);
        }

        Box("LaunchPad", floorParent.transform, V(0, 0.008f, 0), Q0, V(4, 0.005f, 4), matConcreteL);
        Box("LaunchStripe1", floorParent.transform, V(-1.5f, 0.015f, 0), Q0, V(0.12f, 0.005f, 4), nWhite);
        Box("LaunchStripe2", floorParent.transform, V(1.5f, 0.015f, 0), Q0, V(0.12f, 0.005f, 4), nWhite);
        Box("LaunchStripeF", floorParent.transform, V(0, 0.015f, 2f), Q0, V(3, 0.005f, 0.12f), nWhite);
        Box("LaunchStripeB", floorParent.transform, V(0, 0.015f, -2f), Q0, V(3, 0.005f, 0.12f), nWhite);

        // ═══════════════════════════════════════════════════
        //  2. ARENA BOUNDARY (optimized: no transparent nets)
        // ═══════════════════════════════════════════════════
        GameObject boundsParent = Empty("ArenaBoundary", root.transform);
        float arenaHalf = 140f;
        BuildArenaWall(boundsParent.transform, "North", V(0, 0, arenaHalf),  Q(0, 0, 0),   arenaHalf * 2, matConcreteD, matMetal, nCyan, nRed);
        BuildArenaWall(boundsParent.transform, "South", V(0, 0, -arenaHalf), Q(0, 180, 0), arenaHalf * 2, matConcreteD, matMetal, nCyan, nRed);
        BuildArenaWall(boundsParent.transform, "East",  V(arenaHalf, 0, 0),  Q(0, 90, 0),  arenaHalf * 2, matConcreteD, matMetal, nMagenta, nRed);
        BuildArenaWall(boundsParent.transform, "West",  V(-arenaHalf, 0, 0), Q(0, -90, 0), arenaHalf * 2, matConcreteD, matMetal, nMagenta, nRed);

        // ── Invisible collision barriers (prevents drone from leaving the arena) ──
        BuildInvisibleBarriers(boundsParent.transform, arenaHalf, 120f);

        float cornerDist = arenaHalf - 1f;
        Vector3[] corners = {
            V(cornerDist, 0, cornerDist), V(cornerDist, 0, -cornerDist),
            V(-cornerDist, 0, -cornerDist), V(-cornerDist, 0, cornerDist)
        };
        for (int i = 0; i < 4; i++)
            BuildCornerPylon(boundsParent.transform, $"Corner_{i}", corners[i], matConcreteD, matYellow, nRed);

        // ═══════════════════════════════════════════════════
        //  3. RACING GATES (with AR markers on select gates)
        // ═══════════════════════════════════════════════════
        GameObject gatesParent = Empty("RacingGates", root.transform);

        BuildGate(gatesParent.transform, "StartGate",  V(0, 6, 15),  Q0, 10f, 9f, 0.5f, nGreen, matMetal, 0);
        BuildGate(gatesParent.transform, "FinishGate", V(0, 6, -10), Q(0, 180, 0), 10f, 9f, 0.5f, nRed, matMetal, 1);

        BuildGate(gatesParent.transform, "Gate_Low_1", V(0, 5, 40),   Q0, 7f, 6f, 0.35f, nGreen, matConcreteD, 2);
        BuildGate(gatesParent.transform, "Gate_Low_2", V(8, 5, 65),   Q(0, 20, 0), 7f, 6f, 0.35f, nGreen, matConcreteD);
        BuildGate(gatesParent.transform, "Gate_Low_3", V(18, 5, 85),  Q(0, 45, 0), 7f, 6f, 0.35f, nGreen, matConcreteD);

        BuildGate(gatesParent.transform, "Gate_Mid_1", V(30, 12, 100), Q(0, 70, 0), 8f, 7f, 0.35f, nCyan, matConcreteD, 3);
        BuildGate(gatesParent.transform, "Gate_Mid_2", V(40, 18, 90),  Q(0, 120, 0), 8f, 7f, 0.35f, nCyan, matConcreteD);
        BuildGate(gatesParent.transform, "Gate_Mid_3", V(45, 24, 70),  Q(0, 160, 0), 8f, 7f, 0.35f, nCyan, matConcreteD);

        BuildGate(gatesParent.transform, "Gate_High_1", V(40, 35, 50), Q(0, 200, 0), 9f, 8f, 0.4f, nMagenta, matConcreteD, 4);
        BuildGate(gatesParent.transform, "Gate_High_2", V(25, 28, 30), Q(0, 230, 0), 9f, 8f, 0.4f, nMagenta, matConcreteD);
        BuildGate(gatesParent.transform, "Gate_High_3", V(5, 18, 15),  Q(0, 260, 0), 9f, 8f, 0.4f, nMagenta, matConcreteD);

        BuildGate(gatesParent.transform, "Gate_Tilt_1", V(-20, 10, 50),  Q(0, 0, 15), 7f, 7f, 0.35f, nOrange, matConcreteD);
        BuildGate(gatesParent.transform, "Gate_Tilt_2", V(-20, 15, 75),  Q(0, 0, -15), 7f, 7f, 0.35f, nOrange, matConcreteD);
        BuildGate(gatesParent.transform, "Gate_Tilt_3", V(-20, 20, 100), Q(15, 0, 0), 7f, 7f, 0.35f, nOrange, matConcreteD);

        // ═══════════════════════════════════════════════════
        //  4. TOWERS (reduced count, cube beacons)
        // ═══════════════════════════════════════════════════
        GameObject towersParent = Empty("Towers", root.transform);

        BuildTower(towersParent.transform, "Tower_A", V(60, 0, 40), 40f, 2.5f, matConcreteD, nRed);
        BuildTower(towersParent.transform, "Tower_B", V(70, 0, 55), 35f, 2.5f, matConcreteD, nCyan);
        BuildTower(towersParent.transform, "Tower_C", V(55, 0, 65), 45f, 2.5f, matConcreteD, nGreen);

        for (int i = 0; i < 4; i++)
        {
            float z = -30 - i * 20;
            float x = (i % 2 == 0) ? -5 : 5;
            BuildTower(towersParent.transform, $"SpeedPylon_{i}", V(x, 0, z), 20f, 1.0f, matBlack, nCyan);
        }

        // ═══════════════════════════════════════════════════
        //  5. LANDING PLATFORMS
        // ═══════════════════════════════════════════════════
        GameObject platformsParent = Empty("Platforms", root.transform);

        BuildLandingPad(platformsParent.transform, "Pad_Ground", V(-40, 0.05f, 0), 12f, matConcreteL, nGreen, 0f);
        BuildLandingPad(platformsParent.transform, "Pad_Low",  V(-55, 8, 20), 10f, matConcreteL, nYellow, 8f);
        BuildLandingPad(platformsParent.transform, "Pad_Mid",  V(-55, 18, 45), 10f, matConcreteL, nOrange, 18f);
        BuildLandingPad(platformsParent.transform, "Pad_High", V(-55, 30, 70), 10f, matConcreteL, nRed, 30f);

        // ═══════════════════════════════════════════════════
        //  6. TUNNEL (reduced segments)
        // ═══════════════════════════════════════════════════
        GameObject tunnelParent = Empty("Tunnel", root.transform);
        BuildTunnel(tunnelParent.transform, V(-30, 5, -50), Q0, 60f, 8f, 7f, 4, matConcreteD, nCyan);

        // ═══════════════════════════════════════════════════
        //  7. AERIAL SLALOM (reduced)
        // ═══════════════════════════════════════════════════
        GameObject slalomParent = Empty("AerialSlalom", root.transform);
        for (int i = 0; i < 6; i++)
        {
            float z = 20 + i * 15;
            float x = 85 + ((i % 2 == 0) ? -6 : 6);
            float y = 8 + i * 2f;
            Material coneMat = (i % 2 == 0) ? matRed : matBlue;
            Material coneLight = (i % 2 == 0) ? nRed : nCyan;
            BuildSlalomFlag(slalomParent.transform, $"Flag_{i}", V(x, 0, z), y, coneMat, coneLight);
        }

        // ═══════════════════════════════════════════════════
        //  8. VERTICAL CLIMB CHIMNEY (reduced platforms)
        // ═══════════════════════════════════════════════════
        GameObject chimneyParent = Empty("ClimbChimney", root.transform);
        BuildChimney(chimneyParent.transform, V(-80, 0, 50), 60f, 10f, 5, matConcreteD, nGreen);

        // ═══════════════════════════════════════════════════
        //  9. SPEED CORRIDOR
        // ═══════════════════════════════════════════════════
        GameObject corridorParent = Empty("SpeedCorridor", root.transform);
        BuildSpeedCorridor(corridorParent.transform, V(0, 10, -80), Q0, 100f, 6f, 6f, matConcreteD, nOrange);

        // ═══════════════════════════════════════════════════
        // 10. FIGURE-8 LOOP (reduced gates)
        // ═══════════════════════════════════════════════════
        GameObject fig8Parent = Empty("Figure8", root.transform);
        int ringCount = 10;
        float loopR = 25f;
        float fig8Y = 20f;
        for (int i = 0; i < ringCount; i++)
        {
            float t = (float)i / ringCount * Mathf.PI * 2;
            float d = 1 + Mathf.Sin(t) * Mathf.Sin(t);
            float lx = loopR * Mathf.Cos(t) / d;
            float lz = loopR * Mathf.Sin(t) * Mathf.Cos(t) / d;
            Vector3 pos = V(lx - 80, fig8Y + Mathf.Sin(t * 2) * 4f, lz - 20);

            float t2 = (float)(i + 1) / ringCount * Mathf.PI * 2;
            float d2 = 1 + Mathf.Sin(t2) * Mathf.Sin(t2);
            float nx = loopR * Mathf.Cos(t2) / d2;
            float nz = loopR * Mathf.Sin(t2) * Mathf.Cos(t2) / d2;
            Vector3 next = V(nx - 80, fig8Y + Mathf.Sin(t2 * 2) * 4f, nz - 20);

            Vector3 dir = (next - pos).normalized;
            Quaternion rot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : Q0;
            Material ringMat = (i % 2 == 0) ? nCyan : nMagenta;
            BuildGate(fig8Parent.transform, $"Fig8_{i}", pos, rot, 6f, 6f, 0.3f, ringMat, matConcreteD);
        }

        // ═══════════════════════════════════════════════════
        // 11. LINE FOLLOWING COURSE (for RGB down-cam testing)
        // ═══════════════════════════════════════════════════
        GameObject linesParent = Empty("LineFollowingCourse", root.transform);
        BuildLineFollowingCourse(linesParent.transform, matFloor);

        // ═══════════════════════════════════════════════════
        // 12. DECORATIONS (reduced)
        // ═══════════════════════════════════════════════════
        GameObject decoParent = Empty("Decorations", root.transform);
        System.Random rng = new System.Random(42);
        for (int i = 0; i < 10; i++)
        {
            float cx = (float)(rng.NextDouble() * 200 - 100);
            float cz = (float)(rng.NextDouble() * 200 - 100);
            float cs = 1f + (float)rng.NextDouble() * 2f;
            Material cm = (i % 3 == 0) ? matOrange : (i % 3 == 1) ? matConcreteD : matMetal;
            Box($"Crate_{i}", decoParent.transform, V(cx, cs / 2, cz),
                Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0), V(cs, cs, cs), cm);
        }

        // ═══════════════════════════════════════════════════
        // 13. LIGHTING SETUP
        // ═══════════════════════════════════════════════════
        SetupLighting(root.transform);

        // ── Done ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[DroneCourseBuilder] Professional drone racing arena built (optimized).");
    }

    // ════════════════════════════════════════════════════════════
    //  INVISIBLE BARRIERS (collision-only walls + ceiling)
    // ════════════════════════════════════════════════════════════

    static void BuildInvisibleBarriers(Transform parent, float halfSize, float height)
    {
        float thickness = 2f;
        float fullLen = halfSize * 2f + thickness * 2f;

        CreateBarrier(parent, "Barrier_N", V(0, height / 2f, halfSize + thickness / 2f),  V(fullLen, height, thickness));
        CreateBarrier(parent, "Barrier_S", V(0, height / 2f, -halfSize - thickness / 2f), V(fullLen, height, thickness));
        CreateBarrier(parent, "Barrier_E", V(halfSize + thickness / 2f, height / 2f, 0),  V(thickness, height, fullLen));
        CreateBarrier(parent, "Barrier_W", V(-halfSize - thickness / 2f, height / 2f, 0), V(thickness, height, fullLen));
        CreateBarrier(parent, "Barrier_Ceil", V(0, height, 0), V(fullLen, thickness, fullLen));
    }

    static void CreateBarrier(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = size;
        go.layer = 0;
    }

    // ════════════════════════════════════════════════════════════
    //  ARENA WALL (optimized: fewer posts, no transparent nets)
    // ════════════════════════════════════════════════════════════
    static void BuildArenaWall(Transform parent, string name, Vector3 pos, Quaternion rot,
        float length, Material concreteMat, Material metalMat,
        Material accentNeon, Material warningNeon)
    {
        GameObject wall = Empty(name, parent);
        wall.transform.position = pos;
        wall.transform.rotation = rot;

        float halfLen = length / 2f;

        Box("Base", wall.transform, V(0, 0.6f, 0), Q0, V(length, 1.2f, 0.6f), concreteMat);

        int postCount = Mathf.CeilToInt(length / 20f) + 1;
        float postSpacing = length / (postCount - 1);
        for (int i = 0; i < postCount; i++)
        {
            float x = -halfLen + i * postSpacing;
            Box($"Post_{i}", wall.transform, V(x, 4f, 0), Q0, V(0.15f, 8f, 0.15f), metalMat);
        }

        Box("TopRail", wall.transform, V(0, 8f, 0), Q0, V(length, 0.12f, 0.12f), metalMat);

        Box("AccentTop", wall.transform, V(0, 8.15f, 0), Q0, V(length, 0.08f, 0.08f), accentNeon);
        Box("AccentBase", wall.transform, V(0, 1.25f, 0.25f), Q0, V(length, 0.06f, 0.06f), accentNeon);

        int chevronCount = Mathf.FloorToInt(length / 40f);
        for (int i = 0; i < chevronCount; i++)
        {
            float x = -halfLen + 20f + i * 40f;
            Box($"Chevron_{i}", wall.transform, V(x, 0.3f, 0.32f), Q0, V(1.5f, 0.3f, 0.04f), warningNeon);
        }
    }

    static void BuildCornerPylon(Transform parent, string name, Vector3 pos,
        Material bodyMat, Material stripeMat, Material beaconMat)
    {
        GameObject pylon = Empty(name, parent);
        pylon.transform.position = pos;

        Box("Body", pylon.transform, V(0, 5, 0), Q0, V(2.5f, 10, 2.5f), bodyMat);

        for (int i = 0; i < 3; i++)
        {
            float y = 2f + i * 3f;
            Box($"Stripe_{i}", pylon.transform, V(0, y, 1.28f), Q0, V(2.2f, 0.25f, 0.04f), stripeMat);
        }

        Box("Beacon", pylon.transform, V(0, 11, 0), Q0, V(1.5f, 1.5f, 1.5f), beaconMat);
    }

    // ════════════════════════════════════════════════════════════
    //  GATE (with optional AR markers at corners)
    // ════════════════════════════════════════════════════════════
    /// <param name="arMarkerId">ArUco marker ID to place at all 4 corners
    /// (same ID on every corner of this gate, both faces). -1 = no markers.</param>
    static void BuildGate(Transform parent, string name, Vector3 pos, Quaternion rot,
        float w, float h, float thickness, Material neonMat, Material structMat,
        int arMarkerId = -1)
    {
        GameObject gate = Empty(name, parent);
        gate.transform.position = pos;
        gate.transform.rotation = rot;

        float hw = w / 2f;
        float hh = h / 2f;
        float t = thickness;
        float nt = 0.08f;

        Box("PillarL", gate.transform, V(-hw, 0, 0), Q0, V(t, h, t), structMat);
        Box("PillarR", gate.transform, V(hw, 0, 0), Q0, V(t, h, t), structMat);
        Box("BeamTop", gate.transform, V(0, hh, 0), Q0, V(w + t, t, t), structMat);
        Box("BeamBot", gate.transform, V(0, -hh, 0), Q0, V(w + t, t, t), structMat);

        Box("NeonL", gate.transform, V(-hw + t / 2 + nt / 2, 0, 0), Q0, V(nt, h - t, nt), neonMat);
        Box("NeonR", gate.transform, V(hw - t / 2 - nt / 2, 0, 0), Q0, V(nt, h - t, nt), neonMat);
        Box("NeonT", gate.transform, V(0, hh - t / 2 - nt / 2, 0), Q0, V(w - t, nt, nt), neonMat);
        Box("NeonB", gate.transform, V(0, -hh + t / 2 + nt / 2, 0), Q0, V(w - t, nt, nt), neonMat);

        // 5×5 AR markers at all 4 inner corners, on BOTH faces of the gate.
        // Every corner of this gate shows the SAME marker ID so the gate
        // can be uniquely identified regardless of which corner is visible.
        // Both faces use the same pattern for bi-directional course traversal.
        if (arMarkerId >= 0 && arMarkerId < arMarkerPatterns.Length)
        {
            int[,] pattern = arMarkerPatterns[arMarkerId];

            float mSize = 0.70f;
            float mOffset = 0.04f;

            Vector3[] cornerXY = {
                V(-hw + t/2 + mSize/2, hh - t/2 - mSize/2, 0),
                V( hw - t/2 - mSize/2, hh - t/2 - mSize/2, 0),
                V(-hw + t/2 + mSize/2, -hh + t/2 + mSize/2, 0),
                V( hw - t/2 - mSize/2, -hh + t/2 + mSize/2, 0),
            };

            // Front face layers stack +Z (outward), back face layers stack -Z (outward).
            float[] zFaces = { mOffset, -mOffset };
            float[] zSigns = { 1f, -1f };
            for (int face = 0; face < 2; face++)
            {
                for (int c = 0; c < 4; c++)
                {
                    Vector3 markerPos = cornerXY[c] + V(0, 0, zFaces[face]);
                    Build5x5Marker(gate.transform, $"AR_id{arMarkerId}_f{face}_c{c}", markerPos, mSize,
                        pattern, matARWhite, matARBlack, zSigns[face]);
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  5×5 AR MARKER BUILDER
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Real ArUco DICT_5X5_50 marker patterns (IDs 0–4), one per gate.
    /// Generated from OpenCV: cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_50)
    /// Reference: https://chev.me/arucogen/ (Dictionary: 5×5, IDs 0–4)
    /// 1 = white cell, 0 = black cell.  Detectable by any ArUco detector using DICT_5X5_50.
    /// All 4 corners of a gate share the same ID; different gates use different IDs.
    /// </summary>
    static readonly int[][,] arMarkerPatterns = {
        new int[,] { // ArUco DICT_5X5_50  ID 0
            {1,0,1,0,0},
            {0,1,0,1,1},
            {0,1,1,0,0},
            {1,0,1,0,1},
            {1,1,1,0,0},
        },
        new int[,] { // ArUco DICT_5X5_50  ID 1
            {0,0,0,0,1},
            {1,1,0,0,0},
            {0,0,0,0,1},
            {1,0,1,1,1},
            {0,0,1,1,0},
        },
        new int[,] { // ArUco DICT_5X5_50  ID 2
            {1,1,0,1,0},
            {1,1,1,1,0},
            {0,0,0,1,1},
            {1,0,1,1,0},
            {1,1,1,0,1},
        },
        new int[,] { // ArUco DICT_5X5_50  ID 3
            {1,0,0,0,0},
            {0,0,1,1,1},
            {0,0,1,0,1},
            {0,1,1,1,1},
            {1,0,1,1,1},
        },
        new int[,] { // ArUco DICT_5X5_50  ID 4
            {1,1,0,1,0},
            {1,1,1,0,1},
            {0,1,1,0,1},
            {0,1,0,0,1},
            {0,0,1,0,0},
        },
    };

    /// <summary>
    /// zSign controls which direction the layers stack: +1 for front face, -1 for back face.
    /// </summary>
    static void Build5x5Marker(Transform parent, string name, Vector3 pos,
        float totalSize, int[,] pattern, Material whiteMat, Material blackMat,
        float zSign = 1f)
    {
        float cell = totalSize / 7f;
        float depth = 0.015f;

        Box($"{name}_bg", parent, pos, Q0, V(totalSize + 0.04f, totalSize + 0.04f, depth), whiteMat);
        Box($"{name}_body", parent, pos + V(0, 0, zSign * depth * 0.5f), Q0,
            V(totalSize, totalSize, depth), blackMat);

        float cellZ = zSign * depth * 1.1f;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (pattern[row, col] == 1)
                {
                    float cx = (col - 2) * cell;
                    float cy = (2 - row) * cell;
                    Box($"{name}_d{row}{col}", parent,
                        pos + V(cx, cy, cellZ),
                        Q0, V(cell * 0.92f, cell * 0.92f, depth), whiteMat);
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  TOWER (cube beacon instead of sphere)
    // ════════════════════════════════════════════════════════════
    static void BuildTower(Transform parent, string name, Vector3 basePos, float height,
        float radius, Material bodyMat, Material beaconMat)
    {
        GameObject tower = Empty(name, parent);
        tower.transform.position = basePos;

        Cyl("Body", tower.transform, V(0, height / 2, 0), Q0, radius * 2, height, bodyMat);

        int rings = Mathf.Max(1, Mathf.FloorToInt(height / 15f));
        for (int i = 0; i <= rings; i++)
        {
            float y = (i + 1) * (height / (rings + 1));
            Cyl($"Ring_{i}", tower.transform, V(0, y, 0), Q0, radius * 2.3f, 0.15f, beaconMat);
        }

        Box("Beacon", tower.transform, V(0, height + radius * 0.5f, 0), Q0,
            V(radius * 2f, radius * 1f, radius * 2f), beaconMat);
    }

    // ════════════════════════════════════════════════════════════
    //  LANDING PAD (shared pillar material)
    // ════════════════════════════════════════════════════════════
    static void BuildLandingPad(Transform parent, string name, Vector3 pos, float size,
        Material surfMat, Material edgeNeon, float pillarHeight)
    {
        GameObject pad = Empty(name, parent);
        pad.transform.position = pos;

        float thick = 0.35f;
        Box("Surface", pad.transform, V3(0), Q0, V(size, thick, size), surfMat);

        float hs = size / 2f;
        float ny = thick / 2 + 0.02f;
        Box("EdgeN", pad.transform, V(0, ny, hs - 0.15f), Q0, V(size, 0.04f, 0.3f), edgeNeon);
        Box("EdgeS", pad.transform, V(0, ny, -hs + 0.15f), Q0, V(size, 0.04f, 0.3f), edgeNeon);
        Box("EdgeE", pad.transform, V(hs - 0.15f, ny, 0), Q0, V(0.3f, 0.04f, size), edgeNeon);
        Box("EdgeW", pad.transform, V(-hs + 0.15f, ny, 0), Q0, V(0.3f, 0.04f, size), edgeNeon);

        Box("CrossH", pad.transform, V(0, ny, 0), Q0, V(size * 0.6f, 0.02f, 0.2f), edgeNeon);
        Box("CrossV", pad.transform, V(0, ny, 0), Q0, V(0.2f, 0.02f, size * 0.6f), edgeNeon);

        if (pillarHeight > 1f)
        {
            Cyl("Pillar", pad.transform, V(0, -pillarHeight / 2f, 0), Q0, 1.8f, pillarHeight, matPillar);
        }
    }

    static void BuildSlalomFlag(Transform parent, string name, Vector3 basePos, float height,
        Material poleMat, Material bandMat)
    {
        GameObject flag = Empty(name, parent);
        flag.transform.position = basePos;

        Cyl("Pole", flag.transform, V(0, height / 2, 0), Q0, 0.5f, height, poleMat);

        int bands = Mathf.Max(1, Mathf.FloorToInt(height / 8f));
        for (int i = 0; i < bands; i++)
        {
            float y = (i + 1) * (height / (bands + 1));
            Cyl($"Band_{i}", flag.transform, V(0, y, 0), Q0, 0.8f, 0.3f, bandMat);
        }

        Box("Top", flag.transform, V(0, height + 0.3f, 0), Q0, V(0.7f, 0.7f, 0.7f), bandMat);
    }

    static void BuildTunnel(Transform parent, Vector3 startPos, Quaternion rot,
        float length, float width, float height, int segments, Material wallMat, Material lightMat)
    {
        GameObject tunnel = Empty("TunnelStructure", parent);
        tunnel.transform.position = startPos;
        tunnel.transform.rotation = rot;

        float segLen = length / segments;
        float wallThick = 0.4f;

        for (int i = 0; i < segments; i++)
        {
            float z = i * segLen + segLen / 2;
            string p = $"S{i}";

            Box($"{p}_Floor", tunnel.transform, V(0, 0, z), Q0, V(width, wallThick, segLen - 0.2f), wallMat);
            Box($"{p}_Ceil",  tunnel.transform, V(0, height, z), Q0, V(width, wallThick, segLen - 0.2f), wallMat);
            Box($"{p}_L",     tunnel.transform, V(-width / 2, height / 2, z), Q0, V(wallThick, height, segLen - 0.2f), wallMat);
            Box($"{p}_R",     tunnel.transform, V(width / 2, height / 2, z), Q0, V(wallThick, height, segLen - 0.2f), wallMat);

            if (i % 2 == 0)
                Box($"{p}_LED", tunnel.transform, V(0, height - wallThick - 0.1f, z), Q0, V(0.3f, 0.12f, segLen * 0.8f), lightMat);
        }
    }

    static void BuildChimney(Transform parent, Vector3 basePos, float height, float radius,
        int platformCount, Material wallMat, Material platformMat)
    {
        GameObject chimney = Empty("ChimneyStructure", parent);
        chimney.transform.position = basePos;

        float offset = radius * 0.7f;
        Vector3[] cn = {
            V(-offset, 0, -offset), V(offset, 0, -offset),
            V(offset, 0, offset), V(-offset, 0, offset)
        };
        for (int i = 0; i < 4; i++)
            Cyl($"Pillar_{i}", chimney.transform, cn[i] + V(0, height / 2, 0), Q0, 1f, height, wallMat);

        for (int i = 0; i < platformCount; i++)
        {
            float y = (height / (platformCount + 1)) * (i + 1);
            float px = (i % 2 == 0) ? -radius * 0.3f : radius * 0.3f;
            Box($"Plat_{i}", chimney.transform, V(px, y, 0), Q0, V(radius * 1.2f, 0.3f, radius * 1.2f), platformMat);
        }

        BuildGate(chimney.transform, "TopGate", V(0, height + 3, 0), Q0,
            radius * 1.5f, radius * 1.5f, 0.35f, platformMat, wallMat);
    }

    static void BuildSpeedCorridor(Transform parent, Vector3 startPos, Quaternion rot,
        float length, float width, float height, Material wallMat, Material lightMat)
    {
        GameObject corridor = Empty("CorridorStructure", parent);
        corridor.transform.position = startPos;
        corridor.transform.rotation = rot;

        int segs = Mathf.CeilToInt(length / 15f);
        float segLen = length / segs;

        for (int i = 0; i < segs; i++)
        {
            float z = -length / 2 + i * segLen + segLen / 2;
            string p = $"CS{i}";

            Box($"{p}_L", corridor.transform, V(-width / 2, height / 2, z), Q0, V(0.3f, height, segLen - 0.1f), wallMat);
            Box($"{p}_R", corridor.transform, V(width / 2, height / 2, z), Q0, V(0.3f, height, segLen - 0.1f), wallMat);

            if (i % 2 == 0)
                Box($"{p}_LL", corridor.transform, V(-width / 2 + 0.2f, height * 0.7f, z), Q0, V(0.08f, 0.15f, segLen * 0.7f), lightMat);
        }

        BuildGate(corridor.transform, "Entry", V(0, height / 2, -length / 2), Q0,
            width + 0.5f, height + 0.5f, 0.4f, lightMat, wallMat);
        BuildGate(corridor.transform, "Exit", V(0, height / 2, length / 2), Q0,
            width + 0.5f, height + 0.5f, 0.4f, lightMat, wallMat);
    }

    static void SetupLighting(Transform root)
    {
        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.color = new Color(0.35f, 0.40f, 0.55f);
                l.intensity = 0.6f;
                l.transform.rotation = Quaternion.Euler(35, -30, 0);
                l.shadows = LightShadows.Hard;
                l.shadowBias = 0.05f;
                l.shadowNormalBias = 0.4f;
            }
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.14f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 250f;

        RenderSettings.skybox = null;
        foreach (Camera cam in Object.FindObjectsOfType<Camera>())
        {
            if (cam.clearFlags == CameraClearFlags.Skybox)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            }
        }

        GameObject lightsParent = Empty("StadiumLights", root);
        Vector3[] floodPos = {
            V(120, 40, 120), V(120, 40, -120),
            V(-120, 40, -120), V(-120, 40, 120)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject floodObj = Empty($"FloodLight_{i}", lightsParent.transform);
            floodObj.transform.position = floodPos[i];
            floodObj.transform.LookAt(Vector3.zero);

            Light flood = floodObj.AddComponent<Light>();
            flood.type = LightType.Spot;
            flood.color = new Color(0.85f, 0.90f, 1.0f);
            flood.intensity = 3.5f;
            flood.range = 200f;
            flood.spotAngle = 90f;
            flood.shadows = LightShadows.None;
            flood.renderMode = LightRenderMode.Auto;

            Box("Fixture", floodObj.transform, V3(0), Q0, V(2, 1, 2), matFixture);

            Cyl($"LightPost_{i}", lightsParent.transform, V(floodPos[i].x, 20, floodPos[i].z), Q0, 0.5f, 40f, matPost);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LINE FOLLOWING COURSE (coloured tape on the floor)
    //  Every course is a CONNECTED path built from waypoints.
    // ════════════════════════════════════════════════════════════

    static void BuildLineFollowingCourse(Transform parent, Material floorMat)
    {
        // Unlit materials — bypass lighting pipeline entirely to eliminate flicker
        Material padMat     = CreateUnlitMat("LinePad",    new Color(0.30f, 0.30f, 0.32f), 5);
        Material tapeWhite  = CreateUnlitMat("TapeWhite",  Color.white, 10);
        Material tapeBlue   = CreateUnlitMat("TapeBlue",   new Color(0.10f, 0.35f, 0.95f), 10);
        Material tapeGreen  = CreateUnlitMat("TapeGreen",  new Color(0.10f, 0.85f, 0.20f), 10);
        Material tapeRed    = CreateUnlitMat("TapeRed",    new Color(0.90f, 0.10f, 0.10f), 10);
        Material tapeYellow = CreateUnlitMat("TapeYellow", new Color(0.95f, 0.85f, 0.10f), 10);

        float tapeW = 0.15f, tapeH = 0.008f;
        float padY = 0.10f;  // pad centre Y
        float y    = 0.20f;  // tape centre Y (well above pad top)

        // COURSE A — Blue oval loop (closed)
        {
            Vector3 c = V(50f, 0f, -35f);
            UnlitBox("Pad_A", parent, V(c.x, padY, c.z), Q0, V(38, 0.02f, 24), padMat);
            var pts = new System.Collections.Generic.List<Vector3>();
            float halfL = 10f, R = 8f;
            pts.Add(V(c.x - halfL, y, c.z + R));
            pts.Add(V(c.x + halfL, y, c.z + R));
            ArcPoints(pts, V(c.x + halfL, y, c.z), R, 90f, -90f, 12);
            pts.Add(V(c.x - halfL, y, c.z - R));
            ArcPoints(pts, V(c.x - halfL, y, c.z), R, -90f, 90f, 12);
            pts.Add(pts[0]);
            BuildConnectedPath(parent, "Course_A_Blue", pts, tapeW, tapeH, tapeBlue);
        }

        // COURSE B — Green figure-8 (two loops to avoid self-crossing Z-fight)
        {
            Vector3 c = V(50f, 0f, -65f);
            UnlitBox("Pad_B", parent, V(c.x, padY, c.z), Q0, V(32, 0.02f, 18), padMat);
            float R = 6f;
            var ptsR = new System.Collections.Generic.List<Vector3>();
            ArcPoints(ptsR, V(c.x + R, y, c.z), R, 180f, -180f, 20);
            ptsR.Add(ptsR[0]);
            BuildConnectedPath(parent, "Course_B_Green_R", ptsR, tapeW, tapeH, tapeGreen);

            float y2 = y + 0.02f;
            var ptsL = new System.Collections.Generic.List<Vector3>();
            ArcPoints(ptsL, V(c.x - R, y2, c.z), R, 0f, 360f, 20);
            ptsL.Add(ptsL[0]);
            BuildConnectedPath(parent, "Course_B_Green_L", ptsL, tapeW, tapeH, tapeGreen);
        }

        // COURSE C — Red zigzag (connected)
        {
            Vector3 c = V(80f, 0f, -35f);
            UnlitBox("Pad_C", parent, V(c.x, padY, c.z), Q0, V(14, 0.02f, 30), padMat);
            float zigAmp = 4f, zigStep = 4f; int zigCount = 8;
            float startZ2 = c.z - (zigCount * zigStep) / 2f;
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= zigCount; i++)
            {
                float zz = startZ2 + i * zigStep;
                float xx = c.x + ((i % 2 == 0) ? -zigAmp / 2f : zigAmp / 2f);
                pts.Add(V(xx, y, zz));
            }
            BuildConnectedPath(parent, "Course_C_Red", pts, tapeW, tapeH, tapeRed);
        }

        // COURSE D — White closed racetrack with chicane
        {
            Vector3 c = V(50f, 0f, -92f);
            UnlitBox("Pad_D", parent, V(c.x, padY, c.z), Q0, V(36, 0.02f, 20), padMat);
            float W = 14f, H = 8f, R = 3f;
            var pts = new System.Collections.Generic.List<Vector3>();
            pts.Add(V(c.x - W, y, c.z - H));
            pts.Add(V(c.x + W, y, c.z - H));
            ArcPoints(pts, V(c.x + W, y, c.z - H + R), R, -90f, 0f, 6);
            pts.Add(V(c.x + W + R, y, c.z + H));
            ArcPoints(pts, V(c.x + W, y, c.z + H), R, 0f, 90f, 6);
            pts.Add(V(c.x + 4f, y, c.z + H + R));
            pts.Add(V(c.x + 2f, y, c.z + H + R - 2f));
            pts.Add(V(c.x - 2f, y, c.z + H + R + 2f));
            pts.Add(V(c.x - 4f, y, c.z + H + R));
            pts.Add(V(c.x - W, y, c.z + H + R));
            ArcPoints(pts, V(c.x - W, y, c.z + H), R, 90f, 180f, 6);
            pts.Add(V(c.x - W - R, y, c.z - H));
            ArcPoints(pts, V(c.x - W, y, c.z - H), R, 180f, 270f, 6);
            pts.Add(pts[0]);
            BuildConnectedPath(parent, "Course_D_White", pts, tapeW, tapeH, tapeWhite);
        }

        // COURSE E — Yellow S-curve (connected)
        {
            Vector3 c = V(80f, 0f, -65f);
            UnlitBox("Pad_E", parent, V(c.x, padY, c.z), Q0, V(16, 0.02f, 24), padMat);
            float R = 4f;
            var pts = new System.Collections.Generic.List<Vector3>();
            pts.Add(V(c.x, y, c.z - 10f));
            pts.Add(V(c.x, y, c.z - R));
            ArcPoints(pts, V(c.x + R, y, c.z - R), R, 180f, 0f, 10);
            ArcPoints(pts, V(c.x + R, y, c.z + R), R, 0f, 180f, 10);
            pts.Add(V(c.x, y, c.z + 10f));
            BuildConnectedPath(parent, "Course_E_Yellow", pts, tapeW, tapeH, tapeYellow);
        }

        // COURSE F — Multi-line intersection (4 crossing lines)
        {
            Vector3 c = V(65f, 0f, -50f);
            UnlitBox("Pad_F", parent, V(c.x, padY, c.z), Q0, V(24, 0.02f, 24), padMat);
            float L = 10f; float D = L * 0.707f;
            // Each crossing line gets its own render queue so they NEVER Z-fight
            Material xBlue   = CreateUnlitMat("XBlue",   new Color(0.10f, 0.35f, 0.95f), 11);
            Material xRed    = CreateUnlitMat("XRed",    new Color(0.90f, 0.10f, 0.10f), 12);
            Material xGreen  = CreateUnlitMat("XGreen",  new Color(0.10f, 0.85f, 0.20f), 13);
            Material xYellow = CreateUnlitMat("XYellow", new Color(0.95f, 0.85f, 0.10f), 14);
            BuildConnectedPath(parent, "F_Blue_NS", new System.Collections.Generic.List<Vector3>{
                V(c.x, y, c.z - L), V(c.x, y, c.z + L) }, tapeW, tapeH, xBlue);
            BuildConnectedPath(parent, "F_Red_EW", new System.Collections.Generic.List<Vector3>{
                V(c.x - L, y, c.z), V(c.x + L, y, c.z) }, tapeW, tapeH, xRed);
            BuildConnectedPath(parent, "F_Green_D", new System.Collections.Generic.List<Vector3>{
                V(c.x - D, y, c.z - D), V(c.x + D, y, c.z + D) }, tapeW, tapeH, xGreen);
            BuildConnectedPath(parent, "F_Yellow_D", new System.Collections.Generic.List<Vector3>{
                V(c.x + D, y, c.z - D), V(c.x - D, y, c.z + D) }, tapeW, tapeH, xYellow);
        }
    }

    static void BuildConnectedPath(Transform parent, string name,
        System.Collections.Generic.List<Vector3> waypoints, float width, float height, Material mat)
    {
        const float inset = 0.02f;
        GameObject pathObj = Empty(name, parent);
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 a = waypoints[i];
            Vector3 b = waypoints[i + 1];
            Vector3 dir = b - a;
            float fullLen = dir.magnitude;
            if (fullLen < 0.05f) continue;

            Vector3 norm = dir / fullLen;
            Vector3 aInset = a + norm * inset;
            Vector3 bInset = b - norm * inset;
            float len = fullLen - inset * 2f;
            if (len < 0.01f) continue;

            Vector3 mid = (aInset + bInset) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(norm, Vector3.up);
            GameObject seg = Box($"S{i}", pathObj.transform, mid, rot, V(width, height, len), mat);
            Renderer r = seg.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }

    static void ArcPoints(System.Collections.Generic.List<Vector3> pts,
        Vector3 centre, float radius, float startDeg, float endDeg, int segments)
    {
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float ang = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            pts.Add(new Vector3(
                centre.x + Mathf.Cos(ang) * radius,
                centre.y,
                centre.z + Mathf.Sin(ang) * radius
            ));
        }
    }

    // ════════════════════════════════════════════════════════════
    //  GROUND CLEANUP
    // ════════════════════════════════════════════════════════════
    static void CleanExistingGround()
    {
        string[] groundNames = { "Plane", "Terrain", "Ground", "Floor" };
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObj in rootObjects)
        {
            foreach (string gn in groundNames)
            {
                if (rootObj.name == gn)
                {
                    Debug.Log($"[DroneCourseBuilder] Removing scene ground: {rootObj.name}");
                    DestroyImmediate(rootObj);
                    break;
                }
            }
        }

        GameObject obstacles = GameObject.Find("Obstacles");
        if (obstacles != null)
        {
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in obstacles.transform)
                children.Add(child);
            foreach (Transform child in children)
            {
                foreach (string gn in groundNames)
                {
                    if (child.name.StartsWith(gn))
                    {
                        DestroyImmediate(child.gameObject);
                        break;
                    }
                }
            }
            DestroyImmediate(obstacles);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  SHORTHAND HELPERS
    // ════════════════════════════════════════════════════════════

    static readonly Quaternion Q0 = Quaternion.identity;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 V3(float v) => new Vector3(v, v, v);
    static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);

    static GameObject Empty(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        return Prim(name, PrimitiveType.Cube, parent, pos, rot, scale, mat);
    }

    static GameObject Cyl(string name, Transform parent, Vector3 pos, Quaternion rot, float diameter, float height, Material mat)
    {
        return Prim(name, PrimitiveType.Cylinder, parent, pos, rot, V(diameter, height / 2f, diameter), mat);
    }

    static GameObject Prim(string name, PrimitiveType type, Transform parent,
        Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localRotation = rot;
        obj.transform.localScale = scale;
        obj.isStatic = true;
        if (mat != null)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
        return obj;
    }

    static Material CreateMat(string name, Color color, int renderQueueOffset = 0)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        if (renderQueueOffset != 0)
        {
            mat.renderQueue = 2000 + renderQueueOffset;
            mat.SetFloat("_ZWrite", 1);
        }
        return mat;
    }

    static Material CreateEmissiveMat(string name, Color color, float intensity = 2f, int renderQueueOffset = 0)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        mat.SetColor("_EmissionColor", color * intensity);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (renderQueueOffset != 0)
        {
            mat.renderQueue = 2000 + renderQueueOffset;
            mat.SetFloat("_ZWrite", 1);
        }
        return mat;
    }

    static Material CreateUnlitMat(string name, Color color, int renderQueueOffset = 0)
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.name = name;
        mat.color = color;
        if (renderQueueOffset != 0)
            mat.renderQueue = 2000 + renderQueueOffset;
        return mat;
    }

    static GameObject UnlitBox(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        GameObject obj = Box(name, parent, pos, rot, scale, mat);
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return obj;
    }
}
#endif
