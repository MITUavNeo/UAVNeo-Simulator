using UnityEngine;

/// <summary>
/// Simulates the IMU.
/// </summary>
public class PhysicsModule : DroneModule
{
    #region Constants
    /// <summary>
    /// The number of past samples to average for linear acceleration.
    /// </summary>
    private const int accelerationSamples = 4;

    /// <summary>
    /// The average relative error of linear acceleration measurements.
    /// This value is made up (it is NOT specified in the Intel RealSense D435i datasheet).
    /// </summary>
    private const float linearErrorFactor = 0.001f;

    /// <summary>
    /// The average fixed error applied to all linear acceleration measurements.
    /// This value is made up (it is NOT specified in the Intel RealSense D435i datasheet).
    /// </summary>
    private const float linearErrorFixed = 0.05f;

    /// <summary>
    /// The average relative error of angular velocity measurements.
    /// This value is made up (it is NOT specified in the Intel RealSense D435i datasheet).
    /// </summary>
    private const float angularErrorFactor = 0.001f;

    /// <summary>
    /// The average fixed error applied to all angular velocity measurements.
    /// This value is made up (it is NOT specified in the Intel RealSense D435i datasheet).
    /// </summary>
    private const float angularErrorFixed = 0.005f;

    // --------- GPS Reference Point ---------
    // Origin GPS coordinate mapped to Unity world position (0, 0, 0).
    // Default: MIT campus (Cambridge, MA).

    /// <summary>
    /// Reference latitude at Unity world origin (degrees).
    /// </summary>
    private const double gpsOriginLatitude = 42.3601;

    /// <summary>
    /// Reference longitude at Unity world origin (degrees).
    /// </summary>
    private const double gpsOriginLongitude = -71.0942;

    /// <summary>
    /// Reference altitude (meters above sea level) at Unity world Y=0.
    /// </summary>
    private const double gpsOriginAltitude = 5.0;

    /// <summary>
    /// Meters per degree of latitude (approximately constant).
    /// </summary>
    private const double metersPerDegreeLat = 111320.0;
    #endregion

    #region Public Interface
    /// <summary>
    /// The linear acceleration of the drone relative to the drone's transform (in meters/second^2).
    /// </summary>
    public Vector3 LinearAccceleration { get; private set; } = Vector3.zero;

    /// <summary>
    /// The linear velocity of the drone relative to its transform (in meters/second).
    /// This is the true physics velocity — no artificial scaling.
    /// </summary>
    public Vector3 LinearVelocity
    {
        get
        {
            if (!this.linearVelocity.HasValue)
            {
                this.linearVelocity = this.transform.InverseTransformDirection(this.rBody.linearVelocity);
            }
            return this.linearVelocity.Value;
        }
    }

    /// <summary>
    /// The altitude of the drone above the ground plane (in meters).
    /// This is the raw world-space Y position of the drone's transform.
    /// </summary>
    public float Altitude
    {
        get { return this.transform.position.y; }
    }

    /// <summary>
    /// The attitude of the drone as (pitch, roll, yaw) in degrees.
    /// Pitch: nose up is positive, range [-180, 180].
    /// Roll: right wing down is positive, range [-180, 180].
    /// Yaw: clockwise from north is positive, range [0, 360).
    /// </summary>
    public Vector3 Attitude
    {
        get
        {
            Vector3 euler = this.transform.eulerAngles;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            float roll = euler.z > 180f ? euler.z - 360f : euler.z;
            float yaw = euler.y;
            return new Vector3(pitch, roll, yaw);
        }
    }

