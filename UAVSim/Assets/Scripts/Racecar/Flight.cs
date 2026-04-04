using UnityEngine;

/// <summary>
/// Accurate quadrotor flight physics based on the full 6-DOF equations of motion.
///
/// Reference: Wil Selby, "System Modeling" (ArduCopter research)
/// https://wilselby.com/research/arducopter/modeling/
///
/// Implements:
///   - 4-motor thrust model: thrust proportional to w^2 (motor angular velocity squared)
///   - Motor mixing for X-configuration quadrotor
///   - Translational dynamics: gravity + thrust (rotated to global frame) + aerodynamic drag
///   - Rotational dynamics: motor torques + propeller gyroscopic effects
///     (rigid-body gyroscopic term wxIw is handled by Unity's Rigidbody inertia tensor)
///   - PD attitude stabilization controller (inner loop) with properly tuned gains
///   - Motor saturation handling (attitude priority over altitude)
///   - Angular velocity safety limiting
///   - Position hold mode: active velocity damping when no horizontal input
///   - Altitude hold mode: PD altitude controller when no vertical input
///   - Arm/disarm state: motors off until armed, auto-launch on first arm
///
/// The user-facing API remains unchanged: ForwardSpeed, StrafeSpeed, VerticalSpeed,
/// YawSpeed as [-1, 1] inputs, plus MaxSpeed as a scaling factor.
/// Internally, these inputs are translated to desired attitude angles and thrust,
/// which are then achieved through a PD controller that computes individual motor speeds.
/// </summary>
public class Flight : RacecarModule
{
    #region Constants — Physical Parameters

    /// <summary>
    /// The default value of MaxSpeed (user-facing throttle scale, 0-1).
    /// 0.50 gives responsive flight out-of-the-box; user can still bump up to 1.0.
    /// </summary>
    public const float DefaultMaxSpeed = 0.50f;

    // --------- Drone Geometry ---------

    /// <summary>
    /// Distance from the center of mass to each motor (meters).
    /// Measured from propeller center positions in our OBJ model.
    /// </summary>
    private const float armLength = 0.16f;

    /// <summary>
    /// armLength / sqrt(2) -- the effective roll/pitch moment arm for an X-configuration.
    /// In an X-quad, motors are at +/-45 deg from the forward axis, so the perpendicular
    /// distance to the roll or pitch axis is L*sin(45) = L/sqrt(2).
    /// </summary>
    private static readonly float armEffective = armLength * Mathf.Sqrt(2f) / 2f;

    // --------- Motor / Propeller Model ---------

    /// <summary>
    /// Thrust coefficient kf (N/(rad/s)^2).
    /// Thrust per motor: F_i = kf * w_i^2
    /// </summary>
    private const float kf = 9.13e-6f;

    /// <summary>
    /// Torque (drag) coefficient km (N*m/(rad/s)^2).
    /// Reaction torque per motor: tau_i = km * w_i^2
    /// Increased from physical value (1.83e-7) for snappier yaw response in simulation.
    /// </summary>
    private const float km = 5.0e-7f;

    /// <summary>
    /// Propeller moment of inertia about the spin axis (kg*m^2).
    /// Used for the propeller gyroscopic effect: JTP * Omega_r * (w x z_body).
    /// </summary>
    private const float JTP = 3.36e-5f;

    /// <summary>
    /// Maximum motor angular velocity (rad/s). Approx 12000 RPM.
    /// </summary>
    private const float motorSpeedMax = 1256.6f;

    /// <summary>
    /// Minimum motor angular velocity (rad/s). Motors idle at 0.
    /// </summary>
    private const float motorSpeedMin = 0f;

    // --------- Inertia Tensor ---------
    // Set on the Rigidbody in Start() so Unity computes wxIw automatically.

    /// <summary>Moment of inertia about the X axis (pitch). kg*m^2.
    /// Very low for racing-style snap response.</summary>
    private const float Ixx = 0.003f;

    /// <summary>Moment of inertia about the Y axis (yaw). kg*m^2.
    /// Halved for quick yaw flicks.</summary>
    private const float Iyy = 0.006f;

    /// <summary>Moment of inertia about the Z axis (roll). kg*m^2.
    /// Very low for racing-style snap response.</summary>
    private const float Izz = 0.003f;

    // --------- Aerodynamic Drag ---------

