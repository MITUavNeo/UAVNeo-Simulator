using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the heads-up display shown to the user during races with a single drone.
/// </summary>
public class Hud : ScreenManager, IAutograderHud
{
    #region Set in Unity Editor
    /// <summary>
    /// The textbox shown when the user fails an objective.
    /// </summary>
    [SerializeField]
    private GameObject FailureMessage;

    /// <summary>
    /// The textbox shown when the user successfully completes a lab.
    /// </summary>
    [SerializeField]
    private GameObject SuccessMessage;
    #endregion

    #region Constants
    /// <summary>
    /// The color used for the background of sensor visualizations.
    /// </summary>
    public static readonly Color SensorBackgroundColor = new Color(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// The alpha (transparency) of the Python icon when no script is connected.
    /// </summary>
    private const float unconnectedScriptAlpha = 0.25f;

    /// <summary>
    /// In the autograder, when the current time is this fraction of the time limit away from the time limit, the current time is shown as a warning color.
    /// </summary>
    private const float autograderWarningTimeRatio = 0.25f;

    /// <summary>
    /// The background color of the mode label when the simulation is in each SimulationMode.
    /// </summary>
    private static readonly Color[] modeColors =
    {
        new Color(0f, 0.75f, 0.25f), // default flight
        new Color(0.75f, 0f, 0.25f), // user program
        new Color(1f, 0.5f, 0f), // wait
        new Color(0f, 0f, 0f) // finished
    };

    /// <summary>
    /// The text displayed on the mode label when the simulation is in each SimulationMode.
    /// </summary>
    private static readonly string[] modeNames =
    {
        "Default Flight",
        "User Program",
        "Wait",
        "Finished"
    };
    #endregion

    #region Public Interface
    #region Overrides
    public override void HandleWin(float time, bool isNewBestTime = false)
    {
        this.SuccessMessage.SetActive(true);
        this.successMessageText.text = isNewBestTime ? "New Best Time!" : "Mission Accomplished!";
        this.successTimeText.text = $"Time: {time:F3} seconds";
    }

    public override void HandleFailure(int droneIndex, string reason)
    {
        this.FailureMessage.SetActive(true);
        this.failureText.text = reason;
    }

    public override void UpdateConnectedPrograms(bool[] connectedPrograms)
    {
        this.connectedProgramImage.color = connectedPrograms.Length > 0 ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, Hud.unconnectedScriptAlpha);
    }

    public override void UpdateMode(SimulationMode mode)
    {
        this.modeText.text = Hud.modeNames[(int)mode];
        this.modeBackgroundImage.color = Hud.modeColors[(int)mode];
    }

    public override void UpdateTimeScale(float timeScale)
    {
        this.timeWarpImage.color = new Color(1, 1, 1, Mathf.Max(0, 1 - Mathf.Sqrt(timeScale)));
        this.timeScaleText.text = timeScale >= 1 ? string.Empty : $"{Mathf.Round(1 / timeScale)}x Slow Motion";
    }

    public override void UpdateTime(float mainTime, float[] keyPointDurations)
    {
        // Update main time directly (replaces base.UpdateTime which used the
        // inherited texts[] array that Hud no longer populates).
        if (this.mainTimeText != null)
            this.mainTimeText.text = mainTime.ToString("F3");

        // If the level contains checkpoints, show the time spent on each checkpoint
        if (keyPointDurations.Length > 2)
        {
            string text = $"1) {keyPointDurations[1]:F3}";
            for (int i = 2; i < keyPointDurations.Length; i++)
            {
                if (keyPointDurations[i] == 0)
                {
                    text += $"\n{i}) --";
                }
                else
                {
                    text += $"\n{i}) {keyPointDurations[i]:F3}";
                }
            }

            this.checkpointTimesText.text = text;
        }
    }
    #endregion

