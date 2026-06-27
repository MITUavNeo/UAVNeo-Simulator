using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feeds the HUD overhead map the floor-line course geometry. At load it scans the
/// scene for the actual line strips and builds a segment list from them, so adding,
/// moving, or deleting a line in the editor is reflected on the map automatically —
/// the map is never a stale baked copy. Like <see cref="MapLandmark"/>, this is a
/// purely user-facing visual aid: only the Hud minimap reads the registry, and nothing
/// here is exposed to the drone or the Python sensor API, so it does not leak the line
/// layout to student code.
/// </summary>
public class MapLineCourse : MonoBehaviour
{
    /// <summary>
    /// One straight floor-line segment in world XZ (a.x/a.y = world X/Z), with the
    /// color to draw it on the overhead map.
    /// </summary>
    [System.Serializable]
    public struct Segment
    {
        public Vector2 a;
        public Vector2 b;
        public Color color;
    }

    /// <summary>
    /// Material names treated as floor lines (the flat course colors). A renderer using
    /// one of these — and shaped like a thin elongated strip — is mapped as a segment.
    /// </summary>
    private static readonly string[] LineMaterialNames = { "RedFlat", "GreenFlat", "BlueFlat" };

    /// <summary>
    /// The line segments of this course, in world XZ. Rebuilt from the scene at Awake.
    /// </summary>
    private Segment[] segments = new Segment[0];

    /// <summary>
    /// The line segments of this course (read by the Hud each frame).
    /// </summary>
    public Segment[] Segments { get { return this.segments; } }

    /// <summary>
    /// All active line courses in the scene. The Hud overhead map iterates this to
    /// draw the floor lines. Maintained via OnEnable / OnDisable so the Hud never has
    /// to run a scene-wide search.
    /// </summary>
    public static readonly List<MapLineCourse> Active = new List<MapLineCourse>();

    private void Awake()
    {
        this.RebuildFromScene();
    }

    /// <summary>
    /// Scans the scene for floor-line strips (renderers using a flat course-color
    /// material, shaped as a thin elongated strip — which excludes the square joint
    /// discs, walls, floor, ceiling, and arena trim) and rebuilds the segment list
    /// from their live transforms and colors.
    /// </summary>
    public void RebuildFromScene()
    {
        List<Segment> built = new List<Segment>();
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>())
        {
            Material material = renderer.sharedMaterial;
            if (material == null || !MapLineCourse.IsLineMaterial(material.name))
            {
                continue;
            }

            Vector3 scale = renderer.transform.lossyScale;
            float longSide = Mathf.Max(scale.x, scale.z);
            float shortSide = Mathf.Min(scale.x, scale.z);
            // Thin and clearly elongated => a line strip (not a square joint disc).
            if (scale.y > 0.5f || longSide < 2f * shortSide)
            {
                continue;
            }

            // Endpoints lie at the ends of the strip's long local axis.
            Vector3 endA, endB;
            if (scale.z >= scale.x)
            {
                endA = renderer.transform.TransformPoint(new Vector3(0f, 0f, -0.5f));
                endB = renderer.transform.TransformPoint(new Vector3(0f, 0f, 0.5f));
            }
            else
            {
                endA = renderer.transform.TransformPoint(new Vector3(-0.5f, 0f, 0f));
                endB = renderer.transform.TransformPoint(new Vector3(0.5f, 0f, 0f));
            }

            Color color = material.color;
            color.a = 1f;
            built.Add(new Segment
            {
                a = new Vector2(endA.x, endA.z),
                b = new Vector2(endB.x, endB.z),
                color = color,
            });
        }

        this.segments = built.ToArray();
    }

    private static bool IsLineMaterial(string materialName)
    {
        for (int i = 0; i < MapLineCourse.LineMaterialNames.Length; i++)
        {
            // Runtime instances can be suffixed (e.g. "RedFlat (Instance)"); match the prefix.
            if (materialName.StartsWith(MapLineCourse.LineMaterialNames[i]))
            {
                return true;
            }
        }
        return false;
    }

    private void OnEnable()
    {
        if (!MapLineCourse.Active.Contains(this))
        {
            MapLineCourse.Active.Add(this);
        }
    }

    private void OnDisable()
    {
        MapLineCourse.Active.Remove(this);
    }
}
