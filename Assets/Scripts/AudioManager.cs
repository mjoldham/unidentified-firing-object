using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    public class AudioManager : MonoBehaviour
    {
        public AudioAssets DefaultAudioAssets;

        private AudioSource _musicSource;
        private AudioSource _playerFireSource;
        private AudioSource _explosionSource;

        private List<AudioSource> _syncSources = new List<AudioSource>();

        private Coroutine _duckingMusic = null;

        private void InitLoopingSource(AudioSource source, AudioClip clip)
        {
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = true;
            source.volume = 0.0f;

            _syncSources.Add(source);
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
            InitOneShotSource(_musicSource, DefaultAudioAssets.MusicVolume);
            
            _playerFireSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource(_playerFireSource);

            _explosionSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource (_explosionSource);
        }

        private void Start()
        {
            StartCoroutine(SyncingLoops());
        }

        private IEnumerator SyncingLoops()
        {
            yield return null;

            foreach (AudioSource source in _syncSources)
            {
                source.Play();
            }
        }

        public void Play(AudioClip track, double atTime)
        {
            _musicSource.clip = track;
            _musicSource.PlayScheduled(atTime);
        }

        private void OnPlayerFire()
        {
            _playerFireSource.PlayOneShot(DefaultAudioAssets.PlayerFire);
        }

        private IEnumerator DuckMusic(float volumeScale, float duration)
        {
            _musicSource.volume = DefaultAudioAssets.MusicVolume * volumeScale;
            yield return new WaitForSeconds(duration);
            _musicSource.volume = DefaultAudioAssets.MusicVolume;
        }

        private void OnBombUse()
        {
            _explosionSource.PlayOneShot(DefaultAudioAssets.BombUse);
            if (_duckingMusic != null)
            {
                StopCoroutine(_duckingMusic);
            }

            _duckingMusic = StartCoroutine(DuckMusic(0.5f, 0.5f));
        }

        private void OnEnable()
        {
            PlayerController.OnFireStart += OnPlayerFire;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnFireStart -= OnPlayerFire;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
