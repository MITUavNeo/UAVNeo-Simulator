using UnityEngine;

public class RandomizeLighting : MonoBehaviour
{
    void Start()
    {
        float elevation = Random.Range(20f, 70f);
        float azimuth   = Random.Range(0f, 360f);
        transform.eulerAngles = new Vector3(elevation, azimuth, 0f);

        var light = GetComponentInChildren<Light>();
        if (light != null)
        {
            // low elevation = warm golden-hour (~3200K), high = cool noon (~6500K)
            light.colorTemperature = Mathf.Lerp(3200f, 6500f, (elevation - 20f) / 50f);
            light.useColorTemperature = true;
        }
    }
}
