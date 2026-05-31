using UnityEngine;

public class WheelConfig : MonoBehaviour
{
    [Header("Ground")]
    public LayerMask groundMask = 1 << 3;

    [Header("Suspension")]
    public float springStrength = 55000f;
    public float dampenStrength = 6500f;
    public float restLen = 0.5f;
    public float springTravel = 0.2f;
    public float wheelRadius = 0.3f;
    [Range(0f, 1f)] public float bumpStart = 0.8f;
    public float bumpStrength = 90000f;

    [Header("Tire")]
    public float cornerStiffness = 16000f;
    public float maxLateralMu = 2.4f;
    public float maxLongitudinalMu = 2f;

    [Header("Rolling Resistance")]
    public float coastDragStiffness = 550f;

    [Header("Visuals")]
    public bool animateWheelMesh = false;

    void OnValidate()
    {
        springStrength = Mathf.Max(0f, springStrength);
        dampenStrength = Mathf.Max(0f, dampenStrength);
        restLen = Mathf.Max(0f, restLen);
        springTravel = Mathf.Max(0f, springTravel);
        wheelRadius = Mathf.Max(0f, wheelRadius);
        bumpStrength = Mathf.Max(0f, bumpStrength);
        cornerStiffness = Mathf.Max(0f, cornerStiffness);
        maxLateralMu = Mathf.Max(0f, maxLateralMu);
        maxLongitudinalMu = Mathf.Max(0f, maxLongitudinalMu);
        coastDragStiffness = Mathf.Max(0f, coastDragStiffness);
    }
}
