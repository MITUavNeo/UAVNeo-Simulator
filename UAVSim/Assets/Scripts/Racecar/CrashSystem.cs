using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Drone crash system with realistic arm-based breakage.
///
/// At startup, every MeshRenderer in the DroneModel hierarchy is assigned to
/// the nearest propeller's "arm group" (if it's in that arm's quadrant) or to
/// the central body (if it's close to center).
///
/// Damage tiers (based on display speed — raw physics velocity / 10):
///   - Light  (< 0.2 m/s): Physics bounce only, no damage.
///   - Minor  (0.2–0.5 m/s): Metal sparks, camera shake, brief motor stun.
///   - Medium (0.5–1.0 m/s): Nearest arm group breaks off (actual FBX parts fly away).
///   - Hard   (> 1.0 m/s): All arms detach. Drone crash-lands as a broken shell.
///                           No explosion — just falls to the ground in pieces.
///
/// On Tab/reset (via LevelManager.ResetCar), call Repair() to fully restore.
///
/// Attach to the Drone GameObject (same object as Flight, Racecar, etc.).
/// </summary>
public class CrashSystem : MonoBehaviour
{
    #region Constants

    // ── Impact speed thresholds (in m/s — matches display speed directly) ──
    // Raised so normal landings and light bumps don't trigger damage.
    private const float minorThreshold = 14.0f;
    private const float mediumThreshold = 24.0f;
    private const float hardThreshold = 35.0f;

    /// <summary>
    /// Scale factor between collision.relativeVelocity and display speed.
    /// Now 1:1 since PhysicsModule no longer divides by 10.
    /// </summary>
    private const float velocityDisplayScale = 1f;

    // ── Stun ──
    private const float minorStunDuration = 0.5f;
    private const float minorStunMotorScale = 0.75f;
    private const float mediumStunMotorScale = 0.6f;

    // ── Detachment physics ──
    private const float detachForce = 5f;
    private const float detachUpward = 2f;
    private const float detachTorque = 8f;

    // ── Timings ──
    private const float respawnDelay = 3.5f;
    private const float debrisLifetime = 6.0f;
    private const float damageCooldown = 0.6f;

    // ── Particle counts ──
    private const int sparkCountMinor = 25;
    private const int sparkCountMedium = 50;
    private const int sparkCountHard = 80;

    // ── Camera shake ──
    private const float shakeDecay = 6f;

    // ── Arm group detection ──
    /// <summary>
    /// Meshes closer to center than this fraction of the arm length are "body", not arm.
    /// </summary>
    private const float bodyRadiusFraction = 0.35f;

    /// <summary>Max angular velocity after collision to prevent wild spins.</summary>
    private const float postCrashMaxAngVel = 3f;

    private const string debrisNamePrefix = "Debris_";

    #endregion

    #region Public Interface

    /// <summary>Current drone health: 1.0 = pristine, 0.0 = destroyed.</summary>
    public float Health { get; private set; } = 1.0f;

    /// <summary>True when the drone is totally destroyed and awaiting respawn.</summary>
    public bool IsDestroyed { get; private set; } = false;

    /// <summary>Per-motor scale factor (0 = dead, 1 = healthy).</summary>
    public float[] MotorHealth { get; private set; } = { 1f, 1f, 1f, 1f };

    /// <summary>Global motor output scale (for stun effects).</summary>
    public float MotorOutputScale { get; private set; } = 1.0f;

    /// <summary>
    /// Repairs the drone to pristine state. Called on respawn/reset (Tab key).
    /// </summary>
    public void Repair()
    {
        StopAllCoroutines();

        Health = 1.0f;
        IsDestroyed = false;
        MotorOutputScale = 1.0f;
        for (int i = 0; i < 4; i++) MotorHealth[i] = 1f;

        ReattachAllParts();
        CleanupEffects();
        CleanupDebris();

        shakeIntensity = 0f;
        screenFlashAlpha = 0f;
        stunCoroutine = null;
        lastDamageTime = -999f;

        if (rBody != null)
        {
            rBody.velocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;
        }

        if (flight != null)
        {
            flight.Stop();
            if (flight.IsArmed) flight.Relaunch();
        }

        Debug.Log("[CrashSystem] Drone fully repaired!");
    }

