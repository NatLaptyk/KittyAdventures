// A snack Kitty can find in the world. Walk over it to add it to inventory.
// Plays a collect sound and particle, then destroys itself.

using System.Collections;
using UnityEngine;

public class SnackPickup : MonoBehaviour
{
    [Header("Bob Animation")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed  = 2f;
    [SerializeField] private float spinSpeed = 80f;

    [Header("FX")]
    [SerializeField] private ParticleSystem collectFX;
    [SerializeField] private AudioClip      collectSound;
    [SerializeField] private float          destroyDelay = 0.6f;

    Vector3 _startPos;
    bool    _collected = false;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        if (_collected) return;
        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        _collected = true;

        var stats = other.GetComponentInParent<PlayerStats>()
                 ?? other.GetComponent<PlayerStats>();
        stats?.AddSnack(1);

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        StartCoroutine(CollectSequence());
    }

    IEnumerator CollectSequence()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (collectFX != null)
        {
            collectFX.transform.SetParent(null);
            collectFX.Play();
        }

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}