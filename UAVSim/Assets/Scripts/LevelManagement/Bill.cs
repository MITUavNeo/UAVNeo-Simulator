using UnityEngine;

public class Bill : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The maximum speed allowed by Bill. Any drone exceeding this speed will be hit.
    /// </summary>
    [SerializeField]
    private float MaxSpeed = 0.5f;
    #endregion

    #region Constants
    private const float thrust = 100000000;
    #endregion

    private Drone[] drones;

    private GameObject target = null;

    private Rigidbody rBody;

    private void Awake()
    {
        this.rBody = this.GetComponent<Rigidbody>();

        // Find all players in the level
        GameObject[] droneObjects = GameObject.FindGameObjectsWithTag("Player");
        this.drones = new Drone[droneObjects.Length];
        for (int i = 0; i < droneObjects.Length; i++)
        {
            this.drones[i] = droneObjects[i].GetComponent<Drone>();
        }
    }

    private void Update()
    {
        if (this.target == null)
        {
            foreach (Drone drone in this.drones)
            {
                if (drone.Physics.LinearVelocity.magnitude > this.MaxSpeed)
                {
                    this.target = drone.gameObject;
                    break;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (this.target != null)
        {
            this.transform.LookAt(this.target.transform.position);
            this.rBody.AddRelativeForce(0, 0, Bill.thrust * Time.fixedDeltaTime); 
        }
    }
}