    #endregion

    #region Private State

    private Flight flight;
    private Racecar racecar;
    private Rigidbody rBody;
    private Transform droneModelTransform;

    /// <summary>All colliders on the drone, cached for Physics.IgnoreCollision.</summary>
    private Collider[] droneColliders;

    // ── Propeller tracking ──
    private Transform[] propellerTransforms;
    private Vector3[] propellerOriginalLocalPos;
    private Quaternion[] propellerOriginalLocalRot;
    private Vector3[] propellerOriginalLocalScale;
    private bool[] propellerDetached;

    // ── Arm groups: list of renderers that belong to each arm (index matches propeller) ──
    private List<Renderer>[] armGroupRenderers;
    private List<Vector3>[] armGroupOriginalLocalPos;
    private List<Quaternion>[] armGroupOriginalLocalRot;
    private List<Transform>[] armGroupOriginalParents;
    private bool[] armGroupDetached;

    // ── Active debris (cleaned up on repair) ──
    private List<GameObject> activeDebris = new List<GameObject>();

    // ── Active effects ──
    private ParticleSystem activeSmokeSystem;
    private Coroutine stunCoroutine;
    private float lastDamageTime = -999f;

    // ── Camera shake ──
    private float shakeIntensity = 0f;
    private Camera mainCam;
    private Vector3 cameraShakeOffset = Vector3.zero;

    // ── Screen flash ──
    private float screenFlashAlpha = 0f;
    private Texture2D flashTexture;

    #endregion

    // ═══════════════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═══════════════════════════════════════════════════════════════

    private void Start()
    {
        flight = GetComponent<Flight>();
        racecar = GetComponent<Racecar>();
        rBody = GetComponent<Rigidbody>();

        droneColliders = GetComponentsInChildren<Collider>();

        FindPropellers();
        BuildArmGroups();

        mainCam = Camera.main;

        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    private void FindPropellers()
    {
        List<Transform> found = new List<Transform>();
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name.StartsWith("Propeller_"))
                found.Add(t);
        }

        found.Sort((a, b) => a.name.CompareTo(b.name));
        propellerTransforms = found.ToArray();

        propellerOriginalLocalPos = new Vector3[propellerTransforms.Length];
        propellerOriginalLocalRot = new Quaternion[propellerTransforms.Length];
        propellerOriginalLocalScale = new Vector3[propellerTransforms.Length];
        propellerDetached = new bool[propellerTransforms.Length];

        if (propellerTransforms.Length > 0)
            droneModelTransform = propellerTransforms[0].parent;

