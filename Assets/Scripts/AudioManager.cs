using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    public class AudioManager : MonoBehaviour
    {
        public AudioSettings Settings;

        private AudioSource _musicSource;
        private AudioSource _playerFireSource;
        private AudioSource _explosionSource;

        private Dictionary<AudioSource, Coroutine> _loopSources = new Dictionary<AudioSource, Coroutine>();

        private Coroutine _duckingMusic = null;

        private void InitLoopingSource(AudioSource source, AudioClip clip)
        {
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = true;
            source.volume = 0.0f;

            _loopSources[source] = null;
        }

        private void InitOneShotSource(AudioSource source, float volume = 1.0f)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.volume = volume;
        }

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource(_musicSource, Settings.MusicVolume);
            
            _playerFireSource = gameObject.AddComponent<AudioSource>();
            InitLoopingSource(_playerFireSource, Settings.PlayerFire);

            _explosionSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource (_explosionSource);
        }

        private void Start()
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.Play();
            }
        }

        public void Play(AudioClip track, double atTime)
        {
            _musicSource.clip = track;
            _musicSource.PlayScheduled(atTime);
        }

        IEnumerator Fading(AudioSource source, float targetVol, float duration)
        {
            float startVol = source.volume;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                source.volume = Mathf.Lerp(targetVol, startVol, (endTime - Time.time) / duration);
                yield return null;
            }

            source.volume = targetVol;
        }

        private void StartFading(AudioSource source, float targetVol, float duration)
        {
            if (!_loopSources.TryGetValue(source, out Coroutine fading))
            {
                Debug.LogError($"{nameof(source)} was not found in looping sources dictionary.");
                return;
            }

            if (fading != null)
            {
                StopCoroutine(fading);
            }

            fading = StartCoroutine(Fading(source, targetVol, duration));
        }

        private void OnPlayerFireStart()
        {
            StartFading(_playerFireSource, 1.0f, 0.1f);
        }

        private void OnPlayerFireEnd()
        {
            StartFading(_playerFireSource, 0.0f, 0.1f);
        }

        private IEnumerator DuckMusic(float volumeScale, float duration)
        {
            float transition = 0.1f;
            yield return Fading(_musicSource, volumeScale * Settings.MusicVolume, transition);

            yield return new WaitForSeconds(duration - 2.0f * transition);

            yield return Fading(_musicSource, Settings.MusicVolume, transition);
        }

        private void OnBombUse()
        {
            _explosionSource.PlayOneShot(Settings.BombUse);
            if (_duckingMusic != null)
            {
                StopCoroutine(_duckingMusic);
            }

            _duckingMusic = StartCoroutine(DuckMusic(Settings.MusicDuckScale, 0.8f * Settings.BombUse.length));
        }

        private void OnEnable()
        {
            PlayerController.OnFireStart += OnPlayerFireStart;
            PlayerController.OnFireEnd += OnPlayerFireEnd;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnFireStart -= OnPlayerFireStart;
            PlayerController.OnFireEnd -= OnPlayerFireEnd;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
