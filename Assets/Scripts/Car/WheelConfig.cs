using UnityEngine;

public class WheelConfig : MonoBehaviour
{
    [Header("Ground")]
    public LayerMask groundMask;

    [Header("Suspension")]
    public float springStrength = 34000f;
    public float dampenStrength = 6800f;
    public float maxSuspensionForce = 15000f;
    public float restLen = 0.2f;
    public float springTravel = 0.34f;
    public float wheelRadius = 0.69f;
    [Tooltip("Raises the ray origin slightly so the suspension can reacquire the road after a hard compression.")]
    public float raycastRecoveryMargin = 0.2f;
    public float suspensionDeadZone = 0.005f;
    [Range(0f, 1f)] public float bumpStart = 0.72f;
    public float bumpStrength = 12000f;
    public float suspensionForceResponse = 28f;

    [Header("Tire")]
    public float cornerStiffness = 9000f;
    public float maxLateralMu = 1.3f;
    public float frontGripMultiplier = 1.1f;
    public float rearGripMultiplier = 0.92f;
    public float maxLongitudinalMu = 1.15f;
    public float lateralGripResponse = 18f;
    public float longitudinalGripResponse = 12f;
    [Tooltip("Raises drive and brake forces toward the body to reduce arcade-car wheelies and reverse nose lift.")]
    public float longitudinalForceHeightOffset = 0.42f;

    [Header("Rolling Resistance")]
    public float coastDragStiffness = 300f;

    [Header("Visuals")]
    public bool animateWheelMesh = true;
    public float wheelVisualFollowSpeed = 12f;
    public float airborneVisualDroop = 0.18f;

    [Header("Debug")]
    public bool drawDebugForces = true;
    public float debugForceScale = 0.0001f;

    void OnValidate()
    {
        springStrength = Mathf.Max(0f, springStrength);
        dampenStrength = Mathf.Max(0f, dampenStrength);
        maxSuspensionForce = Mathf.Max(0f, maxSuspensionForce);
        restLen = Mathf.Max(0f, restLen);
        springTravel = Mathf.Max(0f, springTravel);
        wheelRadius = Mathf.Max(0f, wheelRadius);
        raycastRecoveryMargin = Mathf.Max(0f, raycastRecoveryMargin);
        suspensionDeadZone = Mathf.Max(0f, suspensionDeadZone);
        bumpStrength = Mathf.Max(0f, bumpStrength);
        suspensionForceResponse = Mathf.Max(0f, suspensionForceResponse);
        cornerStiffness = Mathf.Max(0f, cornerStiffness);
        maxLateralMu = Mathf.Max(0f, maxLateralMu);
        frontGripMultiplier = Mathf.Max(0f, frontGripMultiplier);
        rearGripMultiplier = Mathf.Max(0f, rearGripMultiplier);
        maxLongitudinalMu = Mathf.Max(0f, maxLongitudinalMu);
        lateralGripResponse = Mathf.Max(0f, lateralGripResponse);
        longitudinalGripResponse = Mathf.Max(0f, longitudinalGripResponse);
        longitudinalForceHeightOffset = Mathf.Max(0f, longitudinalForceHeightOffset);
        coastDragStiffness = Mathf.Max(0f, coastDragStiffness);
        wheelVisualFollowSpeed = Mathf.Max(0f, wheelVisualFollowSpeed);
        airborneVisualDroop = Mathf.Max(0f, airborneVisualDroop);
        debugForceScale = Mathf.Max(0f, debugForceScale);
    }
}