    /// <summary>
    /// The simulated GPS reading as (latitude, longitude, altitude) in (degrees, degrees, meters).
    /// Derived from the drone's Unity world position using a fixed reference point.
    /// Altitude is meters above sea level.
    /// </summary>
    public Vector3 GPS
    {
        get
        {
            Vector3 pos = this.transform.position;

            // Unity Z -> North (latitude), Unity X -> East (longitude)
            double lat = gpsOriginLatitude + (double)pos.z / metersPerDegreeLat;
            double metersPerDegreeLon = metersPerDegreeLat * System.Math.Cos(lat * System.Math.PI / 180.0);
            double lon = gpsOriginLongitude + (double)pos.x / metersPerDegreeLon;
            double alt = gpsOriginAltitude + (double)pos.y;

            // Use Vector3 for transport; float precision is ~7 decimal digits,
            // sufficient for ~1cm resolution in GPS coordinates.
            return new Vector3((float)lat, (float)lon, (float)alt);
        }
    }

    /// <summary>
    /// The angular velocity of the drone (in radians/second).
    /// </summary>
    public Vector3 AngularVelocity
    {
        get
        {
            if (!this.angularVelocity.HasValue)
            {
                // Unity uses a left-handed coordinate system, but our API is right-handed
                Vector3 angVel = -this.rBody.angularVelocity;

                if (Settings.IsRealism)
                {
                    angVel *= NormalDist.Random(1, PhysicsModule.angularErrorFactor);
                    angVel.x += NormalDist.Random(0, PhysicsModule.angularErrorFixed);
                    angVel.y += NormalDist.Random(0, PhysicsModule.angularErrorFixed);
                    angVel.z += NormalDist.Random(0, PhysicsModule.angularErrorFixed);
                }

                this.angularVelocity = angVel;
            }
            return this.angularVelocity.Value;
        }
    }
    #endregion

    /// <summary>
    /// The rigidbody of the drone.
    /// </summary>
    private Rigidbody rBody;

    /// <summary>
    /// The previous linear velocity of the drone using the drone's transform as basis vectors.
    /// </summary>
    private Vector3 prevVelocity;

    /// <summary>
    /// Private member for the LinearVelocity accessor
    /// </summary>
    private Vector3? linearVelocity = null;

    /// <summary>
    /// Private member for the AngularVelocity accessor
    /// </summary>
    private Vector3? angularVelocity = null;

    protected override void Awake()
    {
        this.rBody = this.GetComponent<Rigidbody>();

        base.Awake();
    }

    private void Start()
    {
        this.prevVelocity = this.LinearVelocity;
    }

    private void Update()
    {
        if (this.drone.Hud != null)
        {
            // Telemetry panel: altitude, speed, attitude, IMU
            float altitude = this.transform.position.y;
            Vector3 euler = this.transform.eulerAngles;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            float roll = euler.z > 180f ? euler.z - 360f : euler.z;
            float yaw = euler.y;

            this.drone.Hud.UpdateTelemetry(
                altitude,
                this.LinearVelocity.magnitude,
                pitch, roll, yaw,
                this.LinearAccceleration,
                this.AngularVelocity,
                this.transform.position
            );
        }
    }

    private void FixedUpdate()
    {
        // Calculate current linear acceleration, incorporating gravity and error rate
        Vector3 curAcceleration = (this.LinearVelocity - this.prevVelocity) / Time.deltaTime;
        curAcceleration += this.transform.InverseTransformDirection(Vector3.down * 9.81f);
        if (Settings.IsRealism)
        {
            curAcceleration *= NormalDist.Random(1, PhysicsModule.linearErrorFactor);
            curAcceleration.x += NormalDist.Random(0, PhysicsModule.linearErrorFixed);
            curAcceleration.y += NormalDist.Random(0, PhysicsModule.linearErrorFixed);
            curAcceleration.z += NormalDist.Random(0, PhysicsModule.linearErrorFixed);
        }

        // Update linear acceleration running average
        this.LinearAccceleration += (curAcceleration - this.LinearAccceleration) / PhysicsModule.accelerationSamples;
        this.prevVelocity = this.LinearVelocity;
    }

    private void LateUpdate()
    {
        this.linearVelocity = null;
        this.angularVelocity = null;
    }
}
