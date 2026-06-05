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
    [SerializeField] private PropData[] grassProps;

    [SerializeField] private int spawnCountDepots = 4;
    [SerializeField] private int spawnCountTrees = 40;
    [SerializeField] private int spawnCountRocks = 40;
    [SerializeField] private int spawnCountGrassClusters = 30;   

    [Header("Area Settings")]
    [SerializeField] private BoxCollider baseSpawnZone;
    [SerializeField] private BoxCollider depotSpawnZone;
    [SerializeField] private BoxCollider treeSpawnZone;
    [SerializeField] private BoxCollider rockSpawnZone;
    [SerializeField] private BoxCollider grassSpawnZone;
    [SerializeField] private LayerMask groundLayer;

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

    [Header("Grass Natural Settings")]
    [SerializeField] private int grassBladesMin = 2;           
    [SerializeField] private int grassBladesMax = 5;           
    [SerializeField] private float grassClumpRadius = 0.6f;      
    [SerializeField] private float grassMinSpacing = 1.2f;      
    [SerializeField] private float grassScaleMin = 0.75f;
    [SerializeField] private float grassScaleMax = 1.25f;
    [SerializeField] private bool grassAlignToSlope = false;
    [SerializeField] private float grassSlopeBlend = 0.2f;
    [SerializeField] private int grassClusterCount = 8;          
    [SerializeField] private float grassClusterRadius = 10f;
    [SerializeField] private float grassClusterStrength = 0.7f;


    void Start()
    {
        if (baseSpawnZone != null && baseProp.prefab != null)
            SpawnProps(baseSpawnZone, baseProp, 1, false);

        if (depotSpawnZone != null && depotProp.prefab != null)
            SpawnProps(depotSpawnZone, depotProp, spawnCountDepots, false);

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

        if (grassSpawnZone != null)
            SpawnGrass();
    }

    void SpawnGrass()
    {
        if (grassProps == null || grassProps.Length == 0 || grassSpawnZone == null) return;

        Bounds bounds = grassSpawnZone.bounds;

        Vector2[] clusters = new Vector2[grassClusterCount];
        for (int c = 0; c < grassClusterCount; c++)
        {
            clusters[c] = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        List<Vector2> clumpCentres = new List<Vector2>();
        int maxAttempts = spawnCountGrassClusters * 30;
        int clumpsPlaced = 0;
        int attempts = 0;

        while (clumpsPlaced < spawnCountGrassClusters && attempts < maxAttempts)
        {
            attempts++;

            Vector2 raw = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            Vector2 centre = raw;
            if (grassClusterStrength > 0f)
            {
                Vector2 nearest = clusters[0];
                float bestDist = Vector2.Distance(raw, clusters[0]);
                for (int c = 1; c < clusters.Length; c++)
                {
                    float d = Vector2.Distance(raw, clusters[c]);
                    if (d < bestDist) { bestDist = d; nearest = clusters[c]; }
                }

                float t = Mathf.Clamp01(1f - bestDist / grassClusterRadius) * grassClusterStrength;
                centre = Vector2.Lerp(raw, nearest + Random.insideUnitCircle * grassClusterRadius * 0.4f, t);
                centre.x = Mathf.Clamp(centre.x, bounds.min.x, bounds.max.x);
                centre.y = Mathf.Clamp(centre.y, bounds.min.z, bounds.max.z);
            }

            bool tooClose = false;
            foreach (Vector2 existing in clumpCentres)
            {
                if (Vector2.Distance(centre, existing) < grassMinSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            int bladeCount = Random.Range(grassBladesMin, grassBladesMax + 1);
            bool anyBladeSpawned = false;

            for (int b = 0; b < bladeCount; b++)
            {

                Vector2 offset = Random.insideUnitCircle * grassClumpRadius;
                float bx = Mathf.Clamp(centre.x + offset.x, bounds.min.x, bounds.max.x);
                float bz = Mathf.Clamp(centre.y + offset.y, bounds.min.z, bounds.max.z);

                Vector3 rayStart = new Vector3(bx, bounds.max.y + 5f, bz);
                float rayDist = bounds.size.y + 15f;

                PropData chosen = grassProps[Random.Range(0, grassProps.Length)];
                if (chosen.prefab == null) continue;

                Vector3 spawnPos;
                Quaternion spawnRot;

                float yaw = Random.Range(0f, 360f);

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDist, groundLayer))
                {
                    spawnPos = hit.point + Vector3.up * chosen.heightOffset;

                    if (grassAlignToSlope && grassSlopeBlend > 0f)
                    {
                        Quaternion yawRot = Quaternion.Euler(
                            chosen.prefab.transform.rotation.eulerAngles.x,
                            yaw,
                            chosen.prefab.transform.rotation.eulerAngles.z
                        );
                        Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, yaw, 0f);
                        spawnRot = Quaternion.Slerp(yawRot, slopeRot, grassSlopeBlend);
                    }
                    else
                    {
                        spawnRot = Quaternion.Euler(
                            chosen.prefab.transform.rotation.eulerAngles.x,
                            yaw,
                            chosen.prefab.transform.rotation.eulerAngles.z
                        );
                    }
                }
                else
                {
                    spawnPos = new Vector3(bx, bounds.center.y + chosen.heightOffset, bz);
                    spawnRot = Quaternion.Euler(
                        chosen.prefab.transform.rotation.eulerAngles.x,
                        yaw,
                        chosen.prefab.transform.rotation.eulerAngles.z
                    );
                }

                float scale = Random.Range(grassScaleMin, grassScaleMax);
                GameObject go = Instantiate(chosen.prefab, spawnPos, spawnRot, transform);
                go.transform.localScale = chosen.prefab.transform.localScale * scale;
                anyBladeSpawned = true;
            }

            if (anyBladeSpawned)
            {
                clumpCentres.Add(centre);
                clumpsPlaced++;
            }
        }

        if (clumpsPlaced < spawnCountGrassClusters)
            Debug.LogWarning($"[PropSpawner] Only placed {clumpsPlaced}/{spawnCountGrassClusters} grass clumps in '{grassSpawnZone.name}' — try reducing grassMinSpacing or enlarging the zone.");
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

        List<Vector2> placed = new List<Vector2>();
        int maxAttempts = count * 30;
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

            placed.Add(candidate);
            spawned++;
        }

        if (spawned < count)
            Debug.LogWarning($"[PropSpawner] Only placed {spawned}/{count} props in '{zone.name}' — try reducing minSpacing or increasing the zone.");
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
