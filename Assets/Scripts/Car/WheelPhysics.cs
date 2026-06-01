using UnityEngine;

public class WheelPhysics : MonoBehaviour
{
    private Rigidbody rb;
    private WheelConfig config;
    private LayerMask groundMask;
    private Vector3 localForwardAxis = Vector3.forward;
    private Vector3 localRightAxis = Vector3.right;
    private float steerAngle;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private Vector3 wheelMeshRestLocalPosition;
    private Vector3 wheelMeshTargetLocalPosition;
    private float smoothedLateralForce;
    private float smoothedLongitudinalForce;

    public bool isFrontWheel;
    public bool isLeftWheel;

    public bool isGrounded { get; private set; }
    public float compression { get; private set; }
    public float compressionDist { get; private set; }
    public float normalForce { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 groundNormal { get; private set; }
    public float lateralSlipSpeed { get; private set; }
    public float forwardSpeed { get; private set; }
    public float contactSpeed { get; private set; }
    public float normalizedLateralSlip { get; private set; }

    private RaycastHit lastHit;

    [Header("References")]
    [SerializeField] private Transform wheelMesh;

    void Start()
    {
        if (!rb)
            rb = GetComponentInParent<Rigidbody>();

        config = GetComponentInParent<WheelConfig>();

        if (!rb || !config)
        {
            Debug.LogError("WheelPhysics needs a Rigidbody and WheelConfig on the car root.", this);
            enabled = false;
            return;
        }

        groundMask = ResolveGroundMask();
        if (wheelMesh)
        {
            wheelMeshRestLocalPosition = wheelMesh.localPosition;
            wheelMeshTargetLocalPosition = wheelMesh.localPosition;
        }
    }

    void LateUpdate()
    {
        if (!wheelMesh || !config) return;

        Vector3 target = config.animateWheelMesh
            ? wheelMeshTargetLocalPosition
            : wheelMeshRestLocalPosition;
        float follow = 1f - Mathf.Exp(-config.wheelVisualFollowSpeed * Time.deltaTime);
        wheelMesh.localPosition = Vector3.Lerp(wheelMesh.localPosition, target, follow);
    }

    void FixedUpdate()
    {
        if (!rb || !config || groundMask == 0) return;

        float maxLen = config.restLen + config.springTravel;
        Vector3 suspensionUp = rb.transform.up;
        Vector3 rayOrigin = transform.position + suspensionUp * config.raycastRecoveryMargin;
        float rayDistance = maxLen + config.wheelRadius + config.raycastRecoveryMargin;

        bool rayHit = TryGetGroundHit(
            rayOrigin,
            -suspensionUp,
            rayDistance,
            out RaycastHit hit);

        if (config.drawDebugForces)
        {
            Color rayColor = rayHit ? Color.green : Color.red;
            Debug.DrawRay(rayOrigin, -suspensionUp * rayDistance, rayColor);
        }

        if (rayHit)
        {
            float suspensionHitDistance = Mathf.Max(0f, hit.distance - config.raycastRecoveryMargin);
            isGrounded = true;
            lastHit = hit;
            contactPoint = hit.point;
            groundNormal = hit.normal;

            Suspension(hit, suspensionHitDistance);
            LateralGrip(hit);
            WheelVisual(hit);
        }
        else
        {
            isGrounded = false;
            compression = 0f;
            compressionDist = 0f;
            normalForce = 0f;
            contactPoint = Vector3.zero;
            groundNormal = Vector3.zero;
            lateralSlipSpeed = 0f;
            forwardSpeed = 0f;
            contactSpeed = 0f;
            normalizedLateralSlip = 0f;
            smoothedLateralForce = 0f;
            smoothedLongitudinalForce = 0f;
            WheelVisualAirborne();
        }
    }

    LayerMask ResolveGroundMask()
    {
        if (config.groundMask != 0)
            return config.groundMask;

        int namedMask = LayerMask.GetMask("Ground", "Drivable", "Floor");
        if (namedMask != 0)
            return namedMask;

        Debug.LogWarning(
            "WheelPhysics could not find Ground, Drivable, or Floor layers. " +
            "Using default raycast layers for setup; assign WheelConfig.groundMask before shipping.",
            this);
        return Physics.DefaultRaycastLayers;
    }

    bool TryGetGroundHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            groundHits,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = groundHits[i];
            if (!candidate.collider)
                continue;

            Transform hitTransform = candidate.collider.transform;
            if (candidate.rigidbody == rb || hitTransform.IsChildOf(rb.transform))
                continue;

            if (candidate.distance < nearestDistance)
            {
                nearestDistance = candidate.distance;
                nearestHit = candidate;
            }
        }

