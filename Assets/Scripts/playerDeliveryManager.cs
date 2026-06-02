using UnityEngine;

public class playerDeliveryManager : MonoBehaviour
{
    [Header("Delivery State")]
    [SerializeField] private bool hasPackage = false;

    [Header("Target Tags")]
    [SerializeField] private string baseTag = "Base";
    [SerializeField] private string depotTag = "Depot";

    public bool HasPackage => hasPackage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(baseTag))
        {
            if (!hasPackage)
            {
                PickupPackage();
            }
            else
            {
            }
        }

        else if (other.CompareTag(depotTag))
        {
            if (hasPackage)
            {
                DeliverPackage();
            }
            else
            {
            }
        }
    }

    private void PickupPackage()
    {
        hasPackage = true;
    }

    private void DeliverPackage()
    {
        hasPackage = false;

        if (gameManager.Instance != null)
        {
            gameManager.Instance.OnPackageDelivered();
        }
    }
}
