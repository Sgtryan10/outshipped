using UnityEngine;

public class SpiderHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool invincible = true;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private Rigidbody knockbackBody;
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float knockbackUpwardBias = 0.2f;

    public int CurrentHealth { get; private set; }
    private bool isDead;

    void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (!knockbackBody)
            knockbackBody = FindBestKnockbackBody();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (invincible) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
        if (CurrentHealth > 0) return;

        isDead = true;
        gameManager.Instance?.OnEnemyDestroyed();

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    public void ApplyKnockback(Vector3 direction, Vector3 hitPoint, float forceMultiplier = 1f)
    {
        if (!knockbackBody)
            knockbackBody = FindBestKnockbackBody();

        if (!knockbackBody)
            return;

        Vector3 knockbackDirection = direction;
        if (knockbackDirection.sqrMagnitude < 0.0001f)
            knockbackDirection = transform.forward;

        knockbackDirection.y = Mathf.Max(knockbackDirection.y, knockbackUpwardBias);
        knockbackDirection.Normalize();

        knockbackBody.WakeUp();
        knockbackBody.AddForceAtPosition(
            knockbackDirection * knockbackForce * Mathf.Max(0f, forceMultiplier),
            hitPoint,
            ForceMode.Impulse);
    }

    private Rigidbody FindBestKnockbackBody()
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
        Rigidbody bestBody = null;
        float bestMass = float.NegativeInfinity;

        foreach (Rigidbody candidate in bodies)
        {
            if (candidate.mass <= bestMass)
                continue;

            bestBody = candidate;
            bestMass = candidate.mass;
        }

        return bestBody;
    }
}