    /// <summary>
    /// Quadratic drag coefficient in the horizontal plane (N/(m/s)²).
    /// Drag force is proportional to v², giving minimal drag at low speed (explosive
    /// acceleration) and strong drag near terminal velocity (same max speed).
    /// Terminal velocity at full tilt (35°): v_max = sqrt(m*g*tan(35°) / dragXZ) ≈ 35 m/s.
    /// </summary>
    private const float dragXZ = 0.011f;

    /// <summary>
    /// Linear drag coefficient along the vertical axis (N/(m/s)).
    /// Lower = faster ascent/descent response.
    /// </summary>
    private const float dragY = 0.12f;

    /// <summary>
    /// Rotational drag coefficient (N*m/(rad/s)).
    /// </summary>
    private const float angularDragCoeff = 0.005f;

    // --------- Attitude Controller Gains ---------

    /// <summary>Proportional gain for roll and pitch angle (N*m/rad).
    /// Very high = racing snap, drone flicks to desired tilt near-instantly.</summary>
    private const float Kp_rp = 10.0f;

    /// <summary>Derivative gain for roll and pitch rate (N*m/(rad/s)).
    /// Very low = minimal resistance to attitude changes, zero mushiness.</summary>
    private const float Kd_rp = 0.06f;

    /// <summary>Proportional gain for yaw rate control (N*m/(rad/s)).
    /// Time constant = Iyy/Kp_yaw = 0.006/0.55 = 0.011s — instant yaw.
    /// </summary>
    private const float Kp_yaw = 0.55f;

    // --------- User Input Limits ---------

    /// <summary>
    /// Maximum tilt angle the user can command (degrees).
    /// 35° at full scale → ~35 m/s terminal velocity.
    /// </summary>
    private const float maxTiltAngle = 35f;

    /// <summary>
    /// Maximum yaw rate the user can command (degrees/second).
    /// 200 deg/s = full 360 in 1.8 seconds — smooth but responsive.
    /// </summary>
    private const float maxYawRate = 200f;

    /// <summary>
    /// Maximum additional vertical thrust as a fraction of hover thrust.
    /// 1.8 = very punchy vertical movement.
    /// </summary>
    private const float maxVerticalThrustFraction = 1.8f;

    /// <summary>
    /// Maximum angular velocity magnitude (rad/s) before safety clamping.
    /// </summary>
    private const float maxAngularVelocity = 3.0f;

    /// <summary>
    /// Maximum tilt compensation factor for altitude hold under tilt.
    /// </summary>
    private const float maxTiltCompensation = 1.15f;

    /// <summary>
    /// Maximum bank (roll) angle induced by yaw input for coordinated turns (degrees).
    /// Creates a natural banking feeling during yaw rotation, coupling yaw and roll
    /// like a real aircraft. Scales with MaxSpeed.
    /// </summary>
    private const float bankAngle = 15f;

    // --------- Position Hold ---------

    /// <summary>
    /// Gain for velocity-based position hold (normalized input per m/s of raw velocity).
    /// When no horizontal stick input, the drone tilts to oppose its velocity.
    /// At 2 m/s raw: input = 1.2 -> clamped to 1.0 -> max tilt -> aggressive braking.
    /// </summary>
    private const float posHoldVelGain = 0.60f;

    /// <summary>
    /// Heavy linear drag applied in the horizontal plane when no stick input.
    /// This simulates the drone's flight controller actively braking.
    /// </summary>
    private const float posHoldBrakeDrag = 4.5f;

    /// <summary>
    /// Extra drag applied when the stick direction opposes the current velocity.
    /// This lets the drone decelerate hard on direction changes without reducing top speed.
    /// </summary>
    private const float reverseBrakeDrag = 2.5f;

    /// <summary>
    /// Heavy linear drag applied to the velocity component PERPENDICULAR to stick input.
    /// When the pilot changes direction (e.g. forward → right), this kills the unwanted
    /// cross-velocity almost instantly, giving razor-sharp directional control without
    /// affecting acceleration or top speed in the desired direction.
    /// </summary>
    private const float crossBrakeDrag = 6.0f;

    /// <summary>
    /// Horizontal speed (m/s) below which the drone snaps to a full stop.
    /// Prevents tiny residual drift after braking.
    /// </summary>
    private const float posHoldDeadZone = 0.08f;

    // --------- Altitude Hold ---------