    #region IAutograderHud
    void IAutograderHud.SetLevelInfo(int levelIndex, string title, string description)
    {
        this.autograderTitleText.text = $"<b>Trial {levelIndex + 1}</b> - {title}";
        this.autograderDescriptionText.text = description;
    }

    void IAutograderHud.UpdateScore(float score, float maxScore)
    {
        this.autograderScoreText.text = $"{score:F2}/{maxScore:F2}";

        if (score == maxScore)
        {
            this.autograderScoreText.color = Color.green;
        }
    }

    void IAutograderHud.UpdateTime(float time, float timeLimit)
    {
        // Update main time directly (replaces base.UpdateTime which used the
        // inherited texts[] array that Hud no longer populates).
        if (this.mainTimeText != null)
            this.mainTimeText.text = time.ToString("F3");

        if (time >= timeLimit)
        {
            this.mainTimeText.color = Color.red;
        }
        else if (timeLimit - time < timeLimit * Hud.autograderWarningTimeRatio)
        {
            this.mainTimeText.color = Color.yellow;
        }
    }

    void IAutograderHud.SetMaxTime(float maxTime)
    {
        this.maxTimeText.text = $"Max: {maxTime:F1}";
    }

    void IAutograderHud.SetTimeBonus(float maxTime, float bonus, bool isLastBracket)
    {
        if (bonus >= 0)
        {
            this.maxTimeText.text = $"Bonus: +{bonus} (under {maxTime:F1} sec)";
            this.maxTimeText.color = bonus > 0 ? Color.green : Color.white;
        }
        else
        {
            this.maxTimeText.text = $"Penalty: {bonus} (under {maxTime:F1} sec)";
            this.maxTimeText.color = isLastBracket ? Color.red : Color.yellow;
        }
    }
    #endregion

    /// <summary>
    /// The texture containing the depth camera visualization.
    /// </summary>
    public Texture2D DepthVisualization
    {
        get
        {
            return (Texture2D)this.depthFeedImage.texture;
        }
    }

    /// <summary>
    /// Updates the downward RGB camera feed on the HUD.
    /// Called each frame by CameraModule when the downward camera is available.
    /// </summary>
    /// <param name="renderTexture">The render texture from the downward camera.</param>
    public void UpdateDownwardFeed(RenderTexture renderTexture)
    {
        if (this.downwardFeedImage != null && renderTexture != null)
        {
            this.downwardFeedImage.texture = renderTexture;
        }
    }

    #endregion

    /// <summary>
    /// The RawImage for the downward (nadir) RGB camera feed.
    /// Wire up to the authored Nadir Camera panel's RawImage in the prefab Inspector.
    /// </summary>
    [SerializeField] private RawImage downwardFeedImage;

    // ════════════════════════════════════════════════════════════════════
    //   AUTHORED UI REFERENCES — wire up in Inspector
    //   Drag each Text/RawImage from the prefab hierarchy into the matching
    //   slot. Replaces the brittle texts[(int)Texts.X] / images[(int)Images.X]
    //   pattern. Runtime code uses these named fields directly.
    // ════════════════════════════════════════════════════════════════════
    [Header("Authored Texts (drag from prefab hierarchy)")]
    [SerializeField] private Text messageText;
    [SerializeField] private Text mainTimeText;
    [SerializeField] private Text checkpointTimesText;
    [SerializeField] private Text timeScaleText;
    [SerializeField] private Text modeText;
    [SerializeField] private Text failureText;
    [SerializeField] private Text successMessageText;
    [SerializeField] private Text successTimeText;
    [SerializeField] private Text autograderTitleText;
    [SerializeField] private Text autograderDescriptionText;
    [SerializeField] private Text autograderScoreText;
    [SerializeField] private Text maxTimeText;

    [Header("Authored Images (drag from prefab hierarchy)")]
    [SerializeField] private RawImage timeWarpImage;
    [SerializeField] private RawImage colorFeedImage;
    [SerializeField] private RawImage depthFeedImage;
    [SerializeField] private RawImage modeBackgroundImage;
    [SerializeField] private RawImage connectedProgramImage;
    [SerializeField] private RawImage pauseScreenImage;

