using UnityEngine;

public class BossMusicTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.instance.StartBossMusic();
        }
    }
}
