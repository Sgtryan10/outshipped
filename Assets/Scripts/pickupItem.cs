using UnityEngine;

public class pickupItem : MonoBehaviour
{
    public enum pickupType { Amped, Overdrive, EMP, Slow, Armor }

    [Header("Item Configuration")]
    [SerializeField] private pickupType type;

    [Header("Animation Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float height = 0.25f;
    [SerializeField] private float rotationSpeed = 50f;

    private Vector3 startPos;
    private float timeOffset;

    void Start()
    {
        startPos = transform.position;
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float newY = Mathf.Sin((Time.time + timeOffset) * speed) * height + startPos.y;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyPickupEffect();
            Destroy(gameObject);
        }
    }

    private void ApplyPickupEffect()
    {
        if (gameManager.Instance == null) return;

        switch (type)
        {
            case pickupType.Armor:
                gameManager.Instance.AddArmor(1);
                break;
            case pickupType.Amped:
                gameManager.Instance.StoreAbility("AMPED");
                break;
            case pickupType.Overdrive:
                gameManager.Instance.StoreAbility("OVERDRIVE");
                break;
            case pickupType.EMP:
                gameManager.Instance.StoreAbility("EMP");
                break;
            case pickupType.Slow:
                gameManager.Instance.StoreAbility("SLOW");
                break;
        }
    }
}
