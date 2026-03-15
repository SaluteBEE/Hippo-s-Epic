using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class AudioEntry
{
    public string key;
    public AudioClip clip;
}
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("BGM")]
    [SerializeField] private AudioEntry[] bgmEntries;

    [Header("SFX")]
    [SerializeField] private AudioEntry[] sfxEntries;

    [Header("UI")]
    [SerializeField] private AudioEntry[] uiEntries;

    private readonly Dictionary<string, AudioClip> bgmMap = new();
    private readonly Dictionary<string, AudioClip> sfxMap = new();
    private readonly Dictionary<string, AudioClip> uiMap = new();

    private void Awake()
    {
        BuildMap(bgmEntries, bgmMap);
        BuildMap(sfxEntries, sfxMap);
        BuildMap(uiEntries, uiMap);
    }

    private void BuildMap(AudioEntry[] entries, Dictionary<string, AudioClip> map)
    {
        map.Clear();

        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.key)) continue;
            if (entry.clip == null) continue;
            if (map.ContainsKey(entry.key)) continue;

            map.Add(entry.key, entry.clip);
        }
    }

    public void PlayBGM(string key)
    {
        if (!bgmMap.TryGetValue(key, out var clip))
            return;

        if (bgmSource == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }

    public void PlaySFX(string key)
    {
        if (!sfxMap.TryGetValue(key, out var clip))
            return;

        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayUI(string key)
    {
        if (!uiMap.TryGetValue(key, out var clip))
            return;

        if (uiSource == null)
            return;

        uiSource.PlayOneShot(clip);
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
            bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);
    }

    public void SetUIVolume(float volume)
    {
        if (uiSource != null)
            uiSource.volume = Mathf.Clamp01(volume);
    }
}