    /// <summary>
    /// Proportional gain for altitude hold (thrust fraction per meter of altitude error).
    /// At 1m below target: 0.5 * hoverThrust extra -> steady climb.
    /// Clamped to maxVerticalThrustFraction.
    /// </summary>
    private const float Kp_alt = 0.5f;

    /// <summary>
    /// Derivative gain for altitude hold (thrust fraction per m/s of vertical velocity).
    /// Damps vertical oscillation around target altitude.
    /// </summary>
    private const float Kd_alt = 0.8f;

    // --------- Launch ---------

    /// <summary>
    /// Altitude above the starting position (meters) that the drone auto-ascends to on launch.
    /// </summary>
    private const float launchAltitude = 7.5f;

    #endregion

    #region Public Interface

    /// <summary>
    /// The input forward/backward speed, ranging from -1 (full backward) to 1 (full forward).
    /// </summary>
    public float ForwardSpeed { get; set; } = 0;

    /// <summary>
    /// The input strafe (left/right) speed, ranging from -1 (full left) to 1 (full right).
    /// </summary>
    public float StrafeSpeed { get; set; } = 0;

    /// <summary>
    /// The input vertical speed, ranging from -1 (full descent) to 1 (full ascent).
    /// </summary>
    public float VerticalSpeed { get; set; } = 0;

    /// <summary>
    /// The input yaw rotation, ranging from -1 (full left) to 1 (full right).
    /// </summary>
    public float YawSpeed { get; set; } = 0;

    /// <summary>
    /// The max speed multiplier set by the user, ranging from 0 to 1.
    /// </summary>
    public float MaxSpeed { get; set; } = Flight.DefaultMaxSpeed;

    /// <summary>
    /// Current motor angular velocities (rad/s) for all 4 motors.
    /// Index 0 = front-right, 1 = front-left, 2 = back-left, 3 = back-right.
    /// </summary>
    public float[] MotorSpeeds { get; private set; } = new float[4];

    /// <summary>
    /// Current motor RPMs for visualization / telemetry.
    /// </summary>
    public float[] MotorRPMs
    {
        get
        {
            float[] rpms = new float[4];
            for (int i = 0; i < 4; i++)
            {
                rpms[i] = MotorSpeeds[i] * 60f / (2f * Mathf.PI);
            }
            return rpms;
        }
    }

    /// <summary>
    /// Whether the drone is armed (motors active). Starts false.
    /// </summary>
    public bool IsArmed { get; private set; } = false;

    /// <summary>
    /// Whether the drone is currently in the auto-launch ascent phase.
    /// </summary>
    public bool IsLaunching { get; private set; } = false;

    /// <summary>
    /// Whether the drone is in auto-land mode (descending gently to ground, then disarming).
    /// </summary>
    public bool IsLanding { get; private set; } = false;

    /// <summary>
    /// The current target altitude for altitude hold (world Y coordinate).
    /// </summary>
    public float TargetAltitude { get; private set; } = 0f;

    /// <summary>
    /// The Y position of the ground where the drone spawned. Used as landing target.
    /// </summary>
    private float groundY = 0f;

    /// <summary>
    /// Arms the drone and begins the auto-launch sequence.
    /// The drone will ascend to launchAltitude meters above its current position,
    /// then switch to normal pos-hold / alt-hold flight.
    /// </summary>
    public void Arm()
    {
        if (IsArmed) return;
        IsArmed = true;
        IsLaunching = true;
        IsLanding = false;
        groundY = this.transform.position.y;
        TargetAltitude = this.transform.position.y + launchAltitude;

        // Zero out all velocity and force level attitude so launch goes straight up
        this.rBody.linearVelocity = Vector3.zero;
        this.rBody.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(0f, this.transform.eulerAngles.y, 0f);

        Debug.Log($"[Flight] Armed! Launching to altitude {TargetAltitude:F1}m");
    }

    /// <summary>
    /// Re-triggers the auto-launch sequence (used after crash respawn/reset).
    /// Only works if the drone is already armed.
    /// </summary>
    public void Relaunch()
    {
        if (!IsArmed) return;
        IsLaunching = true;
        IsLanding = false;
        groundY = this.transform.position.y;
        TargetAltitude = this.transform.position.y + launchAltitude;

        // Clean start for relaunch
        this.rBody.linearVelocity = Vector3.zero;
        this.rBody.angularVelocity = Vector3.zero;

        Debug.Log($"[Flight] Relaunching to altitude {TargetAltitude:F1}m");
    }

