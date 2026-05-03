using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Logs drone telemetry to a timestamped CSV file for debugging jerky flight behavior.
///
/// Attach this component to the DronePlayer prefab (or any GameObject that also has
/// Drone, Flight, and PhysicsModule components).
///
/// Log files are written to:
///   UAVSim/DroneDebugLogs/<collection>/<scene>/drone_log_<scene>_<collection>_<timestamp>.csv
///
/// Sits alongside Assets/, Library/, Logs/, etc. in the UAVSim project folder.
///
/// Columns logged each sample:
///   collection, scene, time, frame, armed, launching, landing,
///   pos_x, pos_y, pos_z, altitude, target_alt, alt_error,
///   pitch, roll, yaw,
///   vel_x, vel_y, vel_z, speed,
///   ang_vel_x, ang_vel_y, ang_vel_z, ang_vel_mag,
///   ang_accel_mag (jerk proxy),
///   input_forward, input_strafe, input_vertical, input_yaw, max_speed,
///   motor0_rpm, motor1_rpm, motor2_rpm, motor3_rpm,
///   jerk_flag
/// </summary>
public class DroneDebugLogger : MonoBehaviour
{
    #region Inspector Settings

    [Header("Logging")]

    [Tooltip("How many times per second to write a log row. 20 = one row per 0.05s.")]
    [Range(1, 60)]
    public float logRateHz = 20f;

    [Tooltip("Only log while the drone is armed. Uncheck to log even on the ground.")]
    public bool onlyLogWhenArmed = true;

    [Header("Jerk Detection")]

    [Tooltip("Angular acceleration magnitude (rad/s²) above which a row is flagged as JERK.")]
    public float jerkThreshold = 15f;

    #endregion

    #region Private State

    private Drone drone;
    private Flight flight;
    private PhysicsModule physicsModule;

    private StreamWriter writer;
    private string logPath;

    private float logInterval;
    private float nextLogTime;

    private Vector3 prevAngularVelocity;
    private bool firstSample = true;

    private string sceneName;
    private string collectionName;

    #endregion

    // ──────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        this.drone = this.GetComponent<Drone>();
        this.flight = this.GetComponent<Flight>();
        this.physicsModule = this.GetComponent<PhysicsModule>();

