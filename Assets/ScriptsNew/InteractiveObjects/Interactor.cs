using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
}

public class Interactor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader input;
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float range = 2.2f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("UI (optional)")]
    [SerializeField] private InteractPromptUI promptUI;

    private IInteractable current;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (input == null || cam == null) return;

        FindInteractable();

        if (current != null && input.InteractPressed)
            current.Interact(gameObject);
    }

    private void FindInteractable()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide);

        IInteractable found = null;

        if (hitSomething)
            found = hit.collider.GetComponentInParent<IInteractable>();

        // Always update prompt (even if CanInteract is false — shows "need X orbs" etc.)
        if (!ReferenceEquals(found, current))
        {
            current = found;
            if (promptUI != null)
                promptUI.Set(current != null ? current.Prompt : "");
        }
        else if (found != null && promptUI != null)
        {
            // Refresh prompt every frame so orb count updates live
            promptUI.Set(current.Prompt);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cam == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * range);
    }
}