using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks this object's world position as a landmark to be drawn on the HUD's
/// overhead map (e.g. a gate). This is purely a user-facing visual aid: only the
/// Hud minimap reads the shared registry to place an icon. Nothing here is
/// exposed to the drone or the Python sensor API, so it does not leak the gate
/// layout to student code — the drone must still find gates with its own sensors.
/// </summary>
public class MapLandmark : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The color of the icon drawn on the overhead map for this landmark.
    /// </summary>
    [SerializeField]
    private Color iconColor = new Color(1f, 0.84f, 0.25f, 0.95f);

    /// <summary>
    /// Optional: if set, the icon color is taken from this renderer's material color
    /// (keeping the configured alpha). Lets a colored object — e.g. a gate whose neon
    /// strips are tinted per instance — drive its own map-icon color automatically.
    /// </summary>
    [SerializeField]
    private Renderer colorSource;
    #endregion

    /// <summary>
    /// The color of the icon drawn on the overhead map for this landmark.
    /// </summary>
    public Color IconColor { get { return this.iconColor; } }

    /// <summary>
    /// Recolors this landmark's overhead-map icon at runtime (e.g. a stoplight whose
    /// color changed). The Hud reads <see cref="IconColor"/> each frame, so the icon
    /// updates immediately.
    /// </summary>
    public void SetIconColor(Color color)
    {
        this.iconColor = color;
    }

    /// <summary>
    /// All currently-active landmarks in the scene. The Hud overhead map iterates
    /// this to draw one icon per landmark. Maintained via OnEnable / OnDisable so
    /// the Hud never has to run a scene-wide search each frame.
    /// </summary>
    public static readonly List<MapLandmark> Active = new List<MapLandmark>();

    private void OnEnable()
    {
        if (!MapLandmark.Active.Contains(this))
        {
            MapLandmark.Active.Add(this);
        }

        // Adopt the color of the source renderer (e.g. a gate's tinted neon strip),
        // preserving the configured icon alpha.
        if (this.colorSource != null && this.colorSource.sharedMaterial != null)
        {
            Color color = this.colorSource.sharedMaterial.color;
            color.a = this.iconColor.a;
            this.iconColor = color;
        }
    }

    private void OnDisable()
    {
        MapLandmark.Active.Remove(this);
    }
}
