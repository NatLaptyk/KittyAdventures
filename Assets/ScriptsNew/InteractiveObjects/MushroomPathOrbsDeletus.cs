using UnityEngine;

public class MushroomPathOrbsDeletus : MonoBehaviour
{
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            GameObject[] orbs = GameObject.FindGameObjectsWithTag("orb");

            foreach (GameObject orb in orbs)
            {
                Destroy(orb);
            }
        }
    }
}