using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Transform ownerRoot;
    private int damage;

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
        if (ownerRoot && collision.transform.IsChildOf(ownerRoot))
            return;

        SpiderHealth spider = collision.transform.GetComponentInParent<SpiderHealth>();
        if (spider)
            spider.TakeDamage(damage);

        Destroy(gameObject);
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