        if (this.drone == null || this.flight == null || this.physicsModule == null)
        {
            Debug.LogError("[DroneDebugLogger] Missing required components (Drone / Flight / PhysicsModule). Logger disabled.");
            this.enabled = false;
            return;
        }
    }

    private void Start()
    {
        this.logInterval = 1f / Mathf.Max(this.logRateHz, 1f);
        this.nextLogTime = Time.time;
        this.ResolveSceneInfo();
        this.OpenLogFile();
    }

    private void Update()
    {
        if (this.writer == null) return;
        if (this.onlyLogWhenArmed && !this.flight.IsArmed) return;
        if (Time.time < this.nextLogTime) return;

        this.nextLogTime += this.logInterval;
        this.WriteSample();
    }

    private void OnDestroy()
    {
        this.CloseLogFile();
    }

    private void OnApplicationQuit()
    {
        this.CloseLogFile();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Scene / Collection Resolution
    // ──────────────────────────────────────────────────────────────────────

    private void ResolveSceneInfo()
    {
        Scene active = SceneManager.GetActiveScene();
        this.sceneName = active.name;

        // Look up the collection name from LevelCollection registry
        this.collectionName = "Unknown";
        int buildIndex = active.buildIndex;
        foreach (LevelCollection collection in LevelCollection.LevelCollections)
        {
            foreach (LevelInfo level in collection.Levels)
            {
                if (level.BuildIndex == buildIndex)
                {
                    this.collectionName = collection.ShortName;
                    return;
                }
                // Also check random scene variants
                if (level.RandomSceneBuildIndices != null)
                {
                    foreach (int idx in level.RandomSceneBuildIndices)
                    {
                        if (idx == buildIndex)
                        {
                            this.collectionName = collection.ShortName;
                            return;
                        }
                    }
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  File Management
    // ──────────────────────────────────────────────────────────────────────

    private void OpenLogFile()
    {
        try
        {
            // Application.dataPath = UAVSim/Assets — go one level up for the project root
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            // Subdirectory: DroneDebugLogs/<collection>/<scene>/
            string logDir = Path.Combine(projectRoot, "DroneDebugLogs",
                SanitizeName(this.collectionName),
                SanitizeName(this.sceneName));
            Directory.CreateDirectory(logDir);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"drone_log_{SanitizeName(this.sceneName)}_{SanitizeName(this.collectionName)}_{timestamp}.csv";
            this.logPath = Path.Combine(logDir, fileName);

            this.writer = new StreamWriter(this.logPath, append: false, encoding: Encoding.UTF8);
            this.writer.WriteLine(CsvHeader());
            this.writer.Flush();

            Debug.Log($"[DroneDebugLogger] Logging to: {this.logPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DroneDebugLogger] Failed to open log file: {e.Message}");
            this.writer = null;
        }
    }

    private void CloseLogFile()
    {
        if (this.writer != null)
        {
            this.writer.Flush();
            this.writer.Close();
            this.writer = null;
            Debug.Log($"[DroneDebugLogger] Log closed: {this.logPath}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Sampling
    // ──────────────────────────────────────────────────────────────────────

    private void WriteSample()
    {
        // ── Position & Altitude ──
        Vector3 pos = this.transform.position;
        float altitude = this.physicsModule.Altitude;
        float targetAlt = this.flight.TargetAltitude;
        float altError = altitude - targetAlt;

        // ── Attitude (degrees) ──
        Vector3 attitude = this.physicsModule.Attitude; // (pitch, roll, yaw)

        // ── Velocity (body frame, m/s) ──
        Vector3 vel = this.physicsModule.LinearVelocity;
        float speed = vel.magnitude;

        // ── Angular velocity (rad/s) ──
        Vector3 angVel = this.physicsModule.AngularVelocity;
        float angVelMag = angVel.magnitude;

        // ── Angular acceleration (jerk proxy) ──
        float angAccelMag = 0f;
        if (!this.firstSample)
        {
            Vector3 angAccel = (angVel - this.prevAngularVelocity) / this.logInterval;
            angAccelMag = angAccel.magnitude;
        }
        this.prevAngularVelocity = angVel;
        this.firstSample = false;

        bool jerk = angAccelMag > this.jerkThreshold;

        // ── Motor RPMs ──
        float[] rpms = this.flight.MotorRPMs;

        // ── Build CSV row ──
        string row = string.Format(
            "{0},{1}," +
            "{2:F4},{3},{4},{5},{6}," +
            "{7:F3},{8:F3},{9:F3},{10:F3},{11:F3},{12:F3}," +
            "{13:F2},{14:F2},{15:F2}," +
            "{16:F3},{17:F3},{18:F3},{19:F3}," +
            "{20:F4},{21:F4},{22:F4},{23:F4}," +
            "{24:F4}," +
            "{25:F3},{26:F3},{27:F3},{28:F3},{29:F3}," +
            "{30:F0},{31:F0},{32:F0},{33:F0}," +
            "{34}",
            // collection, scene
            this.collectionName, this.sceneName,
            // time, frame, flags
            Time.time, Time.frameCount,
            BoolToInt(this.flight.IsArmed), BoolToInt(this.flight.IsLaunching), BoolToInt(this.flight.IsLanding),
            // position + altitude
            pos.x, pos.y, pos.z, altitude, targetAlt, altError,
            // attitude
            attitude.x, attitude.y, attitude.z,
            // velocity
            vel.x, vel.y, vel.z, speed,
            // angular velocity
            angVel.x, angVel.y, angVel.z, angVelMag,
            // jerk proxy
            angAccelMag,
            // user inputs
            this.flight.ForwardSpeed, this.flight.StrafeSpeed,
            this.flight.VerticalSpeed, this.flight.YawSpeed, this.flight.MaxSpeed,
            // motor RPMs
            rpms[0], rpms[1], rpms[2], rpms[3],
            // jerk flag
            jerk ? "JERK" : ""
        );

        try
        {
            this.writer.WriteLine(row);
            // Flush every 50 samples to avoid stalling on disk writes each frame
            if (Time.frameCount % 50 == 0)
            {
                this.writer.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DroneDebugLogger] Write error: {e.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static string CsvHeader()
    {
        return
            "collection,scene," +
            "time_s,frame,armed,launching,landing," +
            "pos_x,pos_y,pos_z,altitude_m,target_alt_m,alt_error_m," +
            "pitch_deg,roll_deg,yaw_deg," +
            "vel_x_mps,vel_y_mps,vel_z_mps,speed_mps," +
            "ang_vel_x_rps,ang_vel_y_rps,ang_vel_z_rps,ang_vel_mag_rps," +
            "ang_accel_mag_rps2," +
            "input_forward,input_strafe,input_vertical,input_yaw,max_speed," +
            "motor0_rpm,motor1_rpm,motor2_rpm,motor3_rpm," +
            "jerk_flag";
    }

    /// <summary>
    /// Strips characters that are invalid in file/directory names.
    /// </summary>
    private static string SanitizeName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "_");
        return name.Replace(" ", "_");
    }

    private static int BoolToInt(bool value) => value ? 1 : 0;
}
