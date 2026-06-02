using UnityEngine;

public class gameEndOnContact : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            TriggerGameOverState();
        }
    }

    private void TriggerGameOverState()
    {
        if (gameManager.Instance != null)
        {
            gameManager.Instance.TriggerGameOver();
        }
    }
}
