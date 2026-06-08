using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndSceneManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Buttons")]
    public Button replayButton;
    public Button exitButton;

    [Header("UI Elements")]
    [Tooltip("The full-screen panel image — used for fade in.")]
    public Image    panelImage;
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    [Header("Scenes")]
    [Tooltip("Name of your main game scene to reload on Replay.")]
    public string gameSceneName = "MainScene";

    [Header("Timing")]
    public float fadeInDuration  = 1.5f;
    public float textRevealDelay = 0.8f;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        // Make sure time is running (in case it was paused)
        Time.timeScale = 1f;

        // Make sure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Wire buttons
        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplay);
            Debug.Log("[EndScene] ReplayButton wired. Interactable: " + replayButton.interactable);
        }
        else Debug.LogWarning("[EndScene] ReplayButton is NULL!");

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExit);
            Debug.Log("[EndScene] ExitButton wired. Interactable: " + exitButton.interactable);
        }
        else Debug.LogWarning("[EndScene] ExitButton is NULL!");

        Debug.Log("[EndScene] Cursor visible: " + Cursor.visible + " LockState: " + Cursor.lockState);
        Debug.Log("[EndScene] SceneFader instance: " + (SceneFader.Instance != null ? "EXISTS" : "NULL"));
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.ForceUnblock();
            Debug.Log("[EndScene] SceneFader CanvasGroup blocksRaycasts: " + SceneFader.Instance.GetBlocksRaycasts());
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[EndScene] Mouse click detected at: " + Input.mousePosition);
            
            // Check what UI is under the pointer
            var pointerData = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (var r in results)
                Debug.Log("[EndScene] Raycast hit: " + r.gameObject.name + " on " + r.gameObject.layer);
        }
    }

    void OnDestroy()
    {
        if (replayButton != null) replayButton.onClick.RemoveListener(OnReplay);
        if (exitButton   != null) exitButton.onClick.RemoveListener(OnExit);
    }

    // ─────────────────────────────────────────────
    //  BUTTON HANDLERS
    // ─────────────────────────────────────────────

    public void OnReplay()
    {
        Debug.Log("[EndScene] OnReplay clicked!");
        if (GameStats.Instance != null)
            GameStats.Instance.Reset();
        SceneFader.Instance?.FadeTo(gameSceneName);
    }

    public void OnExit()
    {
        Debug.Log("[EndScene] OnExit clicked!");
        SceneFader.Instance?.FadeToQuit();
    }

}