using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Additively loads the GrandPrix exploration scene as a pure environment layer.
/// Attach this to a GameObject in the thin autograder scene (GrandPrix_Additive_Demo).
/// The exploration scene's LevelManager destroys itself when it detects this flag,
/// leaving only the autograder scene's LevelManager active.
/// </summary>
public class GrandPrixEnvironmentLoader : MonoBehaviour
{
    [SerializeField] private string environmentSceneName = "GrandPrix";

    private void Awake()
    {
        LevelManager.IsAdditiveEnvironment = true;
        SceneManager.LoadSceneAsync(environmentSceneName, LoadSceneMode.Additive).completed += _ =>
        {
            LevelManager.IsAdditiveEnvironment = false;
        };
    }
}
