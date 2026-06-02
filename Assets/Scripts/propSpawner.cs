using UnityEngine;
using System.Collections.Generic;

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

    [Header("Tree Natural Settings")]
    [SerializeField] private float treeMinSpacing = 2.5f;
    [SerializeField] private float treeScaleMin = 0.85f;
    [SerializeField] private float treeScaleMax = 1.2f;
    [SerializeField] private bool treeAlignToSlope = true;
    [SerializeField] private float treeSlopeBlend = 0.4f;        // 0 = fully upright, 1 = fully slope-aligned
    [SerializeField] private int treeClusterCount = 5;           // number of cluster centres
    [SerializeField] private float treeClusterRadius = 12f;      // radius each cluster pulls toward
    [SerializeField] private float treeClusterStrength = 0.6f;   // 0 = pure random, 1 = full cluster

    [Header("Rock Natural Settings")]
    [SerializeField] private float rockMinSpacing = 1.5f;
    [SerializeField] private float rockScaleMin = 0.7f;
    [SerializeField] private float rockScaleMax = 1.35f;
    [SerializeField] private bool rockAlignToSlope = true;
    [SerializeField] private float rockSlopeBlend = 0.65f;
    [SerializeField] private int rockClusterCount = 6;
    [SerializeField] private float rockClusterRadius = 7f;
    [SerializeField] private float rockClusterStrength = 0.75f;

    // -------------------------------------------------------------------------

    void Start()
    {
        if (baseSpawnZone != null && baseProp.prefab != null)
            SpawnProps(baseSpawnZone, baseProp, 1, false, default);

        if (depotSpawnZone != null && depotProp.prefab != null)
            SpawnProps(depotSpawnZone, depotProp, spawnCountDepots, false, default);

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

    // -------------------------------------------------------------------------
    // Natural spawner — clustering + min-spacing + scale variation + slope align
    // -------------------------------------------------------------------------
    void SpawnNatural(
        BoxCollider zone, PropData[] propDatas, int count,
        float minSpacing, float scaleMin, float scaleMax,
        bool alignToSlope, float slopeBlend,
        int clusterCount, float clusterRadius, float clusterStrength)
    {
        if (propDatas == null || propDatas.Length == 0 || zone == null) return;

        Bounds bounds = zone.bounds;

        // Build cluster centres inside the zone
        Vector2[] clusters = new Vector2[clusterCount];
        for (int c = 0; c < clusterCount; c++)
        {
            clusters[c] = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        List<Vector2> placed = new List<Vector2>();
        int maxAttempts = count * 30;
        int spawned = 0;
        int attempts = 0;

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            // Candidate position — blend between random and nearest cluster
            float rawX = Random.Range(bounds.min.x, bounds.max.x);
            float rawZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector2 raw = new Vector2(rawX, rawZ);

            Vector2 candidate;
            if (clusterStrength > 0f)
            {
                // Find nearest cluster centre
                Vector2 nearest = clusters[0];
                float bestDist = Vector2.Distance(raw, clusters[0]);
                for (int c = 1; c < clusters.Length; c++)
                {
                    float d = Vector2.Distance(raw, clusters[c]);
                    if (d < bestDist) { bestDist = d; nearest = clusters[c]; }
                }

                // Pull toward it within clusterRadius
                float t = Mathf.Clamp01(1f - bestDist / clusterRadius) * clusterStrength;
                candidate = Vector2.Lerp(raw, nearest + Random.insideUnitCircle * clusterRadius * 0.5f, t);

                // Clamp back inside bounds
                candidate.x = Mathf.Clamp(candidate.x, bounds.min.x, bounds.max.x);
                candidate.y = Mathf.Clamp(candidate.y, bounds.min.z, bounds.max.z);
            }
            else
            {
                candidate = raw;
            }

            // Minimum-spacing rejection
            bool tooClose = false;
            foreach (Vector2 p in placed)
            {
                if (Vector2.Distance(candidate, p) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Raycast for ground
            Vector3 rayStart = new Vector3(candidate.x, bounds.max.y + 5f, candidate.y);
            float rayDist = bounds.size.y + 15f;

            PropData chosen = propDatas[Random.Range(0, propDatas.Length)];
            if (chosen.prefab == null) continue;

            Vector3 spawnPos;
            Quaternion spawnRot;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDist, groundLayer))
            {
                spawnPos = hit.point + Vector3.up * chosen.heightOffset;

                // Rotation: random Y + optional slope alignment
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

            // Randomise scale
            float scale = Random.Range(scaleMin, scaleMax);
            GameObject go = Instantiate(chosen.prefab, spawnPos, spawnRot, transform);
            go.transform.localScale = chosen.prefab.transform.localScale * scale;

            placed.Add(candidate);
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[PropSpawner] Only placed {spawned}/{count} props in '{zone.name}' — try reducing minSpacing or increasing the zone.");
    }

    // -------------------------------------------------------------------------
    // Simple spawner used for base / depots (unchanged behaviour)
    // -------------------------------------------------------------------------
    void SpawnProps(BoxCollider spawnZone, PropData propData, int spawnCount, bool randomizeRotation, System.ValueTuple<float,float> _ )
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
        }
    }
}