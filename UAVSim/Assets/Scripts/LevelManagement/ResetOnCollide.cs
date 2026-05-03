using UnityEngine;

/// <summary>
/// An object which resets a drone back to the previous checkpoint on collision.
/// </summary>
public class ResetOnCollide : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();
        if (drone != null)
        {
            LevelManager.ResetDrone(drone.Index);
        }
    }
}
