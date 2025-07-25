using System;
using System.Collections;
using UnityEngine;

namespace UFO
{
    public class PlayerController : MonoBehaviour, IKillable
    {
        public static PlayerController Instance { get; private set; }

        public PlayerSettings Settings;

        public Transform PowerLevels;

        public static Action OnTick;
        public static Action OnSpawn, OnStartDie, OnDeath, OnGameOver, OnFireStart, OnFireEnd, OnBombUse;
        public static Action OnGetShield, OnGetBomb, OnGetPower, OnGetExtend, OnItemScore;
        public static Action<Vector2, bool> OnMove;

        [HideInInspector]
        public Collider2D Hitbox;

        public bool IsShielded;

        [Min(0)]
        public int ExtendCount = 3; // NB: The initial spawn consumes an extend, so player is allowed 3 deaths before game over.

        [Range(0, 5)]
        public int BombCount = 3;

        [Range(0, 4)]
        public int PowerCount;

        private ShotEmitter[][] _emitters;
        private bool _isFiring;
        private int _shotTimeFrames;

        private TrailRenderer[] _trails;

        private Coroutine _dying;
        private bool _isSpawning, _isInvincible, _isDying;

        public bool IsInvincible { get => _isSpawning || _isInvincible || _isDying; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _emitters = new ShotEmitter[PowerLevels.childCount][];
            for (int i = 0; i < PowerLevels.childCount; i++)
            {
                _emitters[i] = PowerLevels.GetChild(i).GetComponentsInChildren<ShotEmitter>();
                foreach (ShotEmitter emitter in _emitters[i])
                {
                    emitter.Init();
                }
            }

            _trails = GetComponentsInChildren<TrailRenderer>();
            Hitbox = GetComponentInChildren<Collider2D>();
            gameObject.SetActive(false);
        }

        private IEnumerator Spawning()
        {
            _isSpawning = _isInvincible = true;

            foreach (TrailRenderer trail in _trails)
            {
                trail.Clear();
            }

            Vector2 start = new Vector2(0.0f, -GameManager.ScreenHalfHeight - 1.0f);
            Vector2 end = new Vector2(0.0f, GameManager.CutoffHeight);

            double duration = Settings.SpawnBeats * GameManager.BeatLength;
            double endTime = UnityEngine.AudioSettings.dspTime + duration;
            for (double time = UnityEngine.AudioSettings.dspTime; time < endTime; time = UnityEngine.AudioSettings.dspTime)
            {
                transform.position = Vector2.Lerp(end, start, (float)((endTime - time) / duration));
                yield return null;
            }

            transform.position = end;
            _isSpawning = false;

            StartCoroutine(ApplyingInvincibility());
        }

        public void Spawn(int extends)
        {
            gameObject.SetActive(true);

            ExtendCount = extends;
            BombCount = 3;
            PowerCount = 0;

            StartCoroutine(Spawning());

            OnSpawn?.Invoke();
        }

        public void Spawn()
        {
            Spawn(ExtendCount - 1);
        }

        private IEnumerator ApplyingInvincibility()
        {
            _isInvincible = true;
            yield return new WaitForSeconds(Settings.InvincibilityDuration);
            _isInvincible = false;
        }
        
        private IEnumerator Dying()
        {
            OnStartDie?.Invoke();

            _isDying = true;
            yield return new WaitForSeconds(Settings.BombSaveDuration);
            _isDying = false;

            OnDeath?.Invoke();
            if (ExtendCount == 0)
            {
                ExtendCount = 3;
                gameObject.SetActive(false);

                OnGameOver?.Invoke();
                yield break;
            }

            Spawn();
        }

        public void TryDie()
        {
            if (_isInvincible || _isDying)
            {
                return;
            }

            if (IsShielded)
            {
                StartCoroutine(ApplyingInvincibility());
                IsShielded = false;
                return;
            }

            _dying = StartCoroutine(Dying());
        }

        public bool TryKill(EnemyController enemy)
        {
            if (!_isFiring)
            {
                return false;
            }

            Collider2D[] results = new Collider2D[16];
            ContactFilter2D filter = new ContactFilter2D();

            filter.SetLayerMask(GameManager.ShieldMask);
            int count = Physics2D.OverlapCollider(Hitbox, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i].GetComponentInParent<EnemyController>() != enemy)
                {
                    continue;
                }

                GameManager.OnHitShield?.Invoke(results[i].ClosestPoint(Hitbox.transform.position));
                return false;
            }

