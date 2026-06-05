using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("Turret References")]
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Transform barrel;
    [SerializeField] private Transform muzzle;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Vector3 localFiringAxis = Vector3.down;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimMask;
    [SerializeField] private float maxAimDistance = 300f;
    [SerializeField] private float yawSpeed = 260f;
    [SerializeField] private float pitchSpeed = 180f;
    [SerializeField, Range(0f, 180f)] private float maxTurretYaw = 48f;
    [SerializeField, Range(0f, 60f)] private float maxTurretPitchUp = 16f;
    [SerializeField, Range(0f, 60f)] private float maxTurretPitchDown = 7f;
    [SerializeField] private float cameraAimDistance = 14f;
    [SerializeField] private float cameraAimHeight = 1.5f;
    [SerializeField] private float cameraAimSmoothTime = 0.28f;

    [Header("Camera Feel")]
    [SerializeField] private Vector2 cameraComposerDamping = new Vector2(0.65f, 0.85f);
    [SerializeField] private Vector2 cameraDeadZone = new Vector2(0.08f, 0.06f);
    [SerializeField] private Vector2 cameraHardLimits = new Vector2(0.72f, 0.62f);
    [SerializeField] private Vector3 cameraPositionDamping = new Vector3(0.45f, 0.75f, 0.6f);
    [SerializeField] private Vector3 cameraRotationDamping = new Vector3(1.8f, 0.85f, 0.25f);
    [SerializeField, Range(0f, 90f)] private float maxCameraYaw = 38f;
    [SerializeField, Range(0f, 60f)] private float maxCameraPitchUp = 13f;
    [SerializeField, Range(0f, 60f)] private float maxCameraPitchDown = 6f;

    [Header("Hitscan & Visual Settings")]
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private float hitscanMaxRange = 200f;
    [SerializeField] private float muzzleForwardOffset = 0.12f;
    [SerializeField] private int baseDamage = 25;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Buckshot Settings")]
    [SerializeField] private int buckshotPellets = 8;
    [SerializeField] private float buckshotSpread = 5f;

    private readonly RaycastHit[] aimHits = new RaycastHit[16];
    private Transform cameraAimTarget;
    private Vector3 cameraAimVelocity;
    private Vector3 neutralBarrelForwardLocal;

    public int BaseDamage => baseDamage;

    void Awake()
    {
        AutoFindReferences();
        EnsureMuzzleReference();
        RememberNeutralBarrelDirection();
        EnsureCameraAimTarget();
        SnapCameraAimTarget();
    }

    void Start()
    {
        ConfigureCamera();
    }

    void Update()
    {
        AimAtCursor();
    }

    void LateUpdate()
    {
        UpdateCameraAimTarget();
    }

    public void SetBaseDamage(int damageAmount)
    {
        baseDamage = damageAmount;
    }

    public void SetMaxRange(float maxRangeValue)
    {
        hitscanMaxRange = maxRangeValue;
    }

    public void Fire(float damageMultiplier = 1f)
    {
        if (!barrel) return;

        Vector3 baseDirection = FiringDirection();
        Vector3 spawnOrigin = muzzle ? muzzle.position : FindBarrelTip(baseDirection);
        Vector3 spawnPosition = spawnOrigin + barrel.forward * muzzleForwardOffset;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        bool isBuckshot = GameSelection.SelectedTurretType == "BUCKSHOT";
        int pelletsToFire = isBuckshot ? buckshotPellets : 1;

        for (int i = 0; i < pelletsToFire; i++)
        {
            Vector3 finalDirection = baseDirection;

            if (isBuckshot)
            {
                float spreadX = Random.Range(-buckshotSpread, buckshotSpread);
                float spreadY = Random.Range(-buckshotSpread, buckshotSpread);

                Quaternion spreadRotation = Quaternion.Euler(spreadX, spreadY, 0);
                finalDirection = Quaternion.LookRotation(baseDirection) * spreadRotation * Vector3.forward;
            }

            Vector3 targetPoint = spawnPosition + (finalDirection * hitscanMaxRange);
            bool didHit = Physics.Raycast(spawnPosition, finalDirection, out RaycastHit hit, hitscanMaxRange, aimMask);

            if (didHit)
            {
                targetPoint = hit.point;
                float finalDamage = baseDamage * damageMultiplier;

                Debug.Log($"Pellet {i} registered hit on: {hit.collider.name} at {hit.point}. Damage: {finalDamage}");
            }

            if (tracerPrefab)
            {
                GameObject tracerObj = Instantiate(tracerPrefab, spawnPosition, Quaternion.LookRotation(finalDirection));
                tracer tracerComponent = tracerObj.GetComponent<tracer>();

                if (tracerComponent != null)
                {
                    Color activeLaserColor = gameManager.Instance != null
                        ? gameManager.Instance.CurrentTracerColor
                        : Color.white;
                    tracerComponent.InitializeHitscanLine(spawnPosition, targetPoint, activeLaserColor);
                }
            }
        }
    }

    void AimAtCursor()
    {
        if (!yawPivot || !pitchPivot || !barrel || Camera.main == null || Mouse.current == null)
            return;

        Ray cursorRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Vector3 aimPoint = FindAimPoint(cursorRay);
        Vector3 up = transform.up;
        Vector3 desiredDirection = aimPoint - pitchPivot.position;
        if (desiredDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 clampedAimDirection = ClampedDirection(
            desiredDirection.normalized,
            maxTurretYaw,
            maxTurretPitchUp,
            maxTurretPitchDown);

        Vector3 flatDirection = Vector3.ProjectOnPlane(clampedAimDirection, up);
        Vector3 flatBarrelDirection = Vector3.ProjectOnPlane(FiringDirection(), up);
        if (flatDirection.sqrMagnitude > 0.0001f && flatBarrelDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion yawCorrection = Quaternion.FromToRotation(flatBarrelDirection, flatDirection);
            Quaternion targetYaw = yawCorrection * yawPivot.rotation;
            yawPivot.rotation = Quaternion.RotateTowards(yawPivot.rotation, targetYaw, yawSpeed * Time.deltaTime);
        }

        float pitchCorrection = Vector3.SignedAngle(
            FiringDirection(),
            clampedAimDirection,
            pitchPivot.right);
        float pitchStep = Mathf.Clamp(pitchCorrection, -pitchSpeed * Time.deltaTime, pitchSpeed * Time.deltaTime);
        pitchPivot.Rotate(pitchPivot.right, pitchStep, Space.World);
    }

    Vector3 FindAimPoint(Ray cursorRay)
    {
        int hitCount = Physics.RaycastNonAlloc(
            cursorRay,
            aimHits,
            maxAimDistance,
            aimMask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        Vector3 aimPoint = cursorRay.GetPoint(maxAimDistance);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = aimHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform.root))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                aimPoint = hit.point;
            }
        }

        return aimPoint;
    }

    void ConfigureCamera()
    {
        if (!virtualCamera)
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (!virtualCamera || !cameraAimTarget) return;

        virtualCamera.Target.TrackingTarget = transform;
        virtualCamera.Target.LookAtTarget = cameraAimTarget;
        virtualCamera.Target.CustomLookAtTarget = true;

        CinemachineRotationComposer composer = virtualCamera.GetComponent<CinemachineRotationComposer>();
        if (composer)
        {
            composer.TargetOffset = Vector3.zero;
            composer.Damping = cameraComposerDamping;

            ScreenComposerSettings composition = composer.Composition;
            composition.DeadZone.Enabled = true;
            composition.DeadZone.Size = cameraDeadZone;
            composition.HardLimits.Enabled = true;
            composition.HardLimits.Size = cameraHardLimits;
            composer.Composition = composition;
        }

        CinemachineFollow follow = virtualCamera.GetComponent<CinemachineFollow>();
        if (follow)
        {
            follow.TrackerSettings.PositionDamping = cameraPositionDamping;
            follow.TrackerSettings.RotationDamping = cameraRotationDamping;
        }
    }

    void EnsureCameraAimTarget()
    {
        if (cameraAimTarget || !barrel) return;

        GameObject targetObject = new GameObject("Turret Camera Aim Target");
        cameraAimTarget = targetObject.transform;
    }

    void UpdateCameraAimTarget()
    {
        if (!cameraAimTarget || !barrel) return;

        cameraAimTarget.position = Vector3.SmoothDamp(
            cameraAimTarget.position,
            DesiredCameraAimPosition(),
            ref cameraAimVelocity,
            cameraAimSmoothTime);
        cameraAimTarget.rotation = Quaternion.LookRotation(FiringDirection(), transform.up);
    }

    void SnapCameraAimTarget()
    {
        if (!cameraAimTarget || !barrel) return;

        cameraAimTarget.position = DesiredCameraAimPosition();
        cameraAimTarget.rotation = Quaternion.LookRotation(FiringDirection(), transform.up);
    }

    Vector3 DesiredCameraAimPosition()
    {
        Vector3 aimOrigin = pitchPivot ? pitchPivot.position : transform.position;
        return aimOrigin + transform.up * cameraAimHeight + ClampedCameraDirection() * cameraAimDistance;
    }

    Vector3 ClampedCameraDirection()
    {
        return ClampedDirection(FiringDirection(), maxCameraYaw, maxCameraPitchUp, maxCameraPitchDown);
    }

    Vector3 ClampedDirection(Vector3 desiredDirection, float maxYaw, float maxPitchUp, float maxPitchDown)
    {
        Vector3 up = transform.up;
        Vector3 neutralDirection = transform.TransformDirection(neutralBarrelForwardLocal).normalized;
        Vector3 barrelDirection = desiredDirection.normalized;
        Vector3 neutralFlat = Vector3.ProjectOnPlane(neutralDirection, up).normalized;
        Vector3 barrelFlat = Vector3.ProjectOnPlane(barrelDirection, up).normalized;

        if (neutralFlat.sqrMagnitude < 0.0001f || barrelFlat.sqrMagnitude < 0.0001f)
            return neutralDirection;

        float yaw = Mathf.Clamp(Vector3.SignedAngle(neutralFlat, barrelFlat, up), -maxYaw, maxYaw);
        float neutralPitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(neutralDirection, up), -1f, 1f)) * Mathf.Rad2Deg;
        float barrelPitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(barrelDirection, up), -1f, 1f)) * Mathf.Rad2Deg;
        float pitch = neutralPitch + Mathf.Clamp(
            barrelPitch - neutralPitch,
            -maxPitchDown,
            maxPitchUp);

        Vector3 yawDirection = Quaternion.AngleAxis(yaw, up) * neutralFlat;
        float pitchRadians = pitch * Mathf.Deg2Rad;
        return (yawDirection * Mathf.Cos(pitchRadians) + up * Mathf.Sin(pitchRadians)).normalized;
    }

    void RememberNeutralBarrelDirection()
    {
        neutralBarrelForwardLocal = barrel
            ? transform.InverseTransformDirection(FiringDirection()).normalized
            : Vector3.forward;
    }

    Vector3 FiringDirection()
    {
        if (!barrel) return transform.forward;

        Vector3 axis = localFiringAxis.sqrMagnitude > 0.0001f ? localFiringAxis.normalized : Vector3.down;
        return barrel.TransformDirection(axis).normalized;
    }

    void AutoFindReferences()
    {
        if (!yawPivot) yawPivot = FindChild("Yaw");
        if (!pitchPivot) pitchPivot = FindChild("Pitch");
        if (!barrel) barrel = FindChild("Barrel");
        if (!muzzle) muzzle = FindChild("Muzzle");
    }

    void EnsureMuzzleReference()
    {
        if (!barrel || (muzzle && muzzle != barrel))
            return;

        Transform existingMuzzle = FindChild("Muzzle");
        if (existingMuzzle && existingMuzzle != barrel)
        {
            muzzle = existingMuzzle;
            return;
        }

        Vector3 direction = FiringDirection();
        GameObject muzzleObject = new GameObject("Muzzle");
        muzzle = muzzleObject.transform;
        muzzle.SetParent(barrel, true);
        muzzle.position = FindBarrelTip(direction);
        muzzle.rotation = Quaternion.LookRotation(direction, transform.up);
    }

    Vector3 FindBarrelTip(Vector3 direction)
    {
        if (!barrel)
            return transform.position;

        Renderer barrelRenderer = barrel.GetComponentInChildren<Renderer>();
        if (!barrelRenderer)
            return barrel.position;

        Bounds bounds = barrelRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float furthestProjection = float.NegativeInfinity;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    furthestProjection = Mathf.Max(
                        furthestProjection,
                        Vector3.Dot(corner - barrel.position, direction));
                }
            }
        }

        return barrel.position + direction * Mathf.Max(0f, furthestProjection);
    }

    Transform FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    void OnDestroy()
    {
        if (cameraAimTarget)
            Destroy(cameraAimTarget.gameObject);
    }
}
