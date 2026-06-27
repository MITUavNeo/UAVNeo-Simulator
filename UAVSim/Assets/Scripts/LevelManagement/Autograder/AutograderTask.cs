using UnityEngine;

/// <summary>
/// A base class for tasks to be completed in an autograder level.
/// </summary>
public class AutograderTask : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The number of points which the user receives for successfully completing this task.
    /// </summary>
    [SerializeField]
    private float points = 1;
    #endregion

    #region Public Interface
    /// <summary>
    /// The number of points which the user receives for successfully completing this task.
    /// </summary>
    public float Points { get { return this.points; } }

    /// <summary>
    /// Sets this task's point value. Used by runtime task builders (e.g. <see cref="TrackGenerator"/>)
    /// that create checkpoints/finishes on the fly and distribute a level's points across them.
    /// </summary>
    public void SetPoints(float points)
    {
        this.points = points;
    }

    /// <summary>
    /// Called when the task is ready to be completed.
    /// </summary>
    public virtual void Enable()
    {
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when the task is not ready to be completed.
    /// </summary>
    public virtual void Disable()
    {
        this.gameObject.SetActive(false);
    }
    #endregion

    protected virtual void Awake()
    {
        this.Disable();
    }
}