    /// <summary>
    /// Begins a gentle auto-land sequence. The drone descends under altitude hold
    /// to the surface below, then fully disarms. Works on any surface (ground, rooftops, etc.).
    /// </summary>
    public void BeginLanding()
    {
        if (!IsArmed || IsLanding) return;
        IsLanding = true;
        IsLaunching = false;

        // Clear all user inputs
        this.ForwardSpeed = 0;
        this.StrafeSpeed = 0;
        this.VerticalSpeed = 0;
        this.YawSpeed = 0;

        // Raycast down to find the actual surface below (not just the spawn ground)
        float landingY = groundY;
        RaycastHit hit;
        if (Physics.Raycast(this.transform.position, Vector3.down, out hit, 500f))
        {
            landingY = hit.point.y;
        }

        TargetAltitude = landingY + 0.1f;

        Debug.Log($"[Flight] Landing initiated — target surface at Y={landingY:F1}");
    }

    /// <summary>
    /// Fully disarms the drone (called internally when auto-land completes).
    /// </summary>
    private void FinishDisarm()
    {
        IsArmed = false;
        IsLanding = false;
        IsLaunching = false;
        this.ForwardSpeed = 0;
        this.StrafeSpeed = 0;
        this.VerticalSpeed = 0;
        this.YawSpeed = 0;
        this.rBody.linearVelocity = Vector3.zero;
        this.rBody.angularVelocity = Vector3.zero;
        for (int i = 0; i < 4; i++) this.MotorSpeeds[i] = 0f;
        Debug.Log("[Flight] Landed and disarmed");
    }

    /// <summary>
    /// Stops all drone movement (sets all inputs to zero).
    /// Does NOT disarm the drone.
    /// </summary>
    public void Stop()
    {
        this.ForwardSpeed = 0;
        this.StrafeSpeed = 0;
        this.VerticalSpeed = 0;
        this.YawSpeed = 0;
    }

    #endregion

    #region Private State

    /// <summary>
    /// The rigidbody of the drone.
    /// </summary>
    private Rigidbody rBody;

    /// <summary>
    /// Motor yaw torque sign for each motor.
    /// CW motors produce positive yaw reaction torque, CCW produce negative.
    /// X-configuration (looking from above):
    ///   1 (FL, CW)    0 (FR, CCW)
    ///   2 (BL, CCW)   3 (BR, CW)
    /// So: 0(FR,CCW)=-1, 1(FL,CW)=+1, 2(BL,CCW)=-1, 3(BR,CW)=+1.
    /// </summary>
    private static readonly float[] motorYawSign = { -1f, 1f, -1f, 1f };

    /// <summary>
    /// Motor roll torque contribution sign (depends on physical position, not spin).
    /// </summary>
    private static readonly float[] motorRollSign = { -1f, 1f, 1f, -1f };

    /// <summary>
    /// Motor pitch torque contribution sign (depends on physical position, not spin).
    /// </summary>
    private static readonly float[] motorPitchSign = { -1f, -1f, 1f, 1f };

    /// <summary>
    /// Temporary array for per-motor desired thrust during mixing.
    /// </summary>
    private float[] motorThrusts = new float[4];

    /// <summary>
    /// Reference to the CrashSystem for damage state queries.
    /// </summary>
    private CrashSystem crashSystem;

    #endregion

    protected override void Awake()
    {
        this.rBody = this.GetComponent<Rigidbody>();
        this.crashSystem = this.GetComponent<CrashSystem>();
        base.Awake();
    }

    private void Start()
    {
        // === Configure Rigidbody for accurate physics ===
        this.rBody.useGravity = true;
        this.rBody.linearDamping = 0f;
        this.rBody.angularDamping = 0.02f;

        // Explicitly set center of mass at the transform origin.
        // Without this, Unity computes CoM from the collider shape, which can
        // be offset and causes the drone to behave as if its CoG shifts with tilt.
        this.rBody.centerOfMass = Vector3.zero;

        // Set the inertia tensor so Unity correctly computes the rigid-body
        // gyroscopic term (w x Iw) in Euler's rotation equations.
        this.rBody.inertiaTensor = new Vector3(Ixx, Iyy, Izz);
        this.rBody.inertiaTensorRotation = Quaternion.identity;

        this.rBody.constraints = RigidbodyConstraints.None;
        this.rBody.maxAngularVelocity = maxAngularVelocity * 2f;
        this.rBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        this.rBody.interpolation = RigidbodyInterpolation.Interpolate;

        // Initialize target altitude and ground reference
        TargetAltitude = this.transform.position.y;
        groundY = this.transform.position.y;
    }

