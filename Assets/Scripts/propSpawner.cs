using UnityEngine;

public class PropSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct PropData
    {
        public GameObject prefab;
        public float heightOffset;
    }

    [Header("Spawn Settings")]
    [SerializeField] private PropData baseProp;
    [SerializeField] private PropData depotProp;
    [SerializeField] private PropData[] treeProps;
    [SerializeField] private PropData[] rockProps;

    [SerializeField] private int spawnCountDepots = 4;
    [SerializeField] private int spawnCountTrees = 40;
    [SerializeField] private int spawnCountRocks = 40;

    [Header("Area Settings")]
    [SerializeField] private BoxCollider baseSpawnZone;
    [SerializeField] private BoxCollider depotSpawnZone;
    [SerializeField] private BoxCollider treeSpawnZone;
    [SerializeField] private BoxCollider rockSpawnZone;
    [SerializeField] private LayerMask groundLayer;

    void Start()
    {
        // Spawn exactly 1 Base
        if (baseSpawnZone != null && baseProp.prefab != null)
        {
            SpawnProps(baseSpawnZone, baseProp, 1);
        }

        if (depotSpawnZone != null && depotProp.prefab != null)
        {
            SpawnProps(depotSpawnZone, depotProp, spawnCountDepots);
        }

        if (treeSpawnZone != null)
        {
            SpawnProps(treeSpawnZone, treeProps, spawnCountTrees);
        }

        if (rockSpawnZone != null)
        {
            SpawnProps(rockSpawnZone, rockProps, spawnCountRocks);
        }
    }

    void SpawnProps(BoxCollider spawnZone, PropData propData, int spawnCount)
    {
        if (propData.prefab == null) return;
        SpawnProps(spawnZone, new PropData[] { propData }, spawnCount);
    }

    void SpawnProps(BoxCollider spawnZone, PropData[] propDatas, int spawnCount)
    {
        if (propDatas == null || propDatas.Length == 0 || spawnZone == null)
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
            float rayDistance = bounds.size.y + 10f;

            int randomIndex = Random.Range(0, propDatas.Length);
            PropData chosenProp = propDatas[randomIndex];

            if (chosenProp.prefab == null) continue;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
            {
                spawnPosition = hit.point + new Vector3(0, chosenProp.heightOffset, 0);
            }
            else
            {
                spawnPosition = new Vector3(randomX, bounds.center.y + chosenProp.heightOffset, randomZ);
            }

            Instantiate(chosenProp.prefab, spawnPosition, chosenProp.prefab.transform.rotation, transform);
        }
    }
}
