using UnityEngine;

public class BossMusicTrigger : MonoBehaviour
{
    private bool hasTriggered = false;
    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            AudioManager.instance.StartBossMusic();
        }
    }
}