    private void FixedUpdate()
    {
        // ================================================================
        // 0. ARM CHECK — if not armed, zero all motors and do nothing
        // ================================================================
        if (!IsArmed)
        {
            for (int i = 0; i < 4; i++) this.MotorSpeeds[i] = 0f;
            return;
        }

        // ================================================================
        // 0a. LANDING CHECK — if near ground during auto-land, finish disarm
        // ================================================================
        if (IsLanding)
        {
            float altAboveTarget = this.transform.position.y - TargetAltitude;
            float vertSpeed = Mathf.Abs(this.rBody.linearVelocity.y);

            // Landed: close to target surface and barely moving vertically
            if (altAboveTarget < 0.3f && vertSpeed < 0.3f)
            {
                FinishDisarm();
                return;
            }
        }

        // ================================================================
        // 0b. CRASH CHECK — skip all motor logic if drone is destroyed
        // ================================================================
        if (crashSystem != null && crashSystem.IsDestroyed)
        {
            for (int i = 0; i < 4; i++) this.MotorSpeeds[i] = 0f;
            return;
        }

        // ================================================================
        // 1. READ CURRENT STATE
        // ================================================================

        Vector3 euler = this.transform.eulerAngles;
        float currentPitch = NormalizeAngle(euler.x);
        float currentRoll = NormalizeAngle(euler.z);

        Vector3 angVelWorld = this.rBody.angularVelocity;
        Vector3 angVelBody = this.transform.InverseTransformDirection(angVelWorld);
        float p = angVelBody.x; // pitch rate
        float q = angVelBody.y; // yaw rate
        float r = angVelBody.z; // roll rate

        // ================================================================
        // 2. SAFETY: ANGULAR VELOCITY LIMITING
        // ================================================================
        float angVelMag = angVelBody.magnitude;
        if (angVelMag > maxAngularVelocity)
        {
            this.rBody.angularVelocity = this.rBody.angularVelocity * (maxAngularVelocity / angVelMag);
            angVelWorld = this.rBody.angularVelocity;
            angVelBody = this.transform.InverseTransformDirection(angVelWorld);
            p = angVelBody.x;
            q = angVelBody.y;
            r = angVelBody.z;
        }

        // ================================================================
        // 2b. LAUNCH STABILIZATION: kinematic ascent to target altitude.
        //     Bypasses the physics engine entirely — directly sets position,
        //     rotation, and velocity each frame. This eliminates the
        //     limit-cycle oscillation caused by MoveRotation fighting motor
        //     torques on a non-kinematic Rigidbody.
        //
        //     If the user provides any horizontal input, launch ends early
        //     and normal motor-mixing takes over immediately.
        // ================================================================
        if (IsLaunching)
        {
            bool userInput = Mathf.Abs(this.ForwardSpeed) > 0.05f || Mathf.Abs(this.StrafeSpeed) > 0.05f;
            if (userInput)
            {
                IsLaunching = false;
                Debug.Log("[Flight] User took over during launch — exiting launch stabilization");
                // Fall through to normal motor mixing below
            }
            else
            {
                float altError = TargetAltitude - this.transform.position.y;
                float vertVel = this.rBody.linearVelocity.y;

                // Kinematic ascent: move drone directly upward, no physics forces
                float launchSpeed = Mathf.Clamp(altError * Kp_alt - vertVel * Kd_alt, -maxVerticalThrustFraction * 5f, maxVerticalThrustFraction * 5f);
                Vector3 newPos = this.transform.position + Vector3.up * launchSpeed * Time.fixedDeltaTime;

                // Lock attitude flat at current yaw — directly set transform, no physics
                float currentYaw = this.transform.eulerAngles.y;
                this.rBody.Move(newPos, Quaternion.Euler(0f, currentYaw, 0f));

                // Zero all physics velocities so no residual momentum fights us
                this.rBody.linearVelocity = new Vector3(0f, launchSpeed, 0f);
                this.rBody.angularVelocity = Vector3.zero;

                // Display symmetric motor speeds for telemetry only
                float launchGravity = Physics.gravity.magnitude;
                float launchHoverThrust = this.rBody.mass * launchGravity;
                float launchThrustPerMotor = launchHoverThrust / 4f;
                float launchOmega = Mathf.Clamp(Mathf.Sqrt(launchThrustPerMotor / kf), motorSpeedMin, motorSpeedMax);
                for (int i = 0; i < 4; i++) this.MotorSpeeds[i] = launchOmega;

                // Check if launch is complete
                if (Mathf.Abs(altError) < 0.5f && Mathf.Abs(vertVel) < 0.5f)
                {
                    IsLaunching = false;
                    TargetAltitude = this.transform.position.y;
                    Debug.Log("[Flight] Launch complete - entering altitude hold");
                }

                return; // Skip motor mixing — kinematic launch handles everything
            }
        }

        // ================================================================
        // 3. POSITION HOLD: when no horizontal input, oppose velocity
        // ================================================================
        float effectiveForward = this.ForwardSpeed;
        float effectiveStrafe = this.StrafeSpeed;

        bool noHorizontalInput = Mathf.Abs(this.ForwardSpeed) < 0.05f && Mathf.Abs(this.StrafeSpeed) < 0.05f;
        bool noVerticalInput = Mathf.Abs(this.VerticalSpeed) < 0.05f;
        bool pureVerticalFlight = noHorizontalInput && !noVerticalInput;

        if (noHorizontalInput && !pureVerticalFlight)
        {
            // Convert world velocity to body-frame horizontal components
            Vector3 localVel = this.transform.InverseTransformDirection(this.rBody.linearVelocity);

            // Oppose forward velocity with pitch, lateral velocity with roll
            // localVel.z = forward speed in body frame, localVel.x = right speed
            effectiveForward = Mathf.Clamp(-localVel.z * posHoldVelGain, -1f, 1f);
            effectiveStrafe = Mathf.Clamp(-localVel.x * posHoldVelGain, -1f, 1f);
        }
        else if (pureVerticalFlight)
        {
            // Pure vertical flight (Shift held, no WASD): force level attitude.
            // Position hold tilt at high vertical speeds can cause oscillation/instability.
            // Drag alone handles any horizontal drift; PD controller keeps it level.
            effectiveForward = 0f;
            effectiveStrafe = 0f;
        }

        // ================================================================
        // 4. COMPUTE DESIRED STATE FROM INPUTS
        // ================================================================

        float scale = this.MaxSpeed;

        // Forward input -> desired pitch angle
        float desiredPitch = effectiveForward * maxTiltAngle * scale;

        // Strafe input -> desired roll angle
        float desiredRoll = -effectiveStrafe * maxTiltAngle * scale;

        // Yaw input -> desired yaw rate (always full authority, independent of speed scale)
        float desiredYawRate = this.YawSpeed * maxYawRate * Mathf.Deg2Rad;

        // Banking: yaw input induces a coordinated roll into the turn.
        // Positive YawSpeed (right) → negative desiredRoll (bank right).
        desiredRoll -= this.YawSpeed * bankAngle * scale;

        // ================================================================
        // 5. ALTITUDE HOLD / VERTICAL THRUST
        // ================================================================

        float gravity = Physics.gravity.magnitude;
        float hoverThrust = this.rBody.mass * gravity;
        float thrustOffset;

        if (noVerticalInput)
        {
            // PD altitude hold: maintain TargetAltitude
            float altError = TargetAltitude - this.transform.position.y;
            float vertVel = this.rBody.linearVelocity.y;

            float altFraction = Kp_alt * altError - Kd_alt * vertVel;
            altFraction = Mathf.Clamp(altFraction, -maxVerticalThrustFraction, maxVerticalThrustFraction);
            thrustOffset = altFraction * hoverThrust;

            // If launching and close to target, end launch phase
            if (IsLaunching && Mathf.Abs(altError) < 0.5f && Mathf.Abs(vertVel) < 0.5f)
            {
                IsLaunching = false;
                Debug.Log("[Flight] Launch complete - entering altitude hold");
            }
        }
        else
        {
            // User commanding vertical motion: normal thrust
            thrustOffset = this.VerticalSpeed * scale * maxVerticalThrustFraction * hoverThrust;

            // Continuously update target altitude so that when the user releases
            // the stick, altitude hold kicks in at the current altitude.
            TargetAltitude = this.transform.position.y;
        }

        float desiredTotalThrust = hoverThrust + thrustOffset;

        // Gentle tilt compensation
        float cosRoll = Mathf.Cos(currentRoll * Mathf.Deg2Rad);
        float cosPitch = Mathf.Cos(currentPitch * Mathf.Deg2Rad);
        float tiltFactor = cosRoll * cosPitch;
        if (tiltFactor > 0.01f)
        {
            float compensation = Mathf.Min(1f / tiltFactor, maxTiltCompensation);
            desiredTotalThrust *= compensation;
        }

        // ================================================================
        // 6. PD ATTITUDE CONTROLLER -> DESIRED TORQUES
        // ================================================================

        float pitchError = (desiredPitch - currentPitch) * Mathf.Deg2Rad;
        float rollError = (desiredRoll - currentRoll) * Mathf.Deg2Rad;
        float yawRateError = desiredYawRate - q;

        float tauPitch = Kp_rp * pitchError - Kd_rp * p;
        float tauRoll = Kp_rp * rollError - Kd_rp * r;
        float tauYaw = Kp_yaw * yawRateError;

        // ================================================================
        // 7. MOTOR MIXING (X-CONFIGURATION) WITH SATURATION HANDLING
        // ================================================================

        float thrustPerMotor = desiredTotalThrust / 4f;
        float rollMix = 1f / (4f * armEffective);
        float pitchMix = 1f / (4f * armEffective);
        float yawMix = 1f / (4f * (km / kf));

        float minFi = float.MaxValue;
        float maxFi = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            motorThrusts[i] = thrustPerMotor
                             + motorRollSign[i] * tauRoll * rollMix
                             + motorPitchSign[i] * tauPitch * pitchMix
                             + motorYawSign[i] * tauYaw * yawMix;

            if (motorThrusts[i] < minFi) minFi = motorThrusts[i];
            if (motorThrusts[i] > maxFi) maxFi = motorThrusts[i];
        }

