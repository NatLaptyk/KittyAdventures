// ─────────────────────────────────────────────────────────────────────────────
//  NumberTrigger.cs  (patched)
//
//  Patched from groupmate's original:
//   - Replaced Input.GetKeyDown with new InputSystem Keyboard
//   - PlayerController disable/enable kept as-is (still valid)
//   - Added "Press E" prompt that appears when Kitty enters zone
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  UI HIERARCHY — build this in your Canvas:
//
//  Canvas
//    └── PuzzlePrompt              (GameObject — the "Press E" hint)
//          └── PromptText          (TMP — "Press  E  to solve the puzzle")
//
//    └── PuzzleInputUI             (GameObject — the input panel)
//          ├── PanelBackground     (Image — dark rounded panel)
//          ├── PuzzleQuestion      (TMP — "What number am I thinking of?")
//          ├── NumberInputField    (TMP_InputField — player types here)
//          └── SubmitButton        (Button — "Submit")
//
//  On the trigger zone GameObject:
//   - Add a Collider set to Is Trigger
//   - Attach this script
//   - Assign all fields in the Inspector
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class NumberTrigger : MonoBehaviour
{
    /// <summary>Fired once when the player enters the correct answer.</summary>
    public static event System.Action OnPuzzleSolved;

    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("UI Panels")]
    [Tooltip("The full input panel — shown when player presses E.")]
    public GameObject    inputUI;

    [Tooltip("The small 'Press E' prompt — shown when player is in zone.")]
    public GameObject    pressEPrompt;

    [Header("UI Elements")]
    public TMP_InputField numberInput;
    public TMP_Text       feedbackText;    // optional — shows "Correct!" or "Wrong!"
    public Button         submitButton;

    [Header("Player")]
    public PlayerController playerMovement;

    [Header("Puzzle")]
    [Tooltip("The correct answer to the puzzle.")]
    public int correctNumber = 12;

    [Tooltip("How long the feedback text is shown before closing.")]
    public float feedbackDuration = 1.2f;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    bool _playerInZone = false;
    bool _uiActive     = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (inputUI     != null) inputUI.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(SubmitNumber);
    }

    void OnDestroy()
    {
        if (submitButton != null) submitButton.onClick.RemoveListener(SubmitNumber);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Press E to open
        if (_playerInZone && !_uiActive && Keyboard.current.eKey.wasPressedThisFrame)
            OpenUI();

        // Enter to submit
        if (_uiActive && Keyboard.current.enterKey.wasPressedThisFrame)
            SubmitNumber();

        // Escape to close
        if (_uiActive && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseUI();
    }

    // ─────────────────────────────────────────────
    //  UI
    // ─────────────────────────────────────────────

    void OpenUI()
    {
        _uiActive = true;

        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (inputUI      != null) inputUI.SetActive(true);

        if (playerMovement != null) playerMovement.enabled = false;

        if (numberInput != null)
        {
            numberInput.text = "";
            numberInput.ActivateInputField();
        }

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void SubmitNumber()
    {
        if (numberInput == null) return;

        bool correct = false;

        if (int.TryParse(numberInput.text, out int entered))
            correct = entered == correctNumber;

        // Play sound via AudioManager
        if (correct)
            OnPuzzleSolved?.Invoke();

        if (AudioManager.instance != null)
        {
            if (correct)
                AudioManager.instance.PlaySFX(AudioManager.instance.signRight);
            else
                AudioManager.instance.PlaySFX(AudioManager.instance.signWrong);
        }

        // Show feedback text if assigned
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text  = correct ? "✓ Correct!" : "✗ Wrong!";
            feedbackText.color = correct
                ? new Color(0.3f, 1f, 0.3f)
                : new Color(1f, 0.3f, 0.3f);

            StartCoroutine(DelayedClose());
        }
        else
        {
            CloseUI();
        }
    }

    void CloseUI()
    {
        _uiActive = false;

        if (inputUI      != null) inputUI.SetActive(false);
        if (pressEPrompt != null) pressEPrompt.SetActive(_playerInZone);

        if (playerMovement != null) playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    System.Collections.IEnumerator DelayedClose()
    {
        yield return new WaitForSeconds(feedbackDuration);
        CloseUI();
    }

    // ─────────────────────────────────────────────
    //  TRIGGER
    // ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInZone = true;
        if (!_uiActive && pressEPrompt != null)
            pressEPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInZone = false;
        if (pressEPrompt != null) pressEPrompt.SetActive(false);
        if (_uiActive) CloseUI();
    }
}