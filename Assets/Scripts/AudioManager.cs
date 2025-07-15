using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    public class AudioManager : MonoBehaviour
    {
        public AudioAssets DefaultAudioAssets;

        [HideInInspector]
        public int TrackNumber;

        private AudioSource _musicSource;
        private AudioSource _playerFastSource, _playerFireSource;
        private AudioSource _explosionSource;

        private Vector2 _oldMove;

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
            
            _playerFastSource = gameObject.AddComponent<AudioSource>();
            InitLoopingSource(_playerFastSource, DefaultAudioAssets.PlayerFast);

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

        private void FixedUpdate()
        {
            if (!_musicSource.isPlaying)
            {
                if (TrackNumber == DefaultAudioAssets.TrackList.Length)
                {
                    TrackNumber = 0;
                }

                _musicSource.PlayOneShot(DefaultAudioAssets.TrackList[TrackNumber++]);
            }
        }

        private void OnPlayerMove(Vector2 move, bool isFiring)
        {
            //if (isFiring || move.sqrMagnitude == 0.0f)
            //{
            //    _playerFastSource.volume = 0.0f;
            //}
            //else if (_oldMove.sqrMagnitude == 0.0f)
            //{
            //    _playerFastSource.volume = 1.0f;
            //}
            //else if (_playerFastSource.volume > DefaultAudioAssets.PlayerFastMin)
            //{
            //    _playerFastSource.volume *= DefaultAudioAssets.PlayerFastDecay;
            //}
            //else
            //{
            //    _playerFastSource.volume = DefaultAudioAssets.PlayerFastMin;
            //}

            //_oldMove = move;
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
            PlayerController.OnMove += OnPlayerMove;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnFire -= OnPlayerFire;
            PlayerController.OnMove -= OnPlayerMove;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
