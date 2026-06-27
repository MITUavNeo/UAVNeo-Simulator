using UnityEngine;

/// <summary>
/// Authored on each modular track-piece prefab (see Assets/Prefabs/TrackPieces). Describes
/// the piece to <see cref="TrackGenerator"/>: its connector ports, the 10 m grid cells it
/// occupies, and the line-renderer groups it can recolor per branch. Pieces snap together on
/// a 10 m lattice with axis-aligned ports; the generator instantiates and rotates them, matches
/// an exit port of one piece to the entry port of the next, and assigns line colors per the
/// RED &gt; GREEN &gt; BLUE priority.
/// </summary>
public class TrackPiece : MonoBehaviour
{
    public enum Category { Spawn, Straight, Slant, Corner, TIntersection, CrossIntersection, Goal }

    /// <summary>
    /// The role a port plays. Entry is where the line comes in; Exit is the through path that
    /// continues to the next piece; DeadEnd is a branch that stops (lower-priority decoy).
    /// </summary>
    public enum PortRole { Entry, Exit, DeadEnd }

    /// <summary>
    /// A connector at a cell-edge midpoint. Position/direction are local; direction is the
    /// outward cardinal (unit vector in the XZ plane). Two pieces connect when one piece's
    /// exit port and the next piece's entry port share a world position and opposite directions.
    /// </summary>
    [System.Serializable]
    public struct Port
    {
        public Vector3 localPosition;
        public Vector3 localDirection;
        public PortRole role;
    }

    /// <summary>
    /// A recolorable line group. <see cref="exitPortIndex"/> is the port this branch's line
    /// leads to (-1 for a piece with a single through line that has no distinct branch port).
    /// </summary>
    [System.Serializable]
    public struct Branch
    {
        public int exitPortIndex;
        public MeshRenderer[] renderers;
    }

    [SerializeField] private Category category;
    [SerializeField] private Port[] ports;
    [SerializeField] private Branch[] branches;
    [SerializeField] private Vector2Int[] footprintCells;

    public Category PieceCategory { get { return this.category; } }
    public Port[] Ports { get { return this.ports; } }
    public Branch[] Branches { get { return this.branches; } }
    public Vector2Int[] FootprintCells { get { return this.footprintCells; } }

    /// <summary>The world-space position of port <paramref name="i"/>.</summary>
    public Vector3 WorldPortPosition(int i)
    {
        return this.transform.TransformPoint(this.ports[i].localPosition);
    }

    /// <summary>The world-space outward direction of port <paramref name="i"/>.</summary>
    public Vector3 WorldPortDirection(int i)
    {
        return this.transform.TransformDirection(this.ports[i].localDirection);
    }

    /// <summary>The index of this piece's entry port (or -1 for the spawn, which has none).</summary>
    public int EntryPortIndex()
    {
        for (int i = 0; i < this.ports.Length; i++)
        {
            if (this.ports[i].role == PortRole.Entry)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Recolors branch <paramref name="branchIndex"/>'s line to the given material.</summary>
    public void SetBranchColor(int branchIndex, Material material)
    {
        MeshRenderer[] renderers = this.branches[branchIndex].renderers;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
