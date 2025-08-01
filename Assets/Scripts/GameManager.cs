using UnityEngine;
using System.Collections.Generic;
using System;
using static UFO.ShotEmitter;
using static UFO.ShotController;
using System.Linq;
using System.Collections;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private AudioManager _audio;
        private PlayerController _player;

        private bool _gameRunning;

        public const int BeatsPerBar = 4;
        public const int NumLanes = 5;
        public const float ScreenHalfWidth = 3.5f;
        public const float ScreenHalfHeight = 3.5f;
        public const float CutoffHeight = -ScreenHalfHeight + 1.0f;
        public const float GameOverDuration = 3.0f;

        public static int ItemScoreBase = 100, ExtendScore = 250000;

        public static int StartingExtends { get; private set; }

        public static int HitLayer { get; private set; }
        public static int HurtLayer { get; private set; }
        public static int ShieldLayer { get; private set; }

        public static int HitMask { get; private set; }
        public static int HurtMask { get; private set; }
        public static int ShieldMask { get; private set; }

        public static Action OnGameOver, OnGameEnd, OnBeat, OnPause;
        public static Action<int> OnGameStart;
        public static Action<float> OnChangeScroll;
        public static Action<double> OnUnpause;
        public static Action<Vector2> OnHitHurt, OnHitShield;

        public static bool IsOnBeat { get; private set; }
        public static double BeatLength { get; private set; }
        public static double BarLength { get; private set; }

        public static int CurrentScore { get; private set; }
        private static int _extendCounter;

        public static void AddScore(int points)
        {
            _extendCounter -= points;
            CurrentScore += points;
            if (_extendCounter <= 0)
            {
                _extendCounter = ExtendScore;
                PowerupController.SpawnExtend = true;
            }

            if (CurrentScore > Hiscore)
            {
                Hiscore = CurrentScore;
            }
        }

        public static void AddScore(float damage)
        {
            AddScore((int)(100.0f * damage));
        }

        [HideInInspector]
        public static int Hiscore { get; private set; }

        public static int ItemScoreCount;

        public SpriteRenderer BackgroundRenderer;

        public StageSettings[] Stages;
        private int _spawnIndex;

        private Material _bgMaterial;
        private int _scrollID = Shader.PropertyToID(string.Concat("_", nameof(StageSettings.ScrollSpeed)));
        private float _scrollScale = 1.0f;

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
                    if (_activeFX.Count == 0)
                    {
                        break;
                    }

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
                    if (_activeFX.Count == 0)
                    {
                        break;
                    }

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

            public bool Spawn(ShotParams shotParams, float damage, Vector2 position, int angle)
            {
                if (_inactiveShots.Count == 0)
                {
                    return false;
                }

                ShotController shot = _inactiveShots.Dequeue();
                shot.Spawn(shotParams, damage, position, angle);
                _activeShots.Enqueue(shot);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    if (_activeShots.Count == 0)
                    {
                        break;
                    }

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
                float damage = 0.0f;
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    if (_activeShots.Count == 0)
                    {
                        break;
                    }

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
                    if (_activeShots.Count == 0)
                    {
                        break;
                    }

                    ShotController shot = _activeShots.Dequeue();
                    if (shot.TryDamage(player))
                    {
                        _inactiveShots.Enqueue(shot);
                        player.TryDie();
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
                    if (_activeShots.Count == 0)
                    {
                        break;
                    }

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

            public int CheckCount { get; private set; }

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

                if (!enemy.Parameters.ExemptFromCheck)
                {
                    CheckCount++;
                }
                
                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    if (_activeEnemies.Count == 0)
                    {
                        break;
                    }

                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (enemy.Tick(deltaTime))
                    {
                        _activeEnemies.Enqueue(enemy);
                    }
                    else
                    {
                        _inactiveEnemies.Enqueue(enemy);
                        if (!enemy.Parameters.ExemptFromCheck)
                        {
                            CheckCount--;
                        }
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
                    if (_activeEnemies.Count == 0)
                    {
                        break;
                    }

                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (Mathf.Abs(enemy.transform.position.x) > ScreenHalfWidth || Mathf.Abs(enemy.transform.position.y) > ScreenHalfHeight)
                    {
                        _activeEnemies.Enqueue(enemy);
                        continue;
                    }

                    if (killers.Any(killer => killer.TryKill(enemy)))
                    {
                        _inactiveEnemies.Enqueue(enemy);
                        if (!enemy.Parameters.ExemptFromCheck)
                        {
                            CheckCount--;
                        }

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
                    if (_activeEnemies.Count == 0)
                    {
                        break;
                    }

                    EnemyController enemy = _activeEnemies.Dequeue();
                    enemy.gameObject.SetActive(false);
                    _inactiveEnemies.Enqueue(enemy);
                }

                CheckCount = 0;
            }

            public void ApplyBomb(Vector3 source, int maxDamage)
            {
                int count = _activeEnemies.Count;
                float rangeSqr = ScreenHalfWidth * ScreenHalfWidth + ScreenHalfHeight * ScreenHalfHeight;
                for (int i = 0; i < count; i++)
                {
                    if (_activeEnemies.Count == 0)
                    {
                        break;
                    }

                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (Mathf.Abs(enemy.transform.position.x) > ScreenHalfWidth || Mathf.Abs(enemy.transform.position.y) > ScreenHalfHeight)
                    {
                        _activeEnemies.Enqueue(enemy);
                        continue;
                    }

                    OnHitHurt?.Invoke(enemy.transform.position);

                    int damage = (int)Mathf.Lerp(maxDamage, 1, (enemy.transform.position - source).sqrMagnitude / rangeSqr);
                    if (!enemy.TryDie(damage))
                    {
                        _activeEnemies.Enqueue(enemy);
                    }
                    else
                    {
                        _inactiveEnemies.Enqueue(enemy);
                        if (!enemy.Parameters.ExemptFromCheck)
                        {
                            CheckCount--;
                        }
                    }
                }
            }
        }

        public EnemyPool[] EnemyPools;
        private Dictionary<string, EnemyPool> _enemyPoolDict = new Dictionary<string, EnemyPool>();

        public PowerupController PowerupPrefab;
        private PowerupController[] _powerups = new PowerupController[3];
        private int _nextPowerup;

        private int _currentStage = -1, _currentBar, _currentBeat, _totalBeats;

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

        // TODO: find a way to do this offline so each stage has Spawns ready w/o processing.
        private void InitSpawns()
        {
            foreach (StageSettings stage in Stages)
            {
                if (stage.StagePatternPrefab == null)
                {
                    continue;
                }

                BaseSpawnable[] spawns = stage.StagePatternPrefab.GetComponentsInChildren<BaseSpawnable>();
                if (spawns.Length == 0)
                {
                    continue;
                }

                Array.Sort(spawns, (s1, s2) => s1.transform.position.y.CompareTo(s2.transform.position.y));

                stage.Spawns = new SpawnInfo[spawns.Length];
                for (int i = 0; i < spawns.Length; i++)
                {
                    stage.Spawns[i] = new SpawnInfo(spawns[i]);
                    if (spawns[i] is EnemyController && !_enemyPoolDict.ContainsKey(stage.Spawns[i].PrefabName))
                    {
                        Debug.LogError($"{name} has not been included in the GameManagers list of EnemyPools.");
                        return;
                    }
                }
            }
        }

        void Start()
        {
            _audio = GetComponent<AudioManager>();
            _player = PlayerController.Instance;

            BackgroundRenderer.sprite = Stages[0].Background;
            _bgMaterial = BackgroundRenderer.material;
            _bgMaterial.SetFloat(_scrollID, _scrollScale * Stages[0].ScrollSpeed);

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

            for (int i = 0; i < _powerups.Length; i++)
            {
                _powerups[i] = Instantiate(PowerupPrefab);
                _powerups[i].Init();
            }

            InitSpawns();
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

            _totalBeats = _currentBar = 0;

            _nextStageTime = CalculateNextStageTime(startTime, stage.MusicTrack);
            _nextBeatTime = _nextBarTime = startTime;

            BackgroundRenderer.sprite = stage.Background;
            _bgMaterial.SetFloat(_scrollID, _scrollScale * stage.ScrollSpeed);

            if (!_player.IsAlive)
            {
                _player.Spawn(StartingExtends);
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

            foreach (PowerupController powerup in _powerups)
            {
                powerup.gameObject.SetActive(false);
            }
        }

        public void RestartStage()
        {
            ClearScreen();
            _currentStage--;
            CurrentScore = ItemScoreCount = 0;
            _extendCounter = ExtendScore;

            _player.IsAlive = false;
            StartNextStage();
        }

        private void TrySpawning(SpawnInfo[] spawns)
        {
            Debug.Log($"Total: {_totalBeats},\tBar: {_currentBar},\tBeat: {_currentBeat}");
            if (_spawnIndex == spawns.Length)
            {
                return;
            }

            for (SpawnInfo spawnInfo = spawns[_spawnIndex]; spawnInfo.Beat == _totalBeats; spawnInfo = spawns[_spawnIndex])
            {
                if (!spawnInfo.Parameters.CheckForEnemies || EnemyPools.All(pool => pool.CheckCount == 0))
                {
                    if (_enemyPoolDict.TryGetValue(spawnInfo.PrefabName, out EnemyPool pool))
                    {
                        if (!pool.Spawn(spawnInfo))
                        {
                            Debug.Log($"Too many {spawnInfo.PrefabName}s active to spawn more!");
                        }
                    }
                    else
                    {
                        _powerups[_nextPowerup].Spawn(spawnInfo);
                        _nextPowerup = (_nextPowerup + 1) % _powerups.Length;
                    }
                }

                if (++_spawnIndex == spawns.Length)
                {
                    break;
                }
            }
        }

        private void StartNextBar()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _currentBar++;
            _currentBeat = 0;
            _nextBarTime += BarLength;

            StartNextBeat();
        }

        private void StartNextBeat()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _totalBeats++;
            _currentBeat++;
            _nextBeatTime += BeatLength;

            TrySpawning(Stages[_currentStage].Spawns);

            IsOnBeat = true;
            OnBeat?.Invoke();
        }

        public static int RoundToNearest(int value, int nearest)
        {
            return nearest * Mathf.RoundToInt((float)value / nearest);
        }

        public static int AngleToTarget(Vector2 position, Vector2 target)
        {
            return Mathf.RoundToInt(Vector2.SignedAngle(Vector2.down, (target - position).normalized));
        }

        public int AngleToTarget(TargetType target, Vector2 position)
        {
            if (target == TargetType.Enemy)
            {
                Debug.LogError("The player can't have an aimed shot!");
                return 0;
            }

            return (int)Vector2.SignedAngle(Vector2.down, ((Vector2)_player.transform.position - position).normalized);
        }

        public bool SpawnShot(ShotMode mode, ShotParams shotParams, float damage, Vector2 position, ref int angle)
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
                return pool.Spawn(shotParams, damage, position, angle);
            }
            
            if (mode == ShotMode.Random)
            {
                angle = UnityEngine.Random.Range(-angle, angle + 1);
            }

            angle = Wrap(angle + AngleToTarget(shotParams.Target, position));
            return pool.Spawn(shotParams, damage, position, angle);
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
                pool.ApplyBomb(_player.transform.position, _player.Settings.BombMaxDamage);
            }

            _bombFrames = BombFrames;

            BombEffect.transform.position = _player.transform.position;
            BombEffect.material.SetFloat(NormTimeID, 0.0f);
            BombEffect.material.SetColor(ColourID, ShotPlusColour);

            BombEffect.gameObject.SetActive(true);
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
            if (!_gameRunning)
            {
                return;
            }

            // Get effects out of the way.
            _hitFXPool.Tick();
            _killFXPool.Tick();
            BombTick();

            if (_player.IsAlive && !_player.IsSpawning && !_player.IsDying)
            {
                if (_player.CheckEnd())
                {
                    if (_pauseStart > 0.0)
                    {
                        Unpause();
                    }

                    OnGameOver?.Invoke();
                    return;
                }

                if (_player.CheckRestart())
                {
                    if (_pauseStart > 0.0)
                    {
                        Unpause();
                    }

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

            // Updates any active powerups.
            foreach (PowerupController powerup in _powerups)
            {
                if (powerup.gameObject.activeSelf)
                {
                    powerup.Tick(_player, Time.fixedDeltaTime);
                }
            }

            // Moves shots.
            _playerShotPool.Tick(Time.fixedDeltaTime);
            _enemyFriendlyPool.Tick(Time.fixedDeltaTime);
            _enemyShotPool.Tick(Time.fixedDeltaTime);

            if (!_player.IsAlive)
            {
                return;
            }

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

        public void StartGame(int startingExtends)
        {
            _gameRunning = true;

            StartingExtends = startingExtends;
            CurrentScore = ItemScoreCount = 0;
            _extendCounter = ExtendScore;
            _currentStage = -1;
            StartNextStage();
        }

        private IEnumerator GameOvering()
        {
            _gameRunning = false;
            yield return new WaitForSeconds(GameOverDuration);

            ClearScreen();
            OnGameEnd?.Invoke();
        }

        private void GameOver()
        {
            StartCoroutine(GameOvering());
        }

        private void OnItemScore(Vector2 position)
        {
            AddScore(ItemScoreBase << ItemScoreCount);
            if (ItemScoreCount < 8)
            {
                ItemScoreCount++;
            }
        }

        private void ChangeScrollSpeed(float scale)
        {
            _scrollScale = scale;
            _bgMaterial.SetFloat(_scrollID, _scrollScale * Stages[0].ScrollSpeed);
        }

        private void OnEnable()
        {
            OnHitHurt += SpawnHitFX;
            OnHitShield += SpawnHitFX;
            OnGameStart += StartGame;
            OnGameOver += GameOver;
            OnChangeScroll += ChangeScrollSpeed;

            PlayerController.OnBombUse += OnBombUse;
            PlayerController.OnItemScore += OnItemScore;
            EnemyController.OnKill += SpawnKillFX;
        }

        private void OnDisable()
        {
            OnHitHurt -= SpawnHitFX;
            OnHitShield -= SpawnHitFX;
            OnGameStart -= StartGame;
            OnGameOver -= GameOver;
            OnChangeScroll -= ChangeScrollSpeed;

            PlayerController.OnBombUse -= OnBombUse;
            PlayerController.OnItemScore -= OnItemScore;
            EnemyController.OnKill -= SpawnKillFX;
        }
    }

    public interface IKillable
    {
        public bool TryKill(EnemyController enemy);
    }
}
