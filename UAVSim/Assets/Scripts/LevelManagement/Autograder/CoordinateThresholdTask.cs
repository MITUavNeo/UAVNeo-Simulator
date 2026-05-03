using UnityEngine;

/// <summary>
/// An autograder task that completes when the drone crosses a world-coordinate threshold.
/// Used for zone-based objectives such as "reach z > 38" (ArUco maze exit)
/// or "reach x >= 16" (maze navigation exit).
/// </summary>
public class CoordinateThresholdTask : AutograderTask
{
    public enum Axis { X, Y, Z }

    #region Set in Unity Editor
    [Tooltip("World axis to monitor.")]
    [SerializeField]
    private Axis axis = Axis.X;

    [Tooltip("Threshold coordinate value.")]
    [SerializeField]
    private float threshold = 16f;

    [Tooltip("If true, task completes when value >= threshold. If false, when value <= threshold.")]
    [SerializeField]
    private bool greaterThanOrEqual = true;
    #endregion

    private void Update()
    {
        Vector3 pos = LevelManager.GetDrone().Center;
        float value = this.axis == Axis.X ? pos.x
                    : this.axis == Axis.Y ? pos.y
                    : pos.z;

        bool crossed = this.greaterThanOrEqual ? value >= this.threshold
                                               : value <= this.threshold;
        if (crossed)
        {
            AutograderManager.CompleteTask(this);
        }
    }
}