    // Order MUST match enum declarations: Controller.Button (A, B, X, Y, LB, RB, LJOY, RJOY, START, BACK)
    // followed by Controller.Trigger (LEFT, RIGHT), then Controller.Joystick (LEFT, RIGHT) — 14 entries total.
    [Header("Controller Images: btns (A, B, X, Y, LB, RB, LJOY, RJOY, START, BACK), triggers (L, R), joysticks (L, R)")]
    [SerializeField] private RawImage[] controllerImages;

    // ════════════════════════════════════════════════════════════════════
    //   BAKED UI REFERENCES — populated by 'Bake UI from Code' context menu.
    //   The CreateTelemetryPanel / CreateBottomIndicator methods write into
    //   these fields when run in Edit Mode (via the bake [ContextMenu]).
    //   At runtime, Start() skips re-running CreateXxx if these are non-null.
    // ════════════════════════════════════════════════════════════════════
    [Header("Baked Telemetry Panel (run 'Bake UI from Code' to populate)")]
    [SerializeField] private Text telemetryText;

    // ── Bottom horizontal indicator bar components (block-style) ──
    private const int gaugeBlockCount = 20;
    [Header("Baked Bottom Indicator (run 'Bake UI from Code' to populate)")]
    [SerializeField] private RawImage[] pitchBlocks;
    [SerializeField] private Text pitchBarLabel;
    [SerializeField] private RawImage[] speedBlocks;
    [SerializeField] private Text speedBarLabel;

    private Color pitchActiveColor = new Color(0.30f, 0.69f, 0.97f, 0.95f);
    private Color speedActiveColor = new Color(0.30f, 0.85f, 0.45f, 0.95f);
    private Color blockOffColor = new Color(0.12f, 0.15f, 0.20f, 0.5f);

    // ── Overhead minimap components ──
    private Camera minimapCamera;
    private RenderTexture minimapRT;
    private RawImage minimapImage;
    private RectTransform minimapArrowRect;
    private int minimapFrameCounter;
    private const int minimapUpdateInterval = 3; // Only render minimap every 3rd frame

    /// <summary>Max tilt angle for the bar (matches Flight.maxTiltAngle).</summary>
    private const float indicatorMaxPitch = 35f;
    /// <summary>Max speed for the bar (matches ~20 m/s top speed).</summary>
    private const float indicatorMaxSpeed = 35f;

    protected override void Awake()
    {
        // Skip base.Awake (which auto-populates inherited texts[]/images[] arrays
        // via GetComponentsInChildren). Hud uses its own [SerializeField] named
        // fields and overrides ShowMessage / SetPause / UpdateTime / Update so
        // none of the base's array accesses fire on Hud instances.
        this.messagePersistTime = -1;

        if (this.messageText  != null) this.messageText.text = string.Empty;
        if (this.mainTimeText != null) this.mainTimeText.text = string.Empty;
        SetPause(false);

        if (this.depthFeedImage != null)
            this.depthFeedImage.texture = new Texture2D(CameraModule.DepthWidth, CameraModule.DepthHeight);
    }

    /// <summary>
    /// Show a text message to the user. Overrides base to use the named messageText field
    /// instead of the inherited indexed texts[messageTextIndex] lookup.
    /// </summary>
    public override void ShowMessage(string message, Color color, float persistTime = -1, float fadeTime = 1.0f)
    {
        if (this.messageText != null)
        {
            this.messageText.text = message;
            this.messageText.color = color;
        }

        this.messageColor       = color;
        this.messageCounter     = 0;
        this.messagePersistTime = persistTime;
        this.messageFadeTime    = fadeTime;
    }

    /// <summary>
    /// Show or hide the pause screen overlay. Overrides base to use the named pauseScreenImage field.
    /// </summary>
    public override void SetPause(bool isPaused)
    {
        if (this.pauseScreenImage != null)
            this.pauseScreenImage.gameObject.SetActive(isPaused);
    }

