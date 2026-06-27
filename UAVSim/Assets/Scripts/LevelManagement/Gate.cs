using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Lab D navigation gate: a steel frame with four emissive neon strips the drone reads
/// (via the corner ArUco tags + the strip colour) to decide which gate to fly through.
/// Colour priority students' code targets: RED &gt; GREEN &gt; BLUE.
///
/// In exploration mode the user can left-click to select a gate and right-click to cycle
/// its colour (see <see cref="GateInteractor"/>) — the same affordance as the Vent Challenge
/// stoplights. In the autograder the colours are instead assigned per run by
/// <see cref="GateGenerator"/>, which sets the neon materials directly; this component never
/// touches the materials on its own (no auto-apply at Start), so it is inert there.
/// </summary>
public class Gate : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The flat materials this gate cycles through on right-click, in cycle order
    /// (Blue, Green, Red). The drone filters these flat, saturated colours with CV.
    /// </summary>
    [SerializeField]
    private Material[] colorMaterials;
    #endregion

    /// <summary>
    /// How much the gate grows while selected (selection feedback that does not alter the
    /// colour, so the camera still reads the true neon colour).
    /// </summary>
    private const float selectedScale = 1.1f;

    private MeshRenderer[] neonRenderers;
    private MapLandmark mapLandmark;
    private Vector3 baseScale = Vector3.one;
    private bool isSelected;

    private void Awake()
    {
        // The recolourable strips are the children named "Neon*" (NeonL/R/T/B), matching the
        // convention GateGenerator uses. Found by name so the prefab can change without rewiring.
        List<MeshRenderer> neon = new List<MeshRenderer>();
        foreach (MeshRenderer renderer in this.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name.StartsWith("Neon"))
            {
                neon.Add(renderer);
            }
        }
        this.neonRenderers = neon.ToArray();
        this.mapLandmark = this.GetComponent<MapLandmark>();
        this.baseScale = this.transform.localScale;
    }

    /// <summary>
    /// Advance to the next colour (wrapping), starting from whatever colour is currently shown.
    /// Used by exploration-mode right-click.
    /// </summary>
    public void Cycle()
    {
        if (this.colorMaterials == null || this.colorMaterials.Length == 0)
        {
            return;
        }
        this.SetColor((this.CurrentIndex() + 1) % this.colorMaterials.Length);
    }

    /// <summary>
    /// Set this gate to a specific colour (index into <see cref="colorMaterials"/>), updating
    /// both the neon strips and the overhead-map icon.
    /// </summary>
    public void SetColor(int index)
    {
        if (this.colorMaterials == null || this.colorMaterials.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, this.colorMaterials.Length - 1);
        Material material = this.colorMaterials[index];
        if (material == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in this.neonRenderers)
        {
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
        if (this.mapLandmark != null)
        {
            this.mapLandmark.SetIconColor(material.color);
        }
    }

    /// <summary>
    /// Grow / shrink the gate to show selection (exploration-mode feedback only).
    /// </summary>
    public void SetSelected(bool selected)
    {
        this.isSelected = selected;
        this.transform.localScale = this.baseScale * (selected ? Gate.selectedScale : 1f);
    }

    /// <summary>
    /// The index in <see cref="colorMaterials"/> matching the strips' current material, so a
    /// cycle continues from the displayed colour (which the scene baked, not this component).
    /// Defaults to 0 if no match.
    /// </summary>
    private int CurrentIndex()
    {
        if (this.neonRenderers != null && this.neonRenderers.Length > 0 && this.neonRenderers[0] != null)
        {
            Material current = this.neonRenderers[0].sharedMaterial;
            for (int i = 0; i < this.colorMaterials.Length; i++)
            {
                if (this.colorMaterials[i] == current)
                {
                    return i;
                }
            }
        }
        return 0;
    }
}