        for (int i = 0; i < propellerTransforms.Length; i++)
        {
            propellerOriginalLocalPos[i] = propellerTransforms[i].localPosition;
            propellerOriginalLocalRot[i] = propellerTransforms[i].localRotation;
            propellerOriginalLocalScale[i] = propellerTransforms[i].localScale;
            propellerDetached[i] = false;
        }
    }

    /// <summary>
    /// Assigns every MeshRenderer in the DroneModel to an arm group (one per propeller).
    /// A mesh belongs to the arm group of the propeller it's closest to, as long as it's
    /// farther from center than bodyRadiusFraction * armLength. Central meshes are "body"
    /// and don't belong to any arm group.
    /// </summary>
    private void BuildArmGroups()
    {
        int n = propellerTransforms.Length;
        armGroupRenderers = new List<Renderer>[n];
        armGroupOriginalLocalPos = new List<Vector3>[n];
        armGroupOriginalLocalRot = new List<Quaternion>[n];
        armGroupOriginalParents = new List<Transform>[n];
        armGroupDetached = new bool[n];

        for (int i = 0; i < n; i++)
        {
            armGroupRenderers[i] = new List<Renderer>();
            armGroupOriginalLocalPos[i] = new List<Vector3>();
            armGroupOriginalLocalRot[i] = new List<Quaternion>();
            armGroupOriginalParents[i] = new List<Transform>();
            armGroupDetached[i] = false;
        }

        if (droneModelTransform == null || n == 0) return;

        // Compute arm length (average distance from center to propellers in DroneModel local space)
        float avgArmLen = 0f;
        for (int i = 0; i < n; i++)
            avgArmLen += propellerOriginalLocalPos[i].magnitude;
        avgArmLen /= n;
        float bodyRadius = avgArmLen * bodyRadiusFraction;

        // Get all renderers in DroneModel (excluding propellers — they're tracked separately)
        Renderer[] allRenderers = droneModelTransform.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in allRenderers)
        {
            // Skip propeller renderers — they're handled by the propeller tracking
            bool isPropeller = false;
            Transform check = rend.transform;
            while (check != null && check != droneModelTransform)
            {
                if (check.name.StartsWith("Propeller_")) { isPropeller = true; break; }
                check = check.parent;
            }
            if (isPropeller) continue;

            // Compute position in DroneModel local space
            Vector3 localPos = droneModelTransform.InverseTransformPoint(rend.transform.position);
            float distFromCenter = new Vector2(localPos.x, localPos.z).magnitude; // horizontal distance

            // If close to center, it's body — skip
            if (distFromCenter < bodyRadius) continue;

            // Find the nearest propeller
            int nearest = -1;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float d = Vector3.Distance(localPos, propellerOriginalLocalPos[i]);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = i;
                }
            }

            if (nearest >= 0)
            {
                armGroupRenderers[nearest].Add(rend);
                armGroupOriginalLocalPos[nearest].Add(rend.transform.localPosition);
                armGroupOriginalLocalRot[nearest].Add(rend.transform.localRotation);
                armGroupOriginalParents[nearest].Add(rend.transform.parent);
            }
        }

        for (int i = 0; i < n; i++)
        {
            Debug.Log($"[CrashSystem] Arm group {i}: {armGroupRenderers[i].Count} mesh parts");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════════

    private void Update()
    {
        if (shakeIntensity > 0.001f)
            shakeIntensity *= Mathf.Exp(-shakeDecay * Time.deltaTime);
        else
            shakeIntensity = 0f;

        if (screenFlashAlpha > 0f)
        {
            screenFlashAlpha -= Time.deltaTime * 3f;
            if (screenFlashAlpha < 0f) screenFlashAlpha = 0f;
        }
    }

    private void LateUpdate()
    {
        if (mainCam != null)
        {
            mainCam.transform.position -= cameraShakeOffset;

            if (shakeIntensity > 0.005f)
            {
                float shakeX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * shakeIntensity;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * shakeIntensity;
                cameraShakeOffset = new Vector3(shakeX, shakeY, 0f);
                mainCam.transform.position += cameraShakeOffset;
            }
            else
            {
                cameraShakeOffset = Vector3.zero;
            }
        }
    }

    private void OnGUI()
    {
        if (screenFlashAlpha > 0.01f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.95f, 0.8f, screenFlashAlpha * 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
            GUI.color = prev;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COLLISION HANDLING
    // ═══════════════════════════════════════════════════════════════

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.isTrigger) return;
        if (IsDestroyed) return;
        if (collision.gameObject.name.StartsWith(debrisNamePrefix)) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        float impactSpeed = collision.relativeVelocity.magnitude / velocityDisplayScale;
        if (impactSpeed < minorThreshold) return;

        lastDamageTime = Time.time;
        Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 contactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        ClampPostCrashAngularVelocity();

        if (impactSpeed >= hardThreshold)
            HandleHardCrash(contactPoint, contactNormal, impactSpeed);
        else if (impactSpeed >= mediumThreshold)
            HandleMediumCrash(contactPoint, contactNormal, impactSpeed);
        else
            HandleMinorCrash(contactPoint, contactNormal, impactSpeed);
    }

    private void ClampPostCrashAngularVelocity()
    {
        if (rBody == null) return;
        float mag = rBody.angularVelocity.magnitude;
        if (mag > postCrashMaxAngVel)
            rBody.angularVelocity = rBody.angularVelocity.normalized * postCrashMaxAngVel;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MINOR CRASH: sparks + camera shake + gentle stun
    // ═══════════════════════════════════════════════════════════════

    private void HandleMinorCrash(Vector3 point, Vector3 normal, float speed)
    {
        float damage = Mathf.InverseLerp(minorThreshold, mediumThreshold, speed) * 0.1f;
        Health = Mathf.Max(Health - damage, 0f);

        SpawnSparks(point, normal, sparkCountMinor);
        ApplyCameraShake(0.04f + speed * 0.015f);

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(minorStunDuration, minorStunMotorScale));

        Debug.Log($"[CrashSystem] Minor crash at {speed:F2} display-m/s — Health: {Health:P0}");
    }

    // ═══════════════════════════════════════════════════════════════
    //  MEDIUM CRASH: nearest arm group breaks off
    // ═══════════════════════════════════════════════════════════════

    private void HandleMediumCrash(Vector3 point, Vector3 normal, float speed)
    {
        float damage = Mathf.InverseLerp(mediumThreshold, hardThreshold, speed) * 0.35f + 0.15f;
        Health = Mathf.Max(Health - damage, 0f);

        SpawnSparks(point, normal, sparkCountMedium);
        ApplyCameraShake(0.08f + speed * 0.03f);

        // Detach the nearest intact arm group
        int nearest = FindNearestIntactArm(point);
        if (nearest >= 0)
            DetachArmGroup(nearest, point);

        // Extra fast hit = second arm
        if (speed > (mediumThreshold + hardThreshold) / 2f)
        {
            int second = FindNearestIntactArm(point);
            if (second >= 0)
                DetachArmGroup(second, point);
        }

        if (activeSmokeSystem == null)
            activeSmokeSystem = CreateSmokeEffect(20f);

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(minorStunDuration * 1.5f, mediumStunMotorScale));

        Debug.Log($"[CrashSystem] Medium crash at {speed:F2} display-m/s — Health: {Health:P0}");

        if (Health <= 0f)
            HandleTotalDestruction(point);
    }

    // ═══════════════════════════════════════════════════════════════
    //  HARD CRASH: all arms break off, drone crash-lands as a shell
    // ═══════════════════════════════════════════════════════════════

    private void HandleHardCrash(Vector3 point, Vector3 normal, float speed)
    {
        Health = 0f;

        SpawnSparks(point, normal, sparkCountHard);
        ApplyCameraShake(0.15f + speed * 0.04f);
        screenFlashAlpha = 0.8f;

        // Detach all remaining arms — they fall away naturally, no explosion
        for (int i = 0; i < propellerTransforms.Length; i++)
        {
            if (!armGroupDetached[i])
                DetachArmGroup(i, point);
        }

        HandleTotalDestruction(point);

        Debug.Log($"[CrashSystem] HARD crash at {speed:F2} display-m/s — broken apart");
    }

    // ═══════════════════════════════════════════════════════════════
    //  TOTAL DESTRUCTION — no explosion, just dead on the ground
    // ═══════════════════════════════════════════════════════════════

    private void HandleTotalDestruction(Vector3 impactPoint)
    {
        IsDestroyed = true;
        MotorOutputScale = 0f;
        for (int i = 0; i < 4; i++) MotorHealth[i] = 0f;

        // Smoke from the wreckage
        if (activeSmokeSystem == null)
            activeSmokeSystem = CreateSmokeEffect(30f);
        else
        {
            var emission = activeSmokeSystem.emission;
            emission.rateOverTime = 30f;
        }

        // The body stays visible — it's the broken central shell on the ground.
        // No body shatter, no explosion. Just a crashed drone.

        StartCoroutine(RespawnCoroutine());
    }

    // ═══════════════════════════════════════════════════════════════
    //  ARM GROUP DETACHMENT (uses actual FBX model parts)
    // ═══════════════════════════════════════════════════════════════

    private int FindNearestIntactArm(Vector3 worldPoint)
    {
        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < propellerTransforms.Length; i++)
        {
            if (armGroupDetached[i]) continue;
            float dist = Vector3.Distance(propellerTransforms[i].position, worldPoint);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    private void DetachArmGroup(int index, Vector3 impactPoint)
    {
        if (index >= propellerTransforms.Length || armGroupDetached[index]) return;

        armGroupDetached[index] = true;
        propellerDetached[index] = true;
        if (index < MotorHealth.Length) MotorHealth[index] = 0f;

        Vector3 propWorldPos = propellerTransforms[index].position;
        Vector3 awayDir = (propWorldPos - transform.position).normalized;
        if (awayDir.sqrMagnitude < 0.001f) awayDir = Random.onUnitSphere;
        awayDir = (awayDir + Vector3.up * 0.3f).normalized;

        // Create a single debris object containing all arm-group meshes + the propeller
        GameObject armDebris = new GameObject($"{debrisNamePrefix}ArmGroup_{index}");
        armDebris.transform.position = propWorldPos;
        armDebris.transform.rotation = propellerTransforms[index].rotation;

        // Copy arm group renderers into debris
        List<Renderer> groupRenderers = armGroupRenderers[index];
        foreach (Renderer rend in groupRenderers)
        {
            if (rend == null) continue;
            CopyRendererToDebris(rend, armDebris);
            rend.enabled = false; // Hide original
        }

        // Copy propeller mesh into debris
        foreach (MeshFilter mf in propellerTransforms[index].GetComponentsInChildren<MeshFilter>())
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            GameObject meshCopy = new GameObject(mf.name);
            meshCopy.transform.SetParent(armDebris.transform, true);
            meshCopy.transform.position = mf.transform.position;
            meshCopy.transform.rotation = mf.transform.rotation;
            meshCopy.transform.localScale = mf.transform.lossyScale;

            MeshFilter newMf = meshCopy.AddComponent<MeshFilter>();
            newMf.sharedMesh = mf.sharedMesh;
            MeshRenderer newMr = meshCopy.AddComponent<MeshRenderer>();
            newMr.sharedMaterials = mr.sharedMaterials;
        }

        // Hide original propeller
        SetPropellerVisible(index, false);

        // Add physics to the debris group
        Rigidbody debrisRb = armDebris.AddComponent<Rigidbody>();
        debrisRb.mass = 0.12f;
        debrisRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BoxCollider bc = armDebris.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.15f, 0.05f, 0.15f);
        IgnoreCollisionWithDrone(bc);

        // Gentle detach force — not an explosion, just pieces coming apart
        Vector3 force = awayDir * detachForce + Vector3.up * detachUpward;
        // Add the drone's current velocity so debris continues naturally
        if (rBody != null) force += rBody.velocity * 0.5f;

        debrisRb.AddForce(force, ForceMode.Impulse);
        debrisRb.AddTorque(Random.insideUnitSphere * detachTorque, ForceMode.Impulse);

        activeDebris.Add(armDebris);
        Destroy(armDebris, debrisLifetime);

        // Sparks at the break point
        SpawnSparks(propWorldPos, Vector3.up, 15);

        Debug.Log($"[CrashSystem] Arm group {index} detached ({groupRenderers.Count} parts + propeller)");
    }

    private void CopyRendererToDebris(Renderer source, GameObject debrisParent)
    {
        MeshFilter mf = source.GetComponent<MeshFilter>();
        MeshRenderer mr = source as MeshRenderer;
        if (mf == null || mr == null) return;

        GameObject copy = new GameObject(source.name);
        copy.transform.SetParent(debrisParent.transform, true);
        copy.transform.position = source.transform.position;
        copy.transform.rotation = source.transform.rotation;
        copy.transform.localScale = source.transform.lossyScale;

        MeshFilter newMf = copy.AddComponent<MeshFilter>();
        newMf.sharedMesh = mf.sharedMesh;
        MeshRenderer newMr = copy.AddComponent<MeshRenderer>();
        newMr.sharedMaterials = mr.sharedMaterials;
    }

    private void IgnoreCollisionWithDrone(Collider debrisCollider)
    {
        if (droneColliders == null || debrisCollider == null) return;
        foreach (Collider dc in droneColliders)
        {
            if (dc != null)
                Physics.IgnoreCollision(debrisCollider, dc, true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  VISIBILITY & REPAIR
    // ═══════════════════════════════════════════════════════════════

    private void SetPropellerVisible(int index, bool visible)
    {
        if (index >= propellerTransforms.Length || propellerTransforms[index] == null) return;
        foreach (Renderer r in propellerTransforms[index].GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    private void ReattachAllParts()
    {
        // Restore propellers
        for (int i = 0; i < propellerTransforms.Length; i++)
        {
            if (propellerTransforms[i] == null) continue;
            propellerDetached[i] = false;
            SetPropellerVisible(i, true);
            propellerTransforms[i].localPosition = propellerOriginalLocalPos[i];
            propellerTransforms[i].localRotation = propellerOriginalLocalRot[i];
            propellerTransforms[i].localScale = propellerOriginalLocalScale[i];
        }

        // Restore arm group renderers
        for (int i = 0; i < armGroupRenderers.Length; i++)
        {
            armGroupDetached[i] = false;
            for (int j = 0; j < armGroupRenderers[i].Count; j++)
            {
                Renderer rend = armGroupRenderers[i][j];
                if (rend != null)
                    rend.enabled = true;
            }
        }
    }

    private void CleanupEffects()
    {
        if (activeSmokeSystem != null)
        {
            Destroy(activeSmokeSystem.gameObject);
            activeSmokeSystem = null;
        }
    }

    private void CleanupDebris()
    {
        for (int i = activeDebris.Count - 1; i >= 0; i--)
        {
            if (activeDebris[i] != null)
                Destroy(activeDebris[i]);
        }
        activeDebris.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CAMERA SHAKE
    // ═══════════════════════════════════════════════════════════════

    private void ApplyCameraShake(float intensity)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PARTICLE EFFECTS
    // ═══════════════════════════════════════════════════════════════

    private void SpawnSparks(Vector3 position, Vector3 normal, int count)
    {
        GameObject sparkObj = new GameObject("MetalSparks");
        sparkObj.transform.position = position;
        sparkObj.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.02f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.9f, 1f),
            new Color(1f, 0.9f, 0.4f, 1f)
        );
        main.gravityModifier = 3.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, count) });
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60f;
        shape.radius = 0.03f;

        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(new Color(1f, 0.95f, 0.6f, 1f));
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.5f;
        renderer.velocityScale = 0.08f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient sparkGradient = new Gradient();
        sparkGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.3f),
                new GradientColorKey(new Color(0.8f, 0.5f, 0.15f), 0.7f),
                new GradientColorKey(new Color(0.4f, 0.35f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = sparkGradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.15f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ps.Play();
        Destroy(sparkObj, 2f);
    }

    private ParticleSystem CreateSmokeEffect(float rate)
    {
        GameObject smokeObj = new GameObject("CrashSmoke");
        smokeObj.transform.SetParent(this.transform, false);
        smokeObj.transform.localPosition = new Vector3(0, 0.16f, 0);

        ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.2f, 0.2f, 0.2f, 0.7f),
            new Color(0.35f, 0.35f, 0.35f, 0.5f)
        );
        main.gravityModifier = -0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 3.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient smokeGradient = new Gradient();
        smokeGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0f),
                new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = smokeGradient;

        var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(new Color(0.3f, 0.3f, 0.3f, 0.6f));

        ps.Play();
        return ps;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MATERIAL HELPERS
    // ═══════════════════════════════════════════════════════════════

    private Material CreateParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Mode", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    // ═══════════════════════════════════════════════════════════════
    //  COROUTINES
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator StunCoroutine(float duration, float scale)
    {
        MotorOutputScale = scale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            MotorOutputScale = Mathf.Lerp(scale, 1.0f, 1f - Mathf.Pow(1f - t, 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        MotorOutputScale = 1.0f;
        stunCoroutine = null;
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (racecar != null)
            LevelManager.ResetCar(racecar.Index);

        yield return new WaitForFixedUpdate();
        Repair();
    }
}
