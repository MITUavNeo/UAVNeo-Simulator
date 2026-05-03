/*
 * BuildAltitudeGates.cs  —  BWSI Module 2 Altitude Gate Run builder
 *
 * Menu: BWSI ▶ Build Altitude Gates (Module 2)
 *
 * Adds (or rebuilds) a straight-line altitude-profile gate run to the
 * Module2_DroneControl scene.  The run sits at x=0, along the +Z axis,
 * clearly separated from the existing scatter-layout gates.
 *
 * Gate profile (6 gates):
 *
 *   z (m)   altitude (m)   colour      label
 *   ─────   ────────────   ──────      ─────
 *    15       1.5          Blue        AG_1  ← just above hover height
 *    30       4.0          Green       AG_2  ← start climbing
 *    45       7.0          Yellow      AG_3  ← high pass
 *    60       7.0          Red         AG_4  ← hold altitude
 *    75       3.5          Orange      AG_5  ← descend
 *    90       1.5          Purple      AG_6  ← low finish
 *
 * Each gate is visualised as two vertical pillars + a coloured crossbar
 * at exactly the target altitude.  The crossbar is 6 m wide so the drone
 * has a clear visual target.
 *
 * HOW TO USE:
 *   1. Open Module2_DroneControl scene.
 *   2. Run BWSI ▶ Build Altitude Gates (Module 2).
 *   3. Scene is saved automatically.
 *   4. Students run:  drone sim labs/module2/tasks/challenge3_altitude_gates.py
 */

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public static class BuildAltitudeGates
{
    // ── Gate definitions: (z position, target altitude, colour, label) ──────
    private static readonly (float z, float alt, Color color, string label)[] Gates =
    {
        ( 15f, 1.5f, new Color(0.20f, 0.50f, 0.95f), "AG_1_Blue"  ),
        ( 30f, 4.0f, new Color(0.15f, 0.75f, 0.20f), "AG_2_Green" ),
        ( 45f, 7.0f, new Color(0.95f, 0.85f, 0.00f), "AG_3_Yellow"),
        ( 60f, 7.0f, new Color(0.90f, 0.10f, 0.10f), "AG_4_Red"   ),
        ( 75f, 3.5f, new Color(0.95f, 0.50f, 0.00f), "AG_5_Orange"),
        ( 90f, 1.5f, new Color(0.60f, 0.10f, 0.90f), "AG_6_Purple"),
    };

    private const float GATE_WIDTH    = 6f;    // metres, total span left-right
    private const float PILLAR_HEIGHT = 12f;   // metres, tall enough to span all altitudes
    private const float PILLAR_W      = 0.25f; // pillar cross-section (m)
    private const float BAR_HEIGHT    = 0.4f;  // crossbar thickness (m)
    private const string ROOT_NAME    = "AltitudeGateRun";

    [MenuItem("BWSI/Build Altitude Gates (Module 2)")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.Contains("Module2"))
        {
            Debug.LogError($"[AltitudeGates] Wrong scene: '{scene.name}'. Open Module2_DroneControl first.");
            return;
        }

        // ── Delete previous run if it exists ─────────────────────────────────
        var existing = GameObject.Find(ROOT_NAME);
        if (existing != null) Object.DestroyImmediate(existing);

        // ── Root container ────────────────────────────────────────────────────
        var root = new GameObject(ROOT_NAME);
        root.transform.position = Vector3.zero;

        int built = 0;
        foreach (var g in Gates)
        {
            BuildGate(root, g.label, g.z, g.alt, g.color);
            built++;
        }

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[AltitudeGates] Built {built} gates along +Z axis. Scene saved.");
        EditorUtility.DisplayDialog(
            "Altitude Gates Built",
            $"{built} altitude-profile gates added to Module2_DroneControl.\n\n" +
            "Gate profile (z → altitude):\n" +
            "  15 m →  1.5 m  Blue\n" +
            "  30 m →  4.0 m  Green\n" +
            "  45 m →  7.0 m  Yellow\n" +
            "  60 m →  7.0 m  Red\n" +
            "  75 m →  3.5 m  Orange\n" +
            "  90 m →  1.5 m  Purple\n\n" +
            "Students run:\n" +
            "drone sim labs/module2/tasks/challenge3_altitude_gates.py",
            "OK");
    }

    private static void BuildGate(GameObject root, string label, float z, float alt, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogError("BuildAltitudeGates: no usable shader found (tried URP/Lit, Standard, Unlit/Color).");
            return;
        }
        var mat = new Material(shader);
        mat.color = color;
        // URP base colour
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        var gate = new GameObject($"Gate_{label}");
        gate.transform.SetParent(root.transform, false);
        gate.transform.position = new Vector3(0f, 0f, z);

        // Left pillar
        MakeCube(gate, "Pillar_L",
            new Vector3(-GATE_WIDTH * 0.5f, PILLAR_HEIGHT * 0.5f, 0f),
            new Vector3(PILLAR_W, PILLAR_HEIGHT, PILLAR_W),
            mat);

        // Right pillar
        MakeCube(gate, "Pillar_R",
            new Vector3( GATE_WIDTH * 0.5f, PILLAR_HEIGHT * 0.5f, 0f),
            new Vector3(PILLAR_W, PILLAR_HEIGHT, PILLAR_W),
            mat);

        // Crossbar at target altitude
        MakeCube(gate, "Crossbar",
            new Vector3(0f, alt, 0f),
            new Vector3(GATE_WIDTH + PILLAR_W, BAR_HEIGHT, PILLAR_W * 2f),
            mat);

        // Small floating altitude label sphere (helps spot the target visually)
        MakeSphere(gate, "AltMarker",
            new Vector3(0f, alt, -0.5f),
            0.5f,
            mat);
    }

    private static void MakeCube(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        // Remove collider so drone doesn't crash into pillars during exploration
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
    }

    private static void MakeSphere(GameObject parent, string name, Vector3 localPos, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * radius;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
    }
}