        return nearestDistance < float.PositiveInfinity;
    }

    void Suspension(RaycastHit hit, float hitDistance)
    {
        float springLen = Mathf.Max(0f, hitDistance - config.wheelRadius);
        compressionDist = Mathf.Clamp(config.restLen - springLen, 0f, config.springTravel);
        if (compressionDist < config.suspensionDeadZone)
            compressionDist = 0f;

        compression = config.springTravel <= 0.0001f
            ? 0f
            : Mathf.Clamp01(compressionDist / config.springTravel);

        Vector3 suspensionUp = rb.transform.up;
        float suspensionVelocity = Vector3.Dot(rb.GetPointVelocity(transform.position), suspensionUp);
        float netForce = config.springStrength * compressionDist - config.dampenStrength * suspensionVelocity;
        netForce = Mathf.Max(0f, netForce);

        if (compression > config.bumpStart && config.bumpStart < 1f)
        {
            float t = (compression - config.bumpStart) / (1f - config.bumpStart);
            netForce += config.bumpStrength * t * t;
        }

        float targetNormalForce = Mathf.Min(netForce, config.maxSuspensionForce);
        normalForce = SmoothForce(normalForce, targetNormalForce, config.suspensionForceResponse);
        Vector3 suspensionForce = suspensionUp * normalForce;
        rb.AddForceAtPosition(suspensionForce, hit.point);
        DrawForce(hit.point, suspensionForce, Color.cyan);
    }

    void LateralGrip(RaycastHit hit)
    {
        lateralSlipSpeed = 0f;
        forwardSpeed = 0f;
        contactSpeed = 0f;
        normalizedLateralSlip = 0f;

        if (normalForce <= 0.01f) return;

        Vector3 wheelFwd = WheelForward(hit.normal);
        Vector3 wheelRight = WheelRight(hit.normal, wheelFwd);
        Vector3 velocity = rb.GetPointVelocity(hit.point);

        forwardSpeed = Vector3.Dot(velocity, wheelFwd);
        float lateralVel = Vector3.Dot(velocity, wheelRight);
        lateralSlipSpeed = Mathf.Abs(lateralVel);
        contactSpeed = velocity.magnitude;
        float slipRatio = lateralSlipSpeed / (Mathf.Abs(forwardSpeed) + 4f);
        normalizedLateralSlip = Mathf.Clamp01(Mathf.InverseLerp(0.15f, 0.4f, slipRatio));
        float gripMult = Mathf.Lerp(1f, 0.85f, Mathf.InverseLerp(0.15f, 0.4f, slipRatio));
        float axleGrip = isFrontWheel ? config.frontGripMultiplier : config.rearGripMultiplier;

        float desiredForce = -lateralVel * config.cornerStiffness * gripMult * axleGrip;
        float maxForce = config.maxLateralMu * normalForce * axleGrip;
        float targetLateralForce = Mathf.Clamp(desiredForce, -maxForce, maxForce);
        smoothedLateralForce = SmoothForce(
            smoothedLateralForce,
            targetLateralForce,
            config.lateralGripResponse);

        Vector3 gripForce = wheelRight * smoothedLateralForce;
        rb.AddForceAtPosition(gripForce, hit.point);
        DrawForce(hit.point, gripForce, Color.magenta);
    }

    public void UpdateSteering(float angleDeg)
    {
        if (!isFrontWheel) return;

        steerAngle = Mathf.MoveTowards(steerAngle, angleDeg, 250f * Time.fixedDeltaTime);
    }

    public void SetBody(Rigidbody body, Vector3 forwardAxis, Vector3 rightAxis)
    {
        rb = body;
        localForwardAxis = forwardAxis.sqrMagnitude > 0.0001f ? forwardAxis.normalized : Vector3.forward;
        localRightAxis = rightAxis.sqrMagnitude > 0.0001f ? rightAxis.normalized : Vector3.right;
    }

    public void AccelForce(float accelInput, float accelForce)
    {
        if (!isGrounded || normalForce <= 0.01f) return;

        Vector3 forward = WheelForward(lastHit.normal);
        Vector3 rawDrive = forward * accelForce * Mathf.Clamp(accelInput, -1f, 1f);
        float targetForce = Vector3.ClampMagnitude(rawDrive, config.maxLongitudinalMu * normalForce).magnitude;
        targetForce *= Mathf.Sign(accelInput);
        smoothedLongitudinalForce = SmoothForce(
            smoothedLongitudinalForce,
            targetForce,
            config.longitudinalGripResponse);

        Vector3 driveForce = forward * smoothedLongitudinalForce;
        Vector3 forcePoint = LongitudinalForcePoint(lastHit.point);
        rb.AddForceAtPosition(driveForce, forcePoint);
        DrawForce(forcePoint, driveForce, Color.blue);
    }

    public void Brake(float brakeInput, float brakeStrength)
    {
        if (!isGrounded || normalForce <= 0.01f) return;

        ApplyLongitudinalDrag(brakeStrength * Mathf.Clamp01(brakeInput));
    }

    public void CoastDrag()
    {
        if (!isGrounded || normalForce <= 0.01f) return;

        ApplyLongitudinalDrag(config.coastDragStiffness);
    }

    void ApplyLongitudinalDrag(float dragStrength)
    {
        Vector3 forward = WheelForward(lastHit.normal);
        float forwardVel = Vector3.Dot(rb.GetPointVelocity(lastHit.point), forward);
        float desiredForce = -forwardVel * dragStrength;
        float maxForce = config.maxLongitudinalMu * normalForce;
        float targetForce = Mathf.Clamp(desiredForce, -maxForce, maxForce);
        smoothedLongitudinalForce = SmoothForce(
            smoothedLongitudinalForce,
            targetForce,
            config.longitudinalGripResponse);

        Vector3 dragForce = forward * smoothedLongitudinalForce;
        Vector3 forcePoint = LongitudinalForcePoint(lastHit.point);
        rb.AddForceAtPosition(dragForce, forcePoint);
        DrawForce(forcePoint, dragForce, Color.yellow);
    }

    void DrawForce(Vector3 origin, Vector3 force, Color color)
    {
        if (!config.drawDebugForces) return;

        Debug.DrawRay(origin, force * config.debugForceScale, color);
    }

    static float SmoothForce(float current, float target, float response)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, response) * Time.fixedDeltaTime);
        return Mathf.Lerp(current, target, t);
    }

    Vector3 LongitudinalForcePoint(Vector3 contact)
    {
        return contact + rb.transform.up * config.longitudinalForceHeightOffset;
    }

    void WheelVisual(RaycastHit hit)
    {
        if (!wheelMesh || !config.animateWheelMesh) return;

        Vector3 wheelCenter = hit.point + hit.normal * config.wheelRadius;
        SetWheelVisualOffset(wheelCenter - transform.position);
    }

    void WheelVisualAirborne()
    {
        if (!wheelMesh || !config.animateWheelMesh) return;

        float droop = config.restLen + Mathf.Min(config.springTravel, config.airborneVisualDroop);
        SetWheelVisualOffset(-rb.transform.up * droop);
    }

    void SetWheelVisualOffset(Vector3 worldOffset)
    {
        Transform visualParent = wheelMesh.parent;
        Vector3 localOffset = visualParent
            ? visualParent.InverseTransformVector(worldOffset)
            : worldOffset;

        wheelMeshTargetLocalPosition = wheelMeshRestLocalPosition + localOffset;
    }

    Vector3 WheelForward(Vector3 groundNormal)
    {
        Vector3 suspensionUp = rb ? rb.transform.up : Vector3.up;
        Vector3 bodyForward = rb ? rb.transform.TransformDirection(localForwardAxis) : transform.forward;

        if (isFrontWheel)
            bodyForward = Quaternion.AngleAxis(steerAngle, suspensionUp) * bodyForward;

        Vector3 projected = Vector3.ProjectOnPlane(bodyForward, groundNormal);
        if (projected.sqrMagnitude < 0.0001f)
            projected = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);

        return projected.normalized;
    }

    Vector3 WheelRight(Vector3 groundNormal, Vector3 wheelForward)
    {
        Vector3 right = Vector3.Cross(groundNormal, wheelForward);
        if (right.sqrMagnitude < 0.0001f)
            right = rb ? rb.transform.TransformDirection(localRightAxis) : transform.right;

        right.Normalize();

        Vector3 bodyRight = rb ? rb.transform.TransformDirection(localRightAxis) : transform.right;
        if (Vector3.Dot(right, bodyRight) < 0f)
            right = -right;

        return right;
    }
}
