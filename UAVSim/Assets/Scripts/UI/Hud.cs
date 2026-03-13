using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the heads-up display shown to the user during races with a single car.
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
        new Color(0f, 0.75f, 0.25f), // default drive
        new Color(0.75f, 0f, 0.25f), // user program
        new Color(1f, 0.5f, 0f), // wait
        new Color(0f, 0f, 0f) // finished
    };

    /// <summary>
    /// The text displayed on the mode label when the simulation is in each SimulationMode.
    /// </summary>
    private static readonly string[] modeNames =
    {
        "Default Drive",
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
        this.texts[(int)Texts.SuccessMessage].text = isNewBestTime ? "New Best Time!" : "Mission Accomplished!";
        this.texts[(int)Texts.SuccessTime].text = $"Time: {time:F3} seconds";
    }

    public override void HandleFailure(int carIndex, string reason)
    {
        this.FailureMessage.SetActive(true);
        this.texts[(int)Texts.Failure].text = reason;
    }

    public override void UpdateConnectedPrograms(bool[] connectedPrograms)
    {
        this.images[(int)Images.ConnectedProgram].color = connectedPrograms.Length > 0 ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, Hud.unconnectedScriptAlpha);
    }

    public override void UpdateMode(SimulationMode mode)
    {
        this.texts[(int)Texts.Mode].text = Hud.modeNames[(int)mode];
        this.images[(int)Images.ModeBackground].color = Hud.modeColors[(int)mode];
    }

    public override void UpdateTimeScale(float timeScale)
    {
        this.images[(int)Images.TimeWarp].color = new Color(1, 1, 1, Mathf.Max(0, 1 - Mathf.Sqrt(timeScale)));
        this.texts[(int)Texts.TimeScale].text = timeScale >= 1 ? string.Empty : $"{Mathf.Round(1 / timeScale)}x Slow Motion";
    }

    public override void UpdateTime(float mainTime, float[] keyPointDurations)
    {
        base.UpdateTime(mainTime, keyPointDurations);

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

            this.texts[(int)Texts.CheckpointTimes].text = text;
        }
    }
    #endregion

    #region IAutograderHud
    void IAutograderHud.SetLevelInfo(int levelIndex, string title, string description)
    {
        this.texts[(int)Texts.AutograderTitle].text = $"<b>Trial {levelIndex + 1}</b> - {title}";
        this.texts[(int)Texts.AutograderDescription].text = description;
    }

    void IAutograderHud.UpdateScore(float score, float maxScore)
    {
        this.texts[(int)Texts.AutograderScore].text = $"{score:F2}/{maxScore:F2}";

        if (score == maxScore)
        {
            this.texts[(int)Texts.AutograderScore].color = Color.green;
        }
    }

    void IAutograderHud.UpdateTime(float time, float timeLimit)
    {
        base.UpdateTime(time, new float[0]);

        if (time >= timeLimit)
        {
            this.texts[(int)Texts.MainTime].color = Color.red;
        }
        else if (timeLimit - time < timeLimit * Hud.autograderWarningTimeRatio)
        {
            this.texts[(int)Texts.MainTime].color = Color.yellow;
        }
    }

    void IAutograderHud.SetMaxTime(float maxTime)
    {
        this.texts[(int)Texts.MaxTime].text = $"Max: {maxTime:F1}";
    }

    void IAutograderHud.SetTimeBonus(float maxTime, float bonus, bool isLastBracket)
    {
        if (bonus >= 0)
        {
            this.texts[(int)Texts.MaxTime].text = $"Bonus: +{bonus} (under {maxTime:F1} sec)";
            this.texts[(int)Texts.MaxTime].color = bonus > 0 ? Color.green : Color.white;
        }
        else
        {
            this.texts[(int)Texts.MaxTime].text = $"Penalty: {bonus} (under {maxTime:F1} sec)";
            this.texts[(int)Texts.MaxTime].color = isLastBracket ? Color.red : Color.yellow;
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
            return (Texture2D)this.images[(int)Images.DepthFeed].texture;
        }
    }

    /// <summary>
    /// Update the physics statistics shown on the HUD.
    /// </summary>
    /// <param name="speed">The magnitude of the car's linear velocity in m/s.</param>
    /// <param name="linearAcceleration">The car's linear acceleration in m/s^2.</param>
    /// <param name="angularVelocity">The car's angular velocity in rad/s.</param>
    public void UpdatePhysics(float speed, Vector3 linearAcceleration, Vector3 angularVelocity)
    {
        // Old IMU text fields replaced by the new telemetry panel.
        // Hide the legacy labels + values so they don't clutter the screen.
        HideOldPhysicsTexts();
    }

    private bool oldPhysicsHidden = false;
    private void HideOldPhysicsTexts()
    {
        if (oldPhysicsHidden) return;
        oldPhysicsHidden = true;

        // Hide the text elements and their parent labels by walking up one level.
        int[] indices = {
            (int)Texts.TrueSpeed,
            (int)Texts.LinearAcceleration,
            (int)Texts.AngularVelocity
        };
        foreach (int idx in indices)
        {
            if (idx < this.texts.Length && this.texts[idx] != null)
            {
                // Hide the value text itself
                this.texts[idx].gameObject.SetActive(false);
                // Also try to hide the parent row (label + value pair)
                Transform parent = this.texts[idx].transform.parent;
                if (parent != null && parent != this.transform)
                    parent.gameObject.SetActive(false);
            }
        }

        // Also hide the static labels that sit between the values (indices 9-11, 13-14, 16)
        // These are the "True Speed", "Linear Accel", "Angular Vel" headers in the prefab.
        int[] labelIndices = { 9, 10, 11, 13, 14, 16 };
        foreach (int idx in labelIndices)
        {
            if (idx < this.texts.Length && this.texts[idx] != null)
            {
                this.texts[idx].gameObject.SetActive(false);
                Transform parent = this.texts[idx].transform.parent;
                if (parent != null && parent != this.transform)
                    parent.gameObject.SetActive(false);
            }
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
    /// The mutable text fields of the HUD, with values corresponding to the index in texts.
    /// </summary>
    private enum Texts
    {
        Message = 0,
        MainTime = 1,
        CheckpointTimes = 2,
        TimeScale = 3,
        TrueSpeed = 8,
        LinearAcceleration = 12,
        AngularVelocity = 15,
        Mode = 17,
        Failure = 19,
        SuccessMessage = 21,
        SuccessTime = 22,
        AutograderTitle = 25,
        AutograderDescription = 26,
        AutograderScore = 27,
        MaxTime = 28
    }

    /// <summary>
    /// The mutable images of the HUD, with values corresponding to the index in images.
    /// </summary>
    private enum Images
    {
        TimeWarp = 0,
        ColorFeed = 3,
        DepthFeed = 5,
        LidarMap = 7,
        ModeBackground = 9,
        ConnectedProgram = 10,
        ControllerFirstButton = 12
    }

    /// <summary>
    /// The dynamically created RawImage for the downward RGB camera feed.
    /// Not part of the images[] array (created after Awake to avoid index shifting).
    /// </summary>
    private RawImage downwardFeedImage;

    /// <summary>
    /// Text component for the flight telemetry panel (altitude, speed, attitude, IMU).
    /// </summary>
    private Text telemetryText;

    // ── Bottom horizontal indicator bar components (block-style) ──
    private const int gaugeBlockCount = 20;
    private RawImage[] pitchBlocks;
    private Text pitchBarLabel;
    private RawImage[] speedBlocks;
    private Text speedBarLabel;

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
        base.Awake();

        this.images[(int)Images.DepthFeed].texture = new Texture2D(CameraModule.DepthWidth, CameraModule.DepthHeight);
    }

    private void Start()
    {
        this.FailureMessage.SetActive(false);
        this.SuccessMessage.SetActive(false);

        this.texts[(int)Texts.CheckpointTimes].text = string.Empty;

        // --- Create the downward RGB camera panel dynamically ---
        CreateDownwardCameraPanel();

        // --- Create flight telemetry panel ---
        CreateTelemetryPanel();

        // --- Rename sensor panel labels for consistency ---
        RenameSensorLabels();

        // --- Create bottom horizontal indicator bars ---
        CreateBottomIndicator();

        // --- Move the message text up so it doesn't overlap bottom indicator ---
        MoveMessageTextUp();

        // --- Create overhead minimap ---
        CreateMinimap();
    }

    /// <summary>
    /// Resizes the existing 3 sensor panels to make room for the 4th (downward RGB)
    /// and creates the downward camera feed panel dynamically.
    /// </summary>
    private void CreateDownwardCameraPanel()
    {
        // Find the parent containers of the 3 existing sensor panels
        // by going up from the known RawImage elements.
        // Each sensor RawImage is nested: SensorPanel > (Title, Border, RawImage)
        // The parent panel has the anchor coordinates we need to adjust.
        RectTransform colorFeedPanel = FindSensorPanel(this.images[(int)Images.ColorFeed]);
        RectTransform depthFeedPanel = FindSensorPanel(this.images[(int)Images.DepthFeed]);

        if (colorFeedPanel == null || depthFeedPanel == null)
        {
            Debug.LogWarning("[Hud] Could not find sensor panel parents. Downward camera panel not created.");
            return;
        }

        // Hide the Lidar panel if it exists (LIDAR removed from drone)
        RectTransform lidarMapPanel = FindSensorPanel(this.images[(int)Images.LidarMap]);
        if (lidarMapPanel != null)
        {
            lidarMapPanel.gameObject.SetActive(false);
        }

        // Layout for 3 panels (bottom to top):
        //   ColorFeed (RGBD Forward):  y(0.00, 0.32)
        //   DepthFeed:                 y(0.34, 0.66)
        //   Downward RGB:              y(0.68, 1.00)
        colorFeedPanel.anchorMin = new Vector2(0f, 0f);
        colorFeedPanel.anchorMax = new Vector2(0.21f, 0.32f);

        depthFeedPanel.anchorMin = new Vector2(0f, 0.34f);
        depthFeedPanel.anchorMax = new Vector2(0.21f, 0.66f);

        // Create the new downward camera panel
        Canvas canvas = this.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject panelObj = new GameObject("DownwardRGBPanel");
        panelObj.layer = 5; // UI layer
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.SetParent(colorFeedPanel.parent, false);
        panelRect.anchorMin = new Vector2(0f, 0.68f);
        panelRect.anchorMax = new Vector2(0.21f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // Background image (dark semi-transparent)
        RawImage bgImage = panelObj.AddComponent<RawImage>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);
        bgImage.raycastTarget = false;

        // Title label
        GameObject titleObj = new GameObject("Title");
        titleObj.layer = 5;
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.02f, 0.87f);
        titleRect.anchorMax = new Vector2(0.98f, 1f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Color Camera (nadir)";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (titleText.font == null)
        {
            titleText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
        }
        titleText.fontSize = 12;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 8;
        titleText.resizeTextMaxSize = 18;

        // Feed RawImage (the actual camera feed)
        GameObject feedObj = new GameObject("DownwardFeed");
        feedObj.layer = 5;
        RectTransform feedRect = feedObj.AddComponent<RectTransform>();
        feedRect.SetParent(panelRect, false);
        feedRect.anchorMin = new Vector2(0.024f, 0.03f);
        feedRect.anchorMax = new Vector2(0.976f, 0.863f);
        feedRect.anchoredPosition = Vector2.zero;
        feedRect.sizeDelta = Vector2.zero;

        this.downwardFeedImage = feedObj.AddComponent<RawImage>();
        this.downwardFeedImage.color = Color.white;
        // The texture will be assigned by UpdateDownwardFeed when the camera module provides it
    }

    /// <summary>
    /// Find the sensor panel RectTransform that is a direct child of the main HUD canvas.
    /// The sensor RawImage is nested: MainCanvas > SensorPanel > ... > RawImage
    /// We go up from the RawImage until we find a parent whose own parent is the main content area.
    /// </summary>
    private RectTransform FindSensorPanel(RawImage sensorImage)
    {
        if (sensorImage == null) return null;

        // Walk up from the RawImage to find the panel that has anchors in the 0-0.21 x range
        RectTransform current = sensorImage.GetComponent<RectTransform>();
        while (current != null && current.parent != null)
        {
            RectTransform parentRect = current.parent as RectTransform;
            if (parentRect == null) break;

            // The sensor panel containers have anchorMax.x around 0.21-0.22
            // and their parent is the main HUD root
            if (current.anchorMax.x > 0.15f && current.anchorMax.x < 0.25f
                && current.anchorMin.x < 0.01f)
            {
                return current;
            }

            current = parentRect;
        }

        return null;
    }

    /// <summary>
    /// Creates a horizontal flight telemetry bar across the top of the screen.
    /// Shows altitude, speed, attitude (pitch/roll/yaw), and IMU data in a compact strip.
    /// </summary>
    private void CreateTelemetryPanel()
    {
        // Find the root canvas
        Transform canvasRoot = this.transform;
        while (canvasRoot.parent != null && canvasRoot.parent.GetComponent<Canvas>() != null)
        {
            canvasRoot = canvasRoot.parent;
        }

        // Create the panel container — horizontal strip across the top
        GameObject panelObj = new GameObject("TelemetryPanel");
        panelObj.layer = 5; // UI layer
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.SetParent(canvasRoot, false);
        panelRect.anchorMin = new Vector2(0.22f, 0.88f);
        panelRect.anchorMax = new Vector2(0.78f, 1.00f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // Background color matched to the existing sensor panel boxes on the left
        RawImage bg = panelObj.AddComponent<RawImage>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.raycastTarget = false;

        // Telemetry text — horizontal layout, centered
        GameObject textObj = new GameObject("TelemetryText");
        textObj.layer = 5;
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = new Vector2(0.01f, 0.02f);
        textRect.anchorMax = new Vector2(0.99f, 0.98f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        telemetryText = textObj.AddComponent<Text>();
        telemetryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (telemetryText.font == null)
            telemetryText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        telemetryText.fontSize = 18;
        telemetryText.color = new Color(0.90f, 0.95f, 1.0f);
        telemetryText.alignment = TextAnchor.MiddleCenter;
        telemetryText.supportRichText = true;
        telemetryText.resizeTextForBestFit = true;
        telemetryText.resizeTextMinSize = 12;
        telemetryText.resizeTextMaxSize = 24;
        telemetryText.lineSpacing = 1.6f;
        telemetryText.horizontalOverflow = HorizontalWrapMode.Wrap;
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
    /// Creates the horizontal tilt + speed indicator bar at the bottom center of the screen.
    /// Uses a segmented block style for visual feedback.
    /// </summary>
    private void CreateBottomIndicator()
    {
        // Find root canvas
        Transform canvasRoot = this.transform;
        while (canvasRoot.parent != null && canvasRoot.parent.GetComponent<Canvas>() != null)
            canvasRoot = canvasRoot.parent;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 11);

        // ── Container panel across bottom center ──
        GameObject container = new GameObject("BottomIndicator");
        container.layer = 5;
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.SetParent(canvasRoot, false);
        containerRect.anchorMin = new Vector2(0.22f, 0.01f);
        containerRect.anchorMax = new Vector2(0.78f, 0.09f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;

        // Background matched to left sensor panels
        RawImage containerBg = container.AddComponent<RawImage>();
        containerBg.color = new Color(0f, 0f, 0f, 0.7f);
        containerBg.raycastTarget = false;

        // ── TILT gauge (left half) ──
        pitchBlocks = CreateBlockGauge(containerRect, uiFont,
            anchorXMin: 0.02f, anchorXMax: 0.48f,
            label: "TILT",
            activeColor: pitchActiveColor,
            out pitchBarLabel);

        // ── SPEED gauge (right half) ──
        speedBlocks = CreateBlockGauge(containerRect, uiFont,
            anchorXMin: 0.52f, anchorXMax: 0.98f,
            label: "SPEED",
            activeColor: speedActiveColor,
            out speedBarLabel);
    }

    /// <summary>
    /// Helper: creates a single segmented block gauge (label + N blocks + value) inside a parent.
    /// Returns the array of block RawImages for runtime updates.
    /// </summary>
    private RawImage[] CreateBlockGauge(RectTransform parent, Font font,
        float anchorXMin, float anchorXMax,
        string label, Color activeColor,
        out Text valueLabel)
    {
        // ── Label (left-aligned text) ──
        GameObject lblObj = new GameObject(label + "Label");
        lblObj.layer = 5;
        RectTransform lblRect = lblObj.AddComponent<RectTransform>();
        lblRect.SetParent(parent, false);
        lblRect.anchorMin = new Vector2(anchorXMin, 0.08f);
        lblRect.anchorMax = new Vector2(anchorXMin + 0.10f, 0.92f);
        lblRect.anchoredPosition = Vector2.zero;
        lblRect.sizeDelta = Vector2.zero;

        Text lblText = lblObj.AddComponent<Text>();
        lblText.text = label;
        lblText.font = font;
        lblText.fontSize = 18;
        lblText.fontStyle = FontStyle.Bold;
        lblText.color = new Color(0.75f, 0.82f, 0.92f);
        lblText.alignment = TextAnchor.MiddleLeft;
        lblText.resizeTextForBestFit = true;
        lblText.resizeTextMinSize = 12;
        lblText.resizeTextMaxSize = 22;

        // ── Track area (where blocks live) ──
        float trackXMin = anchorXMin + 0.11f;
        float trackXMax = anchorXMax - 0.14f;

        // Create a parent for the blocks so they stay organized
        GameObject trackObj = new GameObject(label + "Track");
        trackObj.layer = 5;
        RectTransform trackRect = trackObj.AddComponent<RectTransform>();
        trackRect.SetParent(parent, false);
        trackRect.anchorMin = new Vector2(trackXMin, 0.15f);
        trackRect.anchorMax = new Vector2(trackXMax, 0.85f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.sizeDelta = Vector2.zero;

        // Dark track background behind blocks
        RawImage trackBg = trackObj.AddComponent<RawImage>();
        trackBg.color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
        trackBg.raycastTarget = false;

        // ── Create individual blocks ──
        RawImage[] blocks = new RawImage[gaugeBlockCount];
        float gapFraction = 0.15f; // fraction of each cell used as gap
        float cellWidth = 1f / gaugeBlockCount;

        for (int i = 0; i < gaugeBlockCount; i++)
        {
            GameObject blockObj = new GameObject(label + "Block" + i);
            blockObj.layer = 5;
            RectTransform blockRect = blockObj.AddComponent<RectTransform>();
            blockRect.SetParent(trackRect, false);

            float cellStart = i * cellWidth;
            float blockStart = cellStart + cellWidth * (gapFraction * 0.5f);
            float blockEnd = cellStart + cellWidth * (1f - gapFraction * 0.5f);

            blockRect.anchorMin = new Vector2(blockStart, 0.1f);
            blockRect.anchorMax = new Vector2(blockEnd, 0.9f);
            blockRect.anchoredPosition = Vector2.zero;
            blockRect.sizeDelta = Vector2.zero;

            RawImage blockImg = blockObj.AddComponent<RawImage>();
            blockImg.color = blockOffColor;
            blockImg.raycastTarget = false;
            blocks[i] = blockImg;
        }

        // ── Value label (right of track) ──
        GameObject valObj = new GameObject(label + "Value");
        valObj.layer = 5;
        RectTransform valRect = valObj.AddComponent<RectTransform>();
        valRect.SetParent(parent, false);
        valRect.anchorMin = new Vector2(trackXMax + 0.01f, 0.08f);
        valRect.anchorMax = new Vector2(anchorXMax, 0.92f);
        valRect.anchoredPosition = Vector2.zero;
        valRect.sizeDelta = Vector2.zero;

        valueLabel = valObj.AddComponent<Text>();
        valueLabel.text = "0";
        valueLabel.font = font;
        valueLabel.fontSize = 18;
        valueLabel.fontStyle = FontStyle.Bold;
        valueLabel.color = Color.white;
        valueLabel.alignment = TextAnchor.MiddleRight;
        valueLabel.resizeTextForBestFit = true;
        valueLabel.resizeTextMinSize = 12;
        valueLabel.resizeTextMaxSize = 22;

        return blocks;
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
        if (this.texts == null || this.texts.Length <= (int)Texts.Message) return;

        Text msgText = this.texts[(int)Texts.Message];
        if (msgText == null) return;

        RectTransform msgRect = msgText.GetComponent<RectTransform>();
        if (msgRect != null)
        {
            // Shift the message up by 80 pixels so it clears the bottom gauge bar
            msgRect.anchoredPosition = new Vector2(msgRect.anchoredPosition.x, msgRect.anchoredPosition.y + 80f);
        }
    }

    /// <summary>
    /// Renames the existing sensor panel titles for consistency.
    /// </summary>
    private void RenameSensorLabels()
    {
        // Rename Color Feed → "Color Camera (fwd)"
        RectTransform colorPanel = FindSensorPanel(this.images[(int)Images.ColorFeed]);
        if (colorPanel != null)
            SetPanelTitle(colorPanel, "Color Camera (fwd)");

        // Rename Depth Feed → "Depth Camera (fwd)"
        RectTransform depthPanel = FindSensorPanel(this.images[(int)Images.DepthFeed]);
        if (depthPanel != null)
            SetPanelTitle(depthPanel, "Depth Camera (fwd)");
    }

    /// <summary>
    /// Sets the title text of a sensor panel. Finds the first Text component
    /// in the panel's children and updates it.
    /// </summary>
    private void SetPanelTitle(RectTransform panel, string title)
    {
        Text[] panelTexts = panel.GetComponentsInChildren<Text>();
        foreach (Text t in panelTexts)
        {
            t.text = title;
            t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 8;
            t.resizeTextMaxSize = 18;
            break; // Only the first text (the title)
        }
    }

    protected override void Update()
    {
        this.UpdateController();
        base.Update();
    }

    /// <summary>
    /// Update the controller icon to show the current buttons, triggers, and joysticks being pressed.
    /// </summary>
    private void UpdateController()
    {
        Array buttons = Enum.GetValues(typeof(Controller.Button));
        Array triggers = Enum.GetValues(typeof(Controller.Trigger));
        Array joysticks = Enum.GetValues(typeof(Controller.Joystick));

        int index = (int)Images.ControllerFirstButton;

        foreach (Controller.Button button in buttons)
        {
            this.images[index].enabled = Controller.IsDown(button);
            index++;
        }

        foreach (Controller.Trigger trigger in triggers)
        {
            this.images[index].enabled = Controller.GetTrigger(trigger) > 0;
            index++;
        }

        foreach (Controller.Joystick joystick in joysticks)
        {
            Vector2 joystickAxes = Controller.GetJoystick(joystick);
            this.images[index].enabled = joystickAxes.x != 0 || joystickAxes.y != 0;
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
