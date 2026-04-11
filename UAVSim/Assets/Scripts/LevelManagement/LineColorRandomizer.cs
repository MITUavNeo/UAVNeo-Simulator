using UnityEngine;

/// <summary>
/// Recolors all Module 6 track pieces to a random colour every Play Mode entry.
///
/// HOW IT WORKS
/// ────────────
/// BuildF1Track.cs adds a LineTrackPiece marker component to every corner
/// and straight piece.  On Start() this script finds all LineTrackPiece
/// objects and sets their material colour using MaterialPropertyBlock —
/// the approach that works in both Built-in and URP pipelines.
///
/// The colour is chosen freshly each time Play is pressed, so students
/// cannot hardcode an HSV range — they must detect it from the camera.
///
/// Console output (once per run):
///   [LineColorRandomizer] This run: Blue (35 pieces recolored)
/// </summary>
public class LineColorRandomizer : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────
    public static readonly string[] ColorNames =
    {
        "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Cyan"
    };

    private static readonly Color[] PaletteColors =
    {
        new Color(0.90f, 0.08f, 0.08f),   // Red
        new Color(0.08f, 0.32f, 0.90f),   // Blue
        new Color(0.08f, 0.78f, 0.12f),   // Green
        new Color(0.95f, 0.85f, 0.00f),   // Yellow
        new Color(0.95f, 0.48f, 0.00f),   // Orange
        new Color(0.55f, 0.08f, 0.90f),   // Purple
        new Color(0.00f, 0.82f, 0.88f),   // Cyan
    };

    // ── Public state ──────────────────────────────────────────────────────────
    public static string CurrentColorName { get; private set; } = "Unknown";
    public static Color  CurrentColor     { get; private set; } = Color.red;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private void Start()
    {
        int idx = Random.Range(0, PaletteColors.Length);
        CurrentColorName = ColorNames[idx];
        CurrentColor     = PaletteColors[idx];

        int count = RecolorTrack(CurrentColor);

        Debug.Log($"[LineColorRandomizer] This run: {CurrentColorName} " +
                  $"({count} pieces recolored). " +
                  $"Detect the colour from your downward camera!");
    }

    private static int RecolorTrack(Color color)
    {
        // MaterialPropertyBlock works in both Built-in and URP/HDRP
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);   // URP / HDRP
        block.SetColor("_Color",     color);   // Built-in fallback

        int count = 0;

        // Find every piece tagged by BuildF1Track with the LineTrackPiece marker
#if UNITY_2022_2_OR_NEWER
        var pieces = FindObjectsByType<LineTrackPiece>(FindObjectsSortMode.None);
#else
        var pieces = FindObjectsOfType<LineTrackPiece>();
#endif
        foreach (var piece in pieces)
        {
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.SetPropertyBlock(block);
                count++;
            }
        }

        if (count == 0)
        {
            Debug.LogWarning("[LineColorRandomizer] No LineTrackPiece objects found! " +
                             "Run BWSI → Build F1 Track (Module 6) once to rebuild the scene.");
        }

        return count;
    }
}
