using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public float MaxSpeed => maxSpeed;
    public float Acceleration => acceleration;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelPhysics[] wheels;
    public bool isAIControlled = false;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 7500f;
    [SerializeField] private float brakeForce = 8000f;
    [SerializeField] private float maxSpeed = 55f;
    [SerializeField] private float throttleResponse = 3.5f;
    [SerializeField] private float throttleReleaseSpeed = 6f;
    [SerializeField] private float speedLimiterResponse = 4f;
    [SerializeField, Range(0f, 0.25f)] private float throttleInputDeadZone = 0.04f;
    [SerializeField] private float directionChangeBrakeSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float directionChangeBrakeScale = 0.85f;
    [SerializeField, Range(0f, 1f)] private float frontLiftDriveScale = 0.35f;
    [SerializeField, Range(0f, 1f)] private float rearLiftReverseScale = 0.55f;
    [SerializeField, Range(0.1f, 1f)] private float axleSupportReference = 0.45f;
    [SerializeField] private float driveGroundingResponse = 8f;

    [Header("Steering")]
    [SerializeField] private float maxSteeringAngle = 48f;
    [SerializeField] private float steerAtMaxSpeed = 12f;
    [SerializeField] private float steerResponse = 3.5f;
    [SerializeField] private float steerReturnSpeed = 6f;
    [SerializeField, Range(0f, 0.25f)] private float steerInputDeadZone = 0.05f;
    [SerializeField, Range(0.1f, 2f)] private float highSpeedSteerCurve = 0.65f;
    [SerializeField] private float turnAssist = 1500f;
    [SerializeField] private float turnAssistResponse = 6f;
    [SerializeField, Range(0f, 1f)] private float lowSpeedTurnAssistScale = 0.45f;
    [SerializeField] private float collisionRecoverySpeed = 2.5f;
    [SerializeField] private float collisionRecoveryTurnMultiplier = 1.45f;
    [SerializeField] private float collisionReleaseForce = 1600f;
    [SerializeField] private float collisionRecoveryDuration = 0.2f;
    private float trackWidth;
    private float wheelBase;
    private Vector3 localForwardAxis = Vector3.forward;
    private Vector3 localRightAxis = Vector3.right;
    private float smoothedThrottleInput;
    private float smoothedSteerInput;
    private float smoothedTurnAssist;
    private float smoothedDriveGroundingScale = 1f;
    private float collisionRecoveryTimer;
    private Vector3 collisionReleaseNormal;

    [Header("Stability")]
    [SerializeField] private float downforce = 80f;
    [SerializeField] private float corneringDownforce = 85f;
    [SerializeField] private float rollStabilize = 9500f;
    [SerializeField] private float rollPitchDamping = 2200f;
    [SerializeField] private float yawDamping = 1.25f;
    [SerializeField] private Transform centerOfMassOverride;
    [SerializeField] private Vector3 fallbackCenterOfMass = new Vector3(0f, -0.48f, 0f);
    [SerializeField] private float maxAngularSpeed = 2.5f;

    [Header("Anti-Roll")]
    [SerializeField] private float frontAntiRoll = 9500f;
    [SerializeField] private float rearAntiRoll = 8000f;
    [SerializeField] private float antiRollResponse = 14f;

    [HideInInspector] public float throttleInput;
    [HideInInspector] public float steerInput;
    [HideInInspector] public bool brakeInput;

    private WheelPhysics FL, FR, BL, BR;

    public Rigidbody Body => rb;
    public WheelPhysics[] Wheels => wheels;
    public float Speed => rb ? rb.linearVelocity.magnitude : 0f;
    public float Speed01 => Mathf.Clamp01(Speed / Mathf.Max(0.1f, maxSpeed));
    public float ThrottleInput => smoothedThrottleInput;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("CarController needs a Rigidbody on the car root.", this);
            enabled = false;
            return;
        }

        if (wheels == null || wheels.Length == 0)
            wheels = GetComponentsInChildren<WheelPhysics>();

        rb.linearDamping = Mathf.Max(rb.linearDamping, 0.05f);
        rb.angularDamping = Mathf.Max(rb.angularDamping, 1f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxAngularVelocity = Mathf.Max(0.1f, maxAngularSpeed);

        if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.centerOfMass = centerOfMassOverride
            ? transform.InverseTransformPoint(centerOfMassOverride.position)
            : fallbackCenterOfMass;

        CalculateDimensions();

        foreach (var wheel in wheels)
        {
            if (wheel)
                wheel.SetBody(rb, localForwardAxis, localRightAxis);
        }
    }

    void CalculateDimensions()
    {
        FL = FR = BL = BR = null;

        foreach (var w in wheels)
        {
            if (!w) continue;

            if (w.isFrontWheel && w.isLeftWheel) FL = w;
            if (w.isFrontWheel && !w.isLeftWheel) FR = w;
            if (!w.isFrontWheel && w.isLeftWheel) BL = w;
            if (!w.isFrontWheel && !w.isLeftWheel) BR = w;
        }

        if (FL && FR && BL && BR)
        {
            Vector3 fl = transform.InverseTransformPoint(FL.transform.position);
            Vector3 fr = transform.InverseTransformPoint(FR.transform.position);
            Vector3 bl = transform.InverseTransformPoint(BL.transform.position);
            Vector3 br = transform.InverseTransformPoint(BR.transform.position);

            Vector3 frontCenter = (fl + fr) * 0.5f;
            Vector3 rearCenter = (bl + br) * 0.5f;
            Vector3 leftCenter = (fl + bl) * 0.5f;
            Vector3 rightCenter = (fr + br) * 0.5f;

            localForwardAxis = (frontCenter - rearCenter).normalized;
            localRightAxis = (rightCenter - leftCenter).normalized;
            wheelBase = Vector3.Distance(frontCenter, rearCenter);
            trackWidth = Vector3.Distance(leftCenter, rightCenter);
        }
        else
        {
            wheelBase = 2f;
            trackWidth = 1.2f;
            localForwardAxis = Vector3.forward;
            localRightAxis = Vector3.right;
        }
    }

    void FixedUpdate()
    {
        if (!rb) return;

        collisionRecoveryTimer = Mathf.Max(0f, collisionRecoveryTimer - Time.fixedDeltaTime);
        float speed = rb.linearVelocity.magnitude;

        if (speed > maxSpeed)
        {
            Vector3 limitedVelocity = rb.linearVelocity.normalized * maxSpeed;
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                limitedVelocity,
                SmoothFactor(speedLimiterResponse));
            speed = rb.linearVelocity.magnitude;
        }

        float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, maxSpeed));
        float throttleRate = Mathf.Abs(throttleInput) > Mathf.Abs(smoothedThrottleInput)
            ? throttleResponse
            : throttleReleaseSpeed;
        smoothedThrottleInput = Mathf.MoveTowards(
            smoothedThrottleInput,
            throttleInput,
            throttleRate * Time.fixedDeltaTime);
        float steerRate = Mathf.Abs(steerInput) > 0.001f ? steerResponse : steerReturnSpeed;
        smoothedSteerInput = Mathf.MoveTowards(
            smoothedSteerInput,
            steerInput,
            steerRate * Time.fixedDeltaTime);
        float steerSpeed01 = Mathf.Pow(speed01, highSpeedSteerCurve);
        float currentMaxSteer = Mathf.Lerp(maxSteeringAngle, steerAtMaxSpeed, steerSpeed01);

        float steerAngleLeft = 0f;
        float steerAngleRight = 0f;

        if (Mathf.Abs(smoothedSteerInput) > 0.001f)
        {
            float steerAngle = Mathf.Abs(smoothedSteerInput) * currentMaxSteer;
            float steerRad = steerAngle * Mathf.Deg2Rad;

            float tan = Mathf.Tan(steerRad);
            tan = Mathf.Clamp(tan, 0.0001f, 9999f);

            float radius = wheelBase / tan;

            float inner = Mathf.Atan(wheelBase / (radius - trackWidth * 0.5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelBase / (radius + trackWidth * 0.5f)) * Mathf.Rad2Deg;

            if (smoothedSteerInput > 0f) // right
            {
                steerAngleLeft = outer;
                steerAngleRight = inner;
            }
            else // left
            {
                steerAngleLeft = -inner;
                steerAngleRight = -outer;
            }
        }

        Vector3 carForward = rb.transform.TransformDirection(localForwardAxis).normalized;
        float forwardSpeed = Vector3.Dot(carForward, rb.linearVelocity);
        float targetDriveGroundingScale = DriveGroundingScale(smoothedThrottleInput);
        smoothedDriveGroundingScale = Mathf.Lerp(
            smoothedDriveGroundingScale,
            targetDriveGroundingScale,
            SmoothFactor(driveGroundingResponse));
        bool changingDirection = IsChangingDirection(smoothedThrottleInput, forwardSpeed);

        foreach (var wheel in wheels)
        {
            if (!wheel) continue;

            if (wheel.isFrontWheel)
            {
                float angle = wheel.isLeftWheel ? steerAngleLeft : steerAngleRight;
                wheel.UpdateSteering(angle);
            }

            if (brakeInput)
            {
                wheel.Brake(1f, brakeForce);
            }
            else
            {
                float reverseLimit = -maxSpeed * 0.5f;

                if (changingDirection)
                {
                    wheel.Brake(directionChangeBrakeScale, brakeForce);
                }
                else if (Mathf.Abs(smoothedThrottleInput) > 0.001f &&
                    (smoothedThrottleInput > 0f ? forwardSpeed < maxSpeed : forwardSpeed > reverseLimit))
                {
                    wheel.AccelForce(smoothedThrottleInput * smoothedDriveGroundingScale, acceleration);
                }
                else if (!isAIControlled)
                {
                    wheel.CoastDrag();
                }
            }
        }

        AntiRoll();

        Vector3 carUp = rb.transform.up;
        float corneringLoad = corneringDownforce * speed * Mathf.Abs(smoothedSteerInput);
        rb.AddForce(-carUp * (downforce * speed + corneringLoad), ForceMode.Force);

        Vector3 torqueAxis = Vector3.Cross(carUp, Vector3.up);
        rb.AddTorque(torqueAxis * rollStabilize, ForceMode.Force);

        Vector3 yawVelocity = Vector3.Project(rb.angularVelocity, carUp);
        Vector3 rollPitchVelocity = rb.angularVelocity - yawVelocity;
        rb.AddTorque(-rollPitchVelocity * rollPitchDamping, ForceMode.Force);
        rb.angularVelocity -= yawVelocity * Mathf.Clamp01(yawDamping * Time.fixedDeltaTime);

        float targetTurnAssist = 0f;
        int groundedWheels = GroundedWheelCount();
        if (groundedWheels >= 2)
        {
            float turnSpeed = Mathf.Abs(forwardSpeed);
            float assistAtSpeed = Mathf.InverseLerp(2f, 18f, turnSpeed);
            float lowSpeedAssist = Mathf.Lerp(lowSpeedTurnAssistScale, 1f, assistAtSpeed);
            float highSpeedFade = Mathf.Lerp(1f, 0.65f, speed01);
            float groundedRatio = groundedWheels / 4f;
            float travelDirection = Mathf.Abs(forwardSpeed) > 0.5f
                ? Mathf.Sign(forwardSpeed)
                : Mathf.Sign(smoothedThrottleInput);
            bool recoveringFromCollision =
                collisionRecoveryTimer > 0f &&
                turnSpeed < collisionRecoverySpeed &&
                Mathf.Abs(smoothedThrottleInput) > 0.1f;
            float collisionMultiplier = recoveringFromCollision
                ? collisionRecoveryTurnMultiplier
                : 1f;
            targetTurnAssist =
                smoothedSteerInput * travelDirection * turnAssist * lowSpeedAssist * highSpeedFade *
                groundedRatio * collisionMultiplier;

            if (recoveringFromCollision && collisionReleaseNormal.sqrMagnitude > 0.0001f)
            {
                float releaseScale = Mathf.Abs(smoothedSteerInput) * Mathf.Abs(smoothedThrottleInput);
                rb.AddForce(collisionReleaseNormal * collisionReleaseForce * releaseScale, ForceMode.Force);
            }
        }

        smoothedTurnAssist = Mathf.Lerp(smoothedTurnAssist, targetTurnAssist, SmoothFactor(turnAssistResponse));
        rb.AddTorque(carUp * smoothedTurnAssist, ForceMode.Force);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!rb) return;

        Vector3 carUp = rb.transform.up;
        Vector3 releaseNormal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (Mathf.Abs(Vector3.Dot(normal, carUp)) < 0.55f)
                releaseNormal += Vector3.ProjectOnPlane(normal, carUp);
        }

        if (releaseNormal.sqrMagnitude <= 0.0001f) return;

        collisionReleaseNormal = releaseNormal.normalized;
        collisionRecoveryTimer = collisionRecoveryDuration;
    }

    void AntiRoll()
    {
        AxleAntiRoll(FL, FR, frontAntiRoll, ref smoothedFrontAntiRoll);
        AxleAntiRoll(BL, BR, rearAntiRoll, ref smoothedRearAntiRoll);
    }

    private float smoothedFrontAntiRoll;
    private float smoothedRearAntiRoll;

    void AxleAntiRoll(WheelPhysics left, WheelPhysics right, float strength, ref float smoothedForce)
    {
        if (!left || !right) return;

        bool leftGround = left.isGrounded;
        bool rightGround = right.isGrounded;
        if (!leftGround && !rightGround)
        {
            smoothedForce = Mathf.Lerp(smoothedForce, 0f, SmoothFactor(antiRollResponse));
            return;
        }

        float travelL = leftGround ? left.compressionDist : 0f;
        float travelR = rightGround ? right.compressionDist : 0f;

        float diff = travelL - travelR; // + if left more compressed
        float targetForce = diff * strength;
        float maxForcePerSide = rb.mass * Physics.gravity.magnitude * 0.6f;
        targetForce = Mathf.Clamp(targetForce, -maxForcePerSide, maxForcePerSide);
        smoothedForce = Mathf.Lerp(smoothedForce, targetForce, SmoothFactor(antiRollResponse));

        Vector3 up = rb.transform.up;

        Vector3 leftForcePoint = leftGround ? left.contactPoint : left.transform.position;
        Vector3 rightForcePoint = rightGround ? right.contactPoint : right.transform.position;
        rb.AddForceAtPosition(up * smoothedForce, leftForcePoint);
        rb.AddForceAtPosition(-up * smoothedForce, rightForcePoint);
    }

    int GroundedWheelCount()
    {
        int count = 0;
        foreach (var wheel in wheels)
        {
            if (wheel && wheel.isGrounded)
                count++;
        }

        return count;
    }

    float DriveGroundingScale(float throttle)
    {
        if (throttle > 0.001f)
            return Mathf.Lerp(frontLiftDriveScale, 1f, AxleSupport01(FL, FR));

        if (throttle < -0.001f)
            return Mathf.Lerp(rearLiftReverseScale, 1f, AxleSupport01(BL, BR));

        return 1f;
    }

    float AxleSupport01(WheelPhysics left, WheelPhysics right)
    {
        float normalForce = 0f;
        if (left && left.isGrounded) normalForce += left.normalForce;
        if (right && right.isGrounded) normalForce += right.normalForce;

        float referenceForce = rb.mass * Physics.gravity.magnitude * Mathf.Max(0.1f, axleSupportReference);
        return Mathf.Clamp01(normalForce / referenceForce);
    }

    bool IsChangingDirection(float throttle, float forwardSpeed)
    {
        return Mathf.Abs(throttle) > 0.001f &&
            Mathf.Abs(forwardSpeed) > directionChangeBrakeSpeed &&
            Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed);
    }

    static float SmoothFactor(float response)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, response) * Time.fixedDeltaTime);
    }

    public void SetInputs(float throttle, float steer, bool brake)
    {
        throttleInput = ApplyDeadZone(Mathf.Clamp(throttle, -1f, 1f), throttleInputDeadZone);
        steerInput = ApplyDeadZone(Mathf.Clamp(steer, -1f, 1f), steerInputDeadZone);
        brakeInput = brake;
    }

    static float ApplyDeadZone(float value, float deadZone)
    {
        float magnitude = Mathf.Abs(value);
        if (magnitude <= deadZone)
            return 0f;

        return Mathf.Sign(value) * Mathf.InverseLerp(deadZone, 1f, magnitude);
    }

    public void SetOverdriveSpeeds(float targetMaxSpeed, float targetAcceleration)
    {
        maxSpeed = targetMaxSpeed;
        acceleration = targetAcceleration;
    }
}