            filter.SetLayerMask(GameManager.HurtMask);
            count = Physics2D.OverlapCollider(Hitbox, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i].GetComponentInParent<EnemyController>() != enemy)
                {
                    continue;
                }

                GameManager.OnHitHurt?.Invoke(results[i].ClosestPoint(Hitbox.transform.position));
                return enemy.TryDie(Settings.HitboxDamage);
            }

            return false;
        }

        public void GetShield()
        {
            if (IsShielded)
            {
                // TODO: score points when exceeding item limits.
                OnItemScore?.Invoke();
                return;
            }

            IsShielded = true;
            OnGetShield?.Invoke();
        }

        public void GetBomb()
        {
            if (BombCount == Settings.BombLimit)
            {
                OnItemScore?.Invoke();
                return;
            }

            BombCount++;
            OnGetBomb?.Invoke();
        }

        public void GetPower()
        {
            if (PowerCount == _emitters.Length - 1)
            {
                OnItemScore?.Invoke();
                return;
            }

            PowerCount++;
            OnGetPower?.Invoke();
        }

        public void GetExtend()
        {
            ExtendCount++;
            OnGetExtend?.Invoke();
        }

        private void HandleMovement(float deltaTime)
        {
            Vector2 move = Vector2.zero;
            if (Settings.LeftAction.IsPressed())
            {
                move.x -= 1.0f;
            }

            if (Settings.RightAction.IsPressed())
            {
                move.x += 1.0f;
            }

            if (Settings.UpAction.IsPressed())
            {
                move.y += 1.0f;
            }

            if (Settings.DownAction.IsPressed())
            {
                move.y -= 1.0f;
            }

            move.Normalize();
            move *= deltaTime * (_isFiring ? Settings.SlowSpeed : Settings.FastSpeed);

            Vector3 oldPos = transform.position;
            transform.position += (Vector3)move;
            transform.position = new Vector2(Mathf.Clamp(transform.position.x, -GameManager.ScreenHalfWidth, GameManager.ScreenHalfWidth),
                Mathf.Clamp(transform.position.y, -GameManager.ScreenHalfHeight, GameManager.ScreenHalfHeight));

            OnMove?.Invoke(transform.position - oldPos, _isFiring);
        }

        private void HandleFiring()
        {
            if (Settings.FireAction.IsPressed())
            {
                _shotTimeFrames = Settings.ShotTimeBuffer;
            }

            bool wasFiring = _isFiring;
            ShotEmitter.Tick(_emitters[PowerCount], ref _isFiring, ref _shotTimeFrames);
            if (_isFiring)
            {
                if (!wasFiring)
                {
                    OnFireStart?.Invoke();
                }
            }
            else if (wasFiring)
            {
                OnFireEnd?.Invoke();
            }
        }

        private void HandleBombing()
        {
            if (BombCount == 0)
            {
                return;
            }

            if (!Settings.BombAction.WasPerformedThisFrame())
            {
                return;
            }

            if (_dying != null)
            {
                StopCoroutine(_dying);
            }

            _isDying = false;
            BombCount--;
            OnBombUse?.Invoke();
        }

        public bool CheckPause()
        {
            return Settings.PauseAction.WasPerformedThisFrame();
        }

        public bool CheckRestart()
        {
            return Settings.RestartAction.WasPerformedThisFrame();
        }

        public void Tick(float deltaTime)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
                return;
            }

            if (_isSpawning)
            {
                HandleFiring();
                OnTick?.Invoke();
                return;
            }

            if (_isDying)
            {
                HandleBombing();
                OnTick?.Invoke();
                return;
            }

            HandleMovement(deltaTime);
            HandleFiring();
            HandleBombing();

            OnTick?.Invoke();
        }

        private void OnEnable()
        {
            Settings.LeftAction.Enable();
            Settings.RightAction.Enable();
            Settings.UpAction.Enable();
            Settings.DownAction.Enable();
            Settings.FireAction.Enable();
            Settings.BombAction.Enable();

            Settings.PauseAction.Enable();
            Settings.RestartAction.Enable();
        }

        private void OnDisable()
        {
            Settings.LeftAction.Disable();
            Settings.RightAction.Disable();
            Settings.UpAction.Disable();
            Settings.DownAction.Disable();
            Settings.FireAction.Disable();
            Settings.BombAction.Disable();

            Settings.PauseAction.Disable();
            Settings.RestartAction.Disable();
        }
    }
}
