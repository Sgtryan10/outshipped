using UnityEngine;

[CreateAssetMenu(menuName = "Outshipped/Car Config")]
public class CarConfig : ScriptableObject
{
    [Header("Movement")]
    public float acceleration = 38f;
    public float reverseAcceleration = 24f;
    public float maxSpeed = 20f;
    public float turnSpeed = 145f;
    public float grip = 9f;
    public float driftGrip = 3.5f;
    public float brakingForce = 18f;
    public float weight = 800f;

    [Header("Sphere Body")]
    public float sphereRadius = 0.65f;
    public Vector3 sphereCenter = new Vector3(0f, -0.15f, 0f);

    [Header("Terrain Feel")]
    public float groundRayLength = 1.4f;
    public float stickToGroundForce = 28f;
    public float slopeAlignSpeed = 10f;
    public float faceMoveDirectionSpeed = 8f;
}
