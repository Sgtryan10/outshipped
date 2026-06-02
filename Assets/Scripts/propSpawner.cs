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
        if (baseSpawnZone != null && baseProp.prefab != null)
        {
            SpawnProps(baseSpawnZone, baseProp, 1, false);
        }

        if (depotSpawnZone != null && depotProp.prefab != null)
        {
            SpawnProps(depotSpawnZone, depotProp, spawnCountDepots, false);
        }

        if (treeSpawnZone != null)
        {
            SpawnProps(treeSpawnZone, treeProps, spawnCountTrees, true);
        }

        if (rockSpawnZone != null)
        {
            SpawnProps(rockSpawnZone, rockProps, spawnCountRocks, true);
        }
    }

    void SpawnProps(BoxCollider spawnZone, PropData propData, int spawnCount, bool randomizeRotation)
    {
        if (propData.prefab == null) return;
        SpawnProps(spawnZone, new PropData[] { propData }, spawnCount, randomizeRotation);
    }

    void SpawnProps(BoxCollider spawnZone, PropData[] propDatas, int spawnCount, bool randomizeRotation)
    {
        if (propDatas == null || propDatas.Length == 0 || spawnZone == null) return;

        Bounds bounds = spawnZone.bounds;

        for (int i = 0; i < spawnCount; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 rayStart = new Vector3(randomX, bounds.max.y, randomZ);
            float rayDistance = bounds.size.y + 10f;

            int randomIndex = Random.Range(0, propDatas.Length);
            PropData chosenProp = propDatas[randomIndex];

            if (chosenProp.prefab == null) continue;

            Vector3 spawnPosition;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
            {
                spawnPosition = hit.point + new Vector3(0, chosenProp.heightOffset, 0);
            }
            else
            {
                spawnPosition = new Vector3(randomX, bounds.center.y + chosenProp.heightOffset, randomZ);
            }

            Quaternion spawnRotation;

            if (randomizeRotation)
            {
                spawnRotation = Quaternion.Euler(
                    chosenProp.prefab.transform.rotation.eulerAngles.x,
                    Random.Range(0f, 360f),
                    chosenProp.prefab.transform.rotation.eulerAngles.z
                );
            }
            else
            {
                spawnRotation = chosenProp.prefab.transform.rotation;
            }

            Instantiate(chosenProp.prefab, spawnPosition, spawnRotation, transform);
        }
    }
}