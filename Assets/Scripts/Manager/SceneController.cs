using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadScene(string sceneName, Action<float> onProgress = null, Action onLoaded = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, onProgress, onLoaded));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action<float> onProgress, Action onLoaded)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            onProgress?.Invoke(progress);
            yield return null;
        }

        onProgress?.Invoke(1f);
        onLoaded?.Invoke();
    }
}