using UnityEngine;

/// <summary>
/// Creates a placeholder drone model from Unity primitives.
/// Attach this to the drone GameObject to auto-generate a visual.
/// This should be replaced once the real drone model (from STEP/FBX) is imported.
/// </summary>
public class DroneModelSetup : MonoBehaviour
{
    #region Constants
    /// <summary>
    /// The color of the drone body.
    /// Neobotics-inspired dark blue.
    /// </summary>
    private static readonly Color bodyColor = new Color(0.1f, 0.15f, 0.3f, 1f);

    /// <summary>
    /// The color of the drone arms.
    /// Neobotics-inspired accent color.
    /// </summary>
    private static readonly Color armColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// The color of the propellers.
    /// </summary>
    private static readonly Color propColor = new Color(0.85f, 0.25f, 0.1f, 1f);

    /// <summary>
    /// Scale of the entire drone model.
    /// </summary>
    private const float droneScale = 0.5f;
    #endregion

    /// <summary>
    /// The generated model root, so it can be removed when the real model is imported.
    /// </summary>
    private GameObject modelRoot;

    private void Awake()
    {
        // Only generate if there are no mesh renderers already on children
        // (i.e., no real model has been imported yet)
        if (this.GetComponentInChildren<MeshRenderer>() == null)
        {
            this.GeneratePlaceholderModel();
        }
    }

    /// <summary>
    /// Generates a simple quadcopter-shaped placeholder from Unity primitives.
    /// </summary>
    private void GeneratePlaceholderModel()
    {
        this.modelRoot = new GameObject("PlaceholderDroneModel");
        this.modelRoot.transform.SetParent(this.transform, false);
        this.modelRoot.transform.localScale = Vector3.one * droneScale;

        // Central body (flattened cylinder)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(this.modelRoot.transform, false);
        body.transform.localScale = new Vector3(1.0f, 0.15f, 1.0f);
        body.transform.localPosition = Vector3.zero;
        body.GetComponent<Renderer>().material.color = bodyColor;
        Destroy(body.GetComponent<Collider>());

        // Front indicator (small sphere at front)
        GameObject frontIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        frontIndicator.name = "FrontIndicator";
        frontIndicator.transform.SetParent(this.modelRoot.transform, false);
        frontIndicator.transform.localScale = Vector3.one * 0.2f;
        frontIndicator.transform.localPosition = new Vector3(0, 0.1f, 0.4f);
        frontIndicator.GetComponent<Renderer>().material.color = propColor;
        Destroy(frontIndicator.GetComponent<Collider>());

        // Create 4 arms with propellers
        float armLength = 1.2f;
        float[] angles = { 45f, 135f, 225f, 315f };

        for (int i = 0; i < 4; i++)
        {
            float angle = angles[i] * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));

            // Arm (stretched cube)
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = $"Arm_{i}";
            arm.transform.SetParent(this.modelRoot.transform, false);
            arm.transform.localScale = new Vector3(0.1f, 0.08f, armLength);
            arm.transform.localPosition = direction * (armLength * 0.35f);
            arm.transform.localPosition += Vector3.up * 0.02f;
            arm.transform.localRotation = Quaternion.LookRotation(direction);
            arm.GetComponent<Renderer>().material.color = armColor;
            Destroy(arm.GetComponent<Collider>());

            // Motor housing (small cylinder at end of arm)
            GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            motor.name = $"Motor_{i}";
            motor.transform.SetParent(this.modelRoot.transform, false);
            motor.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
            motor.transform.localPosition = direction * armLength * 0.65f + Vector3.up * 0.12f;
            motor.GetComponent<Renderer>().material.color = armColor;
            Destroy(motor.GetComponent<Collider>());

            // Propeller (flattened and stretched cylinder)
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            prop.name = $"Propeller_{i}";
            prop.transform.SetParent(this.modelRoot.transform, false);
            prop.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
            prop.transform.localPosition = direction * armLength * 0.65f + Vector3.up * 0.2f;
            prop.GetComponent<Renderer>().material.color = propColor;
            prop.GetComponent<Renderer>().material.SetFloat("_Metallic", 0.5f);
            Destroy(prop.GetComponent<Collider>());
        }

        // Landing gear (4 small legs)
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));

            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"LandingGear_{i}";
            leg.transform.SetParent(this.modelRoot.transform, false);
            leg.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f);
            leg.transform.localPosition = direction * 0.35f + Vector3.down * 0.2f;
            leg.GetComponent<Renderer>().material.color = armColor;
            Destroy(leg.GetComponent<Collider>());
        }
    }
}
