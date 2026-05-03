using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the screen shown to the user during races with multiple drones.
/// </summary>
public class RaceScreen : ScreenManager
{
    #region Constants
    /// <summary>
    /// The color overlaid on a drone's camera view when the drone has not yet been connected to a Python program.
    /// </summary>
    private static readonly Color disabledScreenColor = new Color(0.5f, 0.5f, 0.5f);

    /// <summary>
    /// The message shown over a drone's camera view when the drone has not yet been connected to a Python program.
    /// </summary>
    private const string awaitingConnectionMessage = "Awaiting connection from a Python program...";
    #endregion 

    #region Public Interface
    #region Overrides
    public override void HandleFailure(int droneIndex, string reason)
    {
        // Intentionally left blank (for now)
    }

    public override void HandleWin(float time, bool isNewBestTime)
    {
        this.ShowMessage($"Race completed in {time:F3} seconds!", Color.green);
    }

    public override void UpdateConnectedPrograms(bool[] connectedPrograms)
    {
        int index = 0;
        foreach (bool isConnected in connectedPrograms)
        {
            RaceCameraView view = GetCameraView(index);
            if (isConnected)
            {
                view.Image.color = Color.white;
                view.Text.text = string.Empty;
            }
            else
            {
                view.Image.color = RaceScreen.disabledScreenColor;
                view.Text.text = RaceScreen.awaitingConnectionMessage;
            }
            index++;
        }

        // Any remaining screens beyond the end of the array are not connected
        for (; index < this.numDrones; index++)
        {
            RaceCameraView view = GetCameraView(index);
            view.Image.color = RaceScreen.disabledScreenColor;
            view.Text.text = RaceScreen.awaitingConnectionMessage;
        }
    }

    public override void UpdateMode(SimulationMode mode)
    {
        this.images[(int)Images.WaitMessage].gameObject.SetActive(mode == SimulationMode.Wait);
    }

    public override void UpdateTimeScale(float timeScale)
    {
        // Intentionally blank for now
    }
    #endregion

    /// <summary>
    /// Initializes the camera views of the race screen.
    /// </summary>
    /// <param name="droneCameras">The textures to which each drone-view camera renders, ordered by drone index.</param>
    /// <param name="raceCamera">The texture to which the race cameras render.</param>
    public void SetCameras(Texture[] droneCameras, Texture raceCamera)
    {
        this.numDrones = droneCameras.Length;

        foreach (RaceCameraView cameraView in this.cameraViews)
        {
            cameraView.gameObject.SetActive(false);
        }

        for (int i = 0; i < droneCameras.Length; i++)
        {
            RaceCameraView droneCameraView = GetCameraView(i);
            droneCameraView.Image.texture = droneCameras[i];
            droneCameraView.Image.color = RaceScreen.disabledScreenColor;
            droneCameraView.Text.text = RaceScreen.awaitingConnectionMessage;
            droneCameraView.gameObject.SetActive(true);
        }

        // We only have space for the race camera if there are 3 or fewer drones
        if (this.numDrones <= 3)
        {
            RaceCameraView raceCameraView = GetCameraView(this.numDrones);
            raceCameraView.Image.texture = raceCamera;
            raceCameraView.Text.text = string.Empty;
            raceCameraView.gameObject.SetActive(true);
        }
    }
    #endregion

    /// <summary>
    /// The mutable images of the race screen manager, with values corresponding to the index in images.
    /// </summary>
    private enum Images
    {
        WaitMessage = 9
    }

    /// <summary>
    /// The potential camera views in the race screen.
    /// </summary>
    private RaceCameraView[] cameraViews;

    /// <summary>
    /// The number of drones in the current race.
    /// </summary>
    private int numDrones;

    protected override void Awake()
    {
        this.messageTextIndex = 5;
        this.mainTimeTextIndex = 6;

        base.Awake();

        this.cameraViews = this.GetComponentsInChildren<RaceCameraView>();

        // Unity requires one camera rendering to the display, so create a dummy camera
        // (which is fully blocked by the race screen background)
        this.gameObject.AddComponent<Camera>();
    }

    /// <summary>
    /// Returns the camera view corresponding to a particular drone.
    /// </summary>
    /// <param name="droneIndex">The index of the drone.</param>
    /// <returns>The camera view showing the drone of the specified index.</returns>
    /// <remarks>If droneIndex is one past the last drone, we return the camera view of the race camera.</remarks>
    private RaceCameraView GetCameraView(int droneIndex)
    {
        // When there are only two drones, use the bottom middle screen (index 4) for the race camera
        if (this.numDrones == 2 && droneIndex == 2)
        {
            droneIndex = 4;
        }
        return this.cameraViews[droneIndex];
    }
}
