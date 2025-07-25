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
        private AudioSource _hitSource;
        private AudioSource _criticalSource;

        private Dictionary<AudioSource, Coroutine> _loopSources = new Dictionary<AudioSource, Coroutine>();
        private List<AudioSource> _oneshotSources = new List<AudioSource>();

        private Coroutine _duckingMusic = null;
        private bool _hasHitHurt, _hasHitShield;
        private int _hurtCount, _shieldCount;

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

            _oneshotSources.Add(source);
        }

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource(_musicSource, Settings.MusicVolume);
            
            _playerFireSource = gameObject.AddComponent<AudioSource>();
            InitLoopingSource(_playerFireSource, Settings.PlayerFire);

            _hitSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource(_hitSource);

            _criticalSource = gameObject.AddComponent<AudioSource>();
            InitOneShotSource (_criticalSource);
        }

        private void Start()
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.Play();
            }
        }

        private void LateUpdate()
        {
            _hasHitHurt = _hasHitShield = false;
        }

        public void Play(AudioClip track, double atTime)
        {
            _musicSource.Stop();
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
            StartFading(_playerFireSource, 1.0f, 0.05f);
        }

        private void OnPlayerFireEnd()
        {
            StartFading(_playerFireSource, 0.0f, 0.1f);
        }

        IEnumerator KeepingHurtCount()
        {
            _hurtCount++;
            yield return new WaitForSeconds(Settings.HitHurt.length);
            _hurtCount--;
        }

        IEnumerator KeepingShieldCount()
        {
            _shieldCount++;
            yield return new WaitForSeconds(Settings.HitShield.length);
            _shieldCount--;
        }

        private void OnHitHurt(Vector2 position)
        {
            if (_hasHitHurt || _hurtCount >= 4)
            {
                return;
            }

            _hasHitHurt = true;
            _hitSource.PlayOneShot(Settings.HitHurt/*, 1.0f / (_hurtCount + 1)*/);

            StartCoroutine(KeepingHurtCount());
        }

        private void OnHitShield(Vector2 position)
        {
            if (_hasHitShield || _shieldCount >= 4)
            {
                return;
            }

            _hasHitShield = true;
            _hitSource.PlayOneShot(Settings.HitShield/*, 1.0f / (_shieldCount + 1)*/);

            StartCoroutine(KeepingShieldCount());
        }

        private IEnumerator DuckMusic(float volumeScale, float duration)
        {
            float transition = 0.1f;
            StartCoroutine(Fading(_hitSource, volumeScale, transition));
            yield return Fading(_musicSource, volumeScale * Settings.MusicVolume, transition);

            yield return new WaitForSeconds(duration - 2.0f * transition);

            StartCoroutine(Fading(_hitSource, 1.0f, transition));
            yield return Fading(_musicSource, Settings.MusicVolume, transition);
        }

        private void PlayCritical(AudioClip clip, float duckDuration)
        {
            _criticalSource.PlayOneShot(clip);
            if (_duckingMusic != null)
            {
                StopCoroutine(_duckingMusic);
            }

            _duckingMusic = StartCoroutine(DuckMusic(Settings.MusicDuckScale, duckDuration));
        }

        private void OnStartDie()
        {
            PlayCritical(Settings.PlayerStartDie, Settings.PlayerStartDie.length);
        }

        private void OnDeath()
        {
            PlayCritical(Settings.PlayerDeath, Settings.PlayerDeath.length);
        }

        private void OnBombUse()
        {
            PlayCritical(Settings.BombUse, 0.8f * Settings.BombUse.length);
        }

        private void OnKill(Vector2 position)
        {
            PlayCritical(Settings.Kill, Settings.Kill.length);
        }

        private void OnPause()
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.Pause();
            }

            _oneshotSources.ForEach(source => source.Pause());
            
        }

        private void OnUnpause(double lostTime)
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.UnPause();
            }

            _oneshotSources.ForEach(source => source.UnPause());
        }

        private void OnEnable()
        {
            PlayerController.OnStartDie += OnStartDie;
            PlayerController.OnDeath += OnDeath;
            PlayerController.OnFireStart += OnPlayerFireStart;
            PlayerController.OnFireEnd += OnPlayerFireEnd;
            PlayerController.OnBombUse += OnBombUse;

            EnemyController.OnKill += OnKill;

            GameManager.OnPause += OnPause;
            GameManager.OnUnpause += OnUnpause;
            GameManager.OnHitHurt += OnHitHurt;
            GameManager.OnHitShield += OnHitShield;
        }

        private void OnDisable()
        {
            PlayerController.OnStartDie -= OnStartDie;
            PlayerController.OnDeath -= OnDeath;
            PlayerController.OnFireStart -= OnPlayerFireStart;
            PlayerController.OnFireEnd -= OnPlayerFireEnd;
            PlayerController.OnBombUse -= OnBombUse;

            EnemyController.OnKill -= OnKill;

            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
            GameManager.OnHitHurt -= OnHitHurt;
            GameManager.OnHitShield -= OnHitShield;
        }
    }
}
