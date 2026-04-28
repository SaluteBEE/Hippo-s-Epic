using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadScene(string sceneName, Action<float> onProgress = null, Action onLoaded = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, onProgress, onLoaded));
    }

    private EventSystem _preservedEventSystem;

    private IEnumerator LoadSceneRoutine(string sceneName, Action<float> onProgress, Action onLoaded)
    {
        _preservedEventSystem = FindObjectOfType<EventSystem>();
        if (_preservedEventSystem != null)
        {
            _preservedEventSystem.enabled = false;
            var esGo = _preservedEventSystem.transform.root.gameObject;
            if (esGo != _preservedEventSystem.gameObject)
            {
                _preservedEventSystem.transform.SetParent(null);
                esGo = _preservedEventSystem.gameObject;
            }
            DontDestroyOnLoad(esGo);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            onProgress?.Invoke(progress);
            yield return null;
        }

        onProgress?.Invoke(1f);

        operation.allowSceneActivation = true;
        while (!operation.isDone)
            yield return null;

        RemoveDuplicateEventSystems();
        onLoaded?.Invoke();
    }

    private void RemoveDuplicateEventSystems()
    {
        var systems = FindObjectsOfType<EventSystem>();
        foreach (var sys in systems)
        {
            if (sys == _preservedEventSystem) continue;
            Destroy(sys.gameObject);
        }

        if (_preservedEventSystem != null)
            _preservedEventSystem.enabled = true;
    }
}
