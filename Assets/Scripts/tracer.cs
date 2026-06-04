using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class tracer : MonoBehaviour
{
    [Header("Line Generation")]
    [SerializeField] private float segmentLength = 0.5f;
    [SerializeField] private float noiseScale = 2f;
    [SerializeField] private float noisePower = 1f;

    [Header("Animation Settings")]
    [SerializeField] private float delayBeforeDissolve = 1f;
    [SerializeField] private float dissolveDuration = 1f;

    private LineRenderer lineRenderer;
    private Material materialInstance;
    private Vector3[] basePoints;
    private Vector3 seedOffset;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer.material != null)
        {
            materialInstance = lineRenderer.material;
        }
    }

    public void InitializeHitscanLine(Vector3 startPos, Vector3 endPos)
    {
        seedOffset = new Vector3(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));

        if (materialInstance != null)
        {
            materialInstance.SetFloat("_Path_dissolve", 0f);
            materialInstance.SetFloat("_Dissolve", 0f);
        }

        float totalDistance = Vector3.Distance(startPos, endPos);
        int pointCount = Mathf.Max(2, Mathf.CeilToInt(totalDistance / segmentLength) + 1);
        basePoints = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            basePoints[i] = Vector3.Lerp(startPos, endPos, t);
        }

        lineRenderer.positionCount = pointCount;
        lineRenderer.SetPositions(basePoints);

        StartCoroutine(AnimateLineSequence());
    }

    private IEnumerator AnimateLineSequence()
    {
        float elapsedTime = 0f;

        while (elapsedTime < delayBeforeDissolve)
        {
            elapsedTime += Time.deltaTime;
            float distortionIntensity = Mathf.Clamp01(elapsedTime / delayBeforeDissolve);
            ApplyNoiseDistortion(distortionIntensity);
            yield return null;
        }

        ApplyNoiseDistortion(1f);

        elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / dissolveDuration);

            if (materialInstance != null)
            {
                materialInstance.SetFloat("_Path_dissolve", progress);
                materialInstance.SetFloat("_Dissolve", progress);
            }

            ApplyNoiseDistortion(1f);

            yield return null;
        }

        if (materialInstance != null)
        {
            materialInstance.SetFloat("_Path_dissolve", 1f);
            materialInstance.SetFloat("_Dissolve", 1f);
        }

        lineRenderer.positionCount = 0;

        Destroy(gameObject);
    }

    private void ApplyNoiseDistortion(float intensity)
    {
        if (basePoints == null || basePoints.Length == 0) return;

        Vector3[] distortedPoints = new Vector3[basePoints.Length];

        for (int i = 0; i < basePoints.Length; i++)
        {
            Vector3 originalPoint = basePoints[i];

            if (i == 0 || i == basePoints.Length - 1)
            {
                distortedPoints[i] = originalPoint;
                continue;
            }

            float noiseX = Mathf.PerlinNoise(originalPoint.x * noiseScale + seedOffset.x, originalPoint.y * noiseScale + seedOffset.y) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(originalPoint.y * noiseScale + seedOffset.z, originalPoint.z * noiseScale + seedOffset.x) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(originalPoint.z * noiseScale + seedOffset.y, originalPoint.x * noiseScale + seedOffset.z) * 2f - 1f;

            Vector3 noiseDirection = new Vector3(noiseX, noiseY, noiseZ);

            distortedPoints[i] = originalPoint + (noiseDirection * (noisePower * intensity));
        }

        lineRenderer.SetPositions(distortedPoints);
    }
}
