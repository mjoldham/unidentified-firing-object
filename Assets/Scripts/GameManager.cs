using UnityEngine;
using System.Collections.Generic;
using System;
using static UFO.ShotEmitter;
using static UFO.ShotController;
using System.Linq;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private AudioManager _audio;
        private PlayerController _player;

        public GameObject _mainMenuUI;

        public bool _playing = false;

        public const int BeatsPerBar = 4;
        public const int NumLanes = 5;
        public const float ScreenHalfWidth = 3.5f;
        public const float ScreenHalfHeight = 3.5f;
        public const float CutoffHeight = -ScreenHalfHeight + 1.0f;

        public static int HitLayer { get; private set; }
        public static int HurtLayer { get; private set; }
        public static int ShieldLayer { get; private set; }

        public static int HitMask { get; private set; }
        public static int HurtMask { get; private set; }
        public static int ShieldMask { get; private set; }

        public static Action OnBeat, OnPause;
        public static Action<double> OnUnpause;
        public static Action<Vector2> OnHitHurt, OnHitShield;

        public static bool IsOnBeat { get; private set; }
        public static double BeatLength { get; private set; }
        public static double BarLength { get; private set; }

        public SpriteRenderer BackgroundRenderer;

        public StageSettings[] Stages;
        private int _spawnIndex;

        private Material _bgMaterial;
        private int _scrollID = Shader.PropertyToID(string.Concat("_", nameof(StageSettings.ScrollSpeed)));

        public Color ShotColour, ShotPlusColour;
        public MeshRenderer BombEffect;
        public int BombFrames = 50;
        private int _bombFrames;

        public static readonly int NormTimeID = Shader.PropertyToID("_NormalisedTime");
        public static readonly int ColourID = Shader.PropertyToID("_Colour");

        public class EffectPool
        {
            public int MaxCount { get; private set; }
            public int Frames { get; private set; }

            private Queue<MeshRenderer> _inactiveFX;
            private Queue<(MeshRenderer, int)> _activeFX;

            public EffectPool(int maxCount, int frames, GameObject hitPrefab, Transform parent)
            {
                MaxCount = maxCount;
                Frames = frames;

                _inactiveFX = new Queue<MeshRenderer>();
                _activeFX = new Queue<(MeshRenderer, int)>();

                for (int i = 0; i < MaxCount; i++)
                {
                    MeshRenderer fx = Instantiate(hitPrefab, parent).GetComponent<MeshRenderer>();
                    fx.gameObject.SetActive(false);
                    _inactiveFX.Enqueue(fx);
                }
            }

            public void Spawn(Vector2 position)
            {
                MeshRenderer fx;
                if (_inactiveFX.Count == 0)
                {
                    fx = _activeFX.Dequeue().Item1;
                }
                else
                {
                    fx = _inactiveFX.Dequeue();
                }

                fx.gameObject.SetActive(true);
                fx.transform.position = position;
                fx.material.SetFloat(NormTimeID, 0.0f);

                _activeFX.Enqueue((fx, Frames));
            }

            public void Tick()
            {
                int count = _activeFX.Count;
                for (int i = 0; i < count; i++)
                {
                    (MeshRenderer fx, int frames) = _activeFX.Dequeue();
                    if (--frames > 0)
                    {
                        fx.material.SetFloat(NormTimeID, (float)(Frames - frames) / Frames);
                        _activeFX.Enqueue((fx, frames));
                    }
                    else
                    {
                        fx.gameObject.SetActive(false);
                        _inactiveFX.Enqueue(fx);
                    }
                }
            }

            public void Clear()
            {
                int count = _activeFX.Count;
                for (int i = 0; i < count; i++)
                {
                    (MeshRenderer fx, int _) = _activeFX.Dequeue();
                    fx.gameObject.SetActive(false);
                    _inactiveFX.Enqueue(fx);
                }
            }
        }

        public GameObject HitEffectPrefab;
        public int HitEffectFrames = 50;
        private EffectPool _hitFXPool;

        public GameObject KillEffectPrefab;
        public int KillFrames = 25;
        private EffectPool _killFXPool;

        public class ShotPool : IKillable
        {
            public int MaxCount { get; private set; }

            private Queue<ShotController> _inactiveShots;
            private Queue<ShotController> _activeShots;

            public ShotPool(int maxCount, ShotController baseShot, Transform parent)
            {
                MaxCount = maxCount;

                _inactiveShots = new Queue<ShotController>();
                _activeShots = new Queue<ShotController>();

                for (int i = 0; i < MaxCount; i++)
                {
                    ShotController shot = Instantiate(baseShot, parent);
                    shot.Init();
                    _inactiveShots.Enqueue(shot);
                }
            }

            public bool Spawn(ShotParams shotParams, Vector2 position, int angle)
            {
                if (_inactiveShots.Count == 0)
                {
                    return false;
                }

                ShotController shot = _inactiveShots.Dequeue();
                shot.Spawn(shotParams, position, angle);
                _activeShots.Enqueue(shot);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotController shot = _activeShots.Dequeue();
                    if (shot.Tick(deltaTime))
                    {
                        _activeShots.Enqueue(shot);
                    }
                    else
                    {
                        _inactiveShots.Enqueue(shot);
                    }
                }
            }

            // Returns true if enemy is dead or not.
            public bool TryKill(EnemyController enemy)
            {
                bool isHit = false;
                int damage = 0;
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotController shot = _activeShots.Dequeue();
                    if (shot.TryDamage(enemy, ref damage))
                    {
                        isHit = true;
                        _inactiveShots.Enqueue(shot);
                    }
                    else
                    {
                        _activeShots.Enqueue(shot);
                    }
                }

                return isHit && enemy.TryDie(damage);
            }

            public void TryKill(PlayerController player)
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotController shot = _activeShots.Dequeue();
                    if (shot.TryDamage(player))
                    {
                        _inactiveShots.Enqueue(shot);
                        player.TryDie();

                        OnHitHurt?.Invoke(shot.transform.position);
                    }
                    else
                    {
                        _activeShots.Enqueue(shot);
                    }
                }
            }

            public void Clear()
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotController shot = _activeShots.Dequeue();
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);

                    OnHitHurt?.Invoke(shot.transform.position);
                }
            }
        }

        public ShotController BaseShotPrefab;
        public int PlayerShotMaxCount = 32, EnemyShotMaxCount = 256;
        private ShotPool _playerShotPool, _enemyFriendlyPool, _enemyShotPool;

        [Serializable]
        public class EnemyPool
        {
            public int MaxCount;
            public EnemyController EnemyPrefab;

            private Queue<EnemyController> _inactiveEnemies;
            private Queue<EnemyController> _activeEnemies;

            public int Count { get => _activeEnemies.Count; }

            public void Init(Transform parent)
            {
                _inactiveEnemies = new Queue<EnemyController>();
                _activeEnemies = new Queue<EnemyController>();

                for (int i = 0; i < MaxCount; i++)
                {
                    EnemyController enemy = Instantiate(EnemyPrefab, parent);
                    enemy.Init();
                    _inactiveEnemies.Enqueue(enemy);
                }
            }

            public bool Spawn(SpawnInfo spawnInfo)
            {
                if (_inactiveEnemies.Count == 0)
                {
                    return false;
                }

                EnemyController enemy = _inactiveEnemies.Dequeue();
                enemy.Spawn(spawnInfo);
                _activeEnemies.Enqueue(enemy);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (enemy.Tick(deltaTime))
                    {
                        _activeEnemies.Enqueue(enemy);
                    }
                    else
                    {
                        _inactiveEnemies.Enqueue(enemy);
                    }
                }
            }

            // Checks if any killers damage any enemies, and if any enemies damage the player.
            public void CheckCollisions(params IKillable[] killers)
            {
                PlayerController player = killers.First(killer => killer is PlayerController) as PlayerController;

                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (Mathf.Abs(enemy.transform.position.x) > ScreenHalfWidth || Mathf.Abs(enemy.transform.position.y) > ScreenHalfHeight)
                    {
                        _activeEnemies.Enqueue(enemy);
                        continue;
                    }

                    if (killers.Any(killer => killer.TryKill(enemy)))
                    {
                        _inactiveEnemies.Enqueue(enemy);
                        continue;
                    }

                    _activeEnemies.Enqueue(enemy);
                    enemy.TryDamage(player);
                }
            }

            public void Clear()
            {
                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    enemy.gameObject.SetActive(false);
                    _inactiveEnemies.Enqueue(enemy);
                }
            }

            public void ApplyBomb(Vector3 source)
            {
                int count = _activeEnemies.Count;
                float heightSqr = ScreenHalfHeight * ScreenHalfHeight;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (Mathf.Abs(enemy.transform.position.x) > ScreenHalfWidth || Mathf.Abs(enemy.transform.position.y) > ScreenHalfHeight)
                    {
                        _activeEnemies.Enqueue(enemy);
                        continue;
                    }

                    OnHitHurt?.Invoke(enemy.transform.position);

                    int damage = (int)Mathf.Lerp(100, 1, (enemy.transform.position - source).sqrMagnitude / heightSqr);
                    if (!enemy.TryDie(damage))
                    {
                        _activeEnemies.Enqueue(enemy);
                    }
                    else
                    {
                        _inactiveEnemies.Enqueue(enemy);
                    }
                }
            }
        }

        public EnemyPool[] EnemyPools;
        private Dictionary<string, EnemyPool> _enemyPoolDict = new Dictionary<string, EnemyPool>();

        private int _currentStage = -1, _currentBar, _currentBeat;

        private double _nextStageTime, _nextBarTime, _nextBeatTime;

        private int _currentLoop = 1;
        private bool _isPaused;
        private double _pauseStart;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void VerifySpawnsAndEnemyPools()
        {
            foreach (StageSettings stage in Stages)
            {
                int bar = 0, beat = 0;
                for (int i = 0; i < stage.Spawns.Length; i++)
                {
                    SpawnInfo spawnInfo = stage.Spawns[i];
                    if (spawnInfo.Bar < bar || (spawnInfo.Bar == bar && spawnInfo.Beat < beat))
                    {
                        Debug.LogError($"{stage.name}'s Spawns[{i}] is out of order, needs to be placed earlier in list.", stage);
                        return;
                    }
                    
                    if (!_enemyPoolDict.ContainsKey(spawnInfo.EnemyPrefab.gameObject.name))
                    {
                        Debug.LogError($"{spawnInfo.EnemyPrefab.gameObject.name} has not been included in the GameManagers list of EnemyPools.");
                        return;
                    }

                    bar = spawnInfo.Bar;
                    beat = spawnInfo.Beat;
                }
            }
        }

        void Start()
        {
            _audio = GetComponent<AudioManager>();
            _player = PlayerController.Instance;
            _bgMaterial = BackgroundRenderer.material;

            HitLayer = LayerMask.NameToLayer(nameof(EnemyController.Hitboxes));
            HurtLayer = LayerMask.NameToLayer(nameof(EnemyController.Hurtboxes));
            ShieldLayer = LayerMask.NameToLayer(nameof(EnemyController.Shieldboxes));

            HitMask = LayerMask.GetMask(nameof(EnemyController.Hitboxes));
            HurtMask = LayerMask.GetMask(nameof(EnemyController.Hurtboxes));
            ShieldMask = LayerMask.GetMask(nameof(EnemyController.Shieldboxes));

            BombEffect.gameObject.SetActive(false);

            _playerShotPool = new ShotPool(PlayerShotMaxCount, BaseShotPrefab, transform);
            _enemyFriendlyPool = new ShotPool(EnemyShotMaxCount, BaseShotPrefab, transform);
            _enemyShotPool = new ShotPool(EnemyShotMaxCount, BaseShotPrefab, transform);

            _hitFXPool = new EffectPool(PlayerShotMaxCount, HitEffectFrames, HitEffectPrefab, transform);
            _killFXPool = new EffectPool(PlayerShotMaxCount, KillFrames, KillEffectPrefab, transform);

            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Init(transform);
                _enemyPoolDict[pool.EnemyPrefab.gameObject.name] = pool;
            }

            VerifySpawnsAndEnemyPools();
            //_nextStageTime = UnityEngine.AudioSettings.dspTime + 1.0;
        }

        private double CalculateNextStageTime(double startTime, AudioClip currentClip)
        {
            return startTime + (double)currentClip.samples / currentClip.frequency;
        }

        private void StartNextStage()
        {
            IsOnBeat = false;
            if (++_currentStage == Stages.Length)
            {
                _currentStage = 0;
                _currentLoop++;
            }

            _spawnIndex = 0;

            StageSettings stage = Stages[_currentStage];
            BeatLength = 60.0 / stage.BPM;
            BarLength = BeatsPerBar * BeatLength;

            double startTime = UnityEngine.AudioSettings.dspTime + _player.Settings.SpawnBeats * BeatLength;
            _audio.Play(stage.MusicTrack, startTime);

            _currentBar = -1;

            _nextStageTime = CalculateNextStageTime(startTime, stage.MusicTrack);
            _nextBeatTime = _nextBarTime = startTime;

            BackgroundRenderer.sprite = stage.Background;
            _bgMaterial.SetFloat(_scrollID, stage.ScrollSpeed);

            if (!_player.gameObject.activeSelf)
            {
                _player.Spawn();
            }
        }

        private void ClearScreen()
        {
            _playerShotPool.Clear();
            _enemyFriendlyPool.Clear();
            _enemyShotPool.Clear();

            _hitFXPool.Clear();
            _killFXPool.Clear();

            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Clear();
            }
        }

        public void RestartStage()
        {
            ClearScreen();
            _currentStage--;

            StartNextStage();
        }

        private void TrySpawningEnemies(SpawnInfo[] spawns)
        {
            Debug.Log($"Bar: {_currentBar},\tBeat: {_currentBeat}");
            if (_spawnIndex == spawns.Length)
            {
                return;
            }

            SpawnInfo spawnInfo = spawns[_spawnIndex];
            while (spawnInfo.Bar == _currentBar && spawnInfo.Beat == _currentBeat)
            {
                EnemyPool pool = _enemyPoolDict[spawnInfo.EnemyPrefab.gameObject.name];
                if (!spawnInfo.CheckForEnemies || pool.Count == 0)
                {
                    if (!pool.Spawn(spawnInfo))
                    {
                        Debug.Log($"Too many {spawnInfo.EnemyPrefab.gameObject.name} active to spawn more!");
                    }
                }

                if (++_spawnIndex == spawns.Length)
                {
                    break;
                }

                spawnInfo = spawns[_spawnIndex];
            }
        }

        private void StartNextBar()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _currentBar++;
            _currentBeat = -1;
            _nextBarTime += BarLength;

            StartNextBeat();
        }

        private void StartNextBeat()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _currentBeat++;
            _nextBeatTime += BeatLength;
            TrySpawningEnemies(Stages[_currentStage].Spawns);

            IsOnBeat = true;
            OnBeat?.Invoke();
        }

        private int AngleToTarget(ShotController.TargetType target, Vector2 position)
        {
            if (target == ShotController.TargetType.Enemy)
            {
                Debug.LogError("The player can't have an aimed shot!");
                return 0;
            }

            return (int)Vector2.SignedAngle(Vector2.down, ((Vector2)_player.transform.position - position).normalized);
        }

        public bool SpawnShot(ShotMode mode, ShotParams shotParams, Vector2 position, ref int angle)
        {
            ShotPool pool;
            switch (shotParams.Target)
            {
                case TargetType.Player:
                    pool = _enemyShotPool;
                    break;
                case TargetType.Enemy:
                    pool = _playerShotPool;
                    break;
                case TargetType.Both:
                    pool = _enemyFriendlyPool;
                    break;
                default:
                    Debug.LogError($"{shotParams.Target} not accounted for.");
                    return false;
            }

            if (mode == ShotMode.Static)
            {
                return pool.Spawn(shotParams, position, angle);
            }
            
            if (mode == ShotMode.Random)
            {
                angle = UnityEngine.Random.Range(-angle, angle + 1);
            }

            angle = Wrap(angle + AngleToTarget(shotParams.Target, position));
            return pool.Spawn(shotParams, position, angle);
        }

        private void OnBombUse()
        {
            _playerShotPool.Clear();
            _enemyFriendlyPool.Clear();
            _enemyShotPool.Clear();

            _hitFXPool.Clear();
            _killFXPool.Clear();

            foreach (EnemyPool pool in EnemyPools)
            {
                pool.ApplyBomb(_player.transform.position);
            }

            _bombFrames = BombFrames;

            BombEffect.transform.position = _player.transform.position;
            BombEffect.material.SetFloat(NormTimeID, 0.0f);
            BombEffect.material.SetColor(ColourID, ShotPlusColour);

            BombEffect.gameObject.SetActive(true);
        }

        private void OnGameOver()
        {
            RestartStage();
        }

        private void Unpause()
        {
            _isPaused = false;

            double lostTime = UnityEngine.AudioSettings.dspTime - _pauseStart;
            _pauseStart = 0.0;

            _nextStageTime += lostTime;
            _nextBarTime += lostTime;
            _nextBeatTime += lostTime;

            OnUnpause?.Invoke(lostTime);
        }

        private void BombTick()
        {
            if (!BombEffect.gameObject.activeSelf)
            {
                return;
            }

            if (--_bombFrames <= 0)
            {
                BombEffect.gameObject.SetActive(false);
                return;
            }

            float t = (float)(BombFrames - _bombFrames) / BombFrames;
            BombEffect.material.SetFloat(NormTimeID, t);

            float step = 3.0f;
            BombEffect.material.SetColor(ColourID, Vector4.Lerp(ShotPlusColour, ShotColour, Mathf.Round(t * step) / step));
        }

        void FixedUpdate()
        {
            if (!_playing)
            {
                return;
            }

            // Get effects out of the way.
            _hitFXPool.Tick();
            _killFXPool.Tick();
            BombTick();

            if (_player.CheckRestart())
            {
                Unpause();

                _player.ExtendCount = 3;
                _player.gameObject.SetActive(false);
                RestartStage();
                return;
            }

            _isPaused = _isPaused ? !_player.CheckPause() : _player.CheckPause();
            if (_isPaused)
            {
                if (_pauseStart == 0.0)
                {
                    _pauseStart = UnityEngine.AudioSettings.dspTime;
                    OnPause?.Invoke();
                }

                return;
            }

            if (_pauseStart > 0.0)
            {
                Unpause();
            }

            // Manages timeline.
            double time = UnityEngine.AudioSettings.dspTime;
            if (time >= _nextStageTime)
            {
                StartNextStage();
            }
            else if (time >= _nextBarTime)
            {
                StartNextBar();
            }
            else if (time >= _nextBeatTime)
            {
                StartNextBeat();
            }
            else
            {
                IsOnBeat = false;
            }

            // Updates enemy movement and firing.
            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Tick(Time.fixedDeltaTime);
            }

            // Updates player movement and firing.
            _player.Tick(Time.fixedDeltaTime);

            // Moves shots.
            _playerShotPool.Tick(Time.fixedDeltaTime);
            _enemyFriendlyPool.Tick(Time.fixedDeltaTime);
            _enemyShotPool.Tick(Time.fixedDeltaTime);

            // Checks shot collisions and player hitbox against enemies first. If they survive then enemy hitboxes are checked against the player.
            foreach (EnemyPool pool in EnemyPools)
            {
                pool.CheckCollisions(_enemyFriendlyPool, _playerShotPool, _player);
            }

            // Then checks shots against the player.
            _enemyShotPool.TryKill(_player);
            _enemyFriendlyPool.TryKill(_player);
        }

        private void SpawnHitFX(Vector2 position)
        {
            _hitFXPool.Spawn(position);
        }

        private void SpawnKillFX(Vector2 position)
        {
            _killFXPool.Spawn(position);
        }

        private void OnEnable()
        {
            OnHitHurt += SpawnHitFX;
            OnHitShield += SpawnHitFX;

            PlayerController.OnGameOver += OnGameOver;
            PlayerController.OnBombUse += OnBombUse;

            EnemyController.OnKill += SpawnKillFX;
        }

        private void OnDisable()
        {
            OnHitHurt -= SpawnHitFX;
            OnHitShield -= SpawnHitFX;

            PlayerController.OnGameOver -= OnGameOver;
            PlayerController.OnBombUse -= OnBombUse;

            EnemyController.OnKill -= SpawnKillFX;
        }

        public void StartGame()
        {
            if (_currentStage == -1)
            {
                if (_playing) return;
                _playing = true;

                double startTime = UnityEngine.AudioSettings.dspTime + 1.0;
                _nextStageTime = startTime;
            }
        }
    }

    public interface IKillable
    {
        public bool TryKill(EnemyController enemy);
    }
}
