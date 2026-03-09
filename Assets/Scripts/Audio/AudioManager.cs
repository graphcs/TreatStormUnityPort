using System.Collections.Generic;
using UnityEngine;
using SnackAttack.Core;

namespace SnackAttack.Audio
{
    [DefaultExecutionOrder(10)]
    public class AudioManager : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private AudioClip musicBackground;
        [SerializeField] private AudioClip musicGameplay;

        [Header("SFX")]
        [SerializeField] private AudioClip sfxSelect;
        [SerializeField] private AudioClip sfxDogEat;
        [SerializeField] private AudioClip sfxPointEarned;
        [SerializeField] private AudioClip sfxBroccoli;
        [SerializeField] private AudioClip sfxChilli;
        [SerializeField] private AudioClip sfxRedBull;
        [SerializeField] private AudioClip sfxThunder;
        [SerializeField] private AudioClip sfxCountdown23;
        [SerializeField] private AudioClip sfxCountdown1;
        [SerializeField] private AudioClip sfxStart;

        [Header("Pool")]
        [SerializeField] private int sfxPoolSize = 6;

        private AudioSource _musicSource;
        private AudioSource[] _sfxPool;
        private int _sfxPoolIndex;
        private Dictionary<string, AudioClip> _clipMap;
        private AudioSettingsSO _audioSettings;
        private string _currentMusicTrack;

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].loop = false;
                _sfxPool[i].playOnAwake = false;
            }

            _clipMap = new Dictionary<string, AudioClip>
            {
                { "select", sfxSelect },
                { "dog_eat", sfxDogEat },
                { "point_earned", sfxPointEarned },
                { "broccoli", sfxBroccoli },
                { "chilli", sfxChilli },
                { "red_bull", sfxRedBull },
                { "thunder", sfxThunder },
                { "countdown_2_3", sfxCountdown23 },
                { "countdown_1", sfxCountdown1 },
                { "start", sfxStart },
                { "background", musicBackground },
                { "Gameplay", musicGameplay }
            };

            _audioSettings = GameManager.Instance.AudioSettings;
        }

        private void OnEnable()
        {
            EventBus.Subscribe(GameEvent.PlaySound, OnPlaySound);
            EventBus.Subscribe(GameEvent.PlayMusic, OnPlayMusic);
            EventBus.Subscribe(GameEvent.StopMusic, OnStopMusic);
            EventBus.Subscribe(GameEvent.SettingsChanged, OnSettingsChanged);
            EventBus.Subscribe(GameEvent.SnackCollected, OnSnackCollected);
            EventBus.Subscribe(GameEvent.GamePaused, OnGamePaused);
            EventBus.Subscribe(GameEvent.GameResumed, OnGameResumed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe(GameEvent.PlaySound, OnPlaySound);
            EventBus.Unsubscribe(GameEvent.PlayMusic, OnPlayMusic);
            EventBus.Unsubscribe(GameEvent.StopMusic, OnStopMusic);
            EventBus.Unsubscribe(GameEvent.SettingsChanged, OnSettingsChanged);
            EventBus.Unsubscribe(GameEvent.SnackCollected, OnSnackCollected);
            EventBus.Unsubscribe(GameEvent.GamePaused, OnGamePaused);
            EventBus.Unsubscribe(GameEvent.GameResumed, OnGameResumed);
        }

        private void OnPlaySound(EventData data)
        {
            var sound = data["sound"] as string;
            if (!string.IsNullOrEmpty(sound))
                PlaySfx(sound);
        }

        private void OnPlayMusic(EventData data)
        {
            var track = data["track"] as string;
            if (!string.IsNullOrEmpty(track))
                PlayMusicTrack(track);
        }

        private void OnStopMusic(EventData data)
        {
            StopMusicTrack();
        }

        private void OnSettingsChanged(EventData data)
        {
            ApplyVolumes();
        }

        private void OnSnackCollected(EventData data)
        {
            PlaySfx("dog_eat");

            var snackId = data["snackId"] as string;
            if (string.IsNullOrEmpty(snackId))
                return;

            switch (snackId)
            {
                case "broccoli":
                    PlaySfx("broccoli");
                    break;
                case "red_bull":
                    PlaySfx("red_bull");
                    break;
                case "spicy_pepper":
                    PlaySfx("chilli");
                    break;
                default:
                    PlaySfx("point_earned");
                    break;
            }
        }

        private void OnGamePaused(EventData data)
        {
            _musicSource.Pause();
        }

        private void OnGameResumed(EventData data)
        {
            _musicSource.UnPause();
        }

        private void PlaySfx(string name)
        {
            if (_audioSettings != null && !_audioSettings.sfxEnabled)
                return;

            if (!_clipMap.TryGetValue(name, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] Unknown or null SFX: {name}");
                return;
            }

            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;

            float volume = _audioSettings != null
                ? _audioSettings.masterVolume * _audioSettings.sfxVolume
                : 1f;

            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        private void PlayMusicTrack(string name)
        {
            if (name == _currentMusicTrack && _musicSource.isPlaying)
                return;

            _currentMusicTrack = name;

            if (_audioSettings != null && !_audioSettings.musicEnabled)
                return;

            if (!_clipMap.TryGetValue(name, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] Unknown or null music track: {name}");
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicSource.volume = _audioSettings != null
                ? _audioSettings.masterVolume * _audioSettings.musicVolume
                : 1f;
            _musicSource.Play();
        }

        private void StopMusicTrack()
        {
            _musicSource.Stop();
            _currentMusicTrack = null;
        }

        private void ApplyVolumes()
        {
            if (_audioSettings == null)
                return;

            float musicVol = _audioSettings.masterVolume * _audioSettings.musicVolume;

            if (_audioSettings.musicEnabled)
            {
                _musicSource.volume = musicVol;

                if (!_musicSource.isPlaying && !string.IsNullOrEmpty(_currentMusicTrack))
                {
                    if (_clipMap.TryGetValue(_currentMusicTrack, out var clip) && clip != null)
                    {
                        _musicSource.clip = clip;
                        _musicSource.volume = musicVol;
                        _musicSource.Play();
                    }
                }
            }
            else
            {
                if (_musicSource.isPlaying)
                    _musicSource.Stop();
            }
        }
    }
}
