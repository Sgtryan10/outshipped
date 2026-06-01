using UnityEngine;

public class pickupSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] pickupPrefabs;

    [SerializeField] private int spawnCount = 20;
    [SerializeField] private float heightOffset = 0.5f;

    [Header("Area Settings")]
    [SerializeField] private BoxCollider spawnZone;
    [SerializeField] private LayerMask groundLayer;

    void Start()
    {
        if (spawnZone == null)
        {
            spawnZone = GetComponent<BoxCollider>();
        }

        if (spawnZone != null)
        {
            SpawnPickups();
        }
    }

    void SpawnPickups()
    {
        if (pickupPrefabs == null || pickupPrefabs.Length == 0)
        {
            return;
        }

        Bounds bounds = spawnZone.bounds;

        for (int i = 0; i < spawnCount; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 rayStart = new Vector3(randomX, bounds.max.y, randomZ);
            Vector3 spawnPosition;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, bounds.size.y + 5f, groundLayer))
            {
                spawnPosition = hit.point + new Vector3(0, heightOffset, 0);
            }
            else
            {
                spawnPosition = new Vector3(randomX, bounds.center.y, randomZ);
            }

            int randomIndex = Random.Range(0, pickupPrefabs.Length);
            GameObject chosenPrefab = pickupPrefabs[randomIndex];

            Instantiate(chosenPrefab, spawnPosition, Quaternion.identity, transform);
        }
    }
}
