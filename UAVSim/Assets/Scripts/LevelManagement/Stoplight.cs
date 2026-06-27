using UnityEngine;

/// <summary>
/// A Vent Challenge stoplight: a colored cube the drone reads with its camera to
/// decide how to navigate an intersection. The face shows one of the flat, saturated
/// FlatTexture materials so the color is easy to filter out with computer vision.
/// Color-to-action contract students' code targets:
///   Red = Stop, Orange = Left, Green = Straight, Blue = Right.
///
/// In exploration mode the user can left-click to select a stoplight and right-click
/// to cycle its color (see <see cref="StoplightInteractor"/>). In the autograder the
/// colors are instead assigned per run by <see cref="VariableManager"/>.
/// </summary>
public class Stoplight : MonoBehaviour
{
    /// <summary>
    /// The meaning of each stoplight signal, in the order right-click cycles through.
    /// Must line up with <see cref="signalMaterials"/>.
    /// </summary>
    public enum Signal { Stop, Left, Straight, Right }

    #region Set in Unity Editor
    /// <summary>
    /// The renderer of the colored cube face whose material is swapped to show the signal.
    /// </summary>
    [SerializeField]
    private Renderer faceRenderer;

    /// <summary>
    /// The flat material shown for each <see cref="Signal"/>, in enum order
    /// (Stop = RedFlat, Left = OrangeFlat, Straight = GreenFlat, Right = BlueFlat).
    /// </summary>
    [SerializeField]
    private Material[] signalMaterials;

    /// <summary>
    /// The starting signal shown by this stoplight (index into <see cref="signalMaterials"/>).
    /// </summary>
    [SerializeField]
    private int signalIndex = 0;
    #endregion

    /// <summary>
    /// How much the cube grows while selected (selection feedback that does not alter
    /// the color, so the camera still filters the true signal color).
    /// </summary>
    private const float selectedScale = 1.3f;

    private Transform faceTransform;
    private Vector3 baseScale = Vector3.one;
    private bool isSelected;
    private MapLandmark mapLandmark;

    /// <summary>
    /// The signal this stoplight currently displays.
    /// </summary>
    public Signal CurrentSignal { get { return (Signal)this.signalIndex; } }

    private void Awake()
    {
        if (this.faceRenderer != null)
        {
            this.faceTransform = this.faceRenderer.transform;
            this.baseScale = this.faceTransform.localScale;
        }
        this.mapLandmark = this.GetComponent<MapLandmark>();
    }

    private void Start()
    {
        this.ApplyMaterial();
    }

    /// <summary>
    /// Set this stoplight to a specific signal (index into <see cref="signalMaterials"/>).
    /// Used by the autograder randomizer to assign per-run colors.
    /// </summary>
    public void SetSignal(int index)
    {
        this.signalIndex = index;
        this.ApplyMaterial();
    }

    /// <summary>
    /// Advance to the next signal color (wrapping). Used by exploration-mode right-click.
    /// </summary>
    public void Cycle()
    {
        if (this.signalMaterials == null || this.signalMaterials.Length == 0)
        {
            return;
        }
        this.signalIndex = (this.signalIndex + 1) % this.signalMaterials.Length;
        this.ApplyMaterial();
    }

    /// <summary>
    /// Grow / shrink the cube to show selection (exploration-mode feedback only).
    /// </summary>
    public void SetSelected(bool selected)
    {
        this.isSelected = selected;
        if (this.faceTransform != null)
        {
            this.faceTransform.localScale = this.baseScale * (selected ? Stoplight.selectedScale : 1f);
        }
    }

    /// <summary>
    /// Swap the cube face to the flat material for the current signal.
    /// </summary>
    private void ApplyMaterial()
    {
        if (this.faceRenderer == null || this.signalMaterials == null || this.signalMaterials.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(this.signalIndex, 0, this.signalMaterials.Length - 1);
        if (this.signalMaterials[index] != null)
        {
            this.faceRenderer.sharedMaterial = this.signalMaterials[index];

            // Keep the overhead-map icon in sync with the current signal color.
            if (this.mapLandmark != null)
            {
                this.mapLandmark.SetIconColor(this.signalMaterials[index].color);
            }
        }
    }
}
