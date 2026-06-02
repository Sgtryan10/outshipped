using UnityEngine;

public class SpiderHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = true;

    public int CurrentHealth { get; private set; }
    private bool isDead;

    void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
        if (CurrentHealth > 0) return;

        isDead = true;
        gameManager.Instance?.OnEnemyDestroyed();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