        float Fmax = kf * motorSpeedMax * motorSpeedMax;

        if (minFi < 0f)
        {
            float boost = -minFi;
            for (int i = 0; i < 4; i++) motorThrusts[i] += boost;
            maxFi += boost;
        }

        if (maxFi > Fmax)
        {
            float reduction = maxFi - Fmax;
            for (int i = 0; i < 4; i++) motorThrusts[i] -= reduction;
        }

        for (int i = 0; i < 4; i++)
        {
            float Fi = Mathf.Max(motorThrusts[i], 0f);
            float omega = Mathf.Sqrt(Fi / kf);
            this.MotorSpeeds[i] = Mathf.Clamp(omega, motorSpeedMin, motorSpeedMax);
        }

        // Apply crash damage
        if (crashSystem != null)
        {
            float globalScale = crashSystem.MotorOutputScale;
            for (int i = 0; i < 4; i++)
            {
                this.MotorSpeeds[i] *= crashSystem.MotorHealth[i] * globalScale;
            }
        }

        // ================================================================
        // 8. COMPUTE ACTUAL FORCES & TORQUES FROM MOTOR SPEEDS
        // ================================================================

        float actualThrust = 0f;
        float actualTauPitch = 0f;
        float actualTauRoll = 0f;
        float actualTauYaw = 0f;
        float Omega_r = 0f;

