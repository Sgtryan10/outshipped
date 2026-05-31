using UnityEngine;

public class WheelPhysics : MonoBehaviour
{
    private Rigidbody rb;
    private WheelConfig config;
    private LayerMask groundMask;

    public bool isFrontWheel;
    public bool isLeftWheel;

    public bool isGrounded { get; private set; }
    public float compression { get; private set; }
    public float compressionDist { get; private set; }

    public float normalForce { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 groundNormal { get; private set; }

    private RaycastHit lastHit;
    private float lastSpringLen;

    [Header("References")]
    [SerializeField] private Transform wheelMesh;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        config = GetComponentInParent<WheelConfig>();

        if (!rb || !config)
        {
            Debug.LogError("WheelPhysics needs a Rigidbody and WheelConfig on the car root.", this);
            enabled = false;
            return;
        }

        lastSpringLen = config.restLen;

        groundMask = config.groundMask;
        if (groundMask == 0)
            groundMask = LayerMask.GetMask("Ground", "Floor");
        if (groundMask == 0)
            groundMask = Physics.DefaultRaycastLayers;
    }

    void FixedUpdate()
    {
        float maxLen = config.restLen + config.springTravel;

        RaycastHit hit;
        bool rayHit = Physics.Raycast(transform.position, -transform.up, out hit, maxLen + config.wheelRadius, groundMask);

        float visualHitDist = config.restLen + config.wheelRadius;

        if (rayHit)
        {
            isGrounded = true;
            lastHit = hit;

            contactPoint = hit.point;
            groundNormal = hit.normal;

            Suspension(hit);
            LateralGrip(hit);

            visualHitDist = hit.distance;
        }
        else
        {
            isGrounded = false;
            compression = 0f;
            compressionDist = 0f;
            normalForce = 0f;
        }

        WheelVisual(visualHitDist);
    }

    void Suspension(RaycastHit hit)
    {
        Vector3 contact = hit.point;

        float springLen = Mathf.Max(0f, hit.distance - config.wheelRadius);

        compressionDist = Mathf.Clamp(config.restLen - springLen, 0f, config.springTravel);

        compression = (config.springTravel <= 0.0001f) ? 0f : Mathf.Clamp01(compressionDist / config.springTravel);


        float springVel = (springLen - lastSpringLen) / Time.fixedDeltaTime;
        lastSpringLen = springLen;

        float springForce = config.springStrength * compressionDist; 
        float damperForce = config.dampenStrength * springVel;       

        float net = springForce - damperForce;
        net = Mathf.Max(0f, net);

        if (compression > config.bumpStart && config.bumpStart < 1f)
        {
            float t = (compression - config.bumpStart) / (1f - config.bumpStart); 
            float bumpForce = config.bumpStrength * t * t;   
            net += bumpForce;
        }

        normalForce = net;
        rb.AddForceAtPosition(transform.up * net, contact);
    }

    void LateralGrip(RaycastHit hit)
    {
        if (normalForce <= 0.01f) return;

        Vector3 contact = hit.point;
        Vector3 n = hit.normal;

        Vector3 v = rb.GetPointVelocity(contact);

        Vector3 wheelFwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        Vector3 wheelRight = Vector3.ProjectOnPlane(transform.right, n).normalized;

        float forwardVel = Vector3.Dot(v, wheelFwd);
        float lateralVel = Vector3.Dot(v, wheelRight);

        float desiredLatForce = (-lateralVel) * config.cornerStiffness;

        float slipRatio = Mathf.Abs(lateralVel) / (Mathf.Abs(forwardVel) + 4f);
        float slideT = Mathf.InverseLerp(0.15f, 0.40f, slipRatio);
        float gripMult = Mathf.Lerp(1f, 0.85f, slideT);

        desiredLatForce *= gripMult;

        float maxLatForce = config.maxLateralMu * normalForce;
        float latForce = Mathf.Clamp(desiredLatForce, -maxLatForce, maxLatForce);

        rb.AddForceAtPosition(wheelRight * latForce, contact);
    }

    public void UpdateSteering(float angleDeg)
    {
        if (!isFrontWheel) return;

        float currentAngle = transform.localEulerAngles.y;
        if (currentAngle > 180f) currentAngle -= 360f;

        float newAngle = Mathf.MoveTowards(currentAngle, angleDeg, 250f * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    public void AccelForce(float accelInput, float accelForce)
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;

        float drive = accelForce * Mathf.Clamp(accelInput, -1f, 1f);
        Vector3 rawDrive = fwd * drive;

        float maxLongForce = config.maxLongitudinalMu * normalForce;
        Vector3 driveForce = Vector3.ClampMagnitude(rawDrive, maxLongForce);

        rb.AddForceAtPosition(driveForce, contact);
    }

    public void Brake(float brakeInput, float brakeStrength)
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        float forwardVel = Vector3.Dot(rb.GetPointVelocity(contact), fwd);

        float desired = (-forwardVel) * brakeStrength * Mathf.Clamp01(brakeInput);

        float maxLongForce = config.maxLongitudinalMu * normalForce;
        float clamped = Mathf.Clamp(desired, -maxLongForce, maxLongForce);

        rb.AddForceAtPosition(fwd * clamped, contact);
    }

    public void CoastDrag()
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        float forwardVel = Vector3.Dot(rb.GetPointVelocity(contact), fwd);

        float desired = (-forwardVel) * config.coastDragStiffness;

        float maxLongForce = config.maxLongitudinalMu * normalForce;
        float clamped = Mathf.Clamp(desired, -maxLongForce, maxLongForce);

        rb.AddForceAtPosition(fwd * clamped, contact);
    }

    void WheelVisual(float hitDist)
    {
        if (!wheelMesh || !config.animateWheelMesh) return;

        float springLen = Mathf.Max(0f, hitDist - config.wheelRadius);

        float minLen = config.restLen - config.springTravel;
        float maxLen = config.restLen + config.springTravel;

        springLen = Mathf.Clamp(springLen, minLen, maxLen);

        Vector3 localPos = wheelMesh.localPosition;
        localPos.y = -springLen;
        wheelMesh.localPosition = localPos;
    }
}
