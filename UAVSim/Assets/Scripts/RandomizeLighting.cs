using UnityEngine;

public class RandomizeLighting : MonoBehaviour
{
    enum Weather { Clear, Cloudy, Foggy, Rainy, Night }

    void Start()
    {
        ApplyWeather((Weather)Random.Range(0, 5));
    }

    void ApplyWeather(Weather w)
    {
        float elev   = Random.Range(20f, 70f);
        float azim   = Random.Range(0f, 360f);
        float sunInt = 1f;
        bool  fogOn  = false;
        float fogD   = 0f;
        Color fogC   = Color.grey;
        Color amb    = new Color(0.45f, 0.45f, 0.50f);

        switch (w)
        {
            case Weather.Clear:
                elev = Random.Range(35f, 70f);
                break;

            case Weather.Cloudy:
                elev   = Random.Range(20f, 50f);
                sunInt = 0.40f;
                amb    = new Color(0.30f, 0.30f, 0.35f);
                fogOn  = true; fogD = 0.003f; fogC = new Color(0.65f, 0.65f, 0.70f);
                break;

            case Weather.Foggy:
                elev   = Random.Range(20f, 45f);
                sunInt = 0.25f;
                amb    = new Color(0.25f, 0.25f, 0.28f);
                fogOn  = true; fogD = 0.025f; fogC = new Color(0.78f, 0.78f, 0.80f);
                break;

            case Weather.Rainy:
                elev   = Random.Range(15f, 35f);
                sunInt = 0.12f;
                amb    = new Color(0.18f, 0.20f, 0.26f);
                fogOn  = true; fogD = 0.012f; fogC = new Color(0.42f, 0.45f, 0.52f);
                SpawnRain();
                break;

            case Weather.Night:
                elev   = 210f;
                sunInt = 0f;
                amb    = new Color(0.04f, 0.05f, 0.10f);
                fogOn  = true; fogD = 0.006f; fogC = new Color(0.02f, 0.03f, 0.07f);
                break;
        }

        transform.eulerAngles = new Vector3(elev, azim, 0f);

        var lt = GetComponentInChildren<Light>();
        if (lt != null)
        {
            lt.intensity = sunInt;
            if (w != Weather.Night)
            {
                lt.useColorTemperature = true;
                lt.colorTemperature    = Mathf.Lerp(3200f, 6500f, Mathf.Clamp01((elev - 20f) / 50f));
            }
        }

        RenderSettings.fog        = fogOn;
        RenderSettings.fogMode    = FogMode.Exponential;
        RenderSettings.fogDensity = fogD;
        RenderSettings.fogColor   = fogC;

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = amb;
    }

    void SpawnRain()
    {
        var go = new GameObject("Rain");
        go.transform.position = new Vector3(0f, 35f, 0f);

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = true;
        main.startSpeed      = 0f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.maxParticles    = 8000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor      = new Color(0.70f, 0.75f, 0.85f, 0.55f);
        main.gravityModifier = 0f;

        var em = ps.emission;
        em.rateOverTime = 3000;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(250f, 1f, 250f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-3f,  3f);
        vel.y       = new ParticleSystem.MinMaxCurve(-28f, -18f);
        vel.z       = new ParticleSystem.MinMaxCurve(-3f,  3f);
    }
}
