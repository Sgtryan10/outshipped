using UnityEngine;

public class propSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] rockPrefabs;

    [SerializeField] private int spawnCountTrees = 40;
    [SerializeField] private int spawnCountRocks = 40;
    [SerializeField] private float heightOffset = 0f;

    [Header("Area Settings")]
    [SerializeField] private BoxCollider treeSpawnZone;
    [SerializeField] private BoxCollider rockSpawnZone;
    [SerializeField] private LayerMask groundLayer;

    void Start()
    {
        if (treeSpawnZone != null)
        {
            SpawnProps(treeSpawnZone, treePrefabs, spawnCountTrees);
        }

        if (rockSpawnZone != null)
        {
            SpawnProps(rockSpawnZone, rockPrefabs, spawnCountRocks);
        }
    }

    void SpawnProps(BoxCollider spawnZone, GameObject[] propPrefabs, int spawnCount)
    {
        if (propPrefabs == null || propPrefabs.Length == 0)
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

            int randomIndex = Random.Range(0, propPrefabs.Length);
            GameObject chosenPrefab = propPrefabs[randomIndex];

            Instantiate(chosenPrefab, spawnPosition, Quaternion.identity, transform);
        }
    }
}