    private void Start()
    {
        this.FailureMessage.SetActive(false);
        this.SuccessMessage.SetActive(false);

        this.checkpointTimesText.text = string.Empty;

        // --- Move the message text up so it doesn't overlap bottom indicator ---
        MoveMessageTextUp();

        // --- Create overhead minimap ---
        CreateMinimap();
    }

    /// <summary>
    /// Updates the flight telemetry panel with current data.
    /// Called each frame by PhysicsModule.
    /// </summary>
    public void UpdateTelemetry(float altitude, float speed, float pitch, float roll, float yaw,
                                 Vector3 accel, Vector3 gyro, Vector3 dronePosition)
    {
        if (telemetryText != null)
        {
            // Row 1: Flight state (altitude, speed, attitude)
            // Row 2: IMU sensors (accelerometer + gyroscope) on one line
            telemetryText.text =
                $"<color=#B0BEC5>ALT </color><color=#E0E0E0>{altitude:F1} m</color>      " +
                $"<color=#B0BEC5>SPD </color><color=#E0E0E0>{speed:F1} m/s</color>      " +
                $"<color=#4FC3F7>|</color>      " +
                $"<color=#B0BEC5>TILT </color><color=#E0E0E0>{pitch:F1}°</color>    " +
                $"<color=#B0BEC5>ROLL </color><color=#E0E0E0>{roll:F1}°</color>    " +
                $"<color=#B0BEC5>YAW </color><color=#E0E0E0>{yaw:F1}°</color>" +
                "\n" +
                $"<color=#B0BEC5>ACCEL </color><color=#E0E0E0>( {accel.x:F1} , {accel.y:F1} , {accel.z:F1} )</color>      " +
                $"<color=#4FC3F7>|</color>      " +
                $"<color=#B0BEC5>GYRO </color><color=#E0E0E0>( {gyro.x:F2} , {gyro.y:F2} , {gyro.z:F2} )  rad/s</color>";
        }

        // ── Update bottom indicator bars ──
        UpdateBottomIndicator(Mathf.Abs(pitch), speed);

        // ── Update minimap camera position ──
        UpdateMinimap(dronePosition, yaw);
    }

    /// <summary>
    /// Updates the bottom indicator block gauges and labels.
    /// </summary>
    private void UpdateBottomIndicator(float absPitch, float speed)
    {
        // Pitch blocks: 0 → 35 (indicatorMaxPitch)
        if (pitchBlocks != null)
        {
            float pitchFrac = Mathf.Clamp01(absPitch / indicatorMaxPitch);
            int litCount = Mathf.RoundToInt(pitchFrac * gaugeBlockCount);
            for (int i = 0; i < gaugeBlockCount; i++)
            {
                pitchBlocks[i].color = (i < litCount) ? pitchActiveColor : blockOffColor;
            }
        }
        if (pitchBarLabel != null)
            pitchBarLabel.text = $"{absPitch:F1}\u00B0";

        // Speed blocks: 0 → 20 m/s (indicatorMaxSpeed)
        if (speedBlocks != null)
        {
            float speedFrac = Mathf.Clamp01(speed / indicatorMaxSpeed);
            int litCount = Mathf.RoundToInt(speedFrac * gaugeBlockCount);
            for (int i = 0; i < gaugeBlockCount; i++)
            {
                speedBlocks[i].color = (i < litCount) ? speedActiveColor : blockOffColor;
            }
        }
        if (speedBarLabel != null)
            speedBarLabel.text = $"{speed:F1}";
    }

