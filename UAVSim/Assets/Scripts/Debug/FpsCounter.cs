using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lightweight on-screen FPS / system-status diagnostic.
///
/// Renders a smoothed frame-rate readout in the bottom-right corner of the
/// screen, in white, on its own overlay canvas drawn above all other UI.
///
/// Auto-spawns once after the first scene loads (like PerformanceOptimizer) and
/// persists across scene loads, so it requires no prefab/scene wiring. It is
/// shown ONLY in flight scenes (exploration + autograder), detected by the
/// presence of a <see cref="LevelManager"/> — so it stays hidden in the main
/// menu and settings (which have no LevelManager). Toggle with F3 while flying.
/// </summary>
public class FpsCounter : MonoBehaviour
{
    #region Tuning
    private const int FontSize = 26;
    private const float Margin = 12f;            // pixels from the screen corner
    private const float SampleInterval = 0.25f;  // how often the readout refreshes (seconds)
    private const KeyCode ToggleKey = KeyCode.F3;
    #endregion

    private Canvas canvas;
    private Text label;
    private float accumulatedTime;
    private int accumulatedFrames;

    /// <summary>True while the current scene is a flight scene (has a LevelManager).</summary>
    private bool inFlightScene;

    /// <summary>User-controlled show/hide via <see cref="ToggleKey"/> (within flight scenes).</summary>
    private bool userVisible = true;

    /// <summary>
    /// Creates the diagnostic the first time a scene finishes loading, so it
    /// exists in every scene without being placed by hand.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("FpsCounter");
        DontDestroyOnLoad(go);
        go.AddComponent<FpsCounter>();
    }

    private void Awake()
    {
        this.BuildOverlay();
        SceneManager.sceneLoaded += this.OnSceneLoaded;
        this.RefreshSceneState();   // evaluate the scene we were created in
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= this.OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.RefreshSceneState();
    }

    /// <summary>
    /// A flight scene is any scene that spawns the drone via a LevelManager
    /// (exploration + autograder). The main menu and settings have none.
    /// </summary>
    private void RefreshSceneState()
    {
        this.inFlightScene = Object.FindAnyObjectByType<LevelManager>() != null;
        this.ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (this.canvas != null)
        {
            this.canvas.enabled = this.inFlightScene && this.userVisible;
        }
    }

    /// <summary>
    /// Builds a dedicated screen-space overlay canvas with a single white label
    /// anchored to the bottom-right corner.
    /// </summary>
    private void BuildOverlay()
    {
        this.canvas = this.gameObject.AddComponent<Canvas>();
        this.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        this.canvas.sortingOrder = short.MaxValue;    // draw on top of the HUD and everything else

        GameObject labelObj = new GameObject("FpsLabel");
        labelObj.transform.SetParent(this.transform, false);

        this.label = labelObj.AddComponent<Text>();
        this.label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        this.label.fontSize = FontSize;
        this.label.color = Color.white;
        this.label.alignment = TextAnchor.LowerRight;
        this.label.horizontalOverflow = HorizontalWrapMode.Overflow;
        this.label.verticalOverflow = VerticalWrapMode.Overflow;
        this.label.raycastTarget = false;        // never intercept clicks
        this.label.text = string.Empty;

        // Subtle drop shadow so the white text stays legible over bright scenes.
        Shadow shadow = labelObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(1f, -1f);

        // Pin to the bottom-right corner with a small margin.
        RectTransform rt = this.label.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(340f, 36f);
        rt.anchoredPosition = new Vector2(-Margin, Margin);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            this.userVisible = !this.userVisible;
            this.ApplyVisibility();
        }

        // Nothing to update while hidden (menu/settings, or toggled off).
        if (this.canvas == null || !this.canvas.enabled)
        {
            return;
        }

        // Average over SampleInterval using UNSCALED time, so a paused or
        // time-scaled simulation still reports the true render frame rate.
        this.accumulatedTime += Time.unscaledDeltaTime;
        this.accumulatedFrames++;

        if (this.accumulatedTime >= SampleInterval)
        {
            float fps = this.accumulatedFrames / this.accumulatedTime;
            float milliseconds = 1000f / Mathf.Max(fps, 0.0001f);
            if (this.label != null)
            {
                this.label.text = $"FPS {Mathf.RoundToInt(fps)}  |  {milliseconds:F1} ms";
            }

            this.accumulatedTime = 0f;
            this.accumulatedFrames = 0;
        }
    }
}
