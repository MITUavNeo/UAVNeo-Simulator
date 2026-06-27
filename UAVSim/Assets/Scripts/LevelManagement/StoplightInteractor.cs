using UnityEngine;

/// <summary>
/// Exploration-mode input handler for <see cref="Stoplight"/>s: left-click selects the
/// stoplight under the cursor (highlighting it); right-click cycles the selected
/// stoplight's color. Place one instance in an exploration scene. Autograder scenes
/// omit this — there the stoplight colors are fixed per run by <see cref="VariableManager"/>.
/// </summary>
public class StoplightInteractor : MonoBehaviour
{
    /// <summary>
    /// The maximum distance (m) at which a stoplight can be clicked.
    /// </summary>
    private const float maxClickDistance = 500f;

    /// <summary>
    /// The currently selected stoplight, or null if none is selected.
    /// </summary>
    private Stoplight selected;

    private void Update()
    {
        // Left-click: select the stoplight under the cursor.
        if (Input.GetMouseButtonDown(0) && this.TryRaycastStoplight(out Stoplight clicked))
        {
            this.SetSelected(clicked);
        }

        // Right-click: cycle the selected stoplight's color.
        if (Input.GetMouseButtonDown(1) && this.selected != null)
        {
            this.selected.Cycle();
        }
    }

    /// <summary>
    /// Raycasts from the main camera through the cursor and returns the stoplight hit, if any.
    /// </summary>
    private bool TryRaycastStoplight(out Stoplight stoplight)
    {
        stoplight = null;
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, StoplightInteractor.maxClickDistance))
        {
            stoplight = hit.collider.GetComponentInParent<Stoplight>();
        }

        return stoplight != null;
    }

    /// <summary>
    /// Selects a stoplight, deselecting any previously selected one.
    /// </summary>
    private void SetSelected(Stoplight stoplight)
    {
        if (this.selected != null)
        {
            this.selected.SetSelected(false);
        }

        this.selected = stoplight;
        this.selected.SetSelected(true);
    }
}
