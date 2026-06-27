using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the settings pane of the main menu.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    #region Public Interface
    /// <summary>
    /// Restore the default settings.
    /// </summary>
    public void RestoreDefaultSetting()
    {
        Settings.RestoreDefaults();
        SavedDataManager.Data.ClearCustomization();
        this.UpdateInputs();
    }

    /// <summary>
    /// Save current settings and close the settings screen.
    /// </summary>
    public void SaveSettings()
    {
        Settings.IsRealism = this.toggles[(int)Toggles.IsRealism].isOn;
        Settings.HideCarsInColorCamera = this.toggles[(int)Toggles.HideCarsInColorCamera].isOn;
        Settings.DepthRes = (Settings.DepthResolution)this.dropdowns[(int)Dropdowns.DepthRes].value;
        Settings.Username = this.username.text;

        Settings.SaveSettings();
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Close the settings screen without saving any changes.
    /// </summary>
    public void Cancel()
    {
        this.UpdateInputs();
        this.gameObject.SetActive(false);
    }

    public void ColorChanged()
    {
        // TODO: update on fly
    }
    #endregion

    /// <summary>
    /// The dropdown menus in the main menu.
    /// </summary>
    private enum Dropdowns
    {
        DepthRes
    }

    /// <summary>
    /// The toggles (check boxes) in the main menu.
    /// </summary>
    private enum Toggles
    {
        IsRealism = 0,
        HideCarsInColorCamera = 1,
    }

    /// <summary>
    /// The dropdown menus in the settings pane.
    /// </summary>
    private Dropdown[] dropdowns;

    /// <summary>
    /// The toggles (check boxes) in the settings pane.
    /// </summary>
    private Toggle[] toggles;

    /// <summary>
    /// The input field in which the user enters their OpenEdx username.
    /// </summary>
    private InputField username;

    private void Awake()
    {
        this.dropdowns = this.GetComponentsInChildren<Dropdown>();
        this.toggles = this.GetComponentsInChildren<Toggle>();
        this.username = this.GetComponentInChildren<InputField>();
    }

    private void Start()
    {
        this.UpdateInputs();
    }

    /// <summary>
    /// Update all input values on the settings pane with the current settings.
    /// </summary>
    private void UpdateInputs()
    {
        this.toggles[(int)Toggles.IsRealism].isOn = Settings.IsRealism;
        this.toggles[(int)Toggles.HideCarsInColorCamera].isOn = Settings.HideCarsInColorCamera;
        this.dropdowns[(int)Dropdowns.DepthRes].value = (int)Settings.DepthRes;
        this.username.text = Settings.Username;
    }
}
