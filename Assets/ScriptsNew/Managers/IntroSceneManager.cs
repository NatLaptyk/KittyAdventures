using System.Collections;
using UnityEngine;

public class IntroSceneManager : MonoBehaviour
{
    [Tooltip("Exact name of your main game scene in Build Settings.")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Tooltip("Seconds to wait before automatically loading MainScene. Set to 0 to disable auto-load.")]
    [SerializeField] private float sceneDelay = 5f;

    void Start()
    {
        if (sceneDelay > 0f)
            StartCoroutine(AutoLoad());
    }

    public void LoadMainScene()
    {
        SceneFader.Instance?.FadeTo(mainSceneName);
    }

    IEnumerator AutoLoad()
    {
        yield return new WaitForSeconds(sceneDelay);
        SceneFader.Instance?.FadeTo(mainSceneName);
    }
}