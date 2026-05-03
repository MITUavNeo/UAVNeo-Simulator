using UnityEngine;

/// <summary>
/// An autograder task in which the user must drive to a specific point.
/// </summary>
public class DestinationPoint : AutograderTask
{
    private void OnTriggerEnter(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();
        if (drone != null)
        {
            AutograderManager.CompleteTask(this);
        }
    }
}