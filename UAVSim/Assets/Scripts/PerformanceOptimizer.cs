using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically configures rendering for maximum FPS on any hardware.
/// Runs once before any scene loads — no manual setup required.
///
/// Key optimisations:
///   • VSync OFF  → frame rate no longer capped to 60 Hz
///   • targetFrameRate = -1  → render as fast as the GPU allows (240+ on decent hardware)
///   • Shadow distance reduced for weak GPUs
///   • Pixel light count capped
///   • LOD bias tuned for balanced quality / perf
///   • Camera far-clip planes capped for performance
///   • Hardware auto-detect (VRAM + CPU cores)
/// </summary>
public static class PerformanceOptimizer
{
    private static bool isLowEnd;
    private static bool isHighEnd;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Optimize()
    {
        // Register scene-loaded callback for per-camera optimizations
        SceneManager.sceneLoaded += OnSceneLoaded;
        // ───────────────────────────────────────────────
        //  1.  UNLOCK FRAME RATE
        // ───────────────────────────────────────────────
        QualitySettings.vSyncCount = 0;          // Disable VSync on every quality tier
        Application.targetFrameRate = -1;         // No cap — render as fast as possible

        // ───────────────────────────────────────────────
        //  2.  AUTO-DETECT HARDWARE TIER
        // ───────────────────────────────────────────────
        //  SystemInfo.graphicsMemorySize is in MB.
        //  < 2 GB  →  Low-end laptop (Intel UHD / old MX)
        //  2-4 GB  →  Mid-range (GTX 1650, MX450, M1)
        //  > 4 GB  →  High-end (RTX, M1 Pro+, etc.)
        int vramMB = SystemInfo.graphicsMemorySize;
        int cpuCores = SystemInfo.processorCount;

        isLowEnd  = vramMB < 2048 || cpuCores <= 4;
        isHighEnd = vramMB >= 4096 && cpuCores >= 8;

        Debug.Log($"[PerformanceOptimizer] VRAM={vramMB} MB, CPU cores={cpuCores} → " +
                  $"{(isLowEnd ? "LOW" : isHighEnd ? "HIGH" : "MID")}-end profile  |  VSync OFF  |  FPS uncapped");

        // ───────────────────────────────────────────────
        //  3.  SHADOWS
        // ───────────────────────────────────────────────
        if (isLowEnd)
        {
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 0;
        }
        else if (!isHighEnd)
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.shadowResolution = ShadowResolution.Low;
        }
        else
        {
            // High-end: keep soft shadows but limit distance
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 60f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
        }

        // ───────────────────────────────────────────────
        //  4.  LIGHTING / PIXEL LIGHT COUNT
        // ───────────────────────────────────────────────
        QualitySettings.pixelLightCount = isLowEnd ? 1 : isHighEnd ? 4 : 2;

        // ───────────────────────────────────────────────
        //  5.  LOD BIAS  (higher = higher quality, slower)
        // ───────────────────────────────────────────────
        QualitySettings.lodBias = isLowEnd ? 0.5f : isHighEnd ? 1.5f : 1.0f;

        // ───────────────────────────────────────────────
        //  6.  ANTI-ALIASING
        // ───────────────────────────────────────────────
        QualitySettings.antiAliasing = isLowEnd ? 0 : isHighEnd ? 4 : 2;

        // ───────────────────────────────────────────────
        //  7.  SOFT PARTICLES / VEGETATION
        // ───────────────────────────────────────────────
        QualitySettings.softParticles = isHighEnd;
        QualitySettings.softVegetation = isHighEnd;
        QualitySettings.realtimeReflectionProbes = isHighEnd;
        QualitySettings.billboardsFaceCameraPosition = !isLowEnd;

        // ───────────────────────────────────────────────
        //  8.  TEXTURE QUALITY
        // ───────────────────────────────────────────────
        //  0 = Full res, 1 = Half, 2 = Quarter
#if !UNITY_EDITOR
        QualitySettings.globalTextureMipmapLimit = isLowEnd ? 1 : 0;
#endif

        // ───────────────────────────────────────────────
        //  9.  ANISOTROPIC TEXTURES
        // ───────────────────────────────────────────────
        QualitySettings.anisotropicFiltering = isLowEnd
            ? AnisotropicFiltering.Disable
            : AnisotropicFiltering.Enable;

        // ───────────────────────────────────────────────
        //  10. PHYSICS (keep fixed timestep at 50 Hz — good balance)
        // ───────────────────────────────────────────────
        // Time.fixedDeltaTime = 0.02f;  // already default, don't touch
        // Limit physics catch-up to prevent spiral-of-death on slow machines
        Time.maximumDeltaTime = 0.1f;

        // ───────────────────────────────────────────────
        //  11. SKIN WEIGHTS (mesh deformation — not critical for drones)
        // ───────────────────────────────────────────────
        QualitySettings.skinWeights = isLowEnd ? SkinWeights.OneBone : SkinWeights.TwoBones;

        // ───────────────────────────────────────────────
        //  12. ASYNC GPU UPLOAD (smoother loading)
        // ───────────────────────────────────────────────
        QualitySettings.asyncUploadTimeSlice = isLowEnd ? 4 : 2;
        QualitySettings.asyncUploadBufferSize = isLowEnd ? 8 : 16;
    }

    /// <summary>
    /// Per-scene optimisation: cap camera far-clip planes, disable HDR/MSAA on sensor cameras.
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Optimize ALL cameras in the scene
        foreach (Camera cam in Object.FindObjectsByType<Camera>())
        {
            // ── Sensor cameras (small render textures shown as thumbnails) ──
            if (cam.targetTexture != null)
            {
                cam.allowHDR = false;         // HDR is expensive and unnecessary for thumbnails
                cam.allowMSAA = false;        // The render textures are already small

                // Cap far clip on downward cam (it only needs to see the ground)
                if (cam.name == "DownwardCamera")
                {
                    cam.farClipPlane = Mathf.Min(cam.farClipPlane, 120f);
                }
            }

            // ── Main / player cameras ──
            if (cam.name == "MainCamera" || cam.name == "OverheadCamera" || cam.name == "RearViewCamera")
            {
                // The arena is ≤ 250m across; no need for 1000m far clip
                cam.farClipPlane = Mathf.Min(cam.farClipPlane, 300f);

                if (isLowEnd)
                {
                    cam.allowHDR = false;
                    cam.allowMSAA = false;
                }
            }
        }

        Debug.Log($"[PerformanceOptimizer] Scene '{scene.name}' loaded — cameras optimized");
    }
}
