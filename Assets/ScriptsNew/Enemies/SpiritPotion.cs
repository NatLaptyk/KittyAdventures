// ─────────────────────────────────────────────────────────────────────────────
// SpiritPotion.cs
// Dropped by the Spirit on death. Kitty auto-collects it by walking over it.
// Notifies GameStats, which fires OnPotionCollected → InventoryHUD loads EndScene.

using System.Collections;
using UnityEngine;

public class SpiritPotion : MonoBehaviour
{
    [Header("Bob Animation")]
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed  = 2.2f;
    [SerializeField] private float spinSpeed = 90f;

    [Header("FX")]
    [SerializeField] private ParticleSystem collectFX;
    [SerializeField] private AudioClip      collectSound;
    [SerializeField] private float          destroyDelay = 0.5f;

    private Vector3 _startPos;
    private bool    _collected = false;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        if (_collected) return;

        // Bob up and down
        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Spin
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        _collected = true;
        GameStats.Instance?.RegisterPotionCollected();

        // Play collect sound
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        StartCoroutine(CollectSequence());
    }

    IEnumerator CollectSequence()
    {
        // Hide mesh immediately
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        // Play collect particle if assigned
        if (collectFX != null)
        {
            collectFX.transform.SetParent(null); // detach so it finishes playing
            collectFX.Play();
        }

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}