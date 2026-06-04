using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public class SpiderAgent : Agent
{
    [Header("Mode")]
    public bool trainingMode = true;
    public bool autoFindCourierTarget = true;
    public string courierTag = "Player";
    public bool useTargetPrediction = true;
    public float targetPredictionTime = 0.35f;
    public float targetVelocityObservationScale = 30f;

    [Header("Body")]
    public Rigidbody body;

    [Header("Leg Joints")]
    public ConfigurableJoint[] joints;
    public float jointTargetAngle = 35f;
    public float jointSpring = 3000f;
    public float jointDamper = 250f;
    public float jointMaxForce = 40000f;
    public float hipAngularXLimit = 35f;
    public float hipAngularYZLimit = 20f;
    public float kneeAngularXLimit = 60f;
    public float kneeAngularYZLimit = 10f;
    public float footAngularXLimit = 45f;
    public float footAngularYZLimit = 10f;
    public float passiveFootSpring = 1000f;
    public float passiveFootDamper = 150f;
    public float passiveFootMaxForce = 10000f;
    public float jointProjectionDistance = 0.025f;
    public float jointProjectionAngle = 2f;
    public int limbSolverIterations = 20;
    public int limbSolverVelocityIterations = 10;
    public bool anchorJointsAtPivot = true;

    [Header("Traction")]
    public bool applyRuntimeGrip = true;
    public float limbStaticFriction = 1.4f;
    public float limbDynamicFriction = 1.2f;

    [Header("Jump")]
    public bool useJumpAction = false;
    public float jumpImpulse = 18f;
    [Range(0f, 1f)] public float jumpThreshold = 0.65f;
    public int jumpCooldownSteps = 60;
    public float jumpPenalty = 0.05f;
    public float jumpIntentPenalty = 0f;
    public float airborneJumpPenalty = 0f;
    public float jumpReward = 0f;
    public float targetAlignedJumpReward = 0.08f;
    public float goodLandingProgressRewardScale = 3f;

    [Header("Goal")]
    public Transform target;
    public int maxEpisodeSteps = 12000;
    public float targetReachDistance = 2.5f;
    public float targetMinDistance = 10f;
    public float targetMaxDistance = 16f;
    public float targetHeight = 0.5f;
    public bool keepTargetUntilReached = false;
    public float targetReachReward = 2f;
    public float targetFastReachBonus = 3f;

    [Header("Courier Proxy")]
    public bool moveTrainingTarget = true;
    public float trainingTargetMinSpeed = 1.5f;
    public float trainingTargetMaxSpeed = 4f;
    public float trainingTargetAcceleration = 4f;
    public float trainingTargetTurnInterval = 2.5f;
    [Range(0f, 1f)] public float trainingTargetEvasionBias = 0.15f;
    public float trainingTargetBoundsRadius = 25f;
    [Range(0f, 1f)] public float movingTargetCurriculumStart = 0f;

    [Header("Training Curriculum")]
    public bool useTrainingCurriculum = true;
    public int curriculumRampSteps = 500000;
    [Range(0.1f, 1f)] public float curriculumStartDistanceScale = 0.25f;
    [Range(0.1f, 1f)] public float curriculumStartSpeedScale = 0.2f;
    [Range(0f, 1f)] public float policyActionStartScale = 0.2f;

    [Header("Penalties")]
    public float velocityPenalty = 0f;
    public float groundContactPenalty = 0f;
    public float upsideDownPenalty = 0.03f;
    public float upsideDownEndPenalty = 1f;
    public float upsideDownDotThreshold = -0.25f;
    public int upsideDownEndSteps = 45;
    public float lowBodyHeightPenalty = 0f;
    public float allowedBodyDrop = 0.25f;
    public int allowedGroundedLimbColliders = 4;
    public float legDragPenalty = 0f;
    public float verticalVelocityPenalty = 0f;
    public float airbornePenalty = 0f;
    public float timePenalty = 0.001f;
    public float jointActionPenalty = 0.0005f;
    public float jointActionChangePenalty = 0f;
    public float erraticVelocityChangePenalty = 0.0002f;
    public float erraticAngularVelocityPenalty = 0.0005f;
    public float erraticAngularChangePenalty = 0.0002f;

    [Header("Rewards")]
    public float forwardRewardScale = 0f;
    public float targetDirectionRewardScale = 0.05f;
    public float targetProgressRewardScale = 1.5f;
    public float movementRewardScale = 0f;
    public float targetFacingRewardScale = 0.08f;
    public float targetFacingPenaltyScale = 0.06f;
    [Range(-1f, 1f)] public float targetFacingRewardDot = 0.75f;
    public float targetProximityRewardScale = 0f;
    public float targetProximityRewardDistance = 20f;
    public float uprightRewardScale = 0.01f;
    public int groundedFeetRewardThreshold = 2;
    public float groundedFeetReward = 0.005f;

    [Header("Reset Randomization")]
    public float startPositionRadius = 0.5f;
    public float startYawRange = 20f;

    [Header("Debug")]
    public bool useOscillatingHeuristic = false;
    public float heuristicFrequency = 2f;

    [Header("Training Assist")]
    public bool useTrainingGaitBias = true;
    [Range(0f, 1f)] public float trainingGaitBias = 0.75f;
    public bool useTargetAwareGaitBias = true;
    [Range(0f, 1f)] public float gaitTurnScale = 0.45f;
    [Range(0f, 1f)] public float gaitTurnPoseScale = 0.3f;
    private bool bodyTouchingGround = false;
    private int stepsSinceEpisodeStart = 0;
    private readonly HashSet<Collider> upwardContactColliders = new HashSet<Collider>();
    private readonly HashSet<Collider> limbGroundColliders = new HashSet<Collider>();

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetStartPosition;
    private Vector3 episodeStartPosition;
    private Vector3[] startJointLocalPositions;
    private Quaternion[] startJointLocalRotations;
    private int[] jointLegIndices;
    private int[] jointSegmentIndices;
    private float[] jointSideSigns;
    private int[] controlledJointIndices;
    private PhysicsMaterial runtimeGripMaterial;
    private float previousDistanceToTarget;
    private float lastMeanActionMagnitude;
    private float lastMeanJointActionMagnitude;
    private float lastMeanJointActionDelta;
    private float lastJumpInput;
    private float[] previousJointActions;
    private Vector3 previousBodyVelocity;
    private Vector3 previousBodyAngularVelocity;
    private float lastVelocityChange;
    private float lastAngularVelocity;
    private float lastAngularVelocityChange;
    private float lastFacingTarget;
    private float lastDistanceImprovement;
    private bool waitingForJumpLanding;
    private bool jumpBecameAirborne;
    private float jumpStartDistanceToTarget;
    private int lastEpisodeEndReason;
    private Rigidbody targetBody;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private Vector3 trainingTargetVelocity;
    private Vector3 trainingTargetDirection;
    private float trainingTargetSpeed;
    private float trainingTargetTurnTimer;
    private int jumpCooldown;
    private int upsideDownSteps;
    private bool rerollTargetNextEpisode = true;
    private bool targetReachedThisEpisode;
    private bool episodeStatsRecorded;

    public override void Initialize()
    {
        if (!body)
            body = GetComponent<Rigidbody>();

        if (joints == null || joints.Length == 0)
            joints = GetComponentsInChildren<ConfigurableJoint>();

        if (!body)
        {
            Debug.LogError("SpiderAgent needs a body Rigidbody assigned.", this);
            enabled = false;
            return;
        }

        if (maxEpisodeSteps > 0)
            MaxStep = maxEpisodeSteps;

        startPosition = body.transform.position;
        startRotation = body.transform.rotation;
        if (target)
        {
            targetStartPosition = target.position;
            SetChaseTarget(target);
        }
        
        startJointLocalPositions = new Vector3[joints.Length];
        startJointLocalRotations = new Quaternion[joints.Length];
        jointLegIndices = new int[joints.Length];
        jointSegmentIndices = new int[joints.Length];
        jointSideSigns = new float[joints.Length];
        List<int> controlledJoints = new List<int>();
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null)
                continue;

            startJointLocalPositions[i] = joints[i].transform.localPosition;
            startJointLocalRotations[i] = joints[i].transform.localRotation;
            CacheJointGaitMetadata(i, joints[i]);

            if (IsControlledJoint(i))
                controlledJoints.Add(i);
        }

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null)
                continue;

            ConfigureJointPhysics(joints[i], jointSegmentIndices[i], FindParentBodyForJoint(i));
        }

        controlledJointIndices = controlledJoints.ToArray();
        previousJointActions = new float[controlledJointIndices.Length];

        ConfigureLimbGrip();
        ConfigureLimbContactSensors();
    }

    private void FixedUpdate()
    {
        if (!body)
            return;

        if (trainingMode)
        {
            MoveTrainingTarget();
        }
        else
        {
            FindCourierTargetIfNeeded();
        }

        UpdateTargetVelocity();
    }

    public override void OnEpisodeBegin()
    {
        if (stepsSinceEpisodeStart > 0 && !episodeStatsRecorded)
            RecordEpisodeStats();

        if (!trainingMode)
        {
            stepsSinceEpisodeStart = 0;
            episodeStatsRecorded = false;
            FindCourierTargetIfNeeded();
            previousDistanceToTarget = CurrentDistanceToTarget();
            return;
        }

        stepsSinceEpisodeStart = 0;
        jumpCooldown = 0;
        upsideDownSteps = 0;
        lastJumpInput = 0f;
        lastMeanJointActionMagnitude = 0f;
        lastMeanJointActionDelta = 0f;
        lastVelocityChange = 0f;
        lastAngularVelocity = 0f;
        lastAngularVelocityChange = 0f;
        lastFacingTarget = 0f;
        lastDistanceImprovement = 0f;
        waitingForJumpLanding = false;
        jumpBecameAirborne = false;
        jumpStartDistanceToTarget = 0f;
        lastEpisodeEndReason = 0;
        bodyTouchingGround = false;
        upwardContactColliders.Clear();
        limbGroundColliders.Clear();
        targetReachedThisEpisode = false;
        episodeStatsRecorded = false;
        
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        previousBodyVelocity = Vector3.zero;
        previousBodyAngularVelocity = Vector3.zero;

        body.transform.SetPositionAndRotation(RandomizedStartPosition(), RandomizedStartRotation());
        body.WakeUp();

        if (target && (!keepTargetUntilReached || rerollTargetNextEpisode))
        {
            target.position = RandomizedTargetPosition(body.transform.position);
            rerollTargetNextEpisode = false;
            ResetTrainingTargetMotion();
        }

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null)
                continue;

            Rigidbody rb = joints[i].GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                joints[i].transform.localPosition = startJointLocalPositions[i];
                joints[i].transform.localRotation = startJointLocalRotations[i];
                rb.WakeUp();
            }
        }

        for (int i = 0; i < previousJointActions.Length; i++)
            previousJointActions[i] = 0f;

        episodeStartPosition = body.transform.position;
        previousDistanceToTarget = CurrentDistanceToTarget();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(body.transform.forward);
        sensor.AddObservation(body.transform.up);

        sensor.AddObservation(body.linearVelocity);
        sensor.AddObservation(body.angularVelocity);

        // Add target information
        if (target != null)
        {
            Vector3 directionToTarget = HorizontalDirectionToTarget();
            sensor.AddObservation(directionToTarget);
            sensor.AddObservation(body.transform.InverseTransformDirection(directionToTarget));
            
            float distanceToTarget = CurrentDistanceToTarget();
            sensor.AddObservation(distanceToTarget);
            sensor.AddObservation(Mathf.Clamp01(distanceToTarget / Mathf.Max(targetMaxDistance, 0.01f)));
            sensor.AddObservation(body.transform.InverseTransformDirection(HorizontalTargetVelocity() / Mathf.Max(targetVelocityObservationScale, 0.01f)));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector3.zero);
        }

        // Add body ground contact state
        sensor.AddObservation(bodyTouchingGround ? 1f : 0f);
        sensor.AddObservation(GetUprightDot());
        sensor.AddObservation(Mathf.Clamp01(limbGroundColliders.Count / Mathf.Max(joints.Length, 1f)));
        sensor.AddObservation(jumpCooldownSteps > 0 ? jumpCooldown / (float)jumpCooldownSteps : 0f);

        foreach (ConfigurableJoint joint in joints)
        {
            // Use quaternion representation instead of euler angles to avoid discontinuities
            Quaternion localRot = joint.transform.localRotation;
            sensor.AddObservation(localRot.x);
            sensor.AddObservation(localRot.y);
            sensor.AddObservation(localRot.z);
            sensor.AddObservation(localRot.w);

            Rigidbody rb = joint.GetComponent<Rigidbody>();

            if (rb != null)
            {
                sensor.AddObservation(rb.linearVelocity);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepsSinceEpisodeStart++;
        jumpCooldown = Mathf.Max(0, jumpCooldown - 1);
        var continuousActions = actions.ContinuousActions;
        float actionMagnitudeSum = 0f;
        int appliedActions = 0;
        float jointActionMagnitudeSum = 0f;
        float jointActionDeltaSum = 0f;
        int jointActionCount = 0;
        float policyActionScale = TrainingPolicyActionScale();
        float gaitBias = TrainingGaitBiasScale();

        for (int actionIndex = 0; actionIndex < controlledJointIndices.Length; actionIndex++)
        {
            if (actionIndex >= continuousActions.Length)
                break;

            int jointIndex = controlledJointIndices[actionIndex];
            if (jointIndex >= joints.Length || joints[jointIndex] == null)
                break;

            joints[jointIndex].GetComponent<Rigidbody>()?.WakeUp();

            float rawAction = continuousActions[actionIndex];
            jointActionMagnitudeSum += Mathf.Abs(rawAction);
            jointActionCount++;

            if (previousJointActions != null && actionIndex < previousJointActions.Length)
            {
                jointActionDeltaSum += Mathf.Abs(rawAction - previousJointActions[actionIndex]);
                previousJointActions[actionIndex] = rawAction;
            }

            float action = rawAction * policyActionScale;
            if (useTrainingGaitBias)
                action = Mathf.Clamp(action + GaitAction(jointIndex) * gaitBias, -1f, 1f);

            actionMagnitudeSum += Mathf.Abs(action);
            appliedActions++;

            float targetAngle = action * JointTargetAngleForSegment(jointSegmentIndices[jointIndex]);

            joints[jointIndex].targetRotation =
                Quaternion.Euler(targetAngle, 0f, 0f);
        }

        if (useJumpAction)
            ApplyJumpAction(continuousActions, ref actionMagnitudeSum, ref appliedActions);

        lastMeanActionMagnitude = appliedActions > 0 ? actionMagnitudeSum / appliedActions : 0f;
        lastMeanJointActionMagnitude = jointActionCount > 0 ? jointActionMagnitudeSum / jointActionCount : 0f;
        lastMeanJointActionDelta = jointActionCount > 0 ? jointActionDeltaSum / jointActionCount : 0f;

        if (!trainingMode)
        {
            FindCourierTargetIfNeeded();
            return;
        }

        // Penalty for not moving (but not on frame 0 to avoid initial punishment)
        if (stepsSinceEpisodeStart > 1)
        {
            float currentVelocity = body.linearVelocity.magnitude;
            if (currentVelocity < 0.5f)
            {
                AddReward(-velocityPenalty);
            }
        }

        if (ApplyPosturePenalties())
            return;

        ApplyEfficiencyPenalties();
        ApplyErraticMovementPenalty();
        ApplyHopPenalties();
        ApplyGroundedFeetReward();
        ApplyJumpLandingReward();

        // Reward for controlled movement, with target progress doing most of the teaching.
        float forwardSpeed = Vector3.Dot(body.linearVelocity, body.transform.forward);
        AddReward(Mathf.Max(forwardSpeed, 0f) * forwardRewardScale);
        AddReward(body.linearVelocity.magnitude * movementRewardScale);

        if (ApplyTargetRewards())
            return;

        if (body.transform.position.y < 0.2f)
        {
            EndFailedEpisode(-1f, 2);
        }
    }

    private float CurrentDistanceToTarget()
    {
        return target != null
            ? HorizontalOffsetToTarget(false).magnitude
            : 0f;
    }

    private Vector3 HorizontalOffsetToTarget(bool predicted)
    {
        if (target == null)
            return Vector3.zero;

        Vector3 targetPosition = predicted ? PredictedTargetPosition() : target.position;
        Vector3 offset = targetPosition - body.transform.position;
        offset.y = 0f;
        return offset;
    }

    private Vector3 HorizontalDirectionToTarget()
    {
        Vector3 offset = HorizontalOffsetToTarget(useTargetPrediction);
        return offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.zero;
    }

    private Vector3 PredictedTargetPosition()
    {
        if (target == null || !useTargetPrediction)
            return target ? target.position : Vector3.zero;

        return target.position + targetVelocity * targetPredictionTime;
    }

    private Vector3 HorizontalTargetVelocity()
    {
        Vector3 velocity = targetVelocity;
        velocity.y = 0f;
        return velocity;
    }

    private void ApplyJumpAction(ActionSegment<float> continuousActions, ref float actionMagnitudeSum, ref int appliedActions)
    {
        int jumpActionIndex = controlledJointIndices != null ? controlledJointIndices.Length : 6;
        if (jumpActionIndex >= continuousActions.Length)
            return;

        float jumpInput = Mathf.Clamp01((continuousActions[jumpActionIndex] + 1f) * 0.5f);
        lastJumpInput = jumpInput;
        actionMagnitudeSum += jumpInput;
        appliedActions++;

        if (jumpInput > 0.1f)
            AddReward(-jumpIntentPenalty * jumpInput);

        if (jumpCooldown > 0 || jumpInput < jumpThreshold)
            return;

        if (limbGroundColliders.Count == 0)
        {
            AddReward(-airborneJumpPenalty * jumpInput);
            return;
        }

        body.WakeUp();
        body.AddForce(Vector3.up * (jumpImpulse * jumpInput), ForceMode.Impulse);
        AddReward(jumpReward * jumpInput);

        Vector3 directionToTarget = HorizontalDirectionToTarget();
        float targetAlignment = Mathf.Max(Vector3.Dot(body.transform.forward, directionToTarget), 0f);
        AddReward(targetAlignedJumpReward * targetAlignment * jumpInput);

        if (jumpPenalty > 0f)
            AddReward(-jumpPenalty * jumpInput);

        waitingForJumpLanding = true;
        jumpBecameAirborne = false;
        jumpStartDistanceToTarget = CurrentDistanceToTarget();
        jumpCooldown = jumpCooldownSteps;
    }

    private Vector3 RandomizedStartPosition()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * startPositionRadius;
        return startPosition + new Vector3(offset.x, 0f, offset.y);
    }

    private Quaternion RandomizedStartRotation()
    {
        float yaw = UnityEngine.Random.Range(-startYawRange, startYawRange);
        return Quaternion.AngleAxis(yaw, Vector3.up) * startRotation;
    }

    private Vector3 RandomizedTargetPosition(Vector3 origin)
    {
        float minDistance = Mathf.Max(0.1f, Mathf.Min(targetMinDistance, targetMaxDistance));
        float maxDistance = Mathf.Max(minDistance, targetMaxDistance);
        if (useTrainingCurriculum)
        {
            float curriculum = TrainingCurriculum01();
            float distanceScale = Mathf.Lerp(curriculumStartDistanceScale, 1f, curriculum);
            minDistance *= distanceScale;
            maxDistance *= distanceScale;
        }

        float distance = UnityEngine.Random.Range(minDistance, maxDistance);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        Vector3 targetPosition = origin + offset;
        targetPosition.y = targetHeight;
        return targetPosition;
    }

    private void MoveTrainingTarget()
    {
        if (!trainingMode || !moveTrainingTarget || target == null)
            return;

        if (TrainingCurriculum01() < movingTargetCurriculumStart)
            return;

        float dt = Time.fixedDeltaTime;
        trainingTargetTurnTimer -= dt;

        if (trainingTargetTurnTimer <= 0f || trainingTargetDirection.sqrMagnitude < 0.001f)
            PickTrainingTargetDirection();

        Vector3 fromStart = target.position - startPosition;
        fromStart.y = 0f;
        if (fromStart.magnitude > trainingTargetBoundsRadius)
        {
            trainingTargetDirection = (-fromStart).normalized;
            trainingTargetTurnTimer = trainingTargetTurnInterval;
        }

        Vector3 desiredVelocity = trainingTargetDirection * trainingTargetSpeed;
        trainingTargetVelocity = Vector3.MoveTowards(
            trainingTargetVelocity,
            desiredVelocity,
            trainingTargetAcceleration * dt);

        Vector3 nextPosition = target.position + trainingTargetVelocity * dt;
        nextPosition.y = targetHeight;
        target.position = nextPosition;
    }

    private void ResetTrainingTargetMotion()
    {
        trainingTargetVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;
        lastTargetPosition = target ? target.position : Vector3.zero;
        PickTrainingTargetDirection();
    }

    private void PickTrainingTargetDirection()
    {
        Vector3 awayFromSpider = target != null ? target.position - body.transform.position : Vector3.forward;
        awayFromSpider.y = 0f;
        if (awayFromSpider.sqrMagnitude < 0.001f)
            awayFromSpider = Random.insideUnitSphere;

        awayFromSpider.y = 0f;
        awayFromSpider.Normalize();

        float randomAngle = Random.Range(-130f, 130f);
        Vector3 randomDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * awayFromSpider;
        trainingTargetDirection = Vector3.Slerp(randomDirection, awayFromSpider, trainingTargetEvasionBias).normalized;
        float speedScale = useTrainingCurriculum
            ? Mathf.Lerp(curriculumStartSpeedScale, 1f, TrainingCurriculum01())
            : 1f;
        trainingTargetSpeed = Random.Range(trainingTargetMinSpeed, trainingTargetMaxSpeed) * speedScale;
        trainingTargetTurnTimer = Random.Range(trainingTargetTurnInterval * 0.6f, trainingTargetTurnInterval * 1.4f);
    }

    private float TrainingCurriculum01()
    {
        if (!trainingMode || curriculumRampSteps <= 0)
            return 1f;

        return Mathf.Clamp01(Academy.Instance.StepCount / (float)curriculumRampSteps);
    }

    private float TrainingPolicyActionScale()
    {
        if (!trainingMode || !useTrainingCurriculum)
            return 1f;

        return Mathf.Lerp(policyActionStartScale, 1f, TrainingCurriculum01());
    }

    private float TrainingGaitBiasScale()
    {
        if (!trainingMode || !useTrainingCurriculum)
            return trainingGaitBias;

        return Mathf.Lerp(trainingGaitBias, trainingGaitBias * 0.35f, TrainingCurriculum01());
    }

    public void SetChaseTarget(Transform chaseTarget)
    {
        target = chaseTarget;
        targetBody = target ? target.GetComponentInParent<Rigidbody>() : null;
        lastTargetPosition = target ? target.position : Vector3.zero;
        targetVelocity = Vector3.zero;
    }

    private void FindCourierTargetIfNeeded()
    {
        if (!autoFindCourierTarget)
            return;

        if (target != null && target.gameObject.activeInHierarchy)
            return;

        GameObject courier = GameObject.FindGameObjectWithTag(courierTag);
        if (courier != null)
            SetChaseTarget(courier.transform);
    }

    private void UpdateTargetVelocity()
    {
        if (target == null)
        {
            targetVelocity = Vector3.zero;
            return;
        }

        if (targetBody != null)
        {
            targetVelocity = targetBody.linearVelocity;
            lastTargetPosition = target.position;
            return;
        }

        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        targetVelocity = (target.position - lastTargetPosition) / dt;
        lastTargetPosition = target.position;
    }

    private bool ApplyPosturePenalties()
    {
        float upDot = GetUprightDot();

        if (upDot >= 0f)
            AddReward(upDot * uprightRewardScale);
        else
            AddReward(upDot * upsideDownPenalty);

        if (upDot < upsideDownDotThreshold)
            upsideDownSteps++;
        else
            upsideDownSteps = 0;

        if (upsideDownSteps >= upsideDownEndSteps)
        {
            EndFailedEpisode(-upsideDownEndPenalty, 1);
            return true;
        }

        if (bodyTouchingGround)
            AddReward(-groundContactPenalty);

        float bodyDrop = Mathf.Max(0f, (episodeStartPosition.y - body.transform.position.y) - allowedBodyDrop);
        if (bodyDrop > 0f)
            AddReward(-bodyDrop * lowBodyHeightPenalty);

        int excessGroundedLimbs = Mathf.Max(0, limbGroundColliders.Count - allowedGroundedLimbColliders);
        if (excessGroundedLimbs > 0)
            AddReward(-excessGroundedLimbs * legDragPenalty);

        return false;
    }

    private void ApplyHopPenalties()
    {
        AddReward(-Mathf.Abs(body.linearVelocity.y) * verticalVelocityPenalty);

        if (stepsSinceEpisodeStart > 10 && limbGroundColliders.Count == 0)
            AddReward(-airbornePenalty);
    }

    private void ApplyGroundedFeetReward()
    {
        if (limbGroundColliders.Count >= groundedFeetRewardThreshold)
            AddReward(groundedFeetReward);
    }

    private void ApplyJumpLandingReward()
    {
        if (!waitingForJumpLanding)
            return;

        if (limbGroundColliders.Count == 0)
        {
            jumpBecameAirborne = true;
            return;
        }

        if (!jumpBecameAirborne)
            return;

        float jumpProgress = jumpStartDistanceToTarget - CurrentDistanceToTarget();
        AddReward(Mathf.Max(0f, jumpProgress) * goodLandingProgressRewardScale);
        waitingForJumpLanding = false;
        jumpBecameAirborne = false;
    }

    private void ApplyEfficiencyPenalties()
    {
        AddReward(-timePenalty);
        AddReward(-lastMeanJointActionMagnitude * jointActionPenalty);
        AddReward(-lastMeanJointActionDelta * jointActionChangePenalty);
    }

    private void ApplyErraticMovementPenalty()
    {
        lastVelocityChange = (body.linearVelocity - previousBodyVelocity).magnitude;
        lastAngularVelocity = body.angularVelocity.magnitude;
        lastAngularVelocityChange = (body.angularVelocity - previousBodyAngularVelocity).magnitude;

        AddReward(-lastVelocityChange * erraticVelocityChangePenalty);
        AddReward(-lastAngularVelocity * erraticAngularVelocityPenalty);
        AddReward(-lastAngularVelocityChange * erraticAngularChangePenalty);

        previousBodyVelocity = body.linearVelocity;
        previousBodyAngularVelocity = body.angularVelocity;
    }

    private bool ApplyTargetRewards()
    {
        if (target == null)
            return false;

        float distanceToTarget = CurrentDistanceToTarget();
        float distanceImprovement = previousDistanceToTarget - distanceToTarget;
        lastDistanceImprovement = distanceImprovement;
        AddReward(distanceImprovement * targetProgressRewardScale);
        previousDistanceToTarget = distanceToTarget;

        Vector3 directionToTarget = HorizontalDirectionToTarget();
        Vector3 horizontalVelocity = body.linearVelocity;
        horizontalVelocity.y = 0f;

        float facingTarget = Vector3.Dot(body.transform.forward, directionToTarget);
        lastFacingTarget = facingTarget;
        if (facingTarget >= targetFacingRewardDot)
        {
            float facingReward = Mathf.InverseLerp(targetFacingRewardDot, 1f, facingTarget);
            AddReward(facingReward * targetFacingRewardScale);
        }
        else
        {
            float facingPenalty = targetFacingRewardDot - facingTarget;
            AddReward(-facingPenalty * targetFacingPenaltyScale);
        }

        if (horizontalVelocity.magnitude > 0.1f)
        {
            float speedTowardTarget = Vector3.Dot(horizontalVelocity, directionToTarget);
            AddReward(Mathf.Max(speedTowardTarget, 0f) * targetDirectionRewardScale);
        }

        if (distanceToTarget >= targetReachDistance)
        {
            float proximity = Mathf.InverseLerp(targetProximityRewardDistance, targetReachDistance, distanceToTarget);
            AddReward(Mathf.Max(proximity, 0f) * targetProximityRewardScale);
            return false;
        }

        targetReachedThisEpisode = true;
        rerollTargetNextEpisode = true;
        lastEpisodeEndReason = 3;
        float timeRemaining = maxEpisodeSteps > 0
            ? Mathf.Clamp01(1f - stepsSinceEpisodeStart / (float)maxEpisodeSteps)
            : 0f;

        AddReward(targetReachReward + targetFastReachBonus * timeRemaining);
        RecordEpisodeStats();
        EndEpisode();
        return true;
    }

    private void EndFailedEpisode(float penalty, int reason)
    {
        AddReward(penalty);
        lastEpisodeEndReason = reason;
        rerollTargetNextEpisode = false;
        RecordEpisodeStats();
        EndEpisode();
    }

    private float GetUprightDot()
    {
        return Vector3.Dot(body.transform.up, Vector3.up);
    }

    private void RecordEpisodeStats()
    {
        if (episodeStatsRecorded)
            return;

        float distanceFromStart = Vector3.Distance(episodeStartPosition, body.transform.position);
        Academy.Instance.StatsRecorder.Add("Spider/DistanceFromStart", distanceFromStart);
        Academy.Instance.StatsRecorder.Add("Spider/DistanceToTarget", CurrentDistanceToTarget());
        Academy.Instance.StatsRecorder.Add("Spider/MeanActionMagnitude", lastMeanActionMagnitude);
        Academy.Instance.StatsRecorder.Add("Spider/MeanJointActionMagnitude", lastMeanJointActionMagnitude);
        Academy.Instance.StatsRecorder.Add("Spider/MeanJointActionDelta", lastMeanJointActionDelta);
        Academy.Instance.StatsRecorder.Add("Spider/VelocityChange", lastVelocityChange);
        Academy.Instance.StatsRecorder.Add("Spider/AngularVelocity", lastAngularVelocity);
        Academy.Instance.StatsRecorder.Add("Spider/AngularVelocityChange", lastAngularVelocityChange);
        Academy.Instance.StatsRecorder.Add("Spider/TargetReached", targetReachedThisEpisode ? 1f : 0f);
        Academy.Instance.StatsRecorder.Add("Spider/UprightDot", GetUprightDot());
        Academy.Instance.StatsRecorder.Add("Spider/LimbGroundContacts", limbGroundColliders.Count);
        Academy.Instance.StatsRecorder.Add("Spider/JumpInput", lastJumpInput);
        Academy.Instance.StatsRecorder.Add("Spider/TargetSpeed", HorizontalTargetVelocity().magnitude);
        Academy.Instance.StatsRecorder.Add("Spider/FacingTarget", lastFacingTarget);
        Academy.Instance.StatsRecorder.Add("Spider/DistanceImprovement", lastDistanceImprovement);
        Academy.Instance.StatsRecorder.Add("Spider/Curriculum", TrainingCurriculum01());
        Academy.Instance.StatsRecorder.Add("Spider/PolicyActionScale", TrainingPolicyActionScale());
        Academy.Instance.StatsRecorder.Add("Spider/GaitBiasScale", TrainingGaitBiasScale());
        Academy.Instance.StatsRecorder.Add("Spider/EpisodeEndReason", lastEpisodeEndReason);
        episodeStatsRecorded = true;
    }

    private void ConfigureJointPhysics(ConfigurableJoint joint, int segmentIndex, Rigidbody connectedBody)
    {
        Rigidbody limbBody = joint.GetComponent<Rigidbody>();
        if (limbBody != null)
        {
            limbBody.solverIterations = Mathf.Max(limbBody.solverIterations, limbSolverIterations);
            limbBody.solverVelocityIterations = Mathf.Max(limbBody.solverVelocityIterations, limbSolverVelocityIterations);
            limbBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        joint.connectedBody = connectedBody;
        if (connectedBody != null)
        {
            connectedBody.solverIterations = Mathf.Max(connectedBody.solverIterations, limbSolverIterations);
            connectedBody.solverVelocityIterations = Mathf.Max(connectedBody.solverVelocityIterations, limbSolverVelocityIterations);
            joint.autoConfigureConnectedAnchor = false;
            if (anchorJointsAtPivot)
                joint.anchor = Vector3.zero;

            Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);
            joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(worldAnchor);
        }
        else
        {
            Debug.LogWarning($"Spider joint '{joint.name}' has no connected Rigidbody.", joint);
        }

        joint.enableCollision = false;
        joint.enablePreprocessing = false;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = jointProjectionDistance;
        joint.projectionAngle = jointProjectionAngle;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        float xLimit = segmentIndex switch
        {
            0 => hipAngularXLimit,
            1 => kneeAngularXLimit,
            _ => footAngularXLimit
        };

        float yzLimit = segmentIndex switch
        {
            0 => hipAngularYZLimit,
            1 => kneeAngularYZLimit,
            _ => footAngularYZLimit
        };

        SoftJointLimit lowXLimit = joint.lowAngularXLimit;
        lowXLimit.limit = -xLimit;
        lowXLimit.bounciness = 0f;
        lowXLimit.contactDistance = 1f;
        joint.lowAngularXLimit = lowXLimit;

        SoftJointLimit highXLimit = joint.highAngularXLimit;
        highXLimit.limit = xLimit;
        highXLimit.bounciness = 0f;
        highXLimit.contactDistance = 1f;
        joint.highAngularXLimit = highXLimit;

        SoftJointLimit yLimit = joint.angularYLimit;
        yLimit.limit = yzLimit;
        yLimit.bounciness = 0f;
        yLimit.contactDistance = 1f;
        joint.angularYLimit = yLimit;

        SoftJointLimit zLimit = joint.angularZLimit;
        zLimit.limit = yzLimit;
        zLimit.bounciness = 0f;
        zLimit.contactDistance = 1f;
        joint.angularZLimit = zLimit;

        bool passiveFoot = segmentIndex >= 2;
        float spring = passiveFoot ? passiveFootSpring : jointSpring;
        float damper = passiveFoot ? passiveFootDamper : jointDamper;
        float maxForce = passiveFoot ? passiveFootMaxForce : jointMaxForce;

        JointDrive drive = joint.angularXDrive;

        drive.positionSpring = spring;
        drive.positionDamper = damper;
        drive.maximumForce = maxForce;

        joint.angularXDrive = drive;

        JointDrive yzDrive = joint.angularYZDrive;
        yzDrive.positionSpring = spring;
        yzDrive.positionDamper = damper;
        yzDrive.maximumForce = maxForce;
        joint.angularYZDrive = yzDrive;

        if (passiveFoot)
            joint.targetRotation = Quaternion.identity;
    }

    private void ConfigureLimbGrip()
    {
        if (!applyRuntimeGrip)
            return;

        runtimeGripMaterial = new PhysicsMaterial("Spider Limb Grip")
        {
            staticFriction = limbStaticFriction,
            dynamicFriction = limbDynamicFriction,
            frictionCombine = PhysicsMaterialCombine.Maximum
        };

        foreach (ConfigurableJoint joint in joints)
        {
            if (joint == null)
                continue;

            Collider[] colliders = joint.GetComponentsInChildren<Collider>();
            foreach (Collider limbCollider in colliders)
            {
                limbCollider.material = runtimeGripMaterial;
            }
        }
    }

    private void ConfigureLimbContactSensors()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            ConfigurableJoint joint = joints[i];
            if (joint == null)
                continue;

            if (jointSegmentIndices != null && i < jointSegmentIndices.Length && jointSegmentIndices[i] < 2)
                continue;

            Collider[] colliders = joint.GetComponentsInChildren<Collider>();
            foreach (Collider limbCollider in colliders)
            {
                SpiderLimbContact contact = limbCollider.GetComponent<SpiderLimbContact>();
                if (contact == null)
                    contact = limbCollider.gameObject.AddComponent<SpiderLimbContact>();

                contact.Initialize(this, limbCollider);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        for (int i = 0; i < actions.Length; i++)
        {
            if (!useOscillatingHeuristic || controlledJointIndices == null || i >= controlledJointIndices.Length)
            {
                actions[i] = 0f;
                continue;
            }

            actions[i] = GaitAction(controlledJointIndices[i]);
        }
    }

    private float GaitAction(int actionIndex)
    {
        int legIndex = GetJointLegIndex(actionIndex);
        int segmentIndex = GetJointSegmentIndex(actionIndex);
        float sideSign = GetJointSideSign(actionIndex);
        float legPhase = LegPhase(legIndex);
        float wave = Mathf.Sin(Time.time * heuristicFrequency + legPhase);

        float gaitAction = segmentIndex switch
        {
            0 => wave,
            1 => -wave,
            _ => wave * 0.5f
        };

        if (useTargetAwareGaitBias && body != null && target != null)
        {
            Vector3 localTargetDirection = body.transform.InverseTransformDirection(HorizontalDirectionToTarget());
            float turn = Mathf.Clamp(localTargetDirection.x, -1f, 1f);
            float forwardAlignment = Mathf.Clamp01(localTargetDirection.z * 0.5f + 0.5f);

            float sideMultiplier = 1f + sideSign * turn * gaitTurnScale;
            gaitAction *= Mathf.Max(0.2f, sideMultiplier);
            gaitAction *= Mathf.Lerp(0.65f, 1f, forwardAlignment);

            if (segmentIndex == 0)
                gaitAction += sideSign * turn * gaitTurnPoseScale;
        }

        return Mathf.Clamp(gaitAction, -1f, 1f);
    }

    private void CacheJointGaitMetadata(int index, ConfigurableJoint joint)
    {
        string jointName = joint.name.ToLowerInvariant();

        jointSegmentIndices[index] = jointName.Contains("second")
            ? 1
            : jointName.Contains("third")
                ? 2
                : 0;

        if (jointName.Contains("right"))
        {
            jointLegIndices[index] = 0;
            jointSideSigns[index] = 1f;
        }
        else if (jointName.Contains("left"))
        {
            jointLegIndices[index] = 1;
            jointSideSigns[index] = -1f;
        }
        else
        {
            jointLegIndices[index] = 2;
            jointSideSigns[index] = 0f;
        }
    }

    private int GetJointLegIndex(int actionIndex)
    {
        return jointLegIndices != null && actionIndex < jointLegIndices.Length
            ? jointLegIndices[actionIndex]
            : actionIndex / 3;
    }

    private int GetJointSegmentIndex(int actionIndex)
    {
        return jointSegmentIndices != null && actionIndex < jointSegmentIndices.Length
            ? jointSegmentIndices[actionIndex]
            : actionIndex % 3;
    }

    private bool IsControlledJoint(int jointIndex)
    {
        return GetJointSegmentIndex(jointIndex) < 2;
    }

    private Rigidbody FindParentBodyForJoint(int jointIndex)
    {
        int segmentIndex = GetJointSegmentIndex(jointIndex);
        if (segmentIndex <= 0)
            return body;

        int legIndex = GetJointLegIndex(jointIndex);
        int parentSegmentIndex = segmentIndex - 1;
        for (int i = 0; i < joints.Length; i++)
        {
            if (i == jointIndex || joints[i] == null)
                continue;

            if (GetJointLegIndex(i) != legIndex || GetJointSegmentIndex(i) != parentSegmentIndex)
                continue;

            Rigidbody parentBody = joints[i].GetComponent<Rigidbody>();
            if (parentBody != null)
                return parentBody;
        }

        return body;
    }

    private float JointTargetAngleForSegment(int segmentIndex)
    {
        return segmentIndex == 1
            ? Mathf.Min(jointTargetAngle, kneeAngularXLimit)
            : Mathf.Min(jointTargetAngle, hipAngularXLimit);
    }

    private float GetJointSideSign(int actionIndex)
    {
        return jointSideSigns != null && actionIndex < jointSideSigns.Length
            ? jointSideSigns[actionIndex]
            : 0f;
    }

    private float LegPhase(int legIndex)
    {
        return legIndex switch
        {
            0 => 0f,
            1 => Mathf.PI,
            _ => Mathf.PI * 0.5f
        };
    }

    private void OnCollisionEnter(Collision collision) => UpdateBodyContact(collision);

    private void OnCollisionStay(Collision collision) => UpdateBodyContact(collision);

    private void OnCollisionExit(Collision collision)
    {
        upwardContactColliders.Remove(collision.collider);
        bodyTouchingGround = upwardContactColliders.Count > 0;
    }

    private void UpdateBodyContact(Collision collision)
    {
        if (HasUpwardContact(collision))
            upwardContactColliders.Add(collision.collider);
        else
            upwardContactColliders.Remove(collision.collider);

        bodyTouchingGround = upwardContactColliders.Count > 0;
    }

    private void UpdateLimbContact(Collider limbCollider, Collision collision)
    {
        if (limbCollider == null)
            return;

        if (HasUpwardContact(collision))
            limbGroundColliders.Add(limbCollider);
        else
            limbGroundColliders.Remove(limbCollider);
    }

    private void ClearLimbContact(Collider limbCollider)
    {
        if (limbCollider != null)
            limbGroundColliders.Remove(limbCollider);
    }

    private bool HasUpwardContact(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private class SpiderLimbContact : MonoBehaviour
    {
        private SpiderAgent agent;
        private Collider limbCollider;

        public void Initialize(SpiderAgent owner, Collider observedCollider)
        {
            agent = owner;
            limbCollider = observedCollider;
        }

        private void OnCollisionEnter(Collision collision) => agent?.UpdateLimbContact(limbCollider, collision);

        private void OnCollisionStay(Collision collision) => agent?.UpdateLimbContact(limbCollider, collision);

        private void OnCollisionExit(Collision collision) => agent?.ClearLimbContact(limbCollider);
    }
}