    /// <summary>
    /// Creates an overhead minimap in the bottom-right corner of the HUD.
    /// A camera looks straight down at the drone and renders to a small panel.
    /// A heading arrow overlay shows the drone's forward direction.
    /// </summary>
    private void CreateMinimap()
    {
        // ── Render texture for the minimap camera (128×128, no AA — perf optimised) ──
        minimapRT = new RenderTexture(128, 128, 16);
        minimapRT.antiAliasing = 1; // No AA on minimap — it's a small thumbnail
        minimapRT.filterMode = FilterMode.Bilinear;

        // ── World-space camera that looks straight down ──
        GameObject camObj = new GameObject("MinimapCamera");
        minimapCamera = camObj.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 40f; // 40 m visible radius
        minimapCamera.targetTexture = minimapRT;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1f);
        minimapCamera.cullingMask = ~(1 << 5); // everything except UI layer
        minimapCamera.depth = -10;
        minimapCamera.farClipPlane = 120f; // Reduced from default 1000 — minimap only needs to see the arena
        minimapCamera.nearClipPlane = 1f;
        minimapCamera.allowMSAA = false;
        minimapCamera.allowHDR = false;
        minimapCamera.enabled = false; // We render manually to skip frames
        minimapCamera.transform.position = new Vector3(0f, 80f, 0f);
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // ── UI panel in bottom-right ──
        Transform canvasRoot = this.transform;
        while (canvasRoot.parent != null && canvasRoot.parent.GetComponent<Canvas>() != null)
            canvasRoot = canvasRoot.parent;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 11);

        GameObject panelObj = new GameObject("MinimapPanel");
        panelObj.layer = 5;
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.SetParent(canvasRoot, false);
        panelRect.anchorMin = new Vector2(0.79f, 0.72f);
        panelRect.anchorMax = new Vector2(0.99f, 1.00f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // Background
        RawImage bg = panelObj.AddComponent<RawImage>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.raycastTarget = false;

        // Title
        GameObject titleObj = new GameObject("MinimapTitle");
        titleObj.layer = 5;
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.02f, 0.90f);
        titleRect.anchorMax = new Vector2(0.98f, 1.0f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "OVERHEAD MAP";
        titleText.font = uiFont;
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.31f, 0.76f, 0.97f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 8;
        titleText.resizeTextMaxSize = 18;

        // Camera feed image
        GameObject feedObj = new GameObject("MinimapFeed");
        feedObj.layer = 5;
        RectTransform feedRect = feedObj.AddComponent<RectTransform>();
        feedRect.SetParent(panelRect, false);
        feedRect.anchorMin = new Vector2(0.03f, 0.03f);
        feedRect.anchorMax = new Vector2(0.97f, 0.89f);
        feedRect.anchoredPosition = Vector2.zero;
        feedRect.sizeDelta = Vector2.zero;

        minimapImage = feedObj.AddComponent<RawImage>();
        minimapImage.texture = minimapRT;
        minimapImage.color = Color.white;

        // Heading arrow overlay (centered on the minimap)
        GameObject arrowObj = new GameObject("MinimapArrow");
        arrowObj.layer = 5;
        minimapArrowRect = arrowObj.AddComponent<RectTransform>();
        minimapArrowRect.SetParent(feedRect, false);
        minimapArrowRect.anchorMin = new Vector2(0.42f, 0.42f);
        minimapArrowRect.anchorMax = new Vector2(0.58f, 0.58f);
        minimapArrowRect.anchoredPosition = Vector2.zero;
        minimapArrowRect.sizeDelta = Vector2.zero;

        Text arrowText = arrowObj.AddComponent<Text>();
        arrowText.text = "\u25B2"; // ▲
        arrowText.font = uiFont;
        arrowText.fontSize = 28;
        arrowText.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.resizeTextForBestFit = true;
        arrowText.resizeTextMinSize = 14;
        arrowText.resizeTextMaxSize = 32;
    }

    /// <summary>
    /// Positions the minimap camera above the drone and rotates the heading arrow.
    /// </summary>
    private void UpdateMinimap(Vector3 dronePosition, float yaw)
    {
        if (minimapCamera != null)
        {
            minimapCamera.transform.position = new Vector3(dronePosition.x, dronePosition.y + 80f, dronePosition.z);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Only render every Nth frame to save a full scene draw call
            minimapFrameCounter++;
            if (minimapFrameCounter >= minimapUpdateInterval)
            {
                minimapFrameCounter = 0;
                minimapCamera.Render(); // Manual render since camera.enabled = false
            }
        }

        // Rotate the heading arrow every frame (cheap — just a UI transform)
        if (minimapArrowRect != null)
        {
            minimapArrowRect.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
    }

    /// <summary>
    /// Moves the center-screen message text higher so it doesn't overlap the bottom indicator.
    /// </summary>
    private void MoveMessageTextUp()
    {
        if (this.messageText == null) return;

        RectTransform msgRect = this.messageText.GetComponent<RectTransform>();
        if (msgRect != null)
        {
            // Shift the message up by 80 pixels so it clears the bottom gauge bar
            msgRect.anchoredPosition = new Vector2(msgRect.anchoredPosition.x, msgRect.anchoredPosition.y + 80f);
        }
    }

    protected override void Update()
    {
        this.UpdateController();

        // Message persistence + fade-out. Replicates base.Update's logic but uses
        // the named messageText field so we don't depend on the inherited texts[] array.
        if (this.messagePersistTime > 0)
        {
            this.messageCounter += Time.deltaTime;
            if (this.messageCounter > this.messagePersistTime)
            {
                this.messagePersistTime = 0;
                this.messageCounter     = 0;
            }
        }
        else if (this.messagePersistTime == 0 && this.messageCounter < this.messageFadeTime)
        {
            this.messageCounter += Time.deltaTime;
            if (this.messageText != null)
                this.messageText.color = Color.Lerp(this.messageColor, Color.clear, this.messageCounter / this.messageFadeTime);
        }
    }

    /// <summary>
    /// Update the controller icon to show the current buttons, triggers, and joysticks being pressed.
    /// </summary>
    private void UpdateController()
    {
        if (this.controllerImages == null || this.controllerImages.Length == 0)
            return;

        Array buttons = Enum.GetValues(typeof(Controller.Button));
        Array triggers = Enum.GetValues(typeof(Controller.Trigger));
        Array joysticks = Enum.GetValues(typeof(Controller.Joystick));

        int index = 0;

        foreach (Controller.Button button in buttons)
        {
            if (index < this.controllerImages.Length && this.controllerImages[index] != null)
                this.controllerImages[index].enabled = Controller.IsDown(button);
            index++;
        }

        foreach (Controller.Trigger trigger in triggers)
        {
            if (index < this.controllerImages.Length && this.controllerImages[index] != null)
                this.controllerImages[index].enabled = Controller.GetTrigger(trigger) > 0;
            index++;
        }

        foreach (Controller.Joystick joystick in joysticks)
        {
            Vector2 joystickAxes = Controller.GetJoystick(joystick);
            if (index < this.controllerImages.Length && this.controllerImages[index] != null)
                this.controllerImages[index].enabled = joystickAxes.x != 0 || joystickAxes.y != 0;
            index++;
        }
    }

    /// <summary>
    /// Formats a vector with a constant as a string with a constant width.
    /// </summary>
    /// <param name="vector">The vector to format.</param>
    /// <returns>The vector formatted as a string with exactly 19 characters.</returns>
    private string FormatVector3(Vector3 vector)
    {
        return $"({FormatFloat(vector.x)},{FormatFloat(vector.y)},{FormatFloat(vector.z)})";       
    }

    /// <summary>
    /// Rounds and formats a float as a string with a constant width.
    /// </summary>
    /// <param name="value">A value less than 10.</param>
    /// <returns>The provided value formatted as a string with exactly five characters.</returns>
    private string FormatFloat(float value)
    {
        string str = value.ToString("F2");

        // Add a leading space if there is no negative sign
        if (str[0] != '-')
        {
            return $" {str}";
        }

        return str;
    }
}
