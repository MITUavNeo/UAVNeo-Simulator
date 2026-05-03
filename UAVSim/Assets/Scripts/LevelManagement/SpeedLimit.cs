using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a zone in which drones are not allowed to surpass a certain maximum speed.
/// </summary>
public class SpeedLimit : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The maximum speed (in meters/second) which drones are allowed to travel within this speed limit zone.
    /// </summary>
    [SerializeField]
    private float maxSpeed = 0.5f;
    #endregion

    /// <summary>
    /// The message shown when a drone breaks the speed limit.
    /// </summary>
    private string FailureMessage
    {
        get
        {
            return $"You traveled above {this.maxSpeed} m/s within the speed limit zone";
        }
    }            

    /// <summary>
    /// The drones currently within the speed limit zone.
    /// </summary>
    private readonly HashSet<Drone> drones = new HashSet<Drone>();

    private void Update()
    {
        foreach (Drone drone in this.drones)
        {
            if (drone.Physics.LinearVelocity.magnitude > this.maxSpeed)
            {
                LevelManager.HandleFailure(drone.Index, this.FailureMessage);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();
        if (drone != null)
        {
            drones.Add(drone);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Drone drone = other.GetComponentInParent<Drone>();
        if (drone != null)
        {
            drones.Remove(drone);
        }
    }
}
