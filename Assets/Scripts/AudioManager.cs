using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    public class AudioManager : MonoBehaviour
    {
        public AudioSettings Settings;
        private float _musicScale = 1.0f, _fxScale = 1.0f;

        public static Action<float> OnChangeMusic, OnChangeFX;

        private AudioSource _musicSource;
        private AudioSource _playerFireSource;
        private AudioSource _hitSource;
        private AudioSource _criticalSource;

        private Dictionary<AudioSource, Coroutine> _loopSources = new Dictionary<AudioSource, Coroutine>();
        private List<AudioSource> _oneshotSources = new List<AudioSource>();

        private Coroutine _duckingMusic = null, _menuing = null;
        private bool _hasHitHurt, _hasHitShield, _hasKilled;
        private int _hurtCount, _shieldCount, _killCount;
        private int _critPriority;

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
            _oneshotSources.Remove(_musicSource);
            
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

            PlayMenuLoop();
        }

        private void LateUpdate()
        {
            _hasHitHurt = _hasHitShield = _hasKilled = false;
        }

        private IEnumerator TransitioningToMenu()
        {
            if (_musicSource.isPlaying)
            {
                yield return StartCoroutine(Fading(_musicSource, 0.0f, 0.3f * GameManager.GameOverDuration));
                _musicSource.Stop();
                yield return new WaitForSeconds(0.7f * GameManager.GameOverDuration);
            }

            _musicSource.clip = Settings.MenuLoop;
            _musicSource.loop = true;
            _musicSource.volume = 0.0f;

            _musicSource.Play();
            yield return StartCoroutine(Fading(_musicSource, _musicScale * Settings.MusicVolume, GameManager.GameOverDuration));
        }

        public void PlayMenuLoop()
        {
            if (_menuing != null)
            {
                StopCoroutine(_menuing);
            }

            _menuing = StartCoroutine(TransitioningToMenu());
        }

        private IEnumerator TransitioningFromMenu(AudioClip track, double atTime, float skipToTime)
        {
            double duration = 0.5 * (atTime - UnityEngine.AudioSettings.dspTime);
            yield return StartCoroutine(Fading(_musicSource, 0.0f, (float)duration));
            _musicSource.loop = false;
            Play(track, atTime, skipToTime);
        }

        public void Play(AudioClip track, double atTime, float skipToTime)
        {
            if (_musicSource.loop)
            {
                if (_menuing != null)
                {
                    StopCoroutine(_menuing);
                }

                _menuing = StartCoroutine(TransitioningFromMenu(track, atTime, skipToTime));
                return;
            }

            _musicSource.Stop();

            if (skipToTime >= track.length)
            {
                return;
            }

            _musicSource.clip = track;
            _musicSource.volume = _musicScale * Settings.MusicVolume;
            _musicSource.time = skipToTime;

            _musicSource.PlayScheduled(atTime);
        }

        private IEnumerator Fading(AudioSource source, float targetVol, float duration)
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
            StartFading(_playerFireSource, _fxScale, 0.05f);
        }

        private void OnPlayerFireEnd()
        {
            StartFading(_playerFireSource, 0.0f, 0.1f);
        }

        private IEnumerator KeepingHurtCount()
        {
            _hurtCount++;
            yield return new WaitForSeconds(Settings.HitHurt.length);
            _hurtCount--;
        }

        private IEnumerator KeepingShieldCount()
        {
            _shieldCount++;
            yield return new WaitForSeconds(Settings.HitShield.length);
            _shieldCount--;
        }

        private void OnHitHurt(Vector2 position)
        {
            if (_hasHitHurt || _hurtCount > 2)
            {
                return;
            }

            _hasHitHurt = true;
            _hitSource.PlayOneShot(Settings.HitHurt);

            StartCoroutine(KeepingHurtCount());
        }

        private void OnHitShield(Vector2 position)
        {
            if (_hasHitShield || _shieldCount > 2)
            {
                return;
            }

            _hasHitShield = true;
            _hitSource.PlayOneShot(Settings.HitShield);

            StartCoroutine(KeepingShieldCount());
        }

        private IEnumerator DuckMusic(float volumeScale, float duration)
        {
            float transition = 0.22f;
            StartCoroutine(Fading(_hitSource, volumeScale * _fxScale, transition * duration));
            yield return Fading(_musicSource, volumeScale * _musicScale * Settings.MusicVolume, transition * duration);

            yield return new WaitForSeconds((1.0f - 2.0f * transition) * duration);

            StartCoroutine(Fading(_hitSource, _fxScale, transition * duration));
            yield return Fading(_musicSource, _musicScale * Settings.MusicVolume, transition * duration);
            _critPriority = 0;
        }

        private void PlayCritical(AudioClip clip, float duckDuration, int priority, float volumeScale = 1.0f)
        {
            if (_criticalSource.volume == 0.0f)
            {
                return;
            }

            if (priority < _critPriority)
            {
                return;
            }

            if (priority > _critPriority)
            {
                _critPriority = priority;
                _criticalSource.Stop();
            }

            _criticalSource.PlayOneShot(clip, volumeScale * _fxScale);
            if (_duckingMusic != null)
            {
                StopCoroutine(_duckingMusic);
            }

            _duckingMusic = StartCoroutine(DuckMusic(Settings.MusicDuckScale, duckDuration));
        }

        private void OnStartDie()
        {
            PlayCritical(Settings.PlayerStartDie, Settings.PlayerStartDie.length, 2);
        }

        private void OnDeath()
        {
            PlayCritical(Settings.PlayerDeath, Settings.PlayerDeath.length, 2);
        }

        private void OnShieldDown()
        {
            PlayCritical(Settings.ShieldDown, Settings.ShieldDown.length, 2);
        }

        private void OnBombUse()
        {
            PlayCritical(Settings.BombUse, 0.8f * Settings.BombUse.length, 3);
        }

        private void OnGameOver()
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.volume = 0.0f;
            }

            PlayCritical(Settings.GameOver, Settings.GameOver.length, 3);
            PlayMenuLoop();
        }

        private void OnGetShield(Vector2 position)
        {
            PlayCritical(Settings.ShieldGet, Settings.ShieldGet.length, 2);
        }

        private void OnGetExtend(Vector2 position)
        {
            PlayCritical(Settings.ExtendGet, Settings.ExtendGet.length, 2);
        }

        private void OnGetPower(Vector2 position)
        {
            PlayCritical(Settings.PowerGet, Settings.PowerGet.length, 2);
        }

        private void OnGetBomb(Vector2 position)
        {
            PlayCritical(Settings.BombGet, Settings.BombGet.length, 2);
        }

        private void OnItemScore(Vector2 position)
        {
            PlayCritical(Settings.ItemScore, Settings.ItemScore.length, 2);
        }

        private IEnumerator KeepingKillCount()
        {
            _killCount++;
            yield return new WaitForSeconds(Settings.Kill.length);
            _killCount--;
        }

        private void OnKill(Vector2 position)
        {
            if (_hasKilled || _killCount > 2)
            {
                return;
            }

            _hasKilled = true;
            PlayCritical(Settings.Kill, 0.8f * Settings.Kill.length, 1);

            StartCoroutine(KeepingKillCount());
        }

        private void OnPause()
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.Pause();
            }

            _oneshotSources.ForEach(source => source.Pause());
            _musicSource.Pause();
        }

        private void OnUnpause(double lostTime)
        {
            foreach (AudioSource source in _loopSources.Keys)
            {
                source.UnPause();
            }

            _oneshotSources.ForEach(source => source.UnPause());
            _musicSource.UnPause();
        }

        private void SetMusicScale(float scale)
        {
            _musicScale = scale;
            _musicSource.volume = _musicScale * Settings.MusicVolume;
        }

        private void SetFXScale(float scale)
        {
            _fxScale = scale;
            foreach (AudioSource source in _oneshotSources)
            {
                source.volume = _fxScale;
            }

            if (!_oneshotSources[0].isPlaying)
            {
                _oneshotSources[0].PlayOneShot(Settings.Kill);
            }
        }

        private IEnumerator StageCompleting()
        {
            float delay = (float)(GameManager.BeatsBeforeEnd * GameManager.BeatLength);
            yield return new WaitForSeconds(delay);
            PlayMenuLoop();
        }

        private void OnStageComplete(bool secondLoop)
        {
            if (!secondLoop)
            {
                return;
            }

            foreach (AudioSource source in _loopSources.Keys)
            {
                source.volume = 0.0f;
            }

            StartCoroutine(StageCompleting());
        }

        private void OnEnable()
        {
            OnChangeMusic += SetMusicScale;
            OnChangeFX += SetFXScale;

            PlayerController.OnStartDie += OnStartDie;
            PlayerController.OnDeath += OnDeath;
            PlayerController.OnFireStart += OnPlayerFireStart;
            PlayerController.OnFireEnd += OnPlayerFireEnd;
            PlayerController.OnShieldDown += OnShieldDown;
            PlayerController.OnBombUse += OnBombUse;

            PlayerController.OnGetShield += OnGetShield;
            PlayerController.OnGetExtend += OnGetExtend;
            PlayerController.OnGetPower += OnGetPower;
            PlayerController.OnGetBomb += OnGetBomb;
            PlayerController.OnItemScore += OnItemScore;

            EnemyController.OnKill += OnKill;

            GameManager.OnGameOver += OnGameOver;
            GameManager.OnStageComplete += OnStageComplete;
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
            PlayerController.OnShieldDown -= OnShieldDown;
            PlayerController.OnBombUse -= OnBombUse;

            PlayerController.OnGetShield -= OnGetShield;
            PlayerController.OnGetExtend -= OnGetExtend;
            PlayerController.OnGetPower -= OnGetPower;
            PlayerController.OnGetBomb -= OnGetBomb;
            PlayerController.OnItemScore -= OnItemScore;

            EnemyController.OnKill -= OnKill;

            GameManager.OnGameOver -= OnGameOver;
            GameManager.OnStageComplete -= OnStageComplete;
            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
            GameManager.OnHitHurt -= OnHitHurt;
            GameManager.OnHitShield -= OnHitShield;
        }
    }
}
