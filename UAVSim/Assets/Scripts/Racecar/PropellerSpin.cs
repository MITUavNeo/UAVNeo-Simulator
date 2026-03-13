using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spins the propeller GameObjects based on actual motor speeds from the Flight physics.
///
/// Instead of relying on index ordering (which depends on prefab builder sorting),
/// each propeller's spin direction is determined at runtime by its ACTUAL POSITION
/// relative to the drone center:
///
///   FL (CW ↻)    FR (CCW ↺)
///   BL (CCW ↺)   BR (CW ↻)
///
/// The sign mapping also accounts for whether the propeller mesh's local-Y axis
/// is aligned or inverted relative to the drone's up vector.
/// </summary>
public class PropellerSpin : MonoBehaviour
{
    [SerializeField]
    private float fallbackSpinSpeed = 3000f;

    [SerializeField]
    private float visualSpeedScale = 20.0f;

    private Transform[] propellers;
    private float[] directions;
    private Flight flight;

    private void Start()
    {
        flight = GetComponent<Flight>();
        FindPropellers();
    }

    private void FindPropellers()
    {
        List<Transform> found = new List<Transform>();
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (t.name.StartsWith("Propeller_"))
                found.Add(t);
        }

        // Sort by name so index matches Flight.MotorSpeeds order
        found.Sort((a, b) => a.name.CompareTo(b.name));

        propellers = found.ToArray();
        directions = new float[propellers.Length];

        // Assign spin direction based on each propeller's actual world position.
        // This removes all dependency on the index-to-position mapping.
        for (int i = 0; i < propellers.Length; i++)
        {
            // Get propeller position in the drone's local coordinate system
            // (drone forward = +Z, drone right = +X, drone up = +Y)
            Vector3 localPos = transform.InverseTransformPoint(propellers[i].position);

            bool isFront = localPos.z >= 0;
            bool isLeft = localPos.x < 0;

            // Desired spin directions:
            //   FL = CW,  FR = CCW
            //   BL = CCW, BR = CW
            bool wantCW;
            if (isFront && isLeft) wantCW = true;       // FL → CW
            else if (isFront && !isLeft) wantCW = false; // FR → CCW
            else if (!isFront && isLeft) wantCW = false;  // BL → CCW
            else wantCW = true;                            // BR → CW

            // Check if this propeller's local Y aligns with or opposes the drone's up.
            // FBX imports can flip the local axis on some children. If inverted,
            // we need to reverse the rotation sign so the visual still looks correct.
            float yDot = Vector3.Dot(propellers[i].up, transform.up);
            bool meshInverted = yDot < 0;

            float sign = wantCW ? 1f : -1f;
            if (meshInverted) sign *= -1f;

            directions[i] = sign;

            string quadrant = (isFront ? "Front" : "Back") + (isLeft ? " Left" : " Right");
            string target = wantCW ? "CW" : "CCW";
            Debug.Log($"[PropellerSpin] Propeller_{i} at local({localPos.x:F3}, {localPos.y:F3}, {localPos.z:F3}) → {quadrant}, target={target}, meshInverted={meshInverted}, sign={sign}");
        }

        Debug.Log($"[PropellerSpin] Found {propellers.Length} propellers, Flight={flight != null}");
    }

    private void Update()
    {
        if (propellers == null) return;

        for (int i = 0; i < propellers.Length; i++)
        {
            if (propellers[i] == null) continue;

            float degPerSec;
            if (flight != null && i < flight.MotorSpeeds.Length)
            {
                degPerSec = flight.MotorSpeeds[i] * visualSpeedScale;
                // Ensure propellers always visually spin when the drone is armed.
                // Motor mixing can drive individual motors to near-zero at certain
                // attitudes, but real props never fully stop in flight.
                if (flight.IsArmed)
                    degPerSec = Mathf.Max(degPerSec, 5000f);
            }
            else
            {
                degPerSec = fallbackSpinSpeed;
            }

            propellers[i].Rotate(0f, degPerSec * directions[i] * Time.deltaTime, 0f, Space.Self);
        }
    }
}
