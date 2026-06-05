using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Transform ownerRoot;
    private int damage;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactEffectLifetime = 1f;
    [SerializeField] private float impactLingerTime = 0.05f;
    private bool hasHit;

    public void Initialize(Vector3 direction, float speed, float lifetime, int damageAmount, Transform owner)
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
            rb = gameObject.AddComponent<Rigidbody>();

        ownerRoot = owner;
        damage = Mathf.Max(1, damageAmount);

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction.normalized * speed;

        if (!GetComponentInChildren<Collider>())
            gameObject.AddComponent<SphereCollider>();

        IgnoreOwnerColliders();
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        if (ownerRoot && collision.transform.IsChildOf(ownerRoot))
            return;

        hasHit = true;
        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        SpiderHealth spider = collision.transform.GetComponentInParent<SpiderHealth>();
        if (spider)
        {
            Vector3 knockbackDirection = rb ? rb.linearVelocity : transform.forward;
            spider.TakeDamage(damage);
            spider.ApplyKnockback(knockbackDirection, hitPoint);
        }

        SpawnImpactEffect(hitPoint, collision.contactCount > 0 ? collision.GetContact(0).normal : -transform.forward);
        StopAtHit(hitPoint);
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (!impactEffectPrefab)
            return;

        Quaternion rotation = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;
        GameObject effect = Instantiate(impactEffectPrefab, position, rotation);

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
            particles.Play();

        Destroy(effect, Mathf.Max(0.05f, impactEffectLifetime));
    }

    private void StopAtHit(Vector3 hitPoint)
    {
        transform.position = hitPoint;

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider projectileCollider in GetComponentsInChildren<Collider>())
            projectileCollider.enabled = false;

        Destroy(gameObject, Mathf.Max(0f, impactLingerTime));
    }

    void IgnoreOwnerColliders()
    {
        if (!ownerRoot) return;

        foreach (Collider projectileCollider in GetComponentsInChildren<Collider>())
        {
            foreach (Collider ownerCollider in ownerRoot.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
        }
    }
}
