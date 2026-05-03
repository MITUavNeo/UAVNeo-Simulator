using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utilities to create the UAV (drone) prefab and set up scenes.
/// Access from the Unity menu bar: UAVNeo > ...
/// </summary>
public class DronePrefabBuilder : MonoBehaviour
{
    private const string PrefabPath = "Assets/Prefabs/DronePlayer.prefab";
    private const string DroneFbxPath = "Assets/Models/Drone/uav-neo.fbx";

    /// <summary>
    /// Ensures the FBX import settings are correct for material/color retention.
    /// Called automatically before building the prefab.
    /// </summary>
    private static void ConfigureFbxImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(DroneFbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[DronePrefabBuilder] Could not find ModelImporter for {DroneFbxPath}");
            return;
        }

        bool changed = false;

        // Import materials from the FBX (Fusion 360 embeds appearance colors)
        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            changed = true;
        }

        // Keep materials inside the imported asset (no external extraction needed)
        if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
        {
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            changed = true;
        }

        // Allow mesh reads for collider/debug purposes
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }

        if (changed)
        {
            Debug.Log("[DronePrefabBuilder] Configuring FBX import settings and reimporting...");
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// Searches the FBX hierarchy for propeller objects by name pattern.
    /// Looks for names containing "prop" (case-insensitive).
    /// Renames them to Propeller_0..3 sorted by position for consistent motor mapping.
    /// Returns the number of propellers found.
    /// </summary>
    private static int IdentifyAndRenamePropellers(GameObject modelRoot)
    {
        // Gather all transforms in the hierarchy
        Transform[] allTransforms = modelRoot.GetComponentsInChildren<Transform>(true);

        // Find propeller candidates: any object whose name contains "prop" (case-insensitive)
        List<Transform> candidates = new List<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t == modelRoot.transform) continue; // skip root
            string lower = t.name.ToLowerInvariant();
            if (lower.Contains("prop"))
            {
                candidates.Add(t);
            }
        }

        if (candidates.Count == 0)
        {
            // Fallback: log all children so the user can identify propellers
            Debug.LogWarning("[DronePrefabBuilder] No propeller objects found! Listing all children:");
            foreach (Transform t in allTransforms)
            {
                if (t == modelRoot.transform) continue;
                MeshFilter mf = t.GetComponent<MeshFilter>();
                string meshInfo = mf != null ? $" (mesh: {mf.sharedMesh?.name})" : "";
                Debug.Log($"  - \"{t.name}\" pos={t.localPosition}{meshInfo}");
            }
            return 0;
        }

        Debug.Log($"[DronePrefabBuilder] Found {candidates.Count} propeller candidates:");
        foreach (var c in candidates)
        {
            Debug.Log($"  - \"{c.name}\" localPos={c.localPosition}");
        }

        // Sort into motor positions: FR(0), FL(1), BL(2), BR(3)
        // FBX from Fusion 360 uses Z-up. In the FBX's local space, the horizontal
        // plane is X/Y (Z is up). After our -90 X, -90 Y rotation, the mapping is:
        //   FBX +X → Unity -Z (backward), FBX +Y → Unity +X (right), FBX +Z → Unity +Y (up)
        // So we classify using FBX's X (forward/back) and Y (left/right):
        //   Motor 0 (FR): +X, -Y  →  Motor 1 (FL): +X, +Y
        //   Motor 3 (BR): -X, -Y  →  Motor 2 (BL): -X, +Y
        // We try both X/Z and X/Y quadrant sorting to handle either orientation.
        if (candidates.Count == 4)
        {
            // Detect which axes have spread (FBX could be X/Y horizontal or X/Z horizontal)
            float zSpread = candidates.Max(c => Mathf.Abs(c.localPosition.z));
            float ySpread = candidates.Max(c => Mathf.Abs(c.localPosition.y));

            // Use whichever pair of axes has meaningful spread for the horizontal plane
            bool useXY = ySpread > zSpread;

            Transform[] sorted = new Transform[4];
            foreach (var t in candidates)
            {
                Vector3 p = t.localPosition;
                bool posForward = p.x >= 0;
                bool posRight = useXY ? (p.y < 0) : (p.z < 0); // In FBX Y-axis or Z-axis

                if (posForward && posRight) sorted[0] = t;       // FR
                else if (posForward && !posRight) sorted[1] = t;  // FL
                else if (!posForward && !posRight) sorted[2] = t; // BL
                else sorted[3] = t;                                // BR
            }

            for (int i = 0; i < 4; i++)
            {
                if (sorted[i] != null)
                {
                    string oldName = sorted[i].name;
                    sorted[i].name = $"Propeller_{i}";
                    Debug.Log($"[DronePrefabBuilder] Renamed \"{oldName}\" → \"Propeller_{i}\"");
                }
                else
                {
                    Debug.LogWarning($"[DronePrefabBuilder] Could not assign motor position {i}");
                }
            }
        }
        else if (candidates.Count > 4)
        {
            // More than 4 — might be nested. Try to find exactly 4 top-level prop objects.
            // For now, take the first 4 and sort by name.
            Debug.LogWarning($"[DronePrefabBuilder] Found {candidates.Count} propeller candidates (expected 4). Using first 4.");
            candidates.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < Mathf.Min(4, candidates.Count); i++)
            {
                candidates[i].name = $"Propeller_{i}";
            }
        }
        else
        {
            // Less than 4 — just rename sequentially
            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i].name = $"Propeller_{i}";
            }
        }

        return Mathf.Min(candidates.Count, 4);
    }

    /// <summary>
    /// Creates the drone prefab from the FBX model with all components properly wired.
    /// Menu: UAVNeo > 1. Create Drone Prefab
    /// </summary>
    [MenuItem("UAVNeo/1. Create Drone Prefab")]
    public static void CreateDronePrefab()
    {
        // Ensure FBX import settings are correct (materials, colors, etc.)
        ConfigureFbxImport();

        // Create root "Player" object (matches old structure where LevelManager does GetComponentInChildren<Drone>)
        GameObject playerRoot = new GameObject("DronePlayer");

        // --- Drone body (the main object with all core components) ---
        GameObject drone = new GameObject("Drone");
        drone.transform.SetParent(playerRoot.transform, false);
        drone.layer = LayerMask.NameToLayer("Player");

        // Rigidbody — Flight.cs configures inertia, drag, and constraints at runtime.
        // We just set mass and basic integration settings here.
        Rigidbody rb = drone.AddComponent<Rigidbody>();
        rb.mass = 2.0f;           // ~2 kg drone (frame + battery + payload)
        rb.useGravity = true;
        rb.linearDamping = 0f;             // Flight.cs applies its own aerodynamic drag model
        rb.angularDamping = 0.05f;   // Small value; attitude controller handles rotational damping
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.None; // No frozen axes — attitude PD controller stabilizes

        // Collider — sized for the 3x scaled drone model
        BoxCollider collider = drone.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.0f, 0.32f, 1.0f);
        collider.center = new Vector3(0, 0.16f, 0);

        // --- Load the FBX drone model ---
        bool modelLoaded = false;
        int propCount = 0;

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DroneFbxPath);
        if (fbxAsset != null)
        {
            // Instantiate the entire FBX (preserves hierarchy, materials, colors)
            GameObject droneModel = Instantiate(fbxAsset);
            droneModel.name = "DroneModel";
            droneModel.transform.SetParent(drone.transform, false);

            // Scale and orient:
            //   3× scale (user preference)
            //   FBX from Fusion 360 is Z-up; -90° X converts to Unity Y-up.
            //   -90° Y rotates model front to +Z (Unity forward).
            //   Y offset so drone sits above ground plane.
            droneModel.transform.localScale = Vector3.one * 4.5f;
            droneModel.transform.localRotation = Quaternion.Euler(-90f, -90f, 0f);
            droneModel.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            // Auto-detect and rename propellers for PropellerSpin
            propCount = IdentifyAndRenamePropellers(droneModel);

            // Set all child renderers to the Player layer
            foreach (var renderer in droneModel.GetComponentsInChildren<Renderer>())
            {
                renderer.gameObject.layer = LayerMask.NameToLayer("Player");
            }

            modelLoaded = true;
            Debug.Log($"<color=green>Loaded FBX drone model from {DroneFbxPath} with {propCount} propellers</color>");
        }
        else
        {
            Debug.LogWarning($"Could not find FBX at {DroneFbxPath}. Falling back to placeholder model.");
        }

        // Add placeholder as fallback
        if (!modelLoaded)
        {
            drone.AddComponent<DroneModelSetup>();
        }

        // --- Player cameras (3rd person views, children of root so they don't rotate with drone) ---
        // Adjusted for 2x sized drone (~0.6m wingspan)
        Camera playerCam1 = CreatePlayerCamera(playerRoot, "MainCamera", new Vector3(0, 1.8f, -3.5f), true);
        Camera playerCam2 = CreatePlayerCamera(playerRoot, "OverheadCamera", new Vector3(0, 7.0f, -0.6f), false);
        Camera playerCam3 = CreatePlayerCamera(playerRoot, "RearViewCamera", new Vector3(0, 1.8f, 3.5f), false);

        // --- Sensor cameras (children of drone, move with it) ---

        // 1) RGBD Color camera (Intel RealSense D435 — forward-facing)
        //    Renders to ColorImage.renderTexture; the HUD shows this texture live.
        GameObject colorCamObj = new GameObject("ColorCamera");
        colorCamObj.transform.SetParent(drone.transform, false);
        colorCamObj.transform.localPosition = new Vector3(0, 0.16f, 0.50f);
        Camera colorCam = colorCamObj.AddComponent<Camera>();
        colorCam.fieldOfView = 42.5f;   // RealSense D435 vertical FoV
        colorCam.nearClipPlane = 0.1f;
        colorCam.farClipPlane = 100f;
        colorCam.enabled = true; // Renders to targetTexture each frame (won't appear on screen)

        // 2) RGBD Depth camera (same position as color, used for depth raycasts)
        GameObject depthCamObj = new GameObject("DepthCamera");
        depthCamObj.transform.SetParent(drone.transform, false);
        depthCamObj.transform.localPosition = new Vector3(0, 0.16f, 0.50f);
        Camera depthCam = depthCamObj.AddComponent<Camera>();
        depthCam.fieldOfView = 42.5f;
        depthCam.enabled = false; // Only renders on demand (depth is via raycasts)

        // 3) Downward RGB camera (Arducam 16MP — facing straight down)
        //    Mounted under the drone body, looking at -Y.
        //    Render texture is created at runtime by CameraModule.
        GameObject downCamObj = new GameObject("DownwardCamera");
        downCamObj.transform.SetParent(drone.transform, false);
        downCamObj.transform.localPosition = new Vector3(0, 0.02f, 0); // bottom of drone
        downCamObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // look straight down
        Camera downCam = downCamObj.AddComponent<Camera>();
        downCam.fieldOfView = 65f;    // Arducam 16MP typical FoV
        downCam.nearClipPlane = 0.1f;
        downCam.farClipPlane = 200f;
        downCam.enabled = false; // Enabled at runtime by CameraModule once RenderTexture is created

        // Link render textures for sensor cameras
        RenderTexture colorRT = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Textures/ColorImage.renderTexture");
        RenderTexture depthRT = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Textures/DepthImage.renderTexture");
        if (colorRT != null) colorCam.targetTexture = colorRT;
        if (depthRT != null) depthCam.targetTexture = depthRT;
        // Downward camera render texture is created at runtime by CameraModule

        // --- Core scripts on the drone object ---
        Drone droneScript = drone.AddComponent<Drone>();
        drone.AddComponent<Flight>();
        drone.AddComponent<CameraModule>();
        drone.AddComponent<PhysicsModule>();
        drone.AddComponent<PropellerSpin>();   // Finds Propeller_* children and rotates them
        drone.AddComponent<CrashSystem>();     // Collision damage, prop detachment, sparks, smoke, respawn

        // --- Wire the player cameras into the Drone component's serialized field ---
        SerializedObject so = new SerializedObject(droneScript);
        SerializedProperty cameraProp = so.FindProperty("playerCameras");
        cameraProp.arraySize = 3;
        cameraProp.GetArrayElementAtIndex(0).objectReferenceValue = playerCam1;
        cameraProp.GetArrayElementAtIndex(1).objectReferenceValue = playerCam2;
        cameraProp.GetArrayElementAtIndex(2).objectReferenceValue = playerCam3;
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- Save as prefab ---
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerRoot, PrefabPath);
        DestroyImmediate(playerRoot);

        Debug.Log($"<color=green>✓ Drone prefab created at: {PrefabPath}</color>");
        if (modelLoaded)
        {
            Debug.Log($"Using FBX drone model with {propCount} propellers identified.");
        }
        Debug.Log("Now run: UAVNeo > 2. Setup Current Scene for Drone");

        // Select the prefab in the project window
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    /// <summary>
    /// Swaps the legacy player prefab for the drone in the current scene's LevelManager(s).
    /// Menu: UAVNeo > 2. Setup Current Scene for Drone
    /// </summary>
    [MenuItem("UAVNeo/2. Setup Current Scene for Drone")]
    public static void SetupCurrentScene()
    {
        // Load the drone prefab
        GameObject dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (dronePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"Drone prefab not found at {PrefabPath}.\nRun 'UAVNeo > 1. Create Drone Prefab' first.", "OK");
            return;
        }

        // Find all LevelManagers in the current scene
        LevelManager[] levelManagers = FindObjectsByType<LevelManager>();
        if (levelManagers.Length == 0)
        {
            EditorUtility.DisplayDialog("No LevelManager Found",
                "This scene doesn't have a LevelManager.\nOpen a level scene first, then run this command.", "OK");
            return;
        }

        int count = 0;
        foreach (LevelManager lm in levelManagers)
        {
            SerializedObject so = new SerializedObject(lm);
            SerializedProperty prefabProp = so.FindProperty("playerPrefab");

            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = dronePrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(lm);
                count++;
            }
        }

        // Mark scene as dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"<color=green>✓ Updated {count} LevelManager(s) in current scene to use drone prefab.</color>");
        EditorUtility.DisplayDialog("Done!",
            $"Swapped {count} LevelManager(s) to use the drone prefab.\n\nPress Play to test!", "OK");
    }

    /// <summary>
    /// Batch setup: applies drone prefab to ALL scenes in the build settings.
    /// Menu: UAVNeo > 3. Setup ALL Scenes for Drone (Batch)
    /// </summary>
    [MenuItem("UAVNeo/3. Setup ALL Scenes for Drone (Batch)")]
    public static void SetupAllScenes()
    {
        GameObject dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (dronePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"Drone prefab not found at {PrefabPath}.\nRun 'UAVNeo > 1. Create Drone Prefab' first.", "OK");
            return;
        }

        // Save current scene first
        EditorSceneManager.SaveOpenScenes();
        string currentScenePath = EditorSceneManager.GetActiveScene().path;

        // Find all scene files in the Scenes folder
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int totalUpdated = 0;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            // Skip the Main menu scene
            if (scenePath.Contains("Main")) continue;

            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                LevelManager[] levelManagers = FindObjectsByType<LevelManager>();

                foreach (LevelManager lm in levelManagers)
                {
                    SerializedObject so = new SerializedObject(lm);
                    SerializedProperty prefabProp = so.FindProperty("playerPrefab");

                    if (prefabProp != null)
                    {
                        prefabProp.objectReferenceValue = dronePrefab;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(lm);
                        totalUpdated++;
                    }
                }

                if (levelManagers.Length > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Skipped scene {scenePath}: {e.Message}");
            }
        }

        // Re-open the original scene
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"<color=green>✓ Updated {totalUpdated} LevelManager(s) across all scenes.</color>");
        EditorUtility.DisplayDialog("Batch Complete",
            $"Updated {totalUpdated} LevelManager(s) across all level scenes.\n\nAll scenes saved.", "OK");
    }

    /// <summary>
    /// Helper: creates a player camera with standard settings.
    /// </summary>
    private static Camera CreatePlayerCamera(GameObject parent, string name, Vector3 localPos, bool enabled)
    {
        GameObject camObj = new GameObject(name);
        camObj.tag = enabled ? "MainCamera" : "Untagged";
        camObj.transform.SetParent(parent.transform, false);
        camObj.transform.localPosition = localPos;

        // Aim the camera at the drone visual center
        camObj.transform.LookAt(new Vector3(0, 0.16f, 0));

        Camera cam = camObj.AddComponent<Camera>();
        cam.enabled = enabled;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1000f;
        cam.fieldOfView = 60f;

        AudioListener listener = camObj.AddComponent<AudioListener>();
        listener.enabled = enabled;

        return cam;
    }
}
#endif