        for (int i = 0; i < 4; i++)
        {
            float wi = this.MotorSpeeds[i];
            float Fi = kf * wi * wi;

            actualThrust += Fi;
            actualTauPitch += motorPitchSign[i] * Fi * armEffective;
            actualTauRoll += motorRollSign[i] * Fi * armEffective;
            actualTauYaw += motorYawSign[i] * km * wi * wi;

            Omega_r += motorYawSign[i] * wi;
        }

        // ================================================================
        // 9. APPLY FORCES AND TORQUES
        // ================================================================

        // Thrust along drone's local UP
        this.rBody.AddRelativeForce(Vector3.up * actualThrust);

        // Motor torques in body frame
        this.rBody.AddRelativeTorque(new Vector3(actualTauPitch, actualTauYaw, actualTauRoll));

        // Propeller gyroscopic effect
        Vector3 propGyro = JTP * Omega_r * new Vector3(-r, 0f, p);
        this.rBody.AddRelativeTorque(propGyro);

        // ── Aerodynamic drag (velocity-decomposed) ──
        // Horizontal velocity is split into a component ALIGNED with the stick
        // direction and a CROSS (perpendicular) component.
        //  • Aligned: quadratic drag → near-zero at low speed (explosive accel),
        //    strong at terminal velocity (caps top speed).
        //  • Cross: heavy linear drag → kills unwanted sideways momentum almost
        //    instantly → razor-sharp direction changes.
        // Vertical drag remains simple linear.

