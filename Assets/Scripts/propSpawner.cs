using UnityEngine;
using System.Collections.Generic;

public class propSpawner : MonoBehaviour
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

    [Header("Global Spacing Settings")]
    [Tooltip("Minimum distance required between structures (Base/Depots) and any other object.")]
    [SerializeField] private float structureMinSpacing = 6.0f;

    [Header("Tree Natural Settings")]
    [SerializeField] private float treeMinSpacing = 2.5f;
    [SerializeField] private float treeScaleMin = 0.85f;
    [SerializeField] private float treeScaleMax = 1.2f;
    [SerializeField] private bool treeAlignToSlope = true;
    [SerializeField] private float treeSlopeBlend = 0.4f;
    [SerializeField] private int treeClusterCount = 5;
    [SerializeField] private float treeClusterRadius = 12f;
    [SerializeField] private float treeClusterStrength = 0.6f;

    [Header("Rock Natural Settings")]
    [SerializeField] private float rockMinSpacing = 1.5f;
    [SerializeField] private float rockScaleMin = 0.7f;
    [SerializeField] private float rockScaleMax = 1.35f;
    [SerializeField] private bool rockAlignToSlope = true;
    [SerializeField] private float rockSlopeBlend = 0.65f;
    [SerializeField] private int rockClusterCount = 6;
    [SerializeField] private float rockClusterRadius = 7f;
    [SerializeField] private float rockClusterStrength = 0.75f;

    private struct PlacedObject
    {
        public Vector2 position;
        public float radius;

        public PlacedObject(Vector2 pos, float rad)
        {
            position = pos;
            radius = rad;
        }
    }

    private List<PlacedObject> allPlacedObjects = new List<PlacedObject>();


    void Start()
    {
        allPlacedObjects.Clear();

        if (baseSpawnZone != null && baseProp.prefab != null)
            SpawnProps(baseSpawnZone, baseProp, 1, false, structureMinSpacing);

        if (depotSpawnZone != null && depotProp.prefab != null)
            SpawnProps(depotSpawnZone, depotProp, spawnCountDepots, false, structureMinSpacing);

        if (treeSpawnZone != null)
            SpawnNatural(treeSpawnZone, treeProps, spawnCountTrees,
                treeMinSpacing, treeScaleMin, treeScaleMax,
                treeAlignToSlope, treeSlopeBlend,
                treeClusterCount, treeClusterRadius, treeClusterStrength);

        if (rockSpawnZone != null)
            SpawnNatural(rockSpawnZone, rockProps, spawnCountRocks,
                rockMinSpacing, rockScaleMin, rockScaleMax,
                rockAlignToSlope, rockSlopeBlend,
                rockClusterCount, rockClusterRadius, rockClusterStrength);
    }

    void SpawnNatural(
        BoxCollider zone, PropData[] propDatas, int count,
        float minSpacing, float scaleMin, float scaleMax,
        bool alignToSlope, float slopeBlend,
        int clusterCount, float clusterRadius, float clusterStrength)
    {
        if (propDatas == null || propDatas.Length == 0 || zone == null) return;

        Bounds bounds = zone.bounds;

        Vector2[] clusters = new Vector2[clusterCount];
        for (int c = 0; c < clusterCount; c++)
        {
            clusters[c] = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        int maxAttempts = count * 50;
        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            float rawX = Random.Range(bounds.min.x, bounds.max.x);
            float rawZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector2 raw = new Vector2(rawX, rawZ);

            Vector2 candidate;
            if (clusterStrength > 0f)
            {
                Vector2 nearest = clusters[0];
                float bestDist = Vector2.Distance(raw, clusters[0]);
                for (int c = 1; c < clusters.Length; c++)
                {
                    float d = Vector2.Distance(raw, clusters[c]);
                    if (d < bestDist) { bestDist = d; nearest = clusters[c]; }
                }

                float t = Mathf.Clamp01(1f - bestDist / clusterRadius) * clusterStrength;
                candidate = Vector2.Lerp(raw, nearest + Random.insideUnitCircle * clusterRadius * 0.5f, t);
                candidate.x = Mathf.Clamp(candidate.x, bounds.min.x, bounds.max.x);
                candidate.y = Mathf.Clamp(candidate.y, bounds.min.z, bounds.max.z);
            }
            else
            {
                candidate = raw;
            }

            if (IsOverlapping(candidate, minSpacing))
                continue;

            Vector3 rayStart = new Vector3(candidate.x, bounds.max.y + 5f, candidate.y);
            float rayDist = bounds.size.y + 15f;

            PropData chosen = propDatas[Random.Range(0, propDatas.Length)];
            if (chosen.prefab == null) continue;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDist, groundLayer))
            {
                spawnPos = hit.point + Vector3.up * chosen.heightOffset;

                float yaw = Random.Range(0f, 360f);
                Quaternion yawRot = Quaternion.Euler(
                    chosen.prefab.transform.rotation.eulerAngles.x,
                    yaw,
                    chosen.prefab.transform.rotation.eulerAngles.z
                );

                if (alignToSlope && slopeBlend > 0f)
                {
                    Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, yaw, 0f);
                    spawnRot = Quaternion.Slerp(yawRot, slopeRot, slopeBlend);
                }
                else
                {
                    spawnRot = yawRot;
                }
            }
            else
            {
                spawnPos = new Vector3(candidate.x, bounds.center.y + chosen.heightOffset, candidate.y);
                spawnRot = Quaternion.Euler(
                    chosen.prefab.transform.rotation.eulerAngles.x,
                    Random.Range(0f, 360f),
                    chosen.prefab.transform.rotation.eulerAngles.z
                );
            }

            float scale = Random.Range(scaleMin, scaleMax);
            GameObject go = Instantiate(chosen.prefab, spawnPos, spawnRot, transform);
            go.transform.localScale = chosen.prefab.transform.localScale * scale;

            // Register this object globally
            allPlacedObjects.Add(new PlacedObject(candidate, minSpacing));
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[PropSpawner] Only placed {spawned}/{count} props in '{zone.name}' — try reducing spacing values or increasing the zone size.");
    }

    void SpawnProps(BoxCollider spawnZone, PropData propData, int spawnCount, bool randomizeRotation, float clearanceRadius)
    {
        if (propData.prefab == null) return;
        SpawnProps(spawnZone, new PropData[] { propData }, spawnCount, randomizeRotation, clearanceRadius);
    }

    void SpawnProps(BoxCollider spawnZone, PropData[] propDatas, int spawnCount, bool randomizeRotation, float clearanceRadius)
    {
        if (propDatas == null || propDatas.Length == 0 || spawnZone == null) return;

        Bounds bounds = spawnZone.bounds;
        int maxAttempts = spawnCount * 50;
        int spawned = 0;
        int attempts = 0;

        while (spawned < spawnCount && attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector2 candidate = new Vector2(randomX, randomZ);

            // Structure overlap check
            if (IsOverlapping(candidate, clearanceRadius))
                continue;

            Vector3 rayStart = new Vector3(randomX, bounds.max.y, randomZ);
            float rayDistance = bounds.size.y + 10f;

            PropData chosenProp = propDatas[Random.Range(0, propDatas.Length)];
            if (chosenProp.prefab == null) continue;

            Vector3 spawnPosition;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
                spawnPosition = hit.point + new Vector3(0, chosenProp.heightOffset, 0);
            else
                spawnPosition = new Vector3(randomX, bounds.center.y + chosenProp.heightOffset, randomZ);

            Quaternion spawnRotation = randomizeRotation
                ? Quaternion.Euler(chosenProp.prefab.transform.rotation.eulerAngles.x, Random.Range(0f, 360f), chosenProp.prefab.transform.rotation.eulerAngles.z)
                : chosenProp.prefab.transform.rotation;

            Instantiate(chosenProp.prefab, spawnPosition, spawnRotation, transform);

            allPlacedObjects.Add(new PlacedObject(candidate, clearanceRadius));
            spawned++;
        }
    }

    private bool IsOverlapping(Vector2 candidate, float currentPropSpacing)
    {
        foreach (PlacedObject existing in allPlacedObjects)
        {
            float distance = Vector2.Distance(candidate, existing.position);
            float requiredGap = (currentPropSpacing + existing.radius) * 0.5f;

            if (distance < requiredGap)
            {
                return true;
            }
        }
        return false;
    }
}
