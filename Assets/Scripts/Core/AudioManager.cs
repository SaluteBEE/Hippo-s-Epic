using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GGJ2026
{
    [Serializable]
    public class LevelMusicConfig
    {
        [Tooltip("场景 Build Index（Build Settings 的顺序）。若你更喜欢用场景名，可扩展为 string sceneName。")]
        public int sceneBuildIndex;

        [Tooltip("该关卡的背景音乐")]
        public AudioClip musicClip;

        [Tooltip("是否循环播放")]
        public bool loop = true;

        [Range(0f, 1f)]
        [Tooltip("该关卡背景音乐音量（0~1）")]
        public float volume = 1f;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip[] clips; // 存储所有音效

        [Header("Per-Level Music Config")]
        [SerializeField] private List<LevelMusicConfig> levelMusicConfigs = new();

        [Header("Defaults (when no config found)")]
        [SerializeField] private AudioClip defaultMusic;
        [SerializeField] private bool defaultLoop = true;
        [Range(0f, 1f)]
        [SerializeField] private float defaultMusicVolume = 1f;

        [Header("Runtime Volumes")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        private readonly Dictionary<int, LevelMusicConfig> configByBuildIndex = new();
        private float musicBaseVolume = 1f;

        private void Awake()
        {
            // 单例防重复（避免多场景重复放置导致音乐叠加）
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildLookup();

            if (sfxSource != null)
                sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // 启动时为当前场景应用一次配置
            ApplyMusicForScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void BuildLookup()
        {
            configByBuildIndex.Clear();
            for (int i = 0; i < levelMusicConfigs.Count; i++)
            {
                var cfg = levelMusicConfigs[i];
                if (cfg == null) continue;

                // 后者覆盖前者：便于你在 Inspector 中快速调整
                configByBuildIndex[cfg.sceneBuildIndex] = cfg;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyMusicForScene(scene.buildIndex);
        }

        /// <summary>
        /// 根据关卡配置自动切换/设置背景音乐
        /// </summary>
        private void ApplyMusicForScene(int buildIndex)
        {
            if (musicSource == null)
            {
                Debug.LogError("[AudioManager] Missing musicSource.");
                return;
            }

            // 找到配置；找不到就使用默认
            AudioClip targetClip = defaultMusic;
            bool targetLoop = defaultLoop;
            float targetVol = defaultMusicVolume;

            if (configByBuildIndex.TryGetValue(buildIndex, out var cfg) && cfg != null)
            {
                if (cfg.musicClip != null) targetClip = cfg.musicClip;
                targetLoop = cfg.loop;
                targetVol = Mathf.Clamp01(cfg.volume);
            }

            // 如果 clip 没变，只更新 loop/volume，避免无意义重启音乐
            bool clipChanged = (musicSource.clip != targetClip);

            musicSource.loop = targetLoop;
            musicBaseVolume = targetVol;
            musicSource.volume = targetVol * musicVolume;

            if (clipChanged)
            {
                musicSource.Stop();
                musicSource.clip = targetClip;

                if (musicSource.clip != null)
                    musicSource.Play();
            }
            else
            {
                // clip 相同但可能刚进场景没播：确保播放
                if (musicSource.clip != null && !musicSource.isPlaying)
                    musicSource.Play();
            }
        }

        // -------------------- Public API --------------------

        /// <summary>
        /// 手动覆盖当前音乐（用于剧情/特殊关卡），可选：立即播放
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f, bool restartIfSame = false)
        {
            if (musicSource == null) return;

            volume = Mathf.Clamp01(volume);
            musicBaseVolume = volume;

            bool sameClip = (musicSource.clip == clip);
            musicSource.loop = loop;
            musicSource.volume = volume * musicVolume;

            if (!sameClip || restartIfSame)
            {
                musicSource.Stop();
                musicSource.clip = clip;
                if (clip != null) musicSource.Play();
            }
            else
            {
                if (clip != null && !musicSource.isPlaying)
                    musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource == null) return;
            musicSource.Stop();
        }

        public void PlaySFX(string name)
        {
            if (sfxSource == null || clips == null) return;

            AudioClip s = Array.Find(clips, x => x != null && x.name == name);
            if (s != null) sfxSource.PlayOneShot(s);
        }
        

        public void ToggleMusic()
        {
            if (musicSource == null) return;
            musicSource.mute = !musicSource.mute;
        }

        public void SetMusicVolume(float volume01)
        {
            if (musicSource == null) return;
            musicVolume = Mathf.Clamp01(volume01);
            musicSource.volume = musicBaseVolume * musicVolume;
        }

        public void SetSFXVolume(float volume01)
        {
            if (sfxSource == null) return;
            sfxVolume = Mathf.Clamp01(volume01);
            sfxSource.volume = sfxVolume;
        }

        public float GetMusicVolume()
        {
            return musicVolume;
        }

        public float GetSFXVolume()
        {
            return sfxVolume;
        }

        private void OnValidate()
        {
            if (musicSource != null)
                musicSource.volume = musicBaseVolume * Mathf.Clamp01(musicVolume);
            if (sfxSource != null)
                sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }
    }
}
