using UnityEngine;

/// <summary>
/// Recolors all F1 track pieces to a randomly chosen color every time
/// the scene enters Play Mode.
///
/// Attach this script to any GameObject in Module6_LineFollowing
/// (and the Module 6 autograder scene).  BuildF1Track.cs creates a
/// "LineManager" object automatically — this script lives on that object.
///
/// The chosen color is logged so students can verify visually:
///   [LineColorRandomizer] This run: Blue (35 pieces recolored)
///
/// Students must detect the color dynamically from the downward camera
/// — they cannot hardcode red anymore!
/// </summary>
public class LineColorRandomizer : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────
    // 7 visually distinct colors with good HSV separation so the drone camera
    // can reliably distinguish them.
    public static readonly string[] ColorNames = new string[]
    {
        "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Cyan"
    };

    // Unity Color (linear sRGB) for each palette entry
    private static readonly Color[] PaletteColors = new Color[]
    {
        new Color(0.90f, 0.08f, 0.08f),   // Red
        new Color(0.08f, 0.32f, 0.90f),   // Blue
        new Color(0.08f, 0.78f, 0.12f),   // Green
        new Color(0.95f, 0.85f, 0.00f),   // Yellow
        new Color(0.95f, 0.48f, 0.00f),   // Orange
        new Color(0.55f, 0.08f, 0.90f),   // Purple
        new Color(0.00f, 0.82f, 0.88f),   // Cyan
    };

    // ── Public state (readable by other scripts if needed) ────────────────────
    /// <summary>The name of the color chosen for this run, e.g. "Blue".</summary>
    public static string CurrentColorName  { get; private set; } = "Unknown";

    /// <summary>The Unity Color chosen for this run.</summary>
    public static Color CurrentColor { get; private set; } = Color.red;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private void Start()
    {
        int idx = Random.Range(0, PaletteColors.Length);
        CurrentColorName = ColorNames[idx];
        CurrentColor     = PaletteColors[idx];

        int count = RecolorTrack(CurrentColor);

        Debug.Log($"[LineColorRandomizer] This run: {CurrentColorName} " +
                  $"({count} pieces recolored). " +
                  $"Students: detect the colour from your downward camera!");
    }

    /// <summary>
    /// Sets the material colour on every track piece in the scene.
    /// Pieces are identified by the naming convention used in BuildF1Track.cs:
    ///   "Corner_*"  — smooth arc corners (child mesh named "Mesh")
    ///   "RedLine_*" — flat straight pieces
    /// Using renderer.material (not sharedMaterial) creates per-instance
    /// materials so the shared RedFlat asset is never modified.
    /// </summary>
    private static int RecolorTrack(Color color)
    {
        int count = 0;
        foreach (GameObject go in FindObjectsOfType<GameObject>())
        {
            if (!go.name.StartsWith("Corner_") && !go.name.StartsWith("RedLine_"))
                continue;

            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                // renderer.material auto-creates an instance if needed
                r.material.color = color;
                count++;
            }
        }
        return count;
    }
}
