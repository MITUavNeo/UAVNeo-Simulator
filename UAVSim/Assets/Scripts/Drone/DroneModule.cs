using UnityEngine;

/// <summary>
/// The base class from which all drone modules inherit.
/// 
/// Each module handles a particular aspect of the drone's hardware, such as the camera, controller, etc.
/// </summary>
public abstract class DroneModule : MonoBehaviour
{
    /// <summary>
    /// The parent drone to which this module belongs.
    /// </summary>
    protected Drone drone;

    protected virtual void Awake()
    {
        this.FindParent();
    }

    /// <summary>
    /// Set the pointer to the parent Drone object.
    /// </summary>
    protected virtual void FindParent()
    {
        this.drone = this.GetComponent<Drone>();
    }
}
