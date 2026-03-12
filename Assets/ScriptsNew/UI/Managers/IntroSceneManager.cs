// ─────────────────────────────────────────────────────────────────────────────
//  IntroSceneManager.cs
//
//  Waits for the intro to finish then loads MainScene.
//
//  SETUP
//  ─────────────────────────────────────────────────────────────────────────────
//  1. Attach to any GameObject in the Intro scene
//  2. Set sceneDelay to match the length of your intro (video/animation)
//  3. Or call LoadMainScene() from a button or timeline event
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneManager : MonoBehaviour
{
    [Tooltip("Exact name of your main game scene in Build Settings.")]
    public string mainSceneName = "MainScene";

    [Tooltip("Seconds to wait before automatically loading MainScene. Set to 0 to disable auto-load.")]
    public float sceneDelay = 5f;

    [Tooltip("Optional fullscreen Image to fade to black before transitioning.")]
    public UnityEngine.UI.Image fadeImage;

    void Start()
    {
        if (sceneDelay > 0f)
            StartCoroutine(AutoLoad());
    }

    // Call this from a button or Unity Event to skip/finish intro manually
    public void LoadMainScene()
    {
        StartCoroutine(TransitionTo(mainSceneName));
    }

    IEnumerator AutoLoad()
    {
        yield return new WaitForSeconds(sceneDelay);
        yield return StartCoroutine(TransitionTo(mainSceneName));
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