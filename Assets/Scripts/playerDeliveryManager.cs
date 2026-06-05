using UnityEngine;

public class playerDeliveryManager : MonoBehaviour
{
    [Header("Delivery State")]
    [SerializeField] private bool hasPackage = false;

    [Header("Package Visual")]
    [Tooltip("Optional child object to show while carrying a package. If left empty, the script looks for a child named package or Package Visual.")]
    [SerializeField] private GameObject packageVisual;
    [SerializeField] private bool createPlaceholderIfMissing = true;
    [SerializeField] private Vector3 placeholderLocalPosition = new Vector3(-1.75f, 1f, 0f);
    [SerializeField] private Vector3 placeholderLocalScale = new Vector3(1.5f, 1.5f, 1.5f);
    [SerializeField] private Color placeholderColor = new Color(0.55f, 0.32f, 0.12f);

    [Header("Target Tags")]
    [SerializeField] private string baseTag = "Base";
    [SerializeField] private string depotTag = "Depot";

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip packagePickupSFX;
    [SerializeField] private AudioClip packageDeliverSFX;

    public bool HasPackage => hasPackage;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ResolvePackageVisual();
        SetPackageVisualActive(hasPackage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(baseTag) && !hasPackage)
        {
            PickupPackage();
        }
        else if (other.CompareTag(depotTag) && hasPackage)
        {
            DeliverPackage();
        }
    }

    private void PickupPackage()
    {
        hasPackage = true;
        SetPackageVisualActive(true);

        if (audioSource != null && packagePickupSFX != null)
        {
            audioSource.PlayOneShot(packagePickupSFX);
        }

        if (gameManager.Instance != null)
        {
            gameManager.Instance.TriggerPackagePickupPopup();
        }
    }

    private void DeliverPackage()
    {
        hasPackage = false;
        SetPackageVisualActive(false);

        if (audioSource != null && packageDeliverSFX != null)
        {
            audioSource.PlayOneShot(packageDeliverSFX);
        }

        if (gameManager.Instance != null)
        {
            gameManager.Instance.OnPackageDelivered();
        }
    }

    private void ResolvePackageVisual()
    {
        if (packageVisual == null)
        {
            Transform child = transform.Find("package");
            if (child == null)
            {
                child = transform.Find("Package Visual");
            }

            if (child != null)
            {
                packageVisual = child.gameObject;
            }
        }

        if (packageVisual == null && createPlaceholderIfMissing)
        {
            packageVisual = CreatePlaceholderPackage();
        }

        if (packageVisual != null)
        {
            DisablePackageColliders(packageVisual);
        }
    }

    private GameObject CreatePlaceholderPackage()
    {
        GameObject packageObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        packageObject.name = "Package Visual";
        packageObject.transform.SetParent(transform, false);
        packageObject.transform.localPosition = placeholderLocalPosition;
        packageObject.transform.localRotation = Quaternion.identity;
        packageObject.transform.localScale = placeholderLocalScale;

        Renderer packageRenderer = packageObject.GetComponent<Renderer>();
        if (packageRenderer != null)
        {
            packageRenderer.material.color = placeholderColor;
        }

        return packageObject;
    }

    private void DisablePackageColliders(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void SetPackageVisualActive(bool active)
    {
        if (packageVisual != null)
        {
            packageVisual.SetActive(active);
        }
    }
}
