using UnityEngine;

/// <summary>
/// Encapsulates a UAV (drone) unit.
/// Based on the original RACECAR-MN class, adapted for drone flight.
/// </summary>
public class Racecar : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The cameras through which the user can observe the drone.
    /// </summary>
    [SerializeField]
    private Camera[] playerCameras;
    #endregion

    #region Constants
    /// <summary>
    /// The distance from which each player camera follows the drone.
    /// Close-up aerial perspective.
    /// </summary>
    private static readonly Vector3[] cameraOffsets =
    {
        new Vector3(0, 1.8f, -3.5f),  // Behind and slightly above (zoomed out)
        new Vector3(0, 7.0f, -0.6f),  // Top-down view
        new Vector3(0, 1.8f, 3.5f)    // Front view
    };

    /// <summary>
    /// The speed at which the camera follows the drone.
    /// </summary>
    private const float cameraSpeed = 8;
    #endregion

    #region Public Interface
    /// <summary>
    /// The index of the drone.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Exposes the RealSense D435i color and depth channels.
    /// </summary>
    public CameraModule Camera { get; private set; }

    /// <summary>
    /// Exposes the drone flight controller.
    /// </summary>
    public Flight Flight { get; private set; }

    /// <summary>
    /// Exposes the RealSense D435i IMU.
    /// </summary>
    public PhysicsModule Physics { get; private set; }

    /// <summary>
    /// Exposes the crash / damage system.
    /// </summary>
    public CrashSystem CrashSystem { get; private set; }

    /// <summary>
    /// The heads-up display controlled by this drone, if any.
    /// </summary>
    public Hud Hud { get; set; }

    /// <summary>
    /// The center point of the drone.
    /// </summary>
    public Vector3 Center
    {
        get
        {
            return this.transform.position;
        }
    }

    /// <summary>
    /// Called on the first frame when the drone enters default drive mode.
    /// </summary>
    public void DefaultDriveStart()
    {
        this.Flight.MaxSpeed = Flight.DefaultMaxSpeed;
        this.Flight.Stop();
    }

    /// <summary>
    /// Called each frame that the drone is in default drive mode.
    /// Handles manual flight controls:
    ///   - E / Controller A: Arm drone and auto-launch
    ///   - WASD / Left Joystick: Forward/Back/Strafe (only when armed)
    ///   - Right Shift / Right Trigger: Ascend
    ///   - Left Shift / Left Trigger: Descend
    ///   - Arrow Keys / Right Joystick X: Yaw rotation
    ///   - LB/RB: Adjust max speed
    /// </summary>
    public void DefaultDriveUpdate()
    {
        // ── ARM / LAND TOGGLE ──
        // E key on keyboard, or A button (1 key) on controller
        if (Input.GetKeyDown(KeyCode.E) || Controller.WasPressed(Controller.Button.A))
        {
            if (!this.Flight.IsArmed)
            {
                this.Flight.Arm();
                LevelManager.ShowMessage("Drone activated - launching!", Color.green, 3f);
            }
            else
            {
                this.Flight.BeginLanding();
                LevelManager.ShowMessage("Landing...", Color.yellow, 3f);
            }
        }

        // While disarmed or landing, no manual flight controls
        if (!this.Flight.IsArmed || this.Flight.IsLanding)
        {
            return;
        }

        // ── FLIGHT CONTROLS (only when armed and not landing) ──

        // Left joystick Y (W/S) = forward/backward
        // Left joystick X (A/D) = strafe left/right
        Vector2 leftJoy = Controller.GetJoystick(Controller.Joystick.LEFT);
        this.Flight.ForwardSpeed = leftJoy.y;

        // Backward travel is 60% of forward travel
        if (this.Flight.ForwardSpeed < 0)
            this.Flight.ForwardSpeed *= 0.6f;

        this.Flight.StrafeSpeed = leftJoy.x;

        // Triggers = vertical movement (ascend/descend)
        this.Flight.VerticalSpeed = Controller.GetTrigger(Controller.Trigger.RIGHT) - Controller.GetTrigger(Controller.Trigger.LEFT);

        // Right joystick X (Arrow Left/Right) = yaw rotation
        Vector2 rightJoy = Controller.GetJoystick(Controller.Joystick.RIGHT);
        this.Flight.YawSpeed = rightJoy.x;

        // Use the bumpers to adjust max speed
        if (Controller.WasPressed(Controller.Button.RB))
        {
            this.Flight.MaxSpeed = Mathf.Min(this.Flight.MaxSpeed + 0.1f, 1);
        }
        if (Controller.WasPressed(Controller.Button.LB))
        {
            this.Flight.MaxSpeed = Mathf.Max(this.Flight.MaxSpeed - 0.1f, 0);
        }
    }

    /// <summary>
    /// Sets the render texture and audio listener of the player perspective cameras.
    /// </summary>
    /// <param name="texture">The render texture to which to assign the cameras.</param>
    /// <param name="enableAudio">True if the audio listeners of the cameras should be enabled.</param>
    public void SetPlayerCameraFeatures(RenderTexture texture, bool enableAudio)
    {
        foreach (Camera camera in this.playerCameras)
        {
            camera.targetTexture = texture;
            camera.GetComponent<AudioListener>().enabled = enableAudio;
        }
    }

    /// <summary>
    /// Sets the index of the drone.
    /// </summary>
    /// <param name="index">The index of the drone in the simulation.</param>
    public void SetIndex(int index)
    {
        this.Index = index;
    }

    /// <summary>
    /// Sets the player camera which shows the user's view of the drone.
    /// </summary>
    /// <param name="cameraIndex">The index of the player camera to use.</param>
    public void SetCamera(int cameraIndex)
    {
        this.playerCameras[this.curCamera].enabled = false;
        this.curCamera = cameraIndex;
        this.playerCameras[this.curCamera].enabled = true;
    }
    #endregion

    /// <summary>
    /// The index in PlayerCameras of the current active camera.
    /// </summary>
    private int curCamera;

    private void Awake()
    {
        this.curCamera = 0;

        // Find submodules
        this.Camera = this.GetComponent<CameraModule>();
        this.Flight = this.GetComponent<Flight>();
        this.Physics = this.GetComponent<PhysicsModule>();
        this.CrashSystem = this.GetComponent<CrashSystem>();

        // Begin with main player camera (0th camera)
        if (this.playerCameras.Length > 0)
        {
            this.playerCameras[0].enabled = true;
            for (int i = 1; i < this.playerCameras.Length; i++)
            {
                this.playerCameras[i].enabled = false;
            }
        }
    }

    private void Update()
    {
        // Toggle camera when the space bar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.playerCameras[this.curCamera].enabled = false;
            this.curCamera = (this.curCamera + 1) % this.playerCameras.Length;
            this.playerCameras[this.curCamera].enabled = true;
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < this.playerCameras.Length; i++)
        {
            Vector3 followPoint = this.transform.forward * Racecar.cameraOffsets[i].z;
            Vector3 targetCameraPosition = this.transform.position + new Vector3(followPoint.x, Racecar.cameraOffsets[i].y, followPoint.z);
            this.playerCameras[i].transform.position = Vector3.Lerp(
                this.playerCameras[i].transform.position,
                targetCameraPosition,
                Racecar.cameraSpeed * Time.deltaTime);

            this.playerCameras[i].transform.LookAt(this.transform.position + Vector3.up * 0.8f);
        }
    }
}