        Vector3 vel = this.rBody.linearVelocity;
        float hSpeed = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
        Vector3 dragForce;

        if (noHorizontalInput)
        {
            // Position hold: strong LINEAR braking drag (constant deceleration feel)
            float brakeDrag = posHoldBrakeDrag;

            // Dead zone: snap to zero when nearly stopped
            if (hSpeed < posHoldDeadZone)
            {
                this.rBody.linearVelocity = new Vector3(0f, vel.y, 0f);
                vel = this.rBody.linearVelocity;
            }

            dragForce = new Vector3(
                -brakeDrag * vel.x,
                -dragY * vel.y,
                -brakeDrag * vel.z
            );
        }
        else
        {
            // Active flight: decompose horizontal velocity
            Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);
            Vector3 stickWorld = this.transform.forward * this.ForwardSpeed
                               + this.transform.right * this.StrafeSpeed;
            stickWorld.y = 0f; // Project to horizontal plane — transform.forward tilts with the drone

            if (stickWorld.sqrMagnitude > 0.001f && velXZ.sqrMagnitude > 0.01f)
            {
                Vector3 stickDir = stickWorld.normalized;
                float speedXZ = velXZ.magnitude;
                float alignedMag = Vector3.Dot(velXZ, stickDir);
                Vector3 alignedVel = stickDir * alignedMag;
                Vector3 crossVel = velXZ - alignedVel;

                // Aligned component: quadratic drag (+ reverse brake if opposing)
                float alignedCoeff = dragXZ * Mathf.Abs(alignedMag);
                if (alignedMag < 0f) alignedCoeff += reverseBrakeDrag;

                // Cross component: scale braking by how perpendicular the velocity is
                // to the stick. When steering (W+D), velocity is mostly aligned →
                // minimal cross-brake → speed preserved. When doing a hard 90° swap
                // (release W, press D only), velocity is fully perpendicular → full brake.
                float alignedFraction = Mathf.Clamp01(alignedMag / speedXZ);
                float crossCoeff = crossBrakeDrag * (1f - alignedFraction);

                Vector3 hDrag = -alignedCoeff * alignedVel - crossCoeff * crossVel;
                dragForce = new Vector3(hDrag.x, -dragY * vel.y, hDrag.z);
            }
            else
            {
                // Negligible stick or velocity: fall back to simple quadratic
                float coeff = dragXZ * hSpeed;
                dragForce = new Vector3(-coeff * vel.x, -dragY * vel.y, -coeff * vel.z);
            }
        }

        this.rBody.AddForce(dragForce);

        // Aerodynamic angular drag
        Vector3 angDrag = -angularDragCoeff * angVelBody;
        this.rBody.AddRelativeTorque(angDrag);

        // ================================================================
        // 10. MOMENTUM CONSERVATION DURING YAW
        //     Rotate the horizontal velocity vector to follow the yaw rotation.
        //     This preserves speed when turning — the drone redirects its
        //     momentum instead of fighting drag to rebuild speed in a new direction.
        // ================================================================
        if (Mathf.Abs(this.YawSpeed) > 0.05f)
        {
            Vector3 currentVel = this.rBody.linearVelocity;
            float hSpeedSq = currentVel.x * currentVel.x + currentVel.z * currentVel.z;
            if (hSpeedSq > 0.1f)
            {
                float yawDeltaDeg = desiredYawRate * Mathf.Rad2Deg * Time.fixedDeltaTime;
                Vector3 hVel = new Vector3(currentVel.x, 0f, currentVel.z);
                Vector3 rotatedHVel = Quaternion.Euler(0f, yawDeltaDeg, 0f) * hVel;
                this.rBody.linearVelocity = new Vector3(rotatedHVel.x, currentVel.y, rotatedHVel.z);
            }
        }
    }

    /// <summary>
    /// Normalizes a Unity Euler angle (0-360) to the range [-180, 180].
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}