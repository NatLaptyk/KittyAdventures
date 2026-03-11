// ─────────────────────────────────────────────────────────────────────────────
//  SpiritPotion.cs
//
//  Dropped by the Spirit on death. Kitty auto-collects it by walking over it.
//  Notifies GameStats, which fires OnPotionCollected → InventoryHUD loads EndScene.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Create a Prefab for the potion (e.g. a glowing orb mesh).
//  2. Attach this script to the prefab root.
//  3. Add a Collider → check Is Trigger → set to a small sphere radius (~0.6).
//  4. Tag Kitty as "Player".
//  5. Assign this prefab to SpiritAI → Potion Prefab field.
//
//  Optional: assign a bobSpeed / bobHeight for a floating animation,
//  and a collectFX ParticleSystem that plays on pickup.
// ─────────────────────────────────────────────────────────────────────────────

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