using UnityEngine;

/// <summary>
/// An autograder task that completes when the drone comes within a set distance
/// of this GameObject's world position.
/// Used for objectives such as "fly within 3 m of the SAR landing pad".
/// Prefer this over trigger colliders for flying drones, which can miss fast
/// OnTriggerEnter callbacks at low frame rates.
/// </summary>
public class ProximityTask : AutograderTask
{
    #region Set in Unity Editor
    [Tooltip("Distance in metres within which the task is considered complete.")]
    [SerializeField]
    private float completionRadius = 3f;

    [Tooltip("If set, measure distance to this transform instead of this object's position.")]
    [SerializeField]
    private Transform targetOverride;
    #endregion

    private void Update()
    {
        Vector3 target = this.targetOverride != null
            ? this.targetOverride.position
            : this.transform.position;

        if (Vector3.Distance(LevelManager.GetDrone().Center, target) <= this.completionRadius)
        {
            AutograderManager.CompleteTask(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 target = this.targetOverride != null
            ? this.targetOverride.position
            : this.transform.position;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(target, this.completionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target, this.completionRadius);
    }
}
