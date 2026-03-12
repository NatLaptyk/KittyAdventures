// ─────────────────────────────────────────────────────────────────────────────
//  VictorySceneManager.cs
//
//  Waits for the Victory scene to finish then loads the End scene.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Attach to any GameObject in the Victory scene
//  2. Set sceneDelay to match the length of your victory sequence
//  3. Or call LoadEndScene() from a button or timeline event
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictorySceneManager : MonoBehaviour
{
    [Tooltip("Exact name of your end/credits scene in Build Settings.")]
    public string endSceneName = "End";

    [Tooltip("Seconds to wait before automatically loading End scene. Set to 0 to disable auto-load.")]
    public float sceneDelay = 5f;

    [Tooltip("Optional fullscreen Image to fade to black before transitioning.")]
    public UnityEngine.UI.Image fadeImage;

    void Start()
    {
        if (sceneDelay > 0f)
            StartCoroutine(AutoLoad());
    }

    // Call this from a button or Unity Event to proceed manually
    public void LoadEndScene()
    {
        StartCoroutine(TransitionTo(endSceneName));
    }

    IEnumerator AutoLoad()
    {
        yield return new WaitForSeconds(sceneDelay);
        yield return StartCoroutine(TransitionTo(endSceneName));
    }

    IEnumerator TransitionTo(string sceneName)
    {
        if (fadeImage != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
        }
        SceneManager.LoadScene(sceneName);
    }
}