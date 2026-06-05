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
    private Quaternion wheelMeshRestLocalRotation;
    private float wheelVisualSpin;
    private float wheelVisualForwardSpeed;
    private float smoothedLateralForce;
    private float smoothedLongitudinalForce;
    private bool hasGroundReference;

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
            wheelMeshRestLocalRotation = wheelMesh.localRotation;
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
        UpdateWheelVisualRotation();
    }

    void FixedUpdate()
    {
        if (!rb || !config || groundMask == 0) return;

        float maxLen = config.restLen + config.springTravel;
        Vector3 suspensionUp = rb.transform.up;
        Vector3 rayOrigin = transform.position + suspensionUp * config.raycastRecoveryMargin;
        float rayDistance =
            maxLen + config.wheelRadius + config.raycastRecoveryMargin + config.raycastReacquireExtension;

        bool downwardRayHit = TryGetGroundHit(
            rayOrigin,
            -suspensionUp,
            rayDistance,
            out RaycastHit hit);
        bool recoveredFromPenetration = false;

        if (!downwardRayHit &&
            TryGetGroundHit(rayOrigin, suspensionUp, rayDistance, out RaycastHit recoveryHit) &&
            Vector3.Dot(recoveryHit.normal, suspensionUp) > 0.25f)
        {
            hit = recoveryHit;
            recoveredFromPenetration = true;
        }

        bool rayHit = downwardRayHit || recoveredFromPenetration;

        if (config.drawDebugForces)
        {
            Debug.DrawRay(rayOrigin, -suspensionUp * rayDistance, downwardRayHit ? Color.green : Color.red);
            if (!downwardRayHit)
                Debug.DrawRay(rayOrigin, suspensionUp * rayDistance, recoveredFromPenetration ? Color.yellow : Color.red);
        }

        if (rayHit)
        {
            float suspensionHitDistance = recoveredFromPenetration
                ? 0f
                : Mathf.Max(0f, hit.distance - config.raycastRecoveryMargin);
            isGrounded = true;
            lastHit = hit;
            hasGroundReference = true;
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
        smoothedLateralForce = Mathf.Clamp(smoothedLateralForce, -maxForce, maxForce);

        Vector3 gripForce = wheelRight * smoothedLateralForce;
        Vector3 forcePoint = TractionForcePoint(hit.point);
        rb.AddForceAtPosition(gripForce, forcePoint);
        DrawForce(forcePoint, gripForce, Color.magenta);
    }

    public void UpdateSteering(float angleDeg)
    {
        if (!isFrontWheel) return;

        steerAngle = Mathf.MoveTowards(steerAngle, angleDeg, config.steeringAngleSpeed * Time.fixedDeltaTime);
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
        float gripLimit = LongitudinalGripLimit();
        float targetForce = Vector3.ClampMagnitude(rawDrive, gripLimit).magnitude;
        targetForce *= Mathf.Sign(accelInput);
        smoothedLongitudinalForce = SmoothForce(
            smoothedLongitudinalForce,
            targetForce,
            config.longitudinalGripResponse);
        smoothedLongitudinalForce = Mathf.Clamp(smoothedLongitudinalForce, -gripLimit, gripLimit);

        Vector3 driveForce = forward * smoothedLongitudinalForce;
        Vector3 forcePoint = TractionForcePoint(lastHit.point);
        rb.AddForceAtPosition(driveForce, forcePoint);
        ApplyStepAssist(accelInput, forward);
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
        float maxForce = LongitudinalGripLimit();
        float targetForce = Mathf.Clamp(desiredForce, -maxForce, maxForce);
        smoothedLongitudinalForce = SmoothForce(
            smoothedLongitudinalForce,
            targetForce,
            config.longitudinalGripResponse);
        smoothedLongitudinalForce = Mathf.Clamp(smoothedLongitudinalForce, -maxForce, maxForce);

        Vector3 dragForce = forward * smoothedLongitudinalForce;
        Vector3 forcePoint = TractionForcePoint(lastHit.point);
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

    void ApplyStepAssist(float accelInput, Vector3 wheelForward)
    {
        if (!config.enableStepAssist || Mathf.Abs(accelInput) <= 0.001f)
            return;

        float directionSign = Mathf.Sign(accelInput);
        bool isLeadingWheel = directionSign > 0f ? isFrontWheel : !isFrontWheel;
        if (!isLeadingWheel || rb.linearVelocity.magnitude > config.stepAssistMaxSpeed)
            return;

        Vector3 up = rb.transform.up;
        Vector3 direction = wheelForward * directionSign;
        Vector3 lowOrigin = contactPoint + up * config.stepAssistLowerProbeHeight;
        Vector3 highOrigin = contactPoint + up * config.stepAssistMaxHeight;
        bool lowBlocked = TryGetObstacleHit(lowOrigin, direction, config.stepAssistProbeDistance);
        bool highBlocked = TryGetObstacleHit(highOrigin, direction, config.stepAssistProbeDistance);

        if (config.drawDebugForces)
        {
            Debug.DrawRay(lowOrigin, direction * config.stepAssistProbeDistance, lowBlocked ? Color.yellow : Color.gray);
            Debug.DrawRay(highOrigin, direction * config.stepAssistProbeDistance, highBlocked ? Color.red : Color.green);
        }

        if (!lowBlocked || highBlocked)
            return;

        float throttle = Mathf.Abs(accelInput);
        Vector3 assistForce =
            up * config.stepAssistLiftForce +
            direction * config.stepAssistForwardForce;
        rb.AddForce(assistForce * throttle, ForceMode.Force);
        DrawForce(rb.worldCenterOfMass, assistForce * throttle, Color.white);
    }

    bool TryGetObstacleHit(Vector3 origin, Vector3 direction, float distance)
    {
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            groundHits,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = groundHits[i];
            if (!candidate.collider)
                continue;

            Transform hitTransform = candidate.collider.transform;
            if (candidate.rigidbody == rb || hitTransform.IsChildOf(rb.transform))
                continue;

            return true;
        }

        return false;
    }

    Vector3 TractionForcePoint(Vector3 contact)
    {
        Vector3 suspensionUp = rb.transform.up;
        float heightToCenterOfMass = Vector3.Dot(rb.worldCenterOfMass - contact, suspensionUp);
        return contact + suspensionUp * heightToCenterOfMass;
    }

    float LongitudinalGripLimit()
    {
        float longitudinalLimit = config.maxLongitudinalMu * normalForce;
        float axleGrip = isFrontWheel ? config.frontGripMultiplier : config.rearGripMultiplier;
        float lateralLimit = config.maxLateralMu * normalForce * axleGrip;
        if (lateralLimit <= 0.01f || config.combinedGripBlend <= 0f)
            return longitudinalLimit;

        float lateralLoad = Mathf.Clamp01(Mathf.Abs(smoothedLateralForce) / lateralLimit);
        float frictionCircleScale = Mathf.Sqrt(Mathf.Max(0f, 1f - lateralLoad * lateralLoad));
        float retainedScale = Mathf.Max(config.minimumLongitudinalGripScale, frictionCircleScale);
        return longitudinalLimit * Mathf.Lerp(1f, retainedScale, config.combinedGripBlend);
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
        Vector3 wheelCenter = transform.position - rb.transform.up * droop;

        if (hasGroundReference)
        {
            float pointHeight = Vector3.Dot(transform.position - lastHit.point, lastHit.normal);
            float maxUsefulDistance =
                config.restLen + config.springTravel + config.wheelRadius + config.raycastReacquireExtension;

            if (Mathf.Abs(pointHeight) <= maxUsefulDistance)
            {
                float planeHeight = Vector3.Dot(wheelCenter - lastHit.point, lastHit.normal);
                if (planeHeight < config.wheelRadius)
                    wheelCenter += lastHit.normal * (config.wheelRadius - planeHeight);
            }
        }

        SetWheelVisualOffset(wheelCenter - transform.position);
    }

    void SetWheelVisualOffset(Vector3 worldOffset)
    {
        Vector3 suspensionUp = rb.transform.up;
        float maxDroop = config.restLen + config.springTravel;
        float suspensionOffset = Vector3.Dot(worldOffset, suspensionUp);
        float clampedSuspensionOffset = Mathf.Clamp(suspensionOffset, -maxDroop, 0f);
        worldOffset += suspensionUp * (clampedSuspensionOffset - suspensionOffset);

        Transform visualParent = wheelMesh.parent;
        Vector3 localOffset = visualParent
            ? visualParent.InverseTransformVector(worldOffset)
            : worldOffset;

        wheelMeshTargetLocalPosition = wheelMeshRestLocalPosition + localOffset;
    }

    void UpdateWheelVisualRotation()
    {
        if (!config.animateWheelMesh)
        {
            wheelMesh.localRotation = wheelMeshRestLocalRotation;
            wheelVisualForwardSpeed = 0f;
            return;
        }

        float targetForwardSpeed = isGrounded ? forwardSpeed : 0f;
        float response = isGrounded ? config.wheelVisualFollowSpeed : config.wheelVisualFollowSpeed * 0.35f;
        wheelVisualForwardSpeed = Mathf.Lerp(
            wheelVisualForwardSpeed,
            targetForwardSpeed,
            1f - Mathf.Exp(-response * Time.deltaTime));

        if (config.wheelRadius > 0.001f)
        {
            float spinDegrees = -wheelVisualForwardSpeed / config.wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
            wheelVisualSpin = Mathf.Repeat(wheelVisualSpin + spinDegrees, 360f);
        }

        Transform visualParent = wheelMesh.parent;
        Quaternion parentRotation = visualParent ? visualParent.rotation : Quaternion.identity;
        Quaternion restWorldRotation = parentRotation * wheelMeshRestLocalRotation;

        Vector3 suspensionUp = rb ? rb.transform.up : transform.up;
        Vector3 axle = rb ? rb.transform.TransformDirection(localRightAxis) : transform.right;
        if (isFrontWheel)
            axle = Quaternion.AngleAxis(steerAngle, suspensionUp) * axle;

        if (axle.sqrMagnitude < 0.0001f)
            axle = transform.right;

        Quaternion steerRotation = isFrontWheel
            ? Quaternion.AngleAxis(steerAngle, suspensionUp)
            : Quaternion.identity;
        Quaternion rollRotation = Quaternion.AngleAxis(wheelVisualSpin, axle.normalized);
        Quaternion targetWorldRotation = rollRotation * steerRotation * restWorldRotation;

        wheelMesh.localRotation = visualParent
            ? Quaternion.Inverse(parentRotation) * targetWorldRotation
            : targetWorldRotation;
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
