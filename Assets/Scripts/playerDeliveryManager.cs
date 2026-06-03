using UnityEngine;

public class playerDeliveryManager : MonoBehaviour
{
    [Header("Delivery State")]
    [SerializeField] private bool hasPackage = false;

    [Header("Target Tags")]
    [SerializeField] private string baseTag = "Base";
    [SerializeField] private string depotTag = "Depot";

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip packagePickupSFX;
    [SerializeField] private AudioClip packageDeliverSFX;

    public bool HasPackage => hasPackage;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(baseTag))
        {
            if (!hasPackage)
            {
                PickupPackage();
            }
        }
        else if (other.CompareTag(depotTag))
        {
            if (hasPackage)
            {
                DeliverPackage();
            }
        }
    }

    private void PickupPackage()
    {
        hasPackage = true;

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

        if (audioSource != null && packageDeliverSFX != null)
        {
            audioSource.PlayOneShot(packageDeliverSFX);
        }

        if (gameManager.Instance != null)
        {
            gameManager.Instance.OnPackageDelivered();
        }
    }
}
