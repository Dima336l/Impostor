using UnityEngine;
using System.Collections.Generic;

namespace Impostor.Audio
{
    /// <summary>
    /// Manages sound effects and background music for the game.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip clueSubmittedSound;
        [SerializeField] private AudioClip voteCastSound;
        [SerializeField] private AudioClip roundStartSound;
        [SerializeField] private AudioClip gameEndSound;

        [Header("Settings")]
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 0.7f;

        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Create audio sources if they don't exist
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            // Register audio clips
            RegisterAudioClip("ButtonClick", buttonClickSound);
            RegisterAudioClip("ClueSubmitted", clueSubmittedSound);
            RegisterAudioClip("VoteCast", voteCastSound);
            RegisterAudioClip("RoundStart", roundStartSound);
            RegisterAudioClip("GameEnd", gameEndSound);
        }

        private void Start()
        {
            PlayBackgroundMusic();
        }

        private void RegisterAudioClip(string name, AudioClip clip)
        {
            if (clip != null)
            {
                _audioClips[name] = clip;
            }
        }

        public void PlayBackgroundMusic()
        {
            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        public void StopBackgroundMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PlaySFX(string clipName)
        {
            if (_audioClips.TryGetValue(clipName, out AudioClip clip) && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        public void PlayButtonClick()
        {
            PlaySFX("ButtonClick");
        }

        public void PlayClueSubmitted()
        {
            PlaySFX("ClueSubmitted");
        }

        public void PlayVoteCast()
        {
            PlaySFX("VoteCast");
        }

        public void PlayRoundStart()
        {
            PlaySFX("RoundStart");
        }

        public void PlayGameEnd()
        {
            PlaySFX("GameEnd");
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
    }
}

