using UnityEngine;

/// <summary>
/// Exploration-mode input handler for <see cref="Gate"/>s: left-click selects the gate under
/// the cursor (highlighting it); right-click cycles the selected gate's colour. Place one
/// instance in a Lab D exploration scene. Autograder scenes omit this — there the gate colours
/// are fixed per run by <see cref="GateGenerator"/>. Mirrors <see cref="StoplightInteractor"/>.
/// </summary>
public class GateInteractor : MonoBehaviour
{
    /// <summary>
    /// The maximum distance (m) at which a gate can be clicked.
    /// </summary>
    private const float maxClickDistance = 500f;

    /// <summary>
    /// The currently selected gate, or null if none is selected.
    /// </summary>
    private Gate selected;

    private void Update()
    {
        // Left-click: select the gate under the cursor.
        if (Input.GetMouseButtonDown(0) && this.TryRaycastGate(out Gate clicked))
        {
            this.SetSelected(clicked);
        }

        // Right-click: cycle the selected gate's colour.
        if (Input.GetMouseButtonDown(1) && this.selected != null)
        {
            this.selected.Cycle();
        }
    }

    /// <summary>
    /// Raycasts from the main camera through the cursor and returns the gate hit, if any.
    /// The frame pieces (pillars/beams) carry the colliders the ray lands on.
    /// </summary>
    private bool TryRaycastGate(out Gate gate)
    {
        gate = null;
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, GateInteractor.maxClickDistance))
        {
            gate = hit.collider.GetComponentInParent<Gate>();
        }

        return gate != null;
    }

    /// <summary>
    /// Selects a gate, deselecting any previously selected one.
    /// </summary>
    private void SetSelected(Gate gate)
    {
        if (this.selected != null)
        {
            this.selected.SetSelected(false);
        }

        this.selected = gate;
        this.selected.SetSelected(true);
    }
}
