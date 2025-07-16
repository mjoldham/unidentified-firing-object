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

        private void OnBombUse()
        {
            _explosionSource.PlayOneShot(DefaultAudioAssets.BombUse);
        }

        private void OnEnable()
        {
            PlayerController.OnFire += OnPlayerFire;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnFire -= OnPlayerFire;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
