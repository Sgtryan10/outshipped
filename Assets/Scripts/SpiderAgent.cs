using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SpiderAgent : Agent
{
    [Header("Body")]
    public Rigidbody body;

    [Header("Leg Joints")]
    public ConfigurableJoint[] joints;

    [Header("Goal")]
    public Transform target;
    private float targetReachDistance = 1f;

    [Header("Penalties")]
    public float velocityPenalty = 0.005f;
    public float groundContactPenalty = 0.05f;
    private bool bodyTouchingGround = false;
    private int stepsSinceEpisodeStart = 0;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3[] startJointRotations;

    public override void Initialize()
    {
        startPosition = body.transform.position;
        startRotation = body.transform.rotation;
        
        // Store initial joint rotations
        startJointRotations = new Vector3[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            startJointRotations[i] = joints[i].transform.localEulerAngles;
        }
    }

    public override void OnEpisodeBegin()
    {
        stepsSinceEpisodeStart = 0;
        bodyTouchingGround = false;
        
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        body.transform.position = startPosition;
        body.transform.rotation = startRotation;

        for (int i = 0; i < joints.Length; i++)
        {
            Rigidbody rb = joints[i].GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // Reset joint rotation to prevent physical explosions
                joints[i].transform.localEulerAngles = startJointRotations[i];
            }
        }
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
            Vector3 directionToTarget = (target.position - body.transform.position).normalized;
            sensor.AddObservation(directionToTarget);
            
            float distanceToTarget = Vector3.Distance(body.transform.position, target.position);
            sensor.AddObservation(distanceToTarget);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Add body ground contact state
        sensor.AddObservation(bodyTouchingGround ? 1f : 0f);

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
        var continuousActions = actions.ContinuousActions;

        for (int i = 0; i < joints.Length; i++)
        {
            if (i >= continuousActions.Length)
                break;

            JointDrive drive = joints[i].angularXDrive;

            drive.positionSpring = 500f;
            drive.positionDamper = 50f;
            drive.maximumForce = 1000f;

            joints[i].angularXDrive = drive;

            float targetAngle = continuousActions[i] * 45f;

            joints[i].targetRotation =
                Quaternion.Euler(targetAngle, 0f, 0f);
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

        // Penalty for body touching ground
        if (bodyTouchingGround)
        {
            AddReward(-groundContactPenalty);
        }

        // Reward for moving forward (stronger incentive)
        AddReward(body.linearVelocity.z * 0.05f);

        // Reward for moving towards target
        if (target != null)
        {
            Vector3 directionToTarget = (target.position - body.transform.position).normalized;
            if (body.linearVelocity.magnitude > 0.1f)  // Only reward if actually moving
            {
                float dotProduct = Vector3.Dot(body.linearVelocity.normalized, directionToTarget);
                AddReward(Mathf.Max(dotProduct, 0f) * 0.05f);
            }

            // Reward for reaching the target
            float distanceToTarget = Vector3.Distance(body.transform.position, target.position);
            if (distanceToTarget < targetReachDistance)
            {
                AddReward(1f);
                EndEpisode();
            }
        }

        if (body.transform.position.y < 0.2f)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        for (int i = 0; i < actions.Length; i++)
        {
            actions[i] = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the body is colliding with ground by checking contact normals
        foreach (ContactPoint contact in collision.contacts)
        {
            // If surface normal points mostly upward, it's likely the ground
            if (contact.normal.y > 0.5f)
            {
                bodyTouchingGround = true;
                return;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Keep track while sliding on ground
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                bodyTouchingGround = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Only clear if no other collisions with upward-pointing normals
        bodyTouchingGround = false;
        Collider[] overlappingColliders = Physics.OverlapSphere(body.position, 0.2f);
        foreach (Collider col in overlappingColliders)
        {
            if (col.gameObject != gameObject && col.CompareTag("Ground"))
            {
                bodyTouchingGround = true;
                return;
            }
        }
    }
